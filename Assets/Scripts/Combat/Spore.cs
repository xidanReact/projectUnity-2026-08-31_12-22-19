using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спора грибка: тлеет на месте попадания и тикает уроном по всем, кто рядом.
/// Несколько спор рядом естественным образом складываются в зону — отдельной
/// логики «слияния» не нужно, достаточно того, что тики независимы.
/// </summary>
public class Spore : MonoBehaviour, IPooled
{
    private static readonly List<ICombatTarget> _buffer = new List<ICombatTarget>(32);

    /// Активные споры — нужны мутации «Грибница», которая считает соседей.
    private static readonly List<Spore> _active = new List<Spore>(64);

    private float _damagePerTick;
    private float _tickInterval;
    private float _lifetime;
    private float _radius;
    private float _age;
    private float _tickTimer;

    private float _synergyBonus;
    private float _explosionDamage;

    private SpriteRenderer _renderer;
    private Color _baseColor;

    public Action<Spore> ReleaseCallback;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <param name="synergyBonus">Мутация «Грибница»: прибавка к урону за каждую соседнюю спору.</param>
    /// <param name="explosionDamage">Мутация «Взрывные споры»: урон при догорании.</param>
    public void Plant(
        Vector2 position,
        float damagePerTick,
        float tickInterval,
        float lifetime,
        float radius,
        Color color,
        float synergyBonus = 0f,
        float explosionDamage = 0f)
    {
        transform.position = position;
        transform.localScale = Vector3.one * (radius * 2f);

        _damagePerTick = damagePerTick;
        _tickInterval = Mathf.Max(0.05f, tickInterval);
        _lifetime = lifetime;
        _radius = radius;
        _synergyBonus = synergyBonus;
        _explosionDamage = explosionDamage;
        _age = 0f;

        // Первый тик — сразу, иначе спора под быстрым врагом не успевает сработать вообще.
        _tickTimer = 0f;

        _baseColor = color;
        if (_renderer != null)
        {
            _renderer.color = color;
        }
    }

    public void OnSpawned()
    {
        _age = 0f;
        _tickTimer = 0f;
        _active.Add(this);
    }

    public void OnDespawned()
    {
        _active.Remove(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        _age += dt;

        if (_age >= _lifetime)
        {
            Detonate();
            ReleaseCallback?.Invoke(this);
            return;
        }

        // Спора гаснет к концу жизни — единственная обратная связь, доступная на плейсхолдерах.
        if (_renderer != null)
        {
            Color c = _baseColor;
            c.a = _baseColor.a * Mathf.Lerp(1f, 0.15f, _age / _lifetime);
            _renderer.color = c;
        }

        _tickTimer -= dt;
        if (_tickTimer > 0f)
        {
            return;
        }
        _tickTimer = _tickInterval;

        float damage = _damagePerTick + _synergyBonus * CountNeighbours();

        Battlefield.CollectEnemiesInRadius(transform.position, _radius, Faction.Pathogen, _buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            _buffer[i].ApplyDamage(damage, Faction.Pathogen);
        }
    }

    private void Detonate()
    {
        if (_explosionDamage <= 0f)
        {
            return;
        }

        // Взрыв бьёт шире самой споры — иначе он не отличим от обычного тика.
        Battlefield.CollectEnemiesInRadius(transform.position, _radius * 1.8f, Faction.Pathogen, _buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            _buffer[i].ApplyDamage(_explosionDamage, Faction.Pathogen);
        }
    }

    /// <summary>Сколько других спор перекрывается с этой.</summary>
    private int CountNeighbours()
    {
        if (_synergyBonus <= 0f)
        {
            return 0;
        }

        int count = 0;
        float reach = _radius * 2f;
        float sqrReach = reach * reach;
        Vector2 position = transform.position;

        for (int i = 0; i < _active.Count; i++)
        {
            Spore other = _active[i];
            if (other == this)
            {
                continue;
            }

            if (((Vector2)other.transform.position - position).sqrMagnitude <= sqrReach)
            {
                count++;
            }
        }

        return count;
    }
}
