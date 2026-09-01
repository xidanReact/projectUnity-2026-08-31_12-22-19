using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Строит уровни Биома 1 для прототипа. Смешанный тип наступления из dev-plan.md:
/// нечётные уровни — волны, чётные — сегменты. Состав врагов фиксирован для уровня,
/// а тайминги/порядок рандомизируются уже в спавнере.
/// </summary>
public static class CampaignGenerator
{
    public static List<LevelData> BuildBloodstream(int levelCount = 8)
    {
        var levels = new List<LevelData>(levelCount);

        for (int i = 0; i < levelCount; i++)
        {
            int number = i + 1;

            // Биом закрывается боссом — это финал вертикального среза Фазы 2.
            if (number == levelCount)
            {
                levels.Add(BuildBossLevel(number));
                continue;
            }

            var level = ScriptableObject.CreateInstance<LevelData>();
            level.advanceType = (number % 2 == 0) ? AdvanceType.Segments : AdvanceType.Waves;
            level.levelName = $"Кровоток {number} · {(level.advanceType == AdvanceType.Waves ? "волны" : "сегменты")}";
            level.name = level.levelName;

            // Сегменты давят таймером, поэтому их спуск ускоряется медленнее, чем растёт населённость волн.
            level.segmentDescendSpeed = 0.65f + 0.06f * i;
            level.segmentBreachDamage = 22f + 2f * i;

            int waveCount = Mathf.Clamp(2 + i / 2, 2, 5);
            for (int w = 0; w < waveCount; w++)
            {
                level.waves.Add(BuildWave(number, w));
            }

            levels.Add(level);
        }

        return levels;
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

    /// <summary>Индекс первого босс-уровня в списке — нужен отладочному входу из HUD.</summary>
    public static int FindFirstBossLevel(List<LevelData> levels)
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].advanceType == AdvanceType.Boss)
            {
                return i;
            }
        }
        return 0;
    }

    private static WaveDefinition BuildWave(int levelNumber, int waveIndex)
    {
        var wave = new WaveDefinition();

        // Нейтрофилы — основа любой волны, их количество растёт быстрее всего.
        int rushers = 4 + levelNumber + waveIndex * 2;
        wave.entries.Add(new SpawnEntry(EnemyCatalog.Neutrophil, rushers));

        // Антитела появляются со 2-го уровня: заставляют не стоять на месте.
        if (levelNumber >= 2)
        {
            wave.entries.Add(new SpawnEntry(EnemyCatalog.Antibody, 1 + (levelNumber - 2) / 2 + waveIndex / 2));
        }

        // Макрофаги — с 3-го, по одному на волну, плюс ещё один на поздних уровнях.
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
