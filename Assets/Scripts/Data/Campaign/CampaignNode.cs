using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Узел карты кампании: уровень плюс всё, что нужно карте и брифингу.
/// Не ScriptableObject — кампания собирается кодом, как и остальная сцена Фазы 2.
/// </summary>
public class CampaignNode
{
    /// <summary>
    /// Стабильный идентификатор вида «b1_n3». Уходит в сейв игрока —
    /// после первого релиза переименование стирает чужой прогресс.
    /// </summary>
    public readonly string Id;

    public readonly string DisplayName;
    public readonly LevelData Level;

    /// Порядковый номер узла внутри биома с нуля. От него растёт сложность.
    public readonly int IndexInBiome;

    /// Позиция на карте в координатах макета 720×1280.
    public readonly Vector2 MapPosition;

    public readonly int BaseGold;
    public readonly int BaseBiomass;
    public readonly bool IsBoss;

    /// Состав врагов для брифинга, без повторов и в порядке появления.
    public readonly IReadOnlyList<string> EnemyNames;

    public CampaignNode(
        string id,
        string displayName,
        LevelData level,
        int indexInBiome,
        Vector2 mapPosition,
        int baseGold,
        int baseBiomass)
    {
        Id = id;
        DisplayName = displayName;
        Level = level;
        IndexInBiome = indexInBiome;
        MapPosition = mapPosition;
        BaseGold = baseGold;
        BaseBiomass = baseBiomass;
        IsBoss = level != null && level.advanceType == AdvanceType.Boss;
        EnemyNames = CollectEnemyNames(level);
    }

    private static IReadOnlyList<string> CollectEnemyNames(LevelData level)
    {
        var names = new List<string>();
        if (level == null)
        {
            return names;
        }

        if (level.advanceType == AdvanceType.Boss)
        {
            if (level.bossData != null)
            {
                names.Add(level.bossData.bossName);
            }
            return names;
        }

        for (int w = 0; w < level.waves.Count; w++)
        {
            List<SpawnEntry> entries = level.waves[w].entries;
            for (int e = 0; e < entries.Count; e++)
            {
                if (entries[e] == null || entries[e].enemy == null)
                {
                    continue;
                }

                string name = entries[e].enemy.enemyName;
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }
}
