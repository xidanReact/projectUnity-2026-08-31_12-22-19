using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Билд попытки биома. Правило «апгрейд только за первое прохождение узла»
/// живёт здесь: без него первый узел становится фермой апгрейдов и босс
/// перестаёт быть проверкой билда.
/// </summary>
public class BiomeRunTests
{
    private static BiomeRun MakeRun()
    {
        PathogenData data = PathogenData.CreateDefault(PathogenType.Virus);
        return new BiomeRun(CampaignBuilder.BloodstreamId, data, new PlayerStats(data));
    }

    [Test]
    public void НовыйЗабег_ДаётАпгрейдЗаПервоеПрохождениеУзла()
    {
        BiomeRun run = MakeRun();

        Assert.IsTrue(run.ShouldGrantUpgrade("b1_n1"));
    }

    [Test]
    public void ПовторУзлаВТойЖеПопытке_АпгрейдаНеДаёт()
    {
        BiomeRun run = MakeRun();

        run.MarkUpgradeGranted("b1_n1");

        Assert.IsFalse(run.ShouldGrantUpgrade("b1_n1"),
            "Иначе первый узел фармится до полного билда");
        Assert.IsTrue(run.ShouldGrantUpgrade("b1_n2"), "Другие узлы это не затрагивает");
    }

    [Test]
    public void НоваяПопытка_СбрасываетВыданныеАпгрейды()
    {
        BiomeRun first = MakeRun();
        first.MarkUpgradeGranted("b1_n1");

        BiomeRun second = MakeRun();

        Assert.IsTrue(second.ShouldGrantUpgrade("b1_n1"),
            "Билд сгорает вместе с попыткой — заход с нуля даёт апгрейды заново");
    }

    [Test]
    public void RegisterClear_КопитУбийстваИСчётПройденных()
    {
        BiomeRun run = MakeRun();

        run.RegisterClear("b1_n1", kills: 30);
        run.RegisterClear("b1_n2", kills: 12);
        run.RegisterClear("b1_n1", kills: 5);

        Assert.AreEqual(47, run.TotalKills, "Убийства считаются и за повторы");
        Assert.AreEqual(2, run.NodesCleared, "Уникальных пройденных узлов — два");
    }

    [Test]
    public void Create_НакладываетПерманентныеУлучшенияНаСтартовыеСтаты()
    {
        var go = new GameObject("Meta");
        try
        {
            var meta = go.AddComponent<MetaProgression>();
            meta.Initialize(new FakeStore());
            meta.Progress.SetPerkLevel("perk_hp", 2);

            PathogenData data = PathogenData.CreateDefault(PathogenType.Bacteria);
            BiomeRun run = BiomeRun.Create(CampaignBuilder.BloodstreamId, data, meta);

            Assert.AreEqual(data.maxHealth + 16f, run.Stats.MaxHealth, 0.001f,
                "+8 здоровья за уровень перка, куплено 2");
            Assert.AreEqual(PathogenType.Bacteria, run.Stats.Type);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private class FakeStore : IProgressStore
    {
        private PlayerProgress _stored = new PlayerProgress();
        public PlayerProgress Load() => _stored;
        public void Save(PlayerProgress progress) => _stored = progress;
    }
}
