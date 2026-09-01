using UnityEngine;

/// <summary>
/// Грибок — «Споры». Каждая атака оставляет спору на месте попадания;
/// спора тлеет и бьёт всех рядом. Несколько спор естественным образом
/// складываются в зону, потому что их тики независимы.
/// </summary>
public class FungusAbility : PathogenAbility
{
    private int _planted;

    public override void OnLevelStarted()
    {
        _planted = 0;
    }

    public override void OnPlayerProjectileHit(Vector2 point)
    {
        if (PoolHub.Instance == null)
        {
            return;
        }

        PoolHub.Instance.PlantSpore(point, Stats);
        _planted++;
    }

    public override string StatusLine =>
        $"Споры: {Stats.SporeDamagePerTick:0.#}/тик · {Stats.SporeLifetime:0.#}с · r{Stats.SporeRadius:0.00} · посеяно {_planted}";
}
