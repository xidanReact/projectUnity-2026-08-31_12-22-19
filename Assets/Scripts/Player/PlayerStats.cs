using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Изменяемая копия PathogenData на время забега. Апгрейды правят только её,
/// исходный ScriptableObject остаётся нетронутым (иначе баланс «утечёт» между забегами).
/// </summary>
public class PlayerStats
{
    public readonly PathogenData Source;

    // Общее
    public float MoveSpeed;
    public float MaxHealth;

    // Оружие
    public float AttackDamage;
    public float AttackRate;
    public float AttackRange;
    public float ProjectileSpeed;
    public int ProjectileCount = 1;
    public int Pierce = 0;

    /// Шанс критического попадания (0-1) и во сколько раз он усиливает урон.
    public float CritChance;
    public float CritMultiplier = 2f;

    /// Доля входящего урона, которая срезается до применения (0-1).
    public float DamageReduction;

    // Вирус
    public float InfectionChance;
    public float InfectionDuration;

    /// Какую долю здоровья получает заражённый враг.
    public float InfectedHealthFraction = 0.5f;

    // Бактерия
    public float ShieldCooldown;
    public int ShieldCharges = 1;

    // Грибок
    public float SporeDamagePerTick;
    public float SporeLifetime;
    public float SporeTickInterval;
    public float SporeRadius;

    // Паразит
    public float InvincibilityDuration;
    public int InvincibilityChargesPerLevel = 1;

    // --- Мутации ---
    // В отличие от апгрейдов это не множители к статам, а включатели
    // поведения: ноль/false означает «мутация не взята», и соответствующий
    // кусок боевой логики просто не выполняется.

    /// Общее: снаряд взрывается при попадании.
    public float ExplosiveRadius;
    public float ExplosiveDamageFactor = 0.6f;

    /// Общее: лечение за убийство.
    public float LifestealPerKill;

    /// Общее: рывок скорости после полученного урона.
    public float AdrenalineBonus;
    public float AdrenalineDuration = 2f;

    /// Вирус: заражённые могут заражать дальше.
    public bool ChainInfection;

    /// Вирус: множитель урона заражённых.
    public float InfectedDamageFactor = 1f;

    /// Бактерия: щит при срабатывании бьёт вокруг.
    public float ShieldBurstDamage;
    public float ShieldBurstRadius = 2.2f;

    /// Грибок: соседние споры усиливают друг друга.
    public float SporeSynergyBonus;

    /// Грибок: спора взрывается в конце жизни.
    public float SporeExplosionDamage;

    /// Паразит: регенерация во время пряток.
    public float CloakHealPerSecond;

    /// Паразит: урон вокруг в момент ухода в невидимость.
    public float CloakBurstDamage;
    public float CloakBurstRadius = 3f;

    /// Названия взятых мутаций — только для отображения в HUD.
    public readonly List<string> TakenMutations = new List<string>();

    public PathogenType Type => Source.type;

    public PlayerStats(PathogenData source)
    {
        Source = source;

        MoveSpeed = source.moveSpeed;
        MaxHealth = source.maxHealth;

        AttackDamage = source.attackDamage;
        AttackRate = source.attackRate;
        AttackRange = source.attackRange;
        ProjectileSpeed = source.projectileSpeed;

        InfectionChance = source.infectionChance;
        InfectionDuration = source.infectionDuration;

        ShieldCooldown = source.shieldCooldown;

        SporeDamagePerTick = source.sporeDamagePerTick;
        SporeLifetime = source.sporeLifetime;
        SporeTickInterval = source.sporeTickInterval;
        SporeRadius = source.sporeRadius;

        InvincibilityDuration = source.invincibilityOnDeathDuration;
    }

    public float SecondsBetweenShots => 1f / Mathf.Max(0.05f, AttackRate);
}
