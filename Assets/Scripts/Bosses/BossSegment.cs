using UnityEngine;

/// <summary>
/// Один сегмент урона босса. Живёт как самостоятельная цель: у него своё
/// здоровье и своя атака, которая работает ровно пока сегмент цел.
/// Не в пуле — сегментов за уровень ровно столько, сколько описано в BossData.
/// </summary>
[RequireComponent(typeof(Health))]
public class BossSegment : MonoBehaviour, ICombatTarget
{
    public BossSegmentDefinition Definition { get; private set; }
    public Health Health { get; private set; }

    public Faction Faction => Faction.Immune;
    public float Radius { get; private set; } = 0.85f;
    public Transform Transform => transform;

    /// <summary>
    /// Сегменты не считаются «угрозой» для условия зачистки волны:
    /// конец босс-уровня определяет сам босс, когда падёт последний сегмент.
    /// </summary>
    public bool CountsAsThreat => false;

    private Boss _boss;
    private SpriteRenderer _renderer;
    private Color _baseColor;
    private float _attackTimer;
    private float _flashTimer;
    private bool _dead;

    private void Awake()
    {
        Health = GetComponent<Health>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Configure(BossSegmentDefinition definition, Boss boss, float healthMultiplier)
    {
        Definition = definition;
        _boss = boss;
        Radius = definition.radius;
        _dead = false;
        _flashTimer = 0f;

        // Разводим стартовые таймеры, иначе все сегменты стреляют одним залпом.
        _attackTimer = definition.attackInterval * Random.Range(0.35f, 1f);

        transform.localScale = Vector3.one * (definition.radius * 2f);
        Health.Configure(definition.maxHealth * healthMultiplier);

        if (_renderer != null)
        {
            _baseColor = definition.color;
            _renderer.color = _baseColor;
        }

        Battlefield.Register(this);
    }

    public void ApplyDamage(float amount, Faction source)
    {
        if (_dead || !Health.IsAlive)
        {
            return;
        }

        if (Health.TakeDamage(amount) > 0f)
        {
            _flashTimer = 0.08f;
        }

        if (!Health.IsAlive)
        {
            Die();
        }
    }

    private void Die()
    {
        _dead = true;
        Battlefield.Unregister(this);

        if (_renderer != null)
        {
            // Сегмент остаётся на месте потухшим: так видно, сколько босса уже снято.
            _renderer.color = new Color(0.25f, 0.25f, 0.28f, 0.55f);
        }

        _boss.OnSegmentDestroyed(this);
    }

    public void Teardown()
    {
        Battlefield.Unregister(this);
    }

    private void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_renderer != null)
            {
                _renderer.color = _flashTimer > 0f ? Color.white : _baseColor;
            }
        }

        if (_dead || _boss == null || !_boss.IsEngaged)
        {
            return;
        }

        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f)
        {
            return;
        }

        _attackTimer = Definition.attackInterval * _boss.AttackIntervalScale;
        _boss.ExecuteAttack(this);
    }
}
