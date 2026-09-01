using UnityEngine;

/// <summary>
/// Поведенческий архетип врага. Определяет, как враг двигается и атакует;
/// конкретные цифры лежат в самом EnemyData.
/// </summary>
public enum EnemyArchetype
{
    /// Быстрый ближний бой — доходит до полосы игрока и бьёт в контакт (Нейтрофил).
    Rusher,

    /// Медленный танк — идёт по прямой, при смерти делится на слабых (Макрофаг).
    Tank,

    /// Держит дистанцию и стреляет по игроку (Антитело).
    Shooter
}

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Pathogen/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Общее")]
    public string enemyName;
    public EnemyArchetype archetype;
    public float maxHealth = 20f;
    public float moveSpeed = 2f;
    public float radius = 0.35f;
    public Color bodyColor = Color.white;

    [Header("Атака")]
    public float contactDamage = 8f;
    public float attackInterval = 1f;
    /// Дистанция, на которой Shooter останавливается и начинает стрелять.
    public float standoffDistance = 5f;
    public float projectileSpeed = 6f;
    public float projectileDamage = 6f;

    [Header("Награда")]
    public int scoreValue = 1;

    [Header("Расщепление при смерти (Макрофаг)")]
    public int splitCount = 0;
    public EnemyData splitInto;

    public static EnemyData Create(
        string enemyName,
        EnemyArchetype archetype,
        float maxHealth,
        float moveSpeed,
        float radius,
        Color color)
    {
        var d = CreateInstance<EnemyData>();
        d.enemyName = enemyName;
        d.name = enemyName;
        d.archetype = archetype;
        d.maxHealth = maxHealth;
        d.moveSpeed = moveSpeed;
        d.radius = radius;
        d.bodyColor = color;
        return d;
    }
}
