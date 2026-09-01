using System.Collections.Generic;

/// <summary>
/// Активная попытка биома: билд, который копится через все его узлы и сгорает
/// при смерти или выходе из биома. Босс — проверка того, что игрок собрал,
/// поэтому апгрейды не сбрасываются между узлами.
///
/// Живёт только в памяти. Если ОС убьёт приложение посреди биома, билд пропадёт,
/// а пройденные узлы останутся — известное ограничение, чинится сериализацией
/// PlayerStats и состояния UpgradeSystem отдельной задачей.
/// </summary>
public class BiomeRun
{
    public readonly string BiomeId;
    public readonly PathogenData Pathogen;
    public readonly PlayerStats Stats;

    public int TotalKills { get; private set; }
    public int NodesCleared => _clearedNodes.Count;

    /// <summary>
    /// Узлы, за которые апгрейд уже выдан в этой попытке. Без этого множества
    /// игрок перепроходит первый узел и собирает полный билд, ни разу не
    /// столкнувшись с растущей сложностью.
    /// </summary>
    private readonly HashSet<string> _upgradedNodes = new HashSet<string>();

    private readonly HashSet<string> _clearedNodes = new HashSet<string>();

    public BiomeRun(string biomeId, PathogenData pathogen, PlayerStats stats)
    {
        BiomeId = biomeId;
        Pathogen = pathogen;
        Stats = stats;
    }

    /// <summary>
    /// Собрать попытку: стартовые статы патогена плюс купленные перманентные
    /// улучшения. Перки накладываются здесь, до создания игрока, — здоровье
    /// конфигурируется из Stats.MaxHealth и позже уже не пересчитывается.
    /// </summary>
    public static BiomeRun Create(string biomeId, PathogenData pathogen, MetaProgression meta)
    {
        var stats = new PlayerStats(pathogen);
        if (meta != null)
        {
            meta.ApplyTo(stats);
        }

        return new BiomeRun(biomeId, pathogen, stats);
    }

    public bool ShouldGrantUpgrade(string nodeId) => !_upgradedNodes.Contains(nodeId);

    public void MarkUpgradeGranted(string nodeId) => _upgradedNodes.Add(nodeId);

    public void RegisterClear(string nodeId, int kills)
    {
        TotalKills += kills;
        _clearedNodes.Add(nodeId);
    }
}
