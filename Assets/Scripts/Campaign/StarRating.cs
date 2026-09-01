using UnityEngine;

/// <summary>
/// Звёзды за прохождение узла. Порог не задаётся руками для каждого уровня,
/// а выводится из его собственного расписания спавна: сумма интервалов — это
/// время, за которое враги физически успевают появиться, и быстрее уровень
/// пройти нельзя. Три звезды означают «убивал не медленнее, чем они выходили».
/// </summary>
public static class StarRating
{
    public const int MaxStars = 3;

    public const float ThreeStarFactor = 1.15f;
    public const float TwoStarFactor = 1.7f;

    /// Страховка от вырожденного уровня без волн: ноль превратил бы пороги
    /// в деление на ноль и выдавал бы одну звезду при любом результате.
    private const float MinimumParTime = 5f;

    public static float ParTime(CampaignNode node)
    {
        if (node == null || node.Level == null)
        {
            return MinimumParTime;
        }

        LevelData level = node.Level;

        // У босс-уровня нет расписания спавна, выводить парТайм не из чего.
        if (level.advanceType == AdvanceType.Boss)
        {
            return level.bossData != null
                ? Mathf.Max(MinimumParTime, level.bossData.parTimeSeconds)
                : MinimumParTime;
        }

        float total = 0f;
        for (int i = 0; i < level.waves.Count; i++)
        {
            WaveDefinition wave = level.waves[i];
            float meanInterval = (wave.spawnIntervalRange.x + wave.spawnIntervalRange.y) * 0.5f;
            total += wave.TotalCount * meanInterval + wave.postWaveDelay;
        }

        return Mathf.Max(MinimumParTime, total);
    }

    /// <summary>
    /// Оценка пройденного уровня. Вызывается только для победы: провал звёзд
    /// не даёт вообще, и ноль сюда не возвращается никогда.
    /// </summary>
    public static int Evaluate(CampaignNode node, float elapsedSeconds)
    {
        float par = ParTime(node);

        if (elapsedSeconds <= par * ThreeStarFactor)
        {
            return 3;
        }

        return elapsedSeconds <= par * TwoStarFactor ? 2 : 1;
    }
}
