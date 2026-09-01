using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Экономика метапрогрессии. Здесь ошибка стоит дороже всего: она трогает
/// сохранённый прогресс игрока, а не только текущий забег.
/// </summary>
public class MetaProgressionTests
{
    private GameObject _go;
    private MetaProgression _meta;
    private FakeStore _store;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Meta");
        _meta = _go.AddComponent<MetaProgression>();
        _store = new FakeStore();
        _meta.Initialize(_store);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void Initialize_ЗагружаетПрогрессИзХранилища()
    {
        Assert.IsNotNull(_meta.Progress);
        Assert.AreEqual(1, _store.LoadCalls);
    }

    [Test]
    public void AwardRun_НачисляетПоФормулеСоСрезомЖадности()
    {
        // 10 убийств * 0.5 + 3 уровня * 15 + 1 босс * 60 = 110; 110 * 0.65 = 71.5 -> 72
        int reward = _meta.AwardRun(kills: 10, levelsCleared: 3, bossesDefeated: 1);

        Assert.AreEqual(72, reward);
        Assert.AreEqual(72, _meta.Progress.biomass);
    }

    [Test]
    public void AwardRun_ОбновляетСтатистикуИСохраняет()
    {
        _meta.AwardRun(kills: 4, levelsCleared: 2, bossesDefeated: 0);

        Assert.AreEqual(1, _meta.Progress.totalRuns);
        Assert.AreEqual(4, _meta.Progress.totalKills);
        Assert.AreEqual(2, _meta.Progress.bestLevelReached);
        Assert.GreaterOrEqual(_store.SaveCalls, 1, "Прогресс обязан лечь на диск до экрана результатов");
    }

    [Test]
    public void AwardRun_НеУхудшаетЛучшийРезультат()
    {
        _meta.AwardRun(kills: 0, levelsCleared: 7, bossesDefeated: 0);
        _meta.AwardRun(kills: 0, levelsCleared: 2, bossesDefeated: 0);

        Assert.AreEqual(7, _meta.Progress.bestLevelReached);
    }

    [Test]
    public void TryPurchase_БезДенегНеПроходит()
    {
        PermanentUpgrade upgrade = _meta.Upgrades[0];

        Assert.IsFalse(_meta.CanAfford(upgrade));
        Assert.IsFalse(_meta.TryPurchase(upgrade));
        Assert.AreEqual(0, _meta.LevelOf(upgrade));
    }

    [Test]
    public void TryPurchase_СписываетЦенуИПоднимаетУровень()
    {
        PermanentUpgrade upgrade = _meta.Upgrades[0];
        int cost = upgrade.CostForNextLevel(0);
        _meta.Progress.biomass = cost;

        Assert.IsTrue(_meta.TryPurchase(upgrade));
        Assert.AreEqual(1, _meta.LevelOf(upgrade));
        Assert.AreEqual(0, _meta.Progress.biomass);
    }

    [Test]
    public void CostForNextLevel_РастётЛинейно()
    {
        PermanentUpgrade upgrade = _meta.Upgrades[0];

        Assert.AreEqual(upgrade.CostForNextLevel(0) * 2, upgrade.CostForNextLevel(1));
        Assert.AreEqual(upgrade.CostForNextLevel(0) * 3, upgrade.CostForNextLevel(2));
    }

    [Test]
    public void TryPurchase_УпираетсяВПотолокУровней()
    {
        PermanentUpgrade upgrade = _meta.Upgrades[0];
        _meta.Progress.biomass = 100000;

        for (int i = 0; i < upgrade.MaxLevel; i++)
        {
            Assert.IsTrue(_meta.TryPurchase(upgrade), $"Покупка уровня {i + 1} должна пройти");
        }

        Assert.IsFalse(_meta.TryPurchase(upgrade), "Сверх максимума покупать нельзя");
        Assert.IsFalse(_meta.CanAfford(upgrade));
        Assert.AreEqual(upgrade.MaxLevel, _meta.LevelOf(upgrade));
    }

    [Test]
    public void ApplyTo_БезКупленныхУлучшенийНеМеняетСтаты()
    {
        PathogenData data = PathogenData.CreateDefault(PathogenType.Virus);
        var stats = new PlayerStats(data);

        _meta.ApplyTo(stats);

        Assert.AreEqual(data.maxHealth, stats.MaxHealth);
        Assert.AreEqual(data.attackDamage, stats.AttackDamage);
    }

    [Test]
    public void ApplyTo_ПрименяетКупленныеУлучшения()
    {
        PathogenData data = PathogenData.CreateDefault(PathogenType.Virus);
        _meta.Progress.SetPerkLevel("perk_hp", 3);

        var stats = new PlayerStats(data);
        _meta.ApplyTo(stats);

        Assert.AreEqual(data.maxHealth + 24f, stats.MaxHealth, "+8 здоровья за уровень, куплено 3");
    }

    [Test]
    public void ResetProgress_ОбнуляетВсёИСохраняет()
    {
        _meta.Progress.biomass = 500;
        _meta.Progress.SetPerkLevel("perk_hp", 4);

        _meta.ResetProgress();

        Assert.AreEqual(0, _meta.Progress.biomass);
        Assert.AreEqual(0, _meta.Progress.GetPerkLevel("perk_hp"));
    }

    private class FakeStore : IProgressStore
    {
        public int LoadCalls;
        public int SaveCalls;
        public PlayerProgress Stored = new PlayerProgress();

        public PlayerProgress Load()
        {
            LoadCalls++;
            return Stored;
        }

        public void Save(PlayerProgress progress)
        {
            SaveCalls++;
            Stored = progress;
        }
    }
}
