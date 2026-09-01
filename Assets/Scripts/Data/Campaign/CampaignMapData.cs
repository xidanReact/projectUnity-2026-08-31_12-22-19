using System.Collections.Generic;

/// <summary>Вся кампания. Поиск по идентификатору нужен при восстановлении из сейва.</summary>
public class CampaignMapData
{
    public readonly IReadOnlyList<BiomeData> Biomes;

    public CampaignMapData(IReadOnlyList<BiomeData> biomes)
    {
        Biomes = biomes;
    }

    public CampaignNode FindNode(string id)
    {
        for (int b = 0; b < Biomes.Count; b++)
        {
            IReadOnlyList<CampaignNode> nodes = Biomes[b].Nodes;
            for (int n = 0; n < nodes.Count; n++)
            {
                if (nodes[n].Id == id)
                {
                    return nodes[n];
                }
            }
        }

        return null;
    }

    public BiomeData BiomeOf(CampaignNode node)
    {
        if (node == null)
        {
            return null;
        }

        for (int b = 0; b < Biomes.Count; b++)
        {
            IReadOnlyList<CampaignNode> nodes = Biomes[b].Nodes;
            for (int n = 0; n < nodes.Count; n++)
            {
                if (nodes[n] == node)
                {
                    return Biomes[b];
                }
            }
        }

        return null;
    }

    public int IndexOf(BiomeData biome)
    {
        for (int b = 0; b < Biomes.Count; b++)
        {
            if (Biomes[b] == biome)
            {
                return b;
            }
        }

        return -1;
    }
}
