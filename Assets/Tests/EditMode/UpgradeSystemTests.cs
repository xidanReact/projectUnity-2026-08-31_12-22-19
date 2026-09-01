using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Выдача апгрейдов и мутаций. Правила выдачи невидимы в коде вызывающей стороны,
/// поэтому единственный способ не сломать их случайно — зафиксировать тестами.
/// </summary>
public class UpgradeSystemTests
{
    private GameObject _go;
    private UpgradeSystem _system;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("Upgrades");
        _system = _go.AddComponent<UpgradeSystem>();
        _system.ResetRun();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    private static PlayerStats StatsFor(PathogenType type) =>
        new PlayerStats(PathogenData.CreateDefault(type));

    [Test]
    public void Roll_ВыдаётТриВарианта()
    {
        List<UpgradeDefinition> choices = _system.Roll(StatsFor(PathogenType.Virus));

        Assert.AreEqual(UpgradeSystem.ChoiceCount, choices.Count);
    }

    [Test]
    public void Roll_НеПовторяетВариантыВнутриОдногоВыбора()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(StatsFor(PathogenType.Fungus), levelNumber: 5);
            var ids = new HashSet<string>();

            for (int i = 0; i < choices.Count; i++)
            {
                Assert.IsTrue(ids.Add(choices[i].Id), "Один и тот же апгрейд не должен встречаться дважды в раскладе");
            }
        }
    }

    [Test]
    public void Roll_НеПредлагаетЧужиеПерсональныеАпгрейды()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(StatsFor(PathogenType.Bacteria), levelNumber: 5);

            for (int i = 0; i < choices.Count; i++)
            {
                UpgradeDefinition upgrade = choices[i];
                if (upgrade.RestrictedTo.HasValue)
                {
                    Assert.AreEqual(PathogenType.Bacteria, upgrade.RestrictedTo.Value,
                        $"Бактерии предложен апгрейд другого патогена: {upgrade.Id}");
                }
            }
        }
    }

    [Test]
    public void Roll_НеБольшеОднойМутацииВРаскладе()
    {
        // Выбор «мутация против цифр» читается, выбор из трёх мутаций — уже нет.
        for (int attempt = 0; attempt < 200; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(StatsFor(PathogenType.Parasite), levelNumber: 9);

            int mutations = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].IsMutation)
                {
                    mutations++;
                }
            }

            Assert.LessOrEqual(mutations, 1);
        }
    }

    [Test]
    public void Roll_НаПервомУровнеМутацийНет()
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(StatsFor(PathogenType.Virus), levelNumber: 0);

            for (int i = 0; i < choices.Count; i++)
            {
                Assert.IsFalse(choices[i].IsMutation, "Мутации открываются со второго уровня");
            }
        }
    }

    [Test]
    public void Take_УвеличиваетСчётчикВзятогоИПрименяетЭффект()
    {
        // Берём именно «Вирулентность»: часть апгрейдов (здоровье, лечение) обращается
        // к PlayerController, а поднимать полноценного игрока ради счётчика незачем.
        PlayerStats stats = StatsFor(PathogenType.Virus);
        UpgradeDefinition damage = FindById(stats, "dmg");
        float before = stats.AttackDamage;

        Assert.AreEqual(0, _system.TakenCount(damage.Id));
        _system.Take(damage, stats, null);

        Assert.AreEqual(1, _system.TakenCount(damage.Id));
        Assert.Greater(stats.AttackDamage, before);
    }

    [Test]
    public void Roll_НеПредлагаетИсчерпанныеАпгрейды()
    {
        PlayerStats stats = StatsFor(PathogenType.Virus);

        // «Деление» ограничено тремя взятиями — выбираем его до предела.
        UpgradeDefinition multishot = FindById(stats, "multishot");
        for (int i = 0; i < multishot.MaxTakes; i++)
        {
            _system.Take(multishot, stats, null);
        }

        for (int attempt = 0; attempt < 200; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(stats, levelNumber: 3);
            for (int i = 0; i < choices.Count; i++)
            {
                Assert.AreNotEqual("multishot", choices[i].Id, "Исчерпанный апгрейд не должен предлагаться");
            }
        }
    }

    [Test]
    public void Мутация_БерётсяТолькоОдинРаз()
    {
        PlayerStats stats = StatsFor(PathogenType.Fungus);
        UpgradeDefinition mycelium = FindMutation(stats, "mut_mycelium");

        Assert.AreEqual(1, mycelium.MaxTakes);
    }

    [Test]
    public void Мутация_ЗаписываетсяВСписокВзятых()
    {
        PlayerStats stats = StatsFor(PathogenType.Fungus);
        UpgradeDefinition mycelium = FindMutation(stats, "mut_mycelium");

        _system.Take(mycelium, stats, null);

        Assert.Contains(mycelium.Title, stats.TakenMutations, "Взятая мутация должна попадать в HUD-список");
        Assert.Greater(stats.SporeSynergyBonus, 0f, "Мутация должна включать своё поведение");
    }

    [Test]
    public void ResetRun_ЗабываетВзятоеЗаПрошлыйЗабег()
    {
        PlayerStats stats = StatsFor(PathogenType.Virus);
        UpgradeDefinition damage = FindById(stats, "dmg");
        _system.Take(damage, stats, null);

        _system.ResetRun();

        Assert.AreEqual(0, _system.TakenCount("dmg"));
    }

    /// <summary>Достаёт апгрейд из пула перебором раскладов — публичного доступа к пулу нет намеренно.</summary>
    private UpgradeDefinition FindById(PlayerStats stats, string id)
    {
        for (int attempt = 0; attempt < 2000; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(stats, levelNumber: 0, count: 99);
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id == id)
                {
                    return choices[i];
                }
            }
        }

        Assert.Fail($"Апгрейд {id} не найден в пуле");
        return null;
    }

    private UpgradeDefinition FindMutation(PlayerStats stats, string id)
    {
        for (int attempt = 0; attempt < 2000; attempt++)
        {
            List<UpgradeDefinition> choices = _system.Roll(stats, levelNumber: 5, count: 3);
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Id == id)
                {
                    return choices[i];
                }
            }
        }

        Assert.Fail($"Мутация {id} не найдена в пуле");
        return null;
    }
}
