using UnityEngine;

/// <summary>
/// Уникальная механика патогена (вариант A из dev-plan.md).
/// Все четыре полностью пассивные: игрок не нажимает ничего сверх движения.
/// Способность подключается к бою через три точки — смерть врага, попадание
/// снаряда и входящий урон (последнее — через IDamageInterceptor у Health).
/// </summary>
public abstract class PathogenAbility : MonoBehaviour
{
    protected PlayerStats Stats { get; private set; }

    public virtual void Initialize(PlayerStats stats)
    {
        Stats = stats;
    }

    /// Начало уровня — момент сброса «раз за уровень» ресурсов.
    public virtual void OnLevelStarted()
    {
    }

    /// <summary>
    /// Способность может «забрать» смерть врага: вернуть true, если враг
    /// НЕ должен уходить в пул (вирус поднимает его заражённым).
    /// </summary>
    public virtual bool TryConsumeKill(Enemy enemy) => false;

    /// Попадание снаряда игрока — точка, где грибок сеет спору.
    public virtual void OnPlayerProjectileHit(Vector2 point)
    {
    }

    /// Короткая строка состояния для прототипного HUD.
    public abstract string StatusLine { get; }
}
