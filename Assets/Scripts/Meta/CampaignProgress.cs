using System;
using System.Collections.Generic;

/// <summary>
/// Прогресс по кампании. Как и весь сейв, сериализуется через JsonUtility,
/// поэтому здесь только публичные поля и списки — словарей быть не может.
/// </summary>
[Serializable]
public class CampaignProgress
{
    public List<NodeProgress> nodes = new List<NodeProgress>();
    public List<string> biomesUnlocked = new List<string>();

    public int StarsOf(string nodeId)
    {
        if (nodes == null)
        {
            return 0;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == nodeId)
            {
                return nodes[i].stars;
            }
        }

        return 0;
    }

    public bool IsCleared(string nodeId) => StarsOf(nodeId) > 0;

    /// <summary>
    /// Записывает результат, только если он лучше прежнего: повторный проход
    /// на одну звезду не должен стирать заработанные три.
    /// </summary>
    public void SetStars(string nodeId, int stars)
    {
        if (nodes == null)
        {
            nodes = new List<NodeProgress>();
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == nodeId)
            {
                if (stars > nodes[i].stars)
                {
                    nodes[i].stars = stars;
                }
                return;
            }
        }

        nodes.Add(new NodeProgress { id = nodeId, stars = stars });
    }

    public bool IsBiomeUnlocked(string biomeId)
    {
        return biomesUnlocked != null && biomesUnlocked.Contains(biomeId);
    }

    public void UnlockBiome(string biomeId)
    {
        if (biomesUnlocked == null)
        {
            biomesUnlocked = new List<string>();
        }

        if (!biomesUnlocked.Contains(biomeId))
        {
            biomesUnlocked.Add(biomeId);
        }
    }
}
