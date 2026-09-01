using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Собирает кампанию кодом. Заменяет CampaignGenerator: тот отдавал плоский
/// список уровней для бесконечного забега, здесь — биомы и узлы карты.
///
/// Правила состава волн перенесены из CampaignGenerator без изменений:
/// нечётные узлы — волны, чётные — сегменты, состав фиксирован для узла,
/// а тайминги рандомизируются уже в спавнере.
/// </summary>
public static class CampaignBuilder
{
    /// Сколько узлов в первом биоме, включая босса.
    public const int BloodstreamNodes = 8;

    public const string BloodstreamId = "biome_bloodstream";
    public const string LymphaticId = "biome_lymphatic";
    public const string MarrowId = "biome_marrow";

    /// Горизонтальный разброс дорожки на карте, в координатах макета.
    private const float MapSwing = 150f;

    /// Расстояние между узлами по вертикали.
    private const float MapStep = 152f;

    public static CampaignMapData Build()
    {
        var biomes = new List<BiomeData>
        {
            BuildBloodstream(),

            // Биомы 2 и 3 из dev-plan.md существуют на карте, но врагов и боссов
            // для них ещё нет — это Фаза 3. Подделывать их рескином биома 1 нельзя:
            // такой контент пришлось бы выбрасывать целиком.
            new BiomeData(LymphaticId, "Лимфатическая система",
                new Color(0.45f, 0.70f, 0.85f), playable: false, nodes: new List<CampaignNode>()),

            new BiomeData(MarrowId, "Костный мозг",
                new Color(0.85f, 0.72f, 0.45f), playable: false, nodes: new List<CampaignNode>())
        };

        return new CampaignMapData(biomes);
    }

    private static BiomeData BuildBloodstream()
    {
        var nodes = new List<CampaignNode>(BloodstreamNodes);

        for (int i = 0; i < BloodstreamNodes; i++)
        {
            int number = i + 1;
            bool isBoss = number == BloodstreamNodes;

            LevelData level = isBoss ? BuildBossLevel(number) : BuildBattleLevel(number, i);
            string id = isBoss ? "b1_boss" : $"b1_n{number}";

            int gold = 20 + 6 * i;
            int biomass = 15 + 5 * i;
            if (isBoss)
            {
                gold *= 3;
                biomass *= 3;
            }

            var position = new Vector2(i % 2 == 0 ? -MapSwing : MapSwing, i * MapStep);

            nodes.Add(new CampaignNode(id, level.levelName, level, i, position, gold, biomass));
        }

        return new BiomeData(BloodstreamId, "Кровоток", new Color(0.85f, 0.35f, 0.40f),
            playable: true, nodes: nodes);
    }

    private static LevelData BuildBattleLevel(int number, int index)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = (number % 2 == 0) ? AdvanceType.Segments : AdvanceType.Waves;
        level.levelName = $"Кровоток {number} · {(level.advanceType == AdvanceType.Waves ? "волны" : "сегменты")}";
        level.name = level.levelName;

        // Сегменты давят таймером, поэтому их спуск ускоряется медленнее,
        // чем растёт населённость волн.
        level.segmentDescendSpeed = 0.65f + 0.06f * index;
        level.segmentBreachDamage = 22f + 2f * index;

        int waveCount = Mathf.Clamp(2 + index / 2, 2, 5);
        for (int w = 0; w < waveCount; w++)
        {
            level.waves.Add(BuildWave(number, w));
        }

        return level;
    }

    private static LevelData BuildBossLevel(int number)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Boss;
        level.bossData = BossCatalog.LymphNode;
        level.levelName = $"Кровоток {number} · босс: {level.bossData.bossName}";
        level.name = level.levelName;
        return level;
    }

    private static WaveDefinition BuildWave(int levelNumber, int waveIndex)
    {
        var wave = new WaveDefinition();

        // Нейтрофилы — основа любой волны, их количество растёт быстрее всего.
        int rushers = 4 + levelNumber + waveIndex * 2;
        wave.entries.Add(new SpawnEntry(EnemyCatalog.Neutrophil, rushers));

        // Антитела появляются со 2-го узла: заставляют не стоять на месте.
        if (levelNumber >= 2)
        {
            wave.entries.Add(new SpawnEntry(EnemyCatalog.Antibody, 1 + (levelNumber - 2) / 2 + waveIndex / 2));
        }

        // Макрофаги — с 3-го, по одному на волну, плюс ещё один на поздних узлах.
        if (levelNumber >= 3)
        {
            wave.entries.Add(new SpawnEntry(EnemyCatalog.Macrophage, levelNumber >= 6 ? 2 : 1));
        }

        float fastest = Mathf.Max(0.18f, 0.45f - 0.03f * levelNumber);
        wave.spawnIntervalRange = new Vector2(fastest, fastest + 0.4f);
        wave.postWaveDelay = 1.6f;

        return wave;
    }
}
