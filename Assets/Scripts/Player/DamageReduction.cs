using UnityEngine;

/// <summary>
/// Процентное снижение входящего урона. Отдельный компонент, а не поле в Health:
/// урон уже проходит через цепочку IDamageInterceptor (щит бактерии, прятки паразита),
/// и снижение обязано жить в той же цепочке, иначе порядок применения станет неявным.
/// </summary>
public class DamageReduction : MonoBehaviour, IDamageInterceptor
{
    /// Потолок: без него апгрейды сложились бы в полную неуязвимость.
    private const float MaxReduction = 0.6f;

    private PlayerStats _stats;

    public void Initialize(PlayerStats stats)
    {
        _stats = stats;
    }

    public bool TryIntercept(ref float damage)
    {
        if (_stats == null || _stats.DamageReduction <= 0f)
        {
            return false;
        }

        damage *= 1f - Mathf.Min(MaxReduction, _stats.DamageReduction);

        // Урон уменьшен, но не поглощён — цепочка должна продолжиться.
        return false;
    }
}
