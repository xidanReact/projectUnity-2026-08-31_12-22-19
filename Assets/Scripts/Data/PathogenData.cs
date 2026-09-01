using UnityEngine;

// ScriptableObject = конфиг-ассет, который создаётся прямо в Unity Editor
// (правый клик в Project -> Create -> Pathogen -> Pathogen Data)
// Так балансовые цифры (см. dev-plan.md) правятся без пересборки кода.

public enum PathogenType
{
    Virus,
    Bacteria,
    Fungus,
    Parasite
}

[CreateAssetMenu(fileName = "NewPathogen", menuName = "Pathogen/Pathogen Data")]
public class PathogenData : ScriptableObject
{
    [Header("Общее")]
    public string pathogenName;
    public PathogenType type;
    public float moveSpeed = 5f;
    public float maxHealth = 100f;

    [Header("Оружие / базовая атака")]
    public float attackDamage = 10f;
    public float attackRate = 1f; // атак в секунду
    public float attackRange = 6f;
    public float projectileSpeed = 14f;

    [Header("Уникальная механика (вариант A)")]
    // Вирус: шанс заражения при убийстве (0-1) и длительность заражения (сек)
    public float infectionChance = 0.15f;
    public float infectionDuration = 3f;

    // Бактерия: щит блокирует 1 удар, кулдаун восстановления (сек)
    public float shieldCooldown = 8f;

    // Грибок: спора — урон в тик, время жизни, интервал тика
    public float sporeDamagePerTick = 2f;
    public float sporeLifetime = 4f;
    public float sporeTickInterval = 1f;
    public float sporeRadius = 0.9f;

    // Паразит: неуязвимость при смертельном ударе (сек), один раз за уровень
    public float invincibilityOnDeathDuration = 2f;

    [Header("Плейсхолдер-графика (Фаза 1)")]
    public Color bodyColor = Color.white;

    /// <summary>
    /// Дефолты из dev-plan.md ("Баланс патогенов — стартовые значения").
    /// Нужны, чтобы прототип запускался без единого созданного ассета —
    /// в Фазе 2 значения переезжают в реальные .asset-файлы (см. PrototypeAssetCreator).
    /// </summary>
    public static PathogenData CreateDefault(PathogenType type)
    {
        var d = CreateInstance<PathogenData>();
        d.type = type;

        switch (type)
        {
            case PathogenType.Virus:
                d.pathogenName = "Вирус";
                d.moveSpeed = 6.5f;
                d.maxHealth = 90f;
                d.attackDamage = 9f;
                d.attackRate = 2.4f;
                d.attackRange = 7f;
                d.infectionChance = 0.15f;
                d.infectionDuration = 3f;
                d.bodyColor = new Color(0.85f, 0.30f, 0.75f);
                break;

            case PathogenType.Bacteria:
                d.pathogenName = "Бактерия";
                d.moveSpeed = 5f;
                d.maxHealth = 130f;
                d.attackDamage = 14f;
                d.attackRate = 1.5f;
                d.attackRange = 6.5f;
                d.shieldCooldown = 8f;
                d.bodyColor = new Color(0.35f, 0.80f, 0.45f);
                break;

            case PathogenType.Fungus:
                d.pathogenName = "Грибок";
                d.moveSpeed = 5.2f;
                d.maxHealth = 100f;
                d.attackDamage = 7f;
                d.attackRate = 1.7f;
                d.attackRange = 7f;
                d.sporeDamagePerTick = 2f;
                d.sporeLifetime = 4f;
                d.sporeTickInterval = 1f;
                d.sporeRadius = 0.9f;
                d.bodyColor = new Color(0.95f, 0.70f, 0.25f);
                break;

            case PathogenType.Parasite:
                d.pathogenName = "Паразит";
                d.moveSpeed = 7.5f;
                d.maxHealth = 80f;
                d.attackDamage = 11f;
                d.attackRate = 2.0f;
                d.attackRange = 8f;
                d.invincibilityOnDeathDuration = 2f;
                d.bodyColor = new Color(0.55f, 0.45f, 0.90f);
                break;
        }

        d.name = d.pathogenName;
        return d;
    }
}
