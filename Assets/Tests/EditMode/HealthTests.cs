using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Здоровье и цепочка перехватчиков урона — общий код для игрока и врагов,
/// поэтому ошибка здесь ломает обе стороны боя сразу.
/// </summary>
public class HealthTests
{
    private GameObject _go;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("HealthTest");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    private Health CreateHealth(float max = 100f)
    {
        var health = _go.AddComponent<Health>();
        health.Configure(max);
        return health;
    }

    [Test]
    public void Configure_ЗаполняетЗдоровьеДоМаксимума()
    {
        Health health = CreateHealth(80f);

        Assert.AreEqual(80f, health.Max);
        Assert.AreEqual(80f, health.Current);
        Assert.IsTrue(health.IsAlive);
        Assert.AreEqual(1f, health.Normalized);
    }

    [Test]
    public void TakeDamage_ВозвращаетФактическиНанесённыйУрон()
    {
        Health health = CreateHealth(50f);

        Assert.AreEqual(20f, health.TakeDamage(20f));
        Assert.AreEqual(30f, health.Current);
    }

    [Test]
    public void TakeDamage_НеУходитНижеНуля()
    {
        Health health = CreateHealth(30f);

        // Урон больше остатка засчитывается только по остатку — иначе счётчики
        // нанесённого урона врали бы на добивающих ударах.
        Assert.AreEqual(30f, health.TakeDamage(999f));
        Assert.AreEqual(0f, health.Current);
        Assert.IsFalse(health.IsAlive);
    }

    [Test]
    public void Died_ВызываетсяРовноОдинРаз()
    {
        Health health = CreateHealth(10f);
        int deaths = 0;
        health.Died += () => deaths++;

        health.TakeDamage(10f);
        health.TakeDamage(10f);

        Assert.AreEqual(1, deaths, "Повторный урон по трупу не должен пересчитывать смерть");
    }

    [Test]
    public void TakeDamage_ПоМёртвомуНичегоНеДелает()
    {
        Health health = CreateHealth(10f);
        health.TakeDamage(10f);

        Assert.AreEqual(0f, health.TakeDamage(5f));
    }

    [Test]
    public void Heal_НеПревышаетМаксимум()
    {
        Health health = CreateHealth(40f);
        health.TakeDamage(15f);

        health.Heal(100f);

        Assert.AreEqual(40f, health.Current);
    }

    [Test]
    public void Heal_НеВоскрешаетМёртвого()
    {
        Health health = CreateHealth(10f);
        health.TakeDamage(10f);

        health.Heal(50f);

        Assert.AreEqual(0f, health.Current);
        Assert.IsFalse(health.IsAlive);
    }

    [Test]
    public void AddMaxHealth_ЛечитНаТуЖеВеличину()
    {
        Health health = CreateHealth(50f);
        health.TakeDamage(20f);

        health.AddMaxHealth(10f);

        Assert.AreEqual(60f, health.Max);
        Assert.AreEqual(40f, health.Current, "Прибавка к максимуму должна ощущаться сразу");
    }

    [Test]
    public void Перехватчик_МожетПоглотитьУронЦеликом()
    {
        var absorber = _go.AddComponent<TestAbsorber>();
        Health health = CreateHealth(50f);

        Assert.AreEqual(0f, health.TakeDamage(30f));
        Assert.AreEqual(50f, health.Current);
        Assert.AreEqual(1, absorber.Calls);
    }

    [Test]
    public void Перехватчик_МожетУменьшитьУронНеПоглощаяЕго()
    {
        _go.AddComponent<TestHalver>();
        Health health = CreateHealth(50f);

        Assert.AreEqual(10f, health.TakeDamage(20f));
        Assert.AreEqual(40f, health.Current);
    }

    [Test]
    public void DamageReduction_СрезаетДолюУрона()
    {
        var stats = new PlayerStats(PathogenData.CreateDefault(PathogenType.Bacteria)) { DamageReduction = 0.25f };
        _go.AddComponent<DamageReduction>().Initialize(stats);
        Health health = CreateHealth(100f);

        Assert.AreEqual(75f, health.TakeDamage(100f));
    }

    [Test]
    public void DamageReduction_ОграниченаПотолком()
    {
        // Апгрейды складываются, и без потолка игрок стал бы неуязвимым.
        var stats = new PlayerStats(PathogenData.CreateDefault(PathogenType.Bacteria)) { DamageReduction = 5f };
        _go.AddComponent<DamageReduction>().Initialize(stats);
        Health health = CreateHealth(100f);

        // Допуск обязателен: потолок считается через float-арифметику и даёт
        // 39.9999962 вместо ровных 40 — сравнение на точное равенство здесь
        // проверяет представление float, а не правило потолка.
        Assert.AreEqual(40f, health.TakeDamage(100f), 0.001f, "Снижение урона должно упираться в 60%");
    }

    private class TestAbsorber : MonoBehaviour, IDamageInterceptor
    {
        public int Calls;

        public bool TryIntercept(ref float damage)
        {
            Calls++;
            return true;
        }
    }

    private class TestHalver : MonoBehaviour, IDamageInterceptor
    {
        public bool TryIntercept(ref float damage)
        {
            damage *= 0.5f;
            return false;
        }
    }
}
