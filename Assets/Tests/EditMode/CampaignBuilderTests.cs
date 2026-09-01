using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Структура кампании. Идентификаторы узлов уходят в сейв игрока, поэтому
/// их форма зафиксирована тестом: молчаливое переименование сотрёт прогресс.
/// </summary>
public class CampaignBuilderTests
{
    private CampaignMapData _map;

    [SetUp]
    public void SetUp()
    {
        _map = CampaignBuilder.Build();
    }

    [Test]
    public void Build_ДаётТриБиомаИзКоторыхИграбеленПервый()
    {
        Assert.AreEqual(3, _map.Biomes.Count);
        Assert.IsTrue(_map.Biomes[0].Playable, "Биом «Кровоток» — единственный с существующими врагами");
        Assert.IsFalse(_map.Biomes[1].Playable);
        Assert.IsFalse(_map.Biomes[2].Playable);
    }

    [Test]
    public void ПервыйБиом_ВосемьУзловИПоследнийБосс()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual(8, nodes.Count);
        Assert.IsTrue(nodes[7].IsBoss);
        Assert.AreEqual(AdvanceType.Boss, nodes[7].Level.advanceType);
        Assert.IsNotNull(nodes[7].Level.bossData);

        for (int i = 0; i < 7; i++)
        {
            Assert.IsFalse(nodes[i].IsBoss, $"Узел {i} не должен быть боссом");
            Assert.Greater(nodes[i].Level.waves.Count, 0, $"У узла {i} обязаны быть волны");
        }
    }

    [Test]
    public void ИдентификаторыУзлов_СтабильныИУникальны()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual("b1_n1", nodes[0].Id);
        Assert.AreEqual("b1_n7", nodes[6].Id);
        Assert.AreEqual("b1_boss", nodes[7].Id);

        var seen = new HashSet<string>();
        foreach (BiomeData biome in _map.Biomes)
        {
            foreach (CampaignNode node in biome.Nodes)
            {
                Assert.IsTrue(seen.Add(node.Id), $"Дубликат идентификатора узла: {node.Id}");
            }
        }
    }

    [Test]
    public void FindNode_НаходитПоИдентификаторуИОтдаётNullНаЧужой()
    {
        Assert.AreEqual("b1_n3", _map.FindNode("b1_n3").Id);
        Assert.IsNull(_map.FindNode("нет_такого"));
    }

    [Test]
    public void BiomeOf_ВозвращаетБиомУзла()
    {
        CampaignNode node = _map.FindNode("b1_boss");

        Assert.AreSame(_map.Biomes[0], _map.BiomeOf(node));
        Assert.AreEqual(0, _map.IndexOf(_map.Biomes[0]));
    }

    [Test]
    public void Награда_РастётПоУзламИУтраиваетсяНаБоссе()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual(20, nodes[0].BaseGold, "20 + 6 * 0");
        Assert.AreEqual(15, nodes[0].BaseBiomass, "15 + 5 * 0");
        Assert.AreEqual(26, nodes[1].BaseGold, "20 + 6 * 1");
        Assert.Greater(nodes[7].BaseGold, nodes[6].BaseGold * 2, "Босс утраивает базу");
    }

    [Test]
    public void ТипНаступления_Чередуется()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual(AdvanceType.Waves, nodes[0].Level.advanceType);
        Assert.AreEqual(AdvanceType.Segments, nodes[1].Level.advanceType);
        Assert.AreEqual(AdvanceType.Waves, nodes[2].Level.advanceType);
    }

    [Test]
    public void EnemyNames_ПеречисляетСоставБезПовторов()
    {
        CampaignNode late = _map.Biomes[0].Nodes[5];

        CollectionAssert.AllItemsAreUnique(late.EnemyNames);
        Assert.Contains(EnemyCatalog.Neutrophil.enemyName, (System.Collections.ICollection)late.EnemyNames);
    }

    [Test]
    public void ЗаблокированныеБиомы_ПустыИНеЛомаютОбход()
    {
        Assert.AreEqual(0, _map.Biomes[1].Nodes.Count, "Врагов для биома 2 не существует — узлов быть не может");
        Assert.AreEqual(0, _map.Biomes[2].Nodes.Count);
        Assert.IsNotEmpty(_map.Biomes[1].DisplayName);
    }
}
