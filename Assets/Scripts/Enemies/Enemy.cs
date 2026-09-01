using UnityEngine;

/// <summary>
/// Враг иммунной системы. Один класс на все архетипы биома — они отличаются
/// движением и способом атаки, но не жизненным циклом; плодить наследников
/// ради трёх веток в Update было бы дороже в поддержке.
/// </summary>
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour, IPooled, ICombatTarget
{
    private const float PlayerRadius = 0.42f;

    public EnemyData Data { get; private set; }
    public Health Health { get; private set; }
    public Faction Faction { get; private set; } = Faction.Immune;
    public float Radius { get; private set; } = 0.35f;

    public Transform Transform => transform;

    /// Заражённый враг перестаёт считаться угрозой: иначе вирус, поднявший
    /// последнего врага волны, растягивал бы её до истечения заражения.
    public bool CountsAsThreat => Faction == Faction.Immune;

    /// Кто нанёс последний урон. Нужно вирусу: заражается только тот, кого убил игрок.
    public Faction LastDamageSource { get; private set; } = Faction.Immune;

    /// Сегментный режим: враг идёт строем и не преследует игрока.
    public bool IsSegmentMember { get; private set; }

    private EnemySpawner _owner;
    private SpriteRenderer _renderer;
    private Color _baseColor;

    private float _healthMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _descendSpeed;
    private float _breachDamage;

    private float _attackTimer;
    private float _flashTimer;
    private float _infectionTimer;
    private float _strafeSeed;

    private void Awake()
    {
        Health = GetComponent<Health>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Configure(
        EnemyData data,
        EnemySpawner owner,
        Vector2 position,
        float healthMultiplier,
        float speedMultiplier,
        float descendSpeed,
        float breachDamage)
    {
        Data = data;
        _owner = owner;
        _healthMultiplier = healthMultiplier;
        _speedMultiplier = speedMultiplier;
        _descendSpeed = descendSpeed;
        _breachDamage = breachDamage;
        IsSegmentMember = descendSpeed > 0f;

        Faction = Faction.Immune;
        LastDamageSource = Faction.Immune;
        Radius = data.radius;
        _attackTimer = data.attackInterval * Random.Range(0.2f, 0.8f);
        _infectionTimer = 0f;
        _flashTimer = 0f;
        _strafeSeed = Random.Range(0f, 100f);

        transform.position = position;
        transform.localScale = Vector3.one * (data.radius * 2f);

        Health.Configure(data.maxHealth * healthMultiplier);

        if (_renderer != null)
        {
            _baseColor = data.bodyColor;
            _renderer.color = _baseColor;
            _renderer.sprite = PlaceholderArt.ForArchetype(data.archetype);
        }

        Battlefield.Register(this);
    }

    public void OnSpawned()
    {
    }

    public void OnDespawned()
    {
        Battlefield.Unregister(this);
        _owner = null;
    }

    public void ApplyDamage(float amount, Faction source)
    {
        if (!Health.IsAlive)
        {
            return;
        }

        LastDamageSource = source;
        float applied = Health.TakeDamage(amount);
        if (applied > 0f)
        {
            _flashTimer = 0.08f;
        }

        if (!Health.IsAlive)
        {
            _owner?.HandleEnemyDeath(this);
        }
    }

    /// <summary>Заражение вирусом: враг на время меняет сторону и бьёт своих.</summary>
    public void BecomeInfected(float duration, float healthFraction = 0.5f)
    {
        Faction = Faction.Infected;
        _infectionTimer = duration;
        IsSegmentMember = false;

        Health.Configure(Mathf.Max(1f, Data.maxHealth * _healthMultiplier * healthFraction));

        if (_renderer != null)
        {
            _baseColor = Color.Lerp(Data.bodyColor, new Color(0.85f, 0.30f, 0.75f), 0.75f);
            _renderer.color = _baseColor;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_flashTimer > 0f)
        {
            _flashTimer -= dt;
            if (_renderer != null)
            {
                _renderer.color = _flashTimer > 0f ? Color.white : _baseColor;
            }
        }

        if (Faction == Faction.Infected)
        {
            TickInfected(dt);
            return;
        }

        if (IsSegmentMember)
        {
            TickSegment(dt);
        }
        else
        {
            TickWave(dt);
        }
    }

    // --- Режим волн: свобода позиционирования, враги идут на игрока ---

    private void TickWave(float dt)
    {
        PlayerController player = Battlefield.Player;
        if (player == null)
        {
            return;
        }

        Vector2 position = transform.position;
        Vector2 target = player.transform.position;
        float speed = Data.moveSpeed * _speedMultiplier;

        if (Data.archetype == EnemyArchetype.Shooter)
        {
            float distance = Vector2.Distance(position, target);
            if (distance > Data.standoffDistance)
            {
                position = Vector2.MoveTowards(position, target, speed * dt);
            }
            else
            {
                // На дистанции — покачивание вбок, чтобы стрелков было не так легко расстрелять.
                position.x += Mathf.Sin(Time.time * 1.3f + _strafeSeed) * speed * 0.6f * dt;
            }

            transform.position = ClampInside(position);
            TickShooting(dt, target);
            return;
        }

        if (Data.archetype == EnemyArchetype.Tank)
        {
            // Танк почти не сворачивает — предсказуемая, продавливающая угроза.
            Vector2 direction = (target - position).normalized;
            direction.x *= 0.35f;
            position += direction.normalized * speed * dt;
        }
        else
        {
            position = Vector2.MoveTowards(position, target, speed * dt);
        }

        transform.position = ClampInside(position);
        TickContactAttack(dt, player);
    }

    // --- Режим сегментов: строй спускается, давление создаёт таймер, а не преследование ---

    private void TickSegment(float dt)
    {
        Vector2 position = transform.position;
        position.y -= _descendSpeed * _speedMultiplier * dt;
        transform.position = position;

        Arena arena = Arena.Instance;
        if (arena != null && position.y <= arena.LaneY)
        {
            PlayerController breached = Battlefield.Player;
            if (breached != null)
            {
                breached.Health.TakeDamage(_breachDamage);
            }

            if (_owner != null)
            {
                _owner.HandleSegmentBreach(this);
            }
            return;
        }

        if (Data.archetype == EnemyArchetype.Shooter)
        {
            PlayerController player = Battlefield.Player;
            if (player != null)
            {
                TickShooting(dt, player.transform.position);
            }
        }
    }

    // --- Заражённый: бьёт ближайшего врага, живёт до истечения таймера ---

    private void TickInfected(float dt)
    {
        _infectionTimer -= dt;
        if (_infectionTimer <= 0f)
        {
            if (_owner != null)
            {
                _owner.HandleInfectionExpired(this);
            }
            return;
        }

        ICombatTarget target = Battlefield.FindNearestEnemy(transform.position, Faction.Infected, -1f, this);
        if (target == null)
        {
            return;
        }

        Vector2 position = transform.position;
        float speed = Data.moveSpeed * _speedMultiplier * 1.3f;
        position = Vector2.MoveTowards(position, target.Transform.position, speed * dt);
        transform.position = position;

        _attackTimer -= dt;
        if (_attackTimer > 0f)
        {
            return;
        }

        float reach = Radius + target.Radius + 0.1f;
        if (((Vector2)target.Transform.position - position).sqrMagnitude <= reach * reach)
        {
            _attackTimer = Data.attackInterval;

            float factor = Battlefield.Player != null && Battlefield.Player.Stats != null
                ? Battlefield.Player.Stats.InfectedDamageFactor
                : 1f;
            target.ApplyDamage(Data.contactDamage * factor, Faction.Infected);
        }
    }

    // --- Общие куски атаки ---

    private void TickContactAttack(float dt, PlayerController player)
    {
        _attackTimer -= dt;
        if (_attackTimer > 0f)
        {
            return;
        }

        float reach = Radius + PlayerRadius + 0.05f;
        if (((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude <= reach * reach)
        {
            _attackTimer = Data.attackInterval;
            player.Health.TakeDamage(Data.contactDamage);
        }
    }

    private void TickShooting(float dt, Vector2 target)
    {
        _attackTimer -= dt;
        if (_attackTimer > 0f)
        {
            return;
        }
        _attackTimer = Data.attackInterval;

        Vector2 origin = transform.position;
        Vector2 direction = (target - origin).normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.down;
        }

        if (_owner != null)
        {
            _owner.FireEnemyProjectile(origin, direction, Data);
        }
    }

    private Vector2 ClampInside(Vector2 position)
    {
        Arena arena = Arena.Instance;
        if (arena == null)
        {
            return position;
        }

        position.x = Mathf.Clamp(position.x, -arena.HalfWidth + Radius, arena.HalfWidth - Radius);
        position.y = Mathf.Min(position.y, arena.SpawnY);
        return position;
    }
}
