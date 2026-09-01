using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Исход узла. Проверяется без запуска боя: NodeOutcome — это контракт между
/// GameRunner и экраном результата, и перепутанные поля тихо испортят звёзды.
/// </summary>
public class NodeOutcomeTests
{
    private static CampaignNode Node()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        return new CampaignNode("b1_n1", "Тестовый", level, 0, Vector2.zero, 20, 15);
    }

    [Test]
    public void Победа_НесётВремяИУбийства()
    {
        var outcome = new NodeOutcome(Node(), cleared: true, elapsedSeconds: 42.5f, kills: 61);

        Assert.IsTrue(outcome.Cleared);
        Assert.AreEqual(42.5f, outcome.ElapsedSeconds, 0.001f);
        Assert.AreEqual(61, outcome.Kills);
        Assert.AreEqual("b1_n1", outcome.Node.Id);
    }

    [Test]
    public void Провал_НеДаётЗвёзд()
    {
        var outcome = new NodeOutcome(Node(), cleared: false, elapsedSeconds: 8f, kills: 3);

        Assert.AreEqual(0, outcome.Stars, "Звёзды за провал не начисляются никогда");
    }

    [Test]
    public void Победа_ОцениваетсяПоПарТайму()
    {
        CampaignNode node = Node();
        float par = StarRating.ParTime(node);

        Assert.AreEqual(3, new NodeOutcome(node, true, par, 10).Stars);
        Assert.AreEqual(1, new NodeOutcome(node, true, par * 5f, 10).Stars);
    }
}
