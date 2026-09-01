using UnityEngine;

/// <summary>
/// Всё, во что можно попасть: рядовой враг, заражённый враг, сегмент босса.
/// Введён в Фазе 2 — до неё единственной целью был Enemy, но сегменты босса
/// не враги и не живут в пуле, а поиск целей и снаряды должны работать с ними
/// одинаково.
/// </summary>
public interface ICombatTarget
{
    Faction Faction { get; }

    /// Радиус попадания в мировых единицах.
    float Radius { get; }

    Health Health { get; }

    Transform Transform { get; }

    /// <summary>
    /// Считается ли цель угрозой для условия «уровень зачищен».
    /// Заражённые враги — нет: иначе вирус растягивал бы волну бесконечно.
    /// </summary>
    bool CountsAsThreat { get; }

    void ApplyDamage(float amount, Faction source);
}
