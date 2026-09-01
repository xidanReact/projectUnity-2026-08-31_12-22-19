using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Миграция сейва. Единственное место в проекте, где ошибка стирает прогресс
/// игрока безвозвратно, — поэтому проверяется отдельно от всей остальной меты.
/// </summary>
public class ProgressMigrationTests
{
    private string _dir;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "pathogen_migration_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private void WriteRawSave(string json)
    {
        File.WriteAllText(Path.Combine(_dir, "progress.json"), json);
    }

    [Test]
    public void Миграция_СохраняетБиомассуИПеркиИзВерсии1()
    {
        WriteRawSave("{\"version\":1,\"biomass\":320," +
                     "\"perks\":[{\"id\":\"perk_hp\",\"level\":3}]," +
                     "\"totalRuns\":9,\"bestLevelReached\":5,\"totalKills\":700,\"bossesDefeated\":2}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.AreEqual(320, progress.biomass, "Биомасса обязана пережить миграцию");
        Assert.AreEqual(3, progress.GetPerkLevel("perk_hp"), "Купленные перки обязаны пережить миграцию");
        Assert.AreEqual(9, progress.totalRuns);
        Assert.AreEqual(700, progress.totalKills);
        Assert.AreEqual(2, progress.bossesDefeated);
    }

    [Test]
    public void Миграция_ПоднимаетВерсиюИЗаполняетНовыеПоля()
    {
        WriteRawSave("{\"version\":1,\"biomass\":10,\"perks\":[]}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.AreEqual(ProgressMigration.CurrentVersion, progress.version);
        Assert.AreEqual(0, progress.gold, "Золота в версии 1 не было — начинаем с нуля");
        Assert.IsNotNull(progress.campaign);
        Assert.AreEqual(0, progress.campaign.nodes.Count, "Кампания не проходилась");
        Assert.IsNotNull(progress.settings);
        Assert.AreEqual(1f, progress.settings.masterVolume);
        Assert.AreEqual(string.Empty, progress.settings.playerName);
    }

    [Test]
    public void Миграция_НеТрогаетУжеАктуальныйСейв()
    {
        WriteRawSave("{\"version\":2,\"biomass\":5,\"gold\":77,\"perks\":[]," +
                     "\"settings\":{\"masterVolume\":0.3,\"musicVolume\":0.4,\"sfxVolume\":0.5,\"playerName\":\"мдв\"}," +
                     "\"lastPathogen\":\"Fungus\"," +
                     "\"campaign\":{\"nodes\":[{\"id\":\"b1_n1\",\"stars\":2}],\"biomesUnlocked\":[\"biome_bloodstream\"]}}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.AreEqual(77, progress.gold);
        Assert.AreEqual(0.3f, progress.settings.masterVolume);
        Assert.AreEqual("мдв", progress.settings.playerName);
        Assert.AreEqual("Fungus", progress.lastPathogen);
        Assert.AreEqual(2, progress.campaign.StarsOf("b1_n1"));
        Assert.IsTrue(progress.campaign.IsBiomeUnlocked("biome_bloodstream"));
    }

    [Test]
    public void Миграция_ПереживаетNullВСпискахПослеJsonUtility()
    {
        // JsonUtility кладёт null в список, если в JSON он записан как null.
        WriteRawSave("{\"version\":2,\"biomass\":1,\"perks\":null,\"campaign\":{\"nodes\":null,\"biomesUnlocked\":null}}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.IsNotNull(progress.perks);
        Assert.IsNotNull(progress.campaign.nodes);
        Assert.IsNotNull(progress.campaign.biomesUnlocked);
        Assert.DoesNotThrow(() => progress.campaign.SetStars("b1_n1", 1));
    }

    [Test]
    public void SetStars_ТолькоПовышаетРезультат()
    {
        var campaign = new CampaignProgress();

        campaign.SetStars("b1_n1", 3);
        campaign.SetStars("b1_n1", 1);

        Assert.AreEqual(3, campaign.StarsOf("b1_n1"), "Худший повтор не должен затирать лучший результат");
    }

    [Test]
    public void UnlockBiome_НеДублируетЗаписи()
    {
        var campaign = new CampaignProgress();

        campaign.UnlockBiome("biome_bloodstream");
        campaign.UnlockBiome("biome_bloodstream");

        Assert.AreEqual(1, campaign.biomesUnlocked.Count);
    }
}
