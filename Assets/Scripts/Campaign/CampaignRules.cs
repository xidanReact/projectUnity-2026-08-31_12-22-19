using System.Collections.Generic;

/// <summary>
/// Доступность узлов и биомов. Вынесено из UI намеренно: карта только рисует
/// то, что решили здесь, и правила можно проверить тестами без сцены.
/// </summary>
public static class CampaignRules
{
    /// <summary>Первый биом открыт всегда — иначе новому игроку некуда идти.</summary>
    public static void EnsureFirstBiomeUnlocked(CampaignProgress progress)
    {
        if (progress != null)
        {
            progress.UnlockBiome(CampaignBuilder.BloodstreamId);
        }
    }

    public static bool IsNodeUnlocked(CampaignMapData map, CampaignProgress progress, CampaignNode node)
    {
        if (map == null || progress == null || node == null)
        {
            return false;
        }

        BiomeData biome = map.BiomeOf(node);
        if (biome == null || !biome.Playable || !progress.IsBiomeUnlocked(biome.Id))
        {
            return false;
        }

        IReadOnlyList<CampaignNode> nodes = biome.Nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != node)
            {
                continue;
            }

            // Первый узел биома доступен сразу, остальные — по цепочке.
            return i == 0 || progress.IsCleared(nodes[i - 1].Id);
        }

        return false;
    }

    /// <summary>
    /// Записать результат прохождения. Звёзды только повышаются; босс открывает
    /// следующий биом.
    /// </summary>
    public static void ApplyClear(CampaignMapData map, CampaignProgress progress, CampaignNode node, int stars)
    {
        if (map == null || progress == null || node == null || stars <= 0)
        {
            return;
        }

        progress.SetStars(node.Id, stars);

        if (!node.IsBoss)
        {
            return;
        }

        BiomeData biome = map.BiomeOf(node);
        int index = map.IndexOf(biome);
        if (index >= 0 && index + 1 < map.Biomes.Count)
        {
            progress.UnlockBiome(map.Biomes[index + 1].Id);
        }
    }
}
