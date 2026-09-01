using NUnit.Framework;

/// <summary>
/// Доступность узлов и биомов. Ошибка здесь либо запирает игрока в первом узле,
/// либо открывает ему всю кампанию сразу.
/// </summary>
public class CampaignRulesTests
{
    private CampaignMapData _map;
    private CampaignProgress _progress;

    [SetUp]
    public void SetUp()
    {
        _map = CampaignBuilder.Build();
        _progress = new CampaignProgress();
        _progress.UnlockBiome(CampaignBuilder.BloodstreamId);
    }

    [Test]
    public void ПервыйУзелОткрытогоБиома_ДоступенСразу()
    {
        Assert.IsTrue(CampaignRules.IsNodeUnlocked(_map, _progress, _map.FindNode("b1_n1")));
    }

    [Test]
    public void ВторойУзел_ЗакрытПокаНеПройденПервый()
    {
        CampaignNode second = _map.FindNode("b1_n2");

        Assert.IsFalse(CampaignRules.IsNodeUnlocked(_map, _progress, second));

        _progress.SetStars("b1_n1", 1);

        Assert.IsTrue(CampaignRules.IsNodeUnlocked(_map, _progress, second),
            "Одной звезды достаточно: она и означает «пройден»");
    }

    [Test]
    public void УзлыЗакрытогоБиома_НедоступныДажеПоПорядку()
    {
        var fresh = new CampaignProgress();

        Assert.IsFalse(CampaignRules.IsNodeUnlocked(_map, fresh, _map.FindNode("b1_n1")),
            "Биом не открыт — узлы недоступны");
    }

    [Test]
    public void ApplyClear_ЗаписываетЗвёздыИОткрываетСледующийБиомТолькоЗаБосса()
    {
        CampaignRules.ApplyClear(_map, _progress, _map.FindNode("b1_n1"), 2);

        Assert.AreEqual(2, _progress.StarsOf("b1_n1"));
        Assert.IsFalse(_progress.IsBiomeUnlocked(CampaignBuilder.LymphaticId),
            "Обычный узел следующий биом не открывает");

        CampaignRules.ApplyClear(_map, _progress, _map.FindNode("b1_boss"), 1);

        Assert.IsTrue(_progress.IsBiomeUnlocked(CampaignBuilder.LymphaticId));
    }

    [Test]
    public void ApplyClear_НеОткрываетНичегоЗаПределамиПоследнегоБиома()
    {
        // У последнего биома нет следующего — ApplyClear обязан это пережить.
        BiomeData last = _map.Biomes[_map.Biomes.Count - 1];
        var node = new CampaignNode("b3_boss", "Финальный босс",
            _map.FindNode("b1_boss").Level, 0, UnityEngine.Vector2.zero, 10, 10);

        Assert.AreEqual(0, last.Nodes.Count, "Заглушка биома пуста — узлов в ней нет");
        Assert.DoesNotThrow(() => CampaignRules.ApplyClear(_map, _progress, node, 3),
            "Узел вне карты не должен ронять запись результата");
    }

    [Test]
    public void ApplyClear_ИгнорируетПровал()
    {
        CampaignRules.ApplyClear(_map, _progress, _map.FindNode("b1_n1"), 0);

        Assert.AreEqual(0, _progress.StarsOf("b1_n1"), "Ноль звёзд — это провал, узел не пройден");
    }

    [Test]
    public void EnsureFirstBiomeUnlocked_ОткрываетКровотокНаЧистомПрогрессе()
    {
        var fresh = new CampaignProgress();

        CampaignRules.EnsureFirstBiomeUnlocked(fresh);

        Assert.IsTrue(fresh.IsBiomeUnlocked(CampaignBuilder.BloodstreamId));
    }
}
