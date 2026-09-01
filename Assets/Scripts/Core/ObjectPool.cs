using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Объект, который умеет жить в пуле. Вся инициализация — в OnSpawned,
/// вся очистка (таймеры, ссылки на цель) — в OnDespawned.
/// </summary>
public interface IPooled
{
    void OnSpawned();
    void OnDespawned();
}

/// <summary>
/// Пул объектов. По dev-plan.md заводится с самого начала Фазы 1 —
/// при сотнях врагов на экране Instantiate/Destroy в рантайме недопустим.
/// </summary>
public class ObjectPool<T> where T : Component
{
    private readonly Func<T> _factory;
    private readonly Transform _parent;
    private readonly Stack<T> _idle = new Stack<T>();
    private readonly HashSet<T> _active = new HashSet<T>();

    public ObjectPool(Func<T> factory, Transform parent, int prewarm = 0)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _parent = parent;
        Prewarm(prewarm);
    }

    public int ActiveCount => _active.Count;
    public int IdleCount => _idle.Count;

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T instance = CreateInstance();
            instance.gameObject.SetActive(false);
            _idle.Push(instance);
        }
    }

    public T Get()
    {
        T instance = _idle.Count > 0 ? _idle.Pop() : CreateInstance();

        // Инстанс мог быть уничтожен вместе со сценой — тогда берём новый.
        if (instance == null)
        {
            instance = CreateInstance();
        }

        instance.gameObject.SetActive(true);
        _active.Add(instance);

        if (instance is IPooled pooled)
        {
            pooled.OnSpawned();
        }

        return instance;
    }

    public void Release(T instance)
    {
        if (instance == null || !_active.Remove(instance))
        {
            // Двойной Release — частая ошибка (умер от урона и в тот же кадр от зоны).
            return;
        }

        if (instance is IPooled pooled)
        {
            pooled.OnDespawned();
        }

        instance.gameObject.SetActive(false);
        _idle.Push(instance);
    }

    /// Вернуть в пул всё активное. Нужно между уровнями и при смерти игрока.
    public void ReleaseAll()
    {
        if (_active.Count == 0)
        {
            return;
        }

        var snapshot = new List<T>(_active);
        for (int i = 0; i < snapshot.Count; i++)
        {
            Release(snapshot[i]);
        }
    }

    /// Перебор активных без аллокации списка — для поиска ближайшей цели каждый кадр.
    public HashSet<T>.Enumerator GetActiveEnumerator() => _active.GetEnumerator();

    private T CreateInstance()
    {
        T instance = _factory();
        if (_parent != null)
        {
            instance.transform.SetParent(_parent, false);
        }
        return instance;
    }
}
