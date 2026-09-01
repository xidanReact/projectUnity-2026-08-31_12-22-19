using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реестр всего живого на поле и единственная точка поиска целей.
/// Сейчас это честный перебор — на прототипных объёмах (сотни врагов) его хватает.
/// Когда упрёмся в профайлер (Фаза 5), внутренности меняются на пространственную сетку,
/// а все вызывающие стороны остаются прежними — ради этого поиск и централизован здесь.
/// </summary>
public static class Battlefield
{
    private static readonly List<ICombatTarget> _targets = new List<ICombatTarget>(256);

    public static PlayerController Player { get; set; }
    public static IReadOnlyList<ICombatTarget> Targets => _targets;

    /// Живые цели, которые считаются угрозой — по ним определяется, зачищен ли уровень.
    public static int ThreatCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].CountsAsThreat && _targets[i].Health.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public static void Register(ICombatTarget target)
    {
        if (!_targets.Contains(target))
        {
            _targets.Add(target);
        }
    }

    public static void Unregister(ICombatTarget target)
    {
        _targets.Remove(target);
    }

    public static void Clear()
    {
        _targets.Clear();
    }

    /// <summary>
    /// Ближайшая цель для стороны <paramref name="seeker"/>. maxRange &lt;= 0 — без ограничения.
    /// </summary>
    public static ICombatTarget FindNearestEnemy(Vector2 from, Faction seeker, float maxRange = -1f, ICombatTarget exclude = null)
    {
        ICombatTarget best = null;
        float bestSqr = maxRange > 0f ? maxRange * maxRange : float.MaxValue;

        for (int i = 0; i < _targets.Count; i++)
        {
            ICombatTarget candidate = _targets[i];
            if (candidate == exclude || !candidate.Health.IsAlive || !seeker.IsHostileTo(candidate.Faction))
            {
                continue;
            }

            float sqr = ((Vector2)candidate.Transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// Все цели в радиусе — для спор грибка и площадных эффектов.
    /// Результат пишется в переданный список, чтобы не аллоцировать каждый тик.
    /// </summary>
    public static void CollectEnemiesInRadius(Vector2 center, float radius, Faction seeker, List<ICombatTarget> results)
    {
        results.Clear();
        float sqrRadius = radius * radius;

        for (int i = 0; i < _targets.Count; i++)
        {
            ICombatTarget candidate = _targets[i];
            if (!candidate.Health.IsAlive || !seeker.IsHostileTo(candidate.Faction))
            {
                continue;
            }

            if (((Vector2)candidate.Transform.position - center).sqrMagnitude <= sqrRadius)
            {
                results.Add(candidate);
            }
        }
    }
}
