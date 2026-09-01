using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Экономика узла. Ломается молча: игра продолжает работать, просто прогресс
/// становится либо непроходимым, либо фермой из одного уровня.
/// </summary>
public class CampaignRewardsTests
{
    private static CampaignNode Node(int gold = 100, int biomass = 100)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        return new CampaignNode("test_n1", "Тестовый", level, 0, Vector2.zero, gold, biomass);
    }

    [Test]
    public void StarMultiplier_РастётПоЧетвертиЗаЗвезду()
    {
        Assert.AreEqual(1.00f, CampaignRewards.StarMultiplier(1), 0.001f);
        Assert.AreEqual(1.25f, CampaignRewards.StarMultiplier(2), 0.001f);
        Assert.AreEqual(1.50f, CampaignRewards.StarMultiplier(3), 0.001f);
    }

    [Test]
    public void Payout_ПервоеПрохождениеПлатитПолностью()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 0, newStars: 2);

        Assert.AreEqual(125, reward.Gold);
        Assert.AreEqual(125, reward.Biomass);
    }

    [Test]
    public void Payout_УлучшениеЗвёздПлатитТолькоРазницу()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 2, newStars: 3);

        Assert.AreEqual(25, reward.Gold, "150 за три звезды минус 125 уже полученных");
        Assert.AreEqual(25, reward.Biomass);
    }

    [Test]
    public void Payout_ПовторБезУлучшенияПлатитТреть()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 3, newStars: 3);

        Assert.AreEqual(45, reward.Gold, "30% от полной награды за три звезды");
    }

    [Test]
    public void Payout_ХудшийПовторНеОтнимаетНичего()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 3, newStars: 1);

        Assert.GreaterOrEqual(reward.Gold, 0, "Отрицательная выплата отобрала бы у игрока валюту");
        Assert.AreEqual(45, reward.Gold, "Платится повтор по лучшему результату, а не по текущему");
    }

    [Test]
    public void AwardNode_НачисляетСоСрезомЖадностиИСохраняет()
    {
        var go = new GameObject("Meta");
        try
        {
            var meta = go.AddComponent<MetaProgression>();
            var store = new FakeStore();
            meta.Initialize(store);

            Reward reward = meta.AwardNode(Node(), previousStars: 0, newStars: 3);

            // 150 полной награды × 0.65 = 97.5 -> 98
            Assert.AreEqual(98, reward.Gold);
            Assert.AreEqual(98, meta.Progress.gold);
            Assert.AreEqual(98, meta.Progress.biomass);
            Assert.GreaterOrEqual(store.SaveCalls, 1, "Прогресс обязан лечь на диск до экрана результатов");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private class FakeStore : IProgressStore
    {
        public int SaveCalls;
        private PlayerProgress _stored = new PlayerProgress();

        public PlayerProgress Load() => _stored;

        public void Save(PlayerProgress progress)
        {
            SaveCalls++;
            _stored = progress;
        }
    }
}
