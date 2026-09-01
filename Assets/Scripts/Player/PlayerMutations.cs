using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Поведение, которое включают мутации. Собрано в одном месте намеренно:
/// каждая мутация — это несколько строк в готовой точке подключения, а не
/// новый компонент и не подписка на события, поэтому добавить следующую
/// мутацию стоит ровно столько, сколько стоит её эффект.
/// </summary>
public class PlayerMutations : MonoBehaviour
{
    private static readonly List<ICombatTarget> _buffer = new List<ICombatTarget>(64);

    private PlayerStats _stats;
    private Health _health;
    private float _adrenalineTimer;

    /// Множитель скорости от мутаций. PlayerController умножает на него базовую скорость.
    public float SpeedMultiplier { get; private set; } = 1f;

    public void Initialize(PlayerStats stats, Health health)
    {
        _stats = stats;
        _health = health;
        _adrenalineTimer = 0f;
        SpeedMultiplier = 1f;
    }

    private void Update()
    {
        if (_adrenalineTimer <= 0f)
        {
            return;
        }

        _adrenalineTimer -= Time.deltaTime;
        if (_adrenalineTimer <= 0f)
        {
            SpeedMultiplier = 1f;
        }
    }

    // --- Точки подключения ---

    /// <summary>Снаряд игрока попал в цель.</summary>
    public void OnProjectileHit(Vector2 point)
    {
        if (_stats == null || _stats.ExplosiveRadius <= 0f)
        {
            return;
        }

        DamageArea(point, _stats.ExplosiveRadius, _stats.AttackDamage * _stats.ExplosiveDamageFactor);
    }

    /// <summary>Игрок убил врага.</summary>
    public void OnKill()
    {
        if (_stats == null || _stats.LifestealPerKill <= 0f || _health == null)
        {
            return;
        }

        _health.Heal(_stats.LifestealPerKill);
    }

    /// <summary>Игрок получил урон (уже применённый, после щитов).</summary>
    public void OnDamaged()
    {
        if (_stats == null || _stats.AdrenalineBonus <= 0f)
        {
            return;
        }

        SpeedMultiplier = 1f + _stats.AdrenalineBonus;
        _adrenalineTimer = _stats.AdrenalineDuration;
    }

    /// <summary>Щит бактерии поглотил удар.</summary>
    public void OnShieldAbsorbed()
    {
        if (_stats == null || _stats.ShieldBurstDamage <= 0f)
        {
            return;
        }

        DamageArea(transform.position, _stats.ShieldBurstRadius, _stats.ShieldBurstDamage);
    }

    /// <summary>Паразит ушёл в невидимость.</summary>
    public void OnCloakStarted()
    {
        if (_stats == null || _stats.CloakBurstDamage <= 0f)
        {
            return;
        }

        DamageArea(transform.position, _stats.CloakBurstRadius, _stats.CloakBurstDamage);
    }

    /// <summary>Каждый кадр, пока паразит невидим.</summary>
    public void OnCloakTick(float deltaTime)
    {
        if (_stats == null || _stats.CloakHealPerSecond <= 0f || _health == null)
        {
            return;
        }

        _health.Heal(_stats.CloakHealPerSecond * deltaTime);
    }

    private static void DamageArea(Vector2 center, float radius, float damage)
    {
        Battlefield.CollectEnemiesInRadius(center, radius, Faction.Pathogen, _buffer);
        for (int i = 0; i < _buffer.Count; i++)
        {
            _buffer[i].ApplyDamage(damage, Faction.Pathogen);
        }
    }
}
