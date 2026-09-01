using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Пулевой снаряд. Один и тот же класс для игрока и для врагов —
/// разница только во фракции и в том, кого он ищет при попадании.
/// </summary>
public class Projectile : MonoBehaviour, IPooled
{
    public Faction Faction { get; private set; }

    private Vector2 _velocity;
    private float _damage;
    private float _radius = 0.16f;
    private int _pierceLeft;
    private float _lifetime;
    private float _age;

    private SpriteRenderer _renderer;
    private Action<Vector2> _onHitPoint;
    private readonly HashSet<ICombatTarget> _alreadyHit = new HashSet<ICombatTarget>();

    /// Вызывает пул, когда снаряд отработал. Ставится один раз при создании.
    public Action<Projectile> ReleaseCallback;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <param name="onHitPoint">Колбэк точки попадания — через него грибок сеет споры.</param>
    public void Launch(
        Vector2 position,
        Vector2 direction,
        float speed,
        float damage,
        Faction faction,
        Color color,
        float radius = 0.16f,
        int pierce = 0,
        float lifetime = 4f,
        Action<Vector2> onHitPoint = null)
    {
        transform.position = position;
        transform.localScale = Vector3.one * (radius * 2f);

        _velocity = direction.normalized * speed;
        _damage = damage;
        _radius = radius;
        _pierceLeft = pierce;
        _lifetime = lifetime;
        _age = 0f;
        Faction = faction;
        _onHitPoint = onHitPoint;
        _alreadyHit.Clear();

        if (_renderer != null)
        {
            _renderer.color = color;
        }
    }

    public void OnSpawned()
    {
        _age = 0f;
        _alreadyHit.Clear();
    }

    public void OnDespawned()
    {
        _onHitPoint = null;
        _alreadyHit.Clear();
        _velocity = Vector2.zero;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _age += dt;

        Vector2 position = (Vector2)transform.position + _velocity * dt;
        transform.position = position;

        if (_age >= _lifetime || (Arena.Instance != null && Arena.Instance.IsFarOutside(position, 1.5f)))
        {
            Release();
            return;
        }

        if (Faction == Faction.Immune)
        {
            CheckPlayerHit(position);
        }
        else
        {
            CheckEnemyHit(position);
        }
    }

    private void CheckEnemyHit(Vector2 position)
    {
        var targets = Battlefield.Targets;

        // Идём с конца: попадание может убить врага, а тот снимется с реестра
        // прямо внутри цикла. При обратном обходе удаление затрагивает только
        // уже пройденные индексы, а разделившиеся осколки дописываются в хвост.
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            ICombatTarget target = targets[i];
            if (!target.Health.IsAlive || !Faction.IsHostileTo(target.Faction) || _alreadyHit.Contains(target))
            {
                continue;
            }

            float reach = _radius + target.Radius;
            if (((Vector2)target.Transform.position - position).sqrMagnitude > reach * reach)
            {
                continue;
            }

            _alreadyHit.Add(target);
            target.ApplyDamage(_damage, Faction);
            _onHitPoint?.Invoke(position);

            if (_pierceLeft <= 0)
            {
                Release();
                return;
            }

            _pierceLeft--;
        }
    }

    private void CheckPlayerHit(Vector2 position)
    {
        PlayerController player = Battlefield.Player;
        if (player == null || !player.Health.IsAlive)
        {
            return;
        }

        const float playerRadius = 0.42f;
        float reach = _radius + playerRadius;
        if (((Vector2)player.transform.position - position).sqrMagnitude <= reach * reach)
        {
            player.Health.TakeDamage(_damage);
            Release();
        }
    }

    private void Release()
    {
        ReleaseCallback?.Invoke(this);
    }
}
