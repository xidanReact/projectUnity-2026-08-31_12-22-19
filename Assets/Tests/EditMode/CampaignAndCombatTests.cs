using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Генерация кампании, правила враждебности и копирование статов.
/// Мелкие вещи, на которых стоит весь остальной бой.
/// </summary>
public class CampaignAndCombatTests
{
    // --- Кампания ---

    [Test]
    public void BuildBloodstream_СоздаётЗапрошенноеЧислоУровней()
    {
        List<LevelData> levels = CampaignGenerator.BuildBloodstream(8);

        Assert.AreEqual(8, levels.Count);
    }

    [Test]
    public void BuildBloodstream_ЗакрываетБиомБоссом()
    {
        List<LevelData> levels = CampaignGenerator.BuildBloodstream(8);
        LevelData last = levels[levels.Count - 1];

        Assert.AreEqual(AdvanceType.Boss, last.advanceType);
        Assert.IsNotNull(last.bossData, "У босс-уровня обязан быть настроенный босс");
        Assert.Greater(last.bossData.segments.Count, 0);
    }

    [Test]
    public void BuildBloodstream_ЧередуетВолныИСегменты()
    {
        List<LevelData> levels = CampaignGenerator.BuildBloodstream(8);

        Assert.AreEqual(AdvanceType.Waves, levels[0].advanceType);
        Assert.AreEqual(AdvanceType.Segments, levels[1].advanceType);
        Assert.AreEqual(AdvanceType.Waves, levels[2].advanceType);
    }

    [Test]
    public void BuildBloodstream_ВсеБоевыеУровниИмеютВрагов()
    {
        List<LevelData> levels = CampaignGenerator.BuildBloodstream(8);

        for (int i = 0; i < levels.Count; i++)
        {
            LevelData level = levels[i];
            if (level.advanceType == AdvanceType.Boss)
            {
                continue;
            }

            Assert.Greater(level.waves.Count, 0, $"Уровень {i + 1} без волн подвиснет навсегда");
            for (int w = 0; w < level.waves.Count; w++)
            {
                Assert.Greater(level.waves[w].TotalCount, 0, $"Пустая волна {w} на уровне {i + 1}");
            }
        }
    }

    [Test]
    public void FindFirstBossLevel_НаходитИндексБосса()
    {
        List<LevelData> levels = CampaignGenerator.BuildBloodstream(8);

        Assert.AreEqual(7, CampaignGenerator.FindFirstBossLevel(levels));
    }

    [Test]
    public void БоссДосягаемДляСамогоКороткогоОружия()
    {
        // Если босс встанет дальше дальности атаки, уровень зависнет навсегда.
        BossData boss = BossCatalog.LymphNode;
        float shortestRange = float.MaxValue;

        foreach (PathogenType type in System.Enum.GetValues(typeof(PathogenType)))
        {
            shortestRange = Mathf.Min(shortestRange, PathogenData.CreateDefault(type).attackRange);
        }

        Assert.Less(boss.battleOffsetFromLane, shortestRange,
            "Боевая позиция босса обязана быть ближе самой короткой дальности патогена");
    }

    [Test]
    public void СтрелокПодходитБлижеСамойКороткойДальности()
    {
        // Тот же класс зависания: антитело, стоящее вне досягаемости, не даст зачистить волну.
        float shortestRange = float.MaxValue;
        foreach (PathogenType type in System.Enum.GetValues(typeof(PathogenType)))
        {
            shortestRange = Mathf.Min(shortestRange, PathogenData.CreateDefault(type).attackRange);
        }

        Assert.Less(EnemyCatalog.Antibody.standoffDistance, shortestRange);
    }

    [Test]
    public void Макрофаг_ДелитсяНаОсколки()
    {
        Assert.AreEqual(2, EnemyCatalog.Macrophage.splitCount);
        Assert.IsNotNull(EnemyCatalog.Macrophage.splitInto);
        Assert.AreNotSame(EnemyCatalog.Macrophage, EnemyCatalog.Macrophage.splitInto,
            "Танк не должен делиться сам на себя — это бесконечный уровень");
    }

    // --- Фракции ---

    [Test]
    public void Патоген_ВраждебенТолькоИммунитету()
    {
        Assert.IsTrue(Faction.Pathogen.IsHostileTo(Faction.Immune));
        Assert.IsFalse(Faction.Pathogen.IsHostileTo(Faction.Infected), "Заражённые — союзники, по ним нельзя стрелять");
        Assert.IsFalse(Faction.Pathogen.IsHostileTo(Faction.Pathogen));
    }

    [Test]
    public void Иммунитет_ВраждебенИПатогенуИЗаражённым()
    {
        Assert.IsTrue(Faction.Immune.IsHostileTo(Faction.Pathogen));
        Assert.IsTrue(Faction.Immune.IsHostileTo(Faction.Infected));
        Assert.IsFalse(Faction.Immune.IsHostileTo(Faction.Immune));
    }

    [Test]
    public void Заражённый_ВраждебенТолькоИммунитету()
    {
        Assert.IsTrue(Faction.Infected.IsHostileTo(Faction.Immune));
        Assert.IsFalse(Faction.Infected.IsHostileTo(Faction.Pathogen));
        Assert.IsFalse(Faction.Infected.IsHostileTo(Faction.Infected),
            "Иначе заражённые перебили бы друг друга вместо врагов");
    }

    // --- Статы ---

    [Test]
    public void PlayerStats_КопируетЗначенияИзКонфига()
    {
        PathogenData data = PathogenData.CreateDefault(PathogenType.Fungus);
        var stats = new PlayerStats(data);

        Assert.AreEqual(data.maxHealth, stats.MaxHealth);
        Assert.AreEqual(data.attackDamage, stats.AttackDamage);
        Assert.AreEqual(data.sporeRadius, stats.SporeRadius);
        Assert.AreEqual(PathogenType.Fungus, stats.Type);
    }

    [Test]
    public void PlayerStats_НеМутируетИсходныйКонфиг()
    {
        // Иначе баланс «утёк» бы между забегами: ScriptableObject живёт дольше забега.
        PathogenData data = PathogenData.CreateDefault(PathogenType.Virus);
        float originalDamage = data.attackDamage;

        var stats = new PlayerStats(data);
        stats.AttackDamage *= 10f;

        Assert.AreEqual(originalDamage, data.attackDamage);
    }

    [Test]
    public void SecondsBetweenShots_ОбратенСкоростиАтаки()
    {
        var stats = new PlayerStats(PathogenData.CreateDefault(PathogenType.Virus)) { AttackRate = 4f };

        Assert.AreEqual(0.25f, stats.SecondsBetweenShots, 0.0001f);
    }

    [Test]
    public void ВсеЧетыреПатогенаИмеютСвоиЗначения()
    {
        var names = new HashSet<string>();
        var colors = new HashSet<Color>();

        foreach (PathogenType type in System.Enum.GetValues(typeof(PathogenType)))
        {
            PathogenData data = PathogenData.CreateDefault(type);
            Assert.IsFalse(string.IsNullOrEmpty(data.pathogenName), $"У {type} нет имени");
            Assert.IsTrue(names.Add(data.pathogenName), "Имена патогенов должны различаться");
            Assert.IsTrue(colors.Add(data.bodyColor), "Цвета патогенов должны различаться");
            Assert.Greater(data.attackRange, 0f);
            Assert.Greater(data.maxHealth, 0f);
        }

        Assert.AreEqual(4, names.Count);
    }
}
