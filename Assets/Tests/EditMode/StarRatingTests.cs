using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Оценка прохождения. Пороги считаются из расписания спавна самого уровня,
/// поэтому ошибка здесь перекашивает звёзды сразу по всей кампании.
/// </summary>
public class StarRatingTests
{
    /// <summary>Уровень с предсказуемым парТаймом: 10 врагов × 0.5с + 2с = 7с.</summary>
    private static CampaignNode MakeWaveNode()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        level.levelName = "Тестовый";

        var wave = new WaveDefinition
        {
            spawnIntervalRange = new Vector2(0.4f, 0.6f),
            postWaveDelay = 2f
        };
        wave.entries.Add(new SpawnEntry(EnemyCatalog.Neutrophil, 10));
        level.waves.Add(wave);

        return new CampaignNode("test_n1", "Тестовый", level, 0, Vector2.zero, 10, 10);
    }

    private static CampaignNode MakeBossNode()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Boss;
        level.bossData = BossCatalog.LymphNode;
        level.levelName = "Босс";

        return new CampaignNode("test_boss", "Босс", level, 7, Vector2.zero, 30, 30);
    }

    [Test]
    public void ParTime_СуммируетРасписаниеСпавна()
    {
        Assert.AreEqual(7f, StarRating.ParTime(MakeWaveNode()), 0.001f,
            "10 врагов × средний интервал 0.5с + пауза 2с");
    }

    [Test]
    public void ParTime_ДляБоссаБерётсяИзBossData()
    {
        Assert.AreEqual(BossCatalog.LymphNode.parTimeSeconds, StarRating.ParTime(MakeBossNode()), 0.001f);
    }

    [Test]
    public void Evaluate_ТриЗвездыЗаВремяВнутриПорога()
    {
        CampaignNode node = MakeWaveNode();

        Assert.AreEqual(3, StarRating.Evaluate(node, 1f), "Быстрее порога — максимум");
        Assert.AreEqual(3, StarRating.Evaluate(node, 7f * 1.15f), "Ровно на пороге три звезды ещё дают");
    }

    [Test]
    public void Evaluate_ДвеЗвездыМеждуПорогами()
    {
        CampaignNode node = MakeWaveNode();

        Assert.AreEqual(2, StarRating.Evaluate(node, 7f * 1.15f + 0.01f), "Чуть медленнее — уже две");
        Assert.AreEqual(2, StarRating.Evaluate(node, 7f * 1.7f), "Ровно на втором пороге две звезды ещё дают");
    }

    [Test]
    public void Evaluate_ОднаЗвездаЗаЛюбоеПрохождение()
    {
        CampaignNode node = MakeWaveNode();

        Assert.AreEqual(1, StarRating.Evaluate(node, 7f * 1.7f + 0.01f));
        Assert.AreEqual(1, StarRating.Evaluate(node, 100000f), "Пройденный уровень никогда не даёт ноль");
    }

    [Test]
    public void ParTime_НикогдаНеНоль()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        var empty = new CampaignNode("test_empty", "Пустой", level, 0, Vector2.zero, 1, 1);

        Assert.Greater(StarRating.ParTime(empty), 0f,
            "Нулевой парТайм превратил бы пороги в деление на ноль и отдал бы одну звезду всегда");
    }
}
