using UnityEngine;

/// <summary>
/// Бактерия — «Биоплёнка». Регенерирующий щит вокруг патогена:
/// блокирует один удар целиком, затем восстанавливается за фиксированное время.
/// </summary>
public class BacteriaAbility : PathogenAbility, IDamageInterceptor
{
    private int _charges;
    private float _rechargeTimer;
    private PlayerMutations _mutations;
    private SpriteRenderer _shieldRenderer;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);

        _charges = stats.ShieldCharges;
        _rechargeTimer = 0f;
        _mutations = GetComponent<PlayerMutations>();

        if (_shieldRenderer == null)
        {
            _shieldRenderer = PoolHub.AddSprite(gameObject, PlaceholderArt.Ring, sortingOrder: 11);
            _shieldRenderer.transform.localScale = Vector3.one * 1.45f;
        }
        UpdateVisual();
    }

    public override void OnLevelStarted()
    {
        _charges = Stats.ShieldCharges;
        _rechargeTimer = 0f;
        UpdateVisual();
    }

    public bool TryIntercept(ref float damage)
    {
        if (_charges <= 0)
        {
            return false;
        }

        _charges--;
        // Кулдаун отсчитывается от последнего снятого заряда, а не от каждого по отдельности —
        // так щит не превращается в непрерывную неуязвимость при плотной толпе.
        _rechargeTimer = Stats.ShieldCooldown;
        UpdateVisual();

        if (_mutations != null)
        {
            _mutations.OnShieldAbsorbed();
        }

        return true;
    }

    private void Update()
    {
        if (Stats == null || _charges >= Stats.ShieldCharges)
        {
            return;
        }

        _rechargeTimer -= Time.deltaTime;
        if (_rechargeTimer <= 0f)
        {
            _charges++;
            _rechargeTimer = _charges < Stats.ShieldCharges ? Stats.ShieldCooldown : 0f;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (_shieldRenderer == null)
        {
            return;
        }

        _shieldRenderer.enabled = _charges > 0;
        _shieldRenderer.color = new Color(0.45f, 0.95f, 0.60f, 0.75f);
    }

    public override string StatusLine
    {
        get
        {
            if (Stats == null)
            {
                return "Биоплёнка";
            }
            return _charges >= Stats.ShieldCharges
                ? $"Биоплёнка: готова ({_charges}/{Stats.ShieldCharges})"
                : $"Биоплёнка: {_charges}/{Stats.ShieldCharges}, восст. {_rechargeTimer:0.0}с";
        }
    }
}
