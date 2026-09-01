using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Пул — фундамент производительности всего боя. Ошибка здесь не падает,
/// а тихо превращается в утечку объектов или в двух врагов на одном инстансе.
/// </summary>
public class ObjectPoolTests
{
    private readonly List<GameObject> _created = new List<GameObject>();
    private Transform _root;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("PoolRoot").transform;
        _created.Add(_root.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _created.Count; i++)
        {
            if (_created[i] != null)
            {
                Object.DestroyImmediate(_created[i]);
            }
        }
        _created.Clear();
    }

    private ObjectPool<Probe> CreatePool(int prewarm = 0)
    {
        return new ObjectPool<Probe>(() =>
        {
            var go = new GameObject("Probe");
            _created.Add(go);
            return go.AddComponent<Probe>();
        }, _root, prewarm);
    }

    [Test]
    public void Prewarm_СоздаётОбъектыВыключенными()
    {
        ObjectPool<Probe> pool = CreatePool(prewarm: 3);

        Assert.AreEqual(3, pool.IdleCount);
        Assert.AreEqual(0, pool.ActiveCount);
    }

    [Test]
    public void Get_ВключаетОбъектИСообщаетЕмуОСпавне()
    {
        ObjectPool<Probe> pool = CreatePool(prewarm: 1);

        Probe probe = pool.Get();

        Assert.IsTrue(probe.gameObject.activeSelf);
        Assert.AreEqual(1, probe.Spawned);
        Assert.AreEqual(1, pool.ActiveCount);
        Assert.AreEqual(0, pool.IdleCount);
    }

    [Test]
    public void Release_ВозвращаетТотЖеИнстансВСледующийGet()
    {
        ObjectPool<Probe> pool = CreatePool();

        Probe first = pool.Get();
        pool.Release(first);
        Probe second = pool.Get();

        Assert.AreSame(first, second, "Пул обязан переиспользовать инстанс, а не создавать новый");
    }

    [Test]
    public void Release_ВыключаетОбъектИСообщаетЕмуОВозврате()
    {
        ObjectPool<Probe> pool = CreatePool();
        Probe probe = pool.Get();

        pool.Release(probe);

        Assert.IsFalse(probe.gameObject.activeSelf);
        Assert.AreEqual(1, probe.Despawned);
    }

    [Test]
    public void Release_ПовторныйВызовИгнорируется()
    {
        // Реальный случай: враг умер от снаряда и в тот же кадр от тика споры.
        ObjectPool<Probe> pool = CreatePool();
        Probe probe = pool.Get();

        pool.Release(probe);
        pool.Release(probe);

        Assert.AreEqual(1, probe.Despawned, "Двойной Release не должен вызывать OnDespawned дважды");
        Assert.AreEqual(1, pool.IdleCount, "Инстанс не должен попасть в пул дважды");
    }

    [Test]
    public void Release_ЧужогоИнстансаНеЛоматПул()
    {
        ObjectPool<Probe> pool = CreatePool();
        var strangerObject = new GameObject("Stranger");
        _created.Add(strangerObject);
        var stranger = strangerObject.AddComponent<Probe>();

        pool.Release(stranger);

        Assert.AreEqual(0, pool.IdleCount);
    }

    [Test]
    public void ReleaseAll_ВозвращаетВсёАктивное()
    {
        ObjectPool<Probe> pool = CreatePool();
        for (int i = 0; i < 5; i++)
        {
            pool.Get();
        }

        pool.ReleaseAll();

        Assert.AreEqual(0, pool.ActiveCount);
        Assert.AreEqual(5, pool.IdleCount);
    }

    [Test]
    public void Get_СверхПрогреваСоздаётНовыеИнстансы()
    {
        ObjectPool<Probe> pool = CreatePool(prewarm: 1);

        Probe a = pool.Get();
        Probe b = pool.Get();

        Assert.AreNotSame(a, b);
        Assert.AreEqual(2, pool.ActiveCount);
    }

    private class Probe : MonoBehaviour, IPooled
    {
        public int Spawned;
        public int Despawned;

        public void OnSpawned() => Spawned++;
        public void OnDespawned() => Despawned++;
    }
}
