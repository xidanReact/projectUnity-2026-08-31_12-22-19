using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Тип наступления угрозы. По dev-plan.md уровни смешанные: часть волнами,
/// часть сегментами (угроза «спускается» строем и её надо снести до полосы игрока).
/// </summary>
public enum AdvanceType
{
    Waves,
    Segments,

    /// Босс-уровень: волн нет, на поле одна крупная составная цель.
    Boss
}

[Serializable]
public class SpawnEntry
{
    public EnemyData enemy;
    public int count = 5;

    public SpawnEntry() { }

    public SpawnEntry(EnemyData enemy, int count)
    {
        this.enemy = enemy;
        this.count = count;
    }
}

[Serializable]
public class WaveDefinition
{
    public List<SpawnEntry> entries = new List<SpawnEntry>();

    /// Разброс паузы между спавнами внутри волны — источник «частичной рандомизации»
    /// из Фазы 0: состав фиксирован, тайминги гуляют в заданных рамках.
    public Vector2 spawnIntervalRange = new Vector2(0.35f, 0.75f);

    /// Пауза после того, как волна зачищена, перед следующей.
    public float postWaveDelay = 1.5f;

    /// Суммарное число врагов волны — нужно спавнеру и HUD.
    public int TotalCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].enemy != null)
                {
                    total += entries[i].count;
                }
            }
            return total;
        }
    }
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "Pathogen/Level Data")]
public class LevelData : ScriptableObject
{
    public string levelName = "Кровоток 1";
    public AdvanceType advanceType = AdvanceType.Waves;
    public List<WaveDefinition> waves = new List<WaveDefinition>();

    [Header("Только для Boss")]
    public BossData bossData;

    [Header("Только для Segments")]
    /// Скорость, с которой строй спускается к полосе игрока.
    public float segmentDescendSpeed = 0.75f;

    /// Урон игроку, если сегмент дошёл до полосы (главный источник давления в этом режиме).
    public float segmentBreachDamage = 25f;
}
