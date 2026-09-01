using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Что делает сегмент босса. Каждый сегмент — это отдельная «фаза», которая
/// работает, пока сегмент жив, а не сменяется по порогу здоровья: так игрок
/// сам решает, какое давление снять первым.
/// </summary>
public enum BossAttackKind
{
    /// Призывает подкрепления сверху.
    Summon,

    /// Веер снарядов вниз.
    Volley,

    /// Телеграфированный удар по участку полосы игрока.
    Sweep,

    /// Одиночный быстрый снаряд по игроку.
    Aimed
}

[Serializable]
public class BossSegmentDefinition
{
    public string segmentName = "Сегмент";
    public float maxHealth = 200f;
    public float radius = 0.85f;
    public Color color = Color.white;

    [Header("Атака")]
    public BossAttackKind attack = BossAttackKind.Volley;
    public float attackInterval = 3f;
    public float attackDamage = 10f;
    public float projectileSpeed = 6f;

    [Tooltip("Сколько снарядов в веере (Volley).")]
    public int volleyCount = 5;

    [Tooltip("Ширина зоны удара по полосе (Sweep).")]
    public float sweepWidth = 3.5f;

    [Tooltip("Кого призывает (Summon).")]
    public EnemyData summon;

    [Tooltip("Сколько призывает за раз (Summon).")]
    public int summonCount = 2;

    [Tooltip("Смещение сегмента относительно центра босса, в мировых единицах.")]
    public Vector2 offset;
}

[CreateAssetMenu(fileName = "NewBoss", menuName = "Pathogen/Boss Data")]
public class BossData : ScriptableObject
{
    public string bossName = "Лимфоузел";

    [Tooltip("Насколько выше полосы игрока встаёт центр босса. Должно быть меньше самой короткой дальности атаки патогена, иначе до сегментов не дотянуться.")]
    public float battleOffsetFromLane = 5.6f;

    [Tooltip("Скорость, с которой босс опускается на боевую позицию в начале уровня.")]
    public float entrySpeed = 3.5f;

    [Tooltip("Во сколько раз ускоряются атаки оставшихся сегментов за каждый уничтоженный.")]
    public float rageIntervalScalePerKill = 0.82f;

    public List<BossSegmentDefinition> segments = new List<BossSegmentDefinition>();
}
