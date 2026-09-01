using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент, который может перехватить входящий урон до его применения.
/// Через него работают щит бактерии и невидимость паразита — сами способности
/// ничего не знают про Health, кроме этого контракта.
/// </summary>
public interface IDamageInterceptor
{
    /// Вернуть true, если урон полностью поглощён. damage можно и уменьшить.
    bool TryIntercept(ref float damage);
}

/// <summary>
/// Здоровье. Общий компонент для игрока и врагов — правила урона должны быть
/// одинаковыми, иначе заражённые враги (которые бьют других врагов) поведут себя иначе.
/// </summary>
public class Health : MonoBehaviour
{
    public float Max { get; private set; } = 1f;
    public float Current { get; private set; } = 1f;
    public bool IsAlive => Current > 0f;
    public float Normalized => Max > 0f ? Mathf.Clamp01(Current / Max) : 0f;

    /// (нанесённый урон, текущее здоровье)
    public event Action<float, float> Damaged;
    public event Action Died;

    private readonly List<IDamageInterceptor> _interceptors = new List<IDamageInterceptor>();
    private bool _deathReported;

    /// Полный сброс. Вызывается при выдаче из пула и в начале уровня.
    public void Configure(float max, bool refill = true)
    {
        Max = Mathf.Max(1f, max);
        if (refill || Current > Max)
        {
            Current = Max;
        }
        _deathReported = false;

        _interceptors.Clear();
        GetComponents(_interceptors);
    }

    /// Пересобрать список перехватчиков — нужно, если способность добавлена после Configure.
    public void RefreshInterceptors()
    {
        _interceptors.Clear();
        GetComponents(_interceptors);
    }

    public float TakeDamage(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return 0f;
        }

        for (int i = 0; i < _interceptors.Count; i++)
        {
            if (_interceptors[i] != null && _interceptors[i].TryIntercept(ref amount))
            {
                return 0f;
            }
        }

        if (amount <= 0f)
        {
            return 0f;
        }

        float applied = Mathf.Min(amount, Current);
        Current -= applied;
        Damaged?.Invoke(applied, Current);

        if (Current <= 0f && !_deathReported)
        {
            _deathReported = true;
            Died?.Invoke();
        }

        return applied;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !IsAlive)
        {
            return;
        }
        Current = Mathf.Min(Max, Current + amount);
    }

    /// Прибавка к максимуму от апгрейдов — лечит на ту же величину, чтобы апгрейд ощущался сразу.
    public void AddMaxHealth(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }
        Max += amount;
        Current += amount;
    }
}
