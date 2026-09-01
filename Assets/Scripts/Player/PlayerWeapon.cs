using UnityEngine;

/// <summary>
/// Авто-атака: игрок не целится и не стреляет вручную — оружие само бьёт
/// в направлении ближайшей угрозы в радиусе. Если целей нет, выстрела нет
/// (стрелять «в пустоту вверх» дороже по пулям и хуже читается).
/// </summary>
public class PlayerWeapon : MonoBehaviour
{
    private PlayerStats _stats;
    private PathogenAbility _ability;
    private PlayerMutations _mutations;

    /// Делегат кешируется: иначе каждый выстрел аллоцировал бы новый объект.
    private System.Action<Vector2> _onHit;
    private float _cooldown;
    private bool _enabled = true;

    public ICombatTarget CurrentTarget { get; private set; }

    public void Initialize(PlayerStats stats, PathogenAbility ability, PlayerMutations mutations)
    {
        _stats = stats;
        _ability = ability;
        _mutations = mutations;
        _onHit = HandleProjectileHit;
        _cooldown = 0f;
        CurrentTarget = null;
    }

    public void SetEnabled(bool value)
    {
        _enabled = value;
    }

    private void Update()
    {
        if (!_enabled || _stats == null || PoolHub.Instance == null)
        {
            return;
        }

        _cooldown -= Time.deltaTime;

        CurrentTarget = Battlefield.FindNearestEnemy(transform.position, Faction.Pathogen, _stats.AttackRange);
        if (CurrentTarget == null || _cooldown > 0f)
        {
            return;
        }

        Fire(CurrentTarget);
        _cooldown = _stats.SecondsBetweenShots;
    }

    /// <summary>
    /// Единая точка попадания: и способность патогена (споры грибка),
    /// и мутации (разрывной снаряд) висят на одном событии.
    /// </summary>
    private void HandleProjectileHit(Vector2 point)
    {
        if (_ability != null)
        {
            _ability.OnPlayerProjectileHit(point);
        }

        if (_mutations != null)
        {
            _mutations.OnProjectileHit(point);
        }
    }

    private void Fire(ICombatTarget target)
    {
        Vector2 origin = transform.position;
        Vector2 baseDirection = ((Vector2)target.Transform.position - origin).normalized;
        if (baseDirection.sqrMagnitude < 0.0001f)
        {
            baseDirection = Vector2.up;
        }

        // Крит роллится один раз на выстрел, а не на каждый снаряд веера:
        // иначе мультивыстрел неявно умножал бы ценность шанса крита.
        bool crit = _stats.CritChance > 0f && Random.value < _stats.CritChance;
        float damage = crit ? _stats.AttackDamage * _stats.CritMultiplier : _stats.AttackDamage;

        int count = Mathf.Max(1, _stats.ProjectileCount);
        // Веер: одиночный выстрел летит строго в цель, несколько — расходятся вокруг неё.
        const float spreadPerProjectile = 9f;
        float totalSpread = (count - 1) * spreadPerProjectile;

        for (int i = 0; i < count; i++)
        {
            float angle = -totalSpread * 0.5f + spreadPerProjectile * i;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;

            Projectile projectile = PoolHub.Instance.Projectiles.Get();
            projectile.Launch(
                origin,
                direction,
                _stats.ProjectileSpeed,
                damage,
                Faction.Pathogen,
                crit ? Color.Lerp(_stats.Source.bodyColor, Color.white, 0.6f) : _stats.Source.bodyColor,
                radius: 0.16f,
                pierce: _stats.Pierce,
                lifetime: 3f,
                onHitPoint: _onHit);
        }
    }
}
