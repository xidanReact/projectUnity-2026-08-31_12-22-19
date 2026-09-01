using System;

/// <summary>
/// Перманентное улучшение, которое покупается между забегами за биомассу.
/// В отличие от апгрейдов внутри забега, эффект применяется к стартовым статам
/// в момент старта — поэтому это чистая арифметика без точек подключения в бою.
/// </summary>
public class PermanentUpgrade
{
    public readonly string Id;
    public readonly string Title;

    /// Описание одного уровня, для витрины.
    public readonly string PerLevelDescription;

    public readonly int MaxLevel;

    private readonly int _baseCost;
    private readonly Action<PlayerStats, int> _apply;

    public PermanentUpgrade(
        string id,
        string title,
        string perLevelDescription,
        int maxLevel,
        int baseCost,
        Action<PlayerStats, int> apply)
    {
        Id = id;
        Title = title;
        PerLevelDescription = perLevelDescription;
        MaxLevel = maxLevel;
        _baseCost = baseCost;
        _apply = apply;
    }

    /// <summary>
    /// Цена следующего уровня. Линейный рост: каждый следующий уровень дороже
    /// предыдущего на базовую цену — по dev-plan.md фарм должен быть заметным,
    /// но не превращаться в экспоненциальную стену уже на третьем уровне.
    /// </summary>
    public int CostForNextLevel(int currentLevel) => _baseCost * (currentLevel + 1);

    public bool IsMaxed(int currentLevel) => currentLevel >= MaxLevel;

    public void Apply(PlayerStats stats, int level)
    {
        if (level > 0)
        {
            _apply(stats, level);
        }
    }
}
