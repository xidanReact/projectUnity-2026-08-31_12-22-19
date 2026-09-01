using UnityEngine;

/// <summary>
/// Паразит — «Прятки». При смертельном ударе патоген уходит в невидимость
/// на пару секунд: страховка от досадной смерти, один раз за уровень.
/// Во время окна враги продолжают бить, но урон полностью поглощается.
/// </summary>
public class ParasiteAbility : PathogenAbility, IDamageInterceptor
{
    private Health _health;
    private SpriteRenderer[] _renderers;
    private int _chargesLeft;
    private float _invincibleTimer;
    private PlayerMutations _mutations;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);

        _health = GetComponent<Health>();
        _mutations = GetComponent<PlayerMutations>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _chargesLeft = stats.InvincibilityChargesPerLevel;
        _invincibleTimer = 0f;
        ApplyVisual();
    }

    public override void OnLevelStarted()
    {
        // По плану заряд сбрасывается между уровнями, но не внутри одного забега произвольно.
        _chargesLeft = Stats.InvincibilityChargesPerLevel;
        _invincibleTimer = 0f;
        ApplyVisual();
    }

    public bool TryIntercept(ref float damage)
    {
        if (_invincibleTimer > 0f)
        {
            return true;
        }

        if (_chargesLeft <= 0 || _health == null)
        {
            return false;
        }

        // Срабатывает только на добивающий удар — иначе это был бы просто периодический иммунитет.
        if (damage < _health.Current)
        {
            return false;
        }

        _chargesLeft--;
        _invincibleTimer = Stats.InvincibilityDuration;
        ApplyVisual();

        if (_mutations != null)
        {
            _mutations.OnCloakStarted();
        }

        return true;
    }

    private void Update()
    {
        if (_invincibleTimer <= 0f)
        {
            return;
        }

        if (_mutations != null)
        {
            _mutations.OnCloakTick(Time.deltaTime);
        }

        _invincibleTimer -= Time.deltaTime;
        if (_invincibleTimer <= 0f)
        {
            _invincibleTimer = 0f;
        }
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (_renderers == null)
        {
            return;
        }

        float alpha = _invincibleTimer > 0f ? 0.3f : 1f;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
            {
                continue;
            }
            Color c = _renderers[i].color;
            c.a = alpha;
            _renderers[i].color = c;
        }
    }

    public override string StatusLine
    {
        get
        {
            if (_invincibleTimer > 0f)
            {
                return $"Прятки: активны {_invincibleTimer:0.0}с";
            }
            return _chargesLeft > 0
                ? $"Прятки: готовы ({_chargesLeft})"
                : "Прятки: израсходованы до конца уровня";
        }
    }
}
