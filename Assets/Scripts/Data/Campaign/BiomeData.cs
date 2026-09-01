using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Биом кампании. Playable отделяет настоящий контент от заглушек: биомы 2 и 3
/// нарисованы на карте, но врагов для них ещё не существует.
/// </summary>
public class BiomeData
{
    public readonly string Id;
    public readonly string DisplayName;
    public readonly Color AccentColor;
    public readonly bool Playable;
    public readonly IReadOnlyList<CampaignNode> Nodes;

    public BiomeData(string id, string displayName, Color accentColor, bool playable, IReadOnlyList<CampaignNode> nodes)
    {
        Id = id;
        DisplayName = displayName;
        AccentColor = accentColor;
        Playable = playable;
        Nodes = nodes ?? new List<CampaignNode>();
    }

    public CampaignNode BossNode => Nodes.Count > 0 && Nodes[Nodes.Count - 1].IsBoss
        ? Nodes[Nodes.Count - 1]
        : null;
}
