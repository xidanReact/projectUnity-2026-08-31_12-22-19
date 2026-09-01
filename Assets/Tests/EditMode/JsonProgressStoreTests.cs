using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Запись прогресса на диск. Единственное место в проекте, где ошибка
/// уничтожает данные игрока безвозвратно, поэтому проверяется и нормальный
/// путь, и повреждённый файл.
/// </summary>
public class JsonProgressStoreTests
{
    private string _directory;

    [SetUp]
    public void SetUp()
    {
        // Отдельная папка на каждый прогон: тесты не должны трогать
        // настоящий progress.json игрока в persistentDataPath.
        _directory = Path.Combine(Path.GetTempPath(), "pathogen_tests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string SaveFilePath => Path.Combine(_directory, "progress.json");

    [Test]
    public void Load_БезФайлаОтдаётЧистыйПрогресс()
    {
        var store = new JsonProgressStore(_directory);

        PlayerProgress progress = store.Load();

        Assert.IsNotNull(progress);
        Assert.AreEqual(0, progress.biomass);
        Assert.IsNotNull(progress.perks);
    }

    [Test]
    public void SaveLoad_СохраняетВалютуИСтатистику()
    {
        var store = new JsonProgressStore(_directory);
        var progress = new PlayerProgress
        {
            biomass = 1234,
            totalRuns = 7,
            totalKills = 890,
            bossesDefeated = 2,
            bestLevelReached = 5
        };

        store.Save(progress);
        PlayerProgress loaded = new JsonProgressStore(_directory).Load();

        Assert.AreEqual(1234, loaded.biomass);
        Assert.AreEqual(7, loaded.totalRuns);
        Assert.AreEqual(890, loaded.totalKills);
        Assert.AreEqual(2, loaded.bossesDefeated);
        Assert.AreEqual(5, loaded.bestLevelReached);
    }

    [Test]
    public void SaveLoad_СохраняетУровниУлучшений()
    {
        var store = new JsonProgressStore(_directory);
        var progress = new PlayerProgress();
        progress.SetPerkLevel("perk_hp", 3);
        progress.SetPerkLevel("perk_damage", 5);

        store.Save(progress);
        PlayerProgress loaded = new JsonProgressStore(_directory).Load();

        Assert.AreEqual(3, loaded.GetPerkLevel("perk_hp"));
        Assert.AreEqual(5, loaded.GetPerkLevel("perk_damage"));
        Assert.AreEqual(0, loaded.GetPerkLevel("perk_move"), "Некупленное улучшение читается как нулевой уровень");
    }

    [Test]
    public void SetPerkLevel_ПерезаписываетСуществующийУровень()
    {
        var progress = new PlayerProgress();
        progress.SetPerkLevel("perk_hp", 1);
        progress.SetPerkLevel("perk_hp", 4);

        Assert.AreEqual(4, progress.GetPerkLevel("perk_hp"));
        Assert.AreEqual(1, progress.perks.Count, "Уровень должен обновляться, а не дублироваться");
    }

    [Test]
    public void Save_НеОставляетВременныйФайл()
    {
        var store = new JsonProgressStore(_directory);

        store.Save(new PlayerProgress { biomass = 10 });

        Assert.IsTrue(File.Exists(SaveFilePath));
        Assert.IsFalse(File.Exists(SaveFilePath + ".tmp"), "Временный файл должен быть переименован, а не оставлен");
    }

    [Test]
    public void Save_ПерезаписываетПредыдущийФайл()
    {
        var store = new JsonProgressStore(_directory);

        store.Save(new PlayerProgress { biomass = 10 });
        store.Save(new PlayerProgress { biomass = 20 });

        Assert.AreEqual(20, store.Load().biomass);
    }

    [Test]
    public void Load_ПовреждённыйФайлНеРонитИгруИПишетВЛог()
    {
        File.WriteAllText(SaveFilePath, "{ это не json");
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[Meta\]"));

        PlayerProgress progress = new JsonProgressStore(_directory).Load();

        Assert.IsNotNull(progress);
        Assert.AreEqual(0, progress.biomass);
    }

    [Test]
    public void Load_ПустойФайлОтдаётЧистыйПрогресс()
    {
        File.WriteAllText(SaveFilePath, string.Empty);
        LogAssert.ignoreFailingMessages = true;

        PlayerProgress progress = new JsonProgressStore(_directory).Load();

        LogAssert.ignoreFailingMessages = false;
        Assert.IsNotNull(progress);
        Assert.AreEqual(0, progress.biomass);
    }
}
