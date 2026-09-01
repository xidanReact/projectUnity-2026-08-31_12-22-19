using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Босс-уровень: крупная неподвижная цель из нескольких сегментов урона.
/// Вариация core loop, а не отдельная система — патоген по-прежнему ходит
/// влево-вправо и бьёт автоматически, меняется только то, во что он бьёт
/// и чем ему отвечают.
/// </summary>
public class Boss : MonoBehaviour
{
    public BossData Data { get; private set; }

    /// Босс занял боевую позицию и сегменты начали атаковать.
    public bool IsEngaged { get; private set; }

    /// Все сегменты уничтожены.
    public bool IsDefeated { get; private set; }

    /// Множитель интервала атак: падает с каждым снятым сегментом.
    public float AttackIntervalScale { get; private set; } = 1f;

    public IReadOnlyList<BossSegment> Segments => _segments;

    private readonly List<BossSegment> _segments = new List<BossSegment>();
    private EnemySpawner _spawner;
    private LaneStrike _laneStrike;
    private float _targetY;
    private int _segmentsAlive;

    public void Configure(BossData data, EnemySpawner spawner, float healthMultiplier)
    {
        Data = data;
        _spawner = spawner;
        IsDefeated = false;
        IsEngaged = false;
        AttackIntervalScale = 1f;

        Arena arena = Arena.Instance;
        _targetY = arena.LaneY + data.battleOffsetFromLane;

        // Въезд сверху: даёт секунду понять, что уровень другой.
        transform.position = new Vector3(0f, arena.SpawnY + 2f, 0f);

        BuildBody();
        BuildSegments(healthMultiplier);

        _segmentsAlive = _segments.Count;
    }

    private void BuildBody()
    {
        var body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        SpriteRenderer renderer = PoolHub.AddSprite(body, PlaceholderArt.Circle, sortingOrder: 3);
        renderer.color = new Color(0.55f, 0.25f, 0.40f, 0.85f);
        body.transform.localScale = Vector3.one * 5.2f;

        var strikeObject = new GameObject("LaneStrike");
        strikeObject.transform.SetParent(transform.parent, false);
        SpriteRenderer strikeRenderer = PoolHub.AddSprite(strikeObject, PlaceholderArt.Square, sortingOrder: -5);
        strikeRenderer.color = Color.clear;
        _laneStrike = strikeObject.AddComponent<LaneStrike>();
    }

    private void BuildSegments(float healthMultiplier)
    {
        for (int i = 0; i < Data.segments.Count; i++)
        {
            BossSegmentDefinition definition = Data.segments[i];

            var segmentObject = new GameObject($"Segment_{definition.segmentName}");
            segmentObject.transform.SetParent(transform, false);
            segmentObject.transform.localPosition = definition.offset;
            PoolHub.AddSprite(segmentObject, PlaceholderArt.Circle, sortingOrder: 6);

            segmentObject.AddComponent<Health>();
            BossSegment segment = segmentObject.AddComponent<BossSegment>();
            segment.Configure(definition, this, healthMultiplier);

            _segments.Add(segment);
        }
    }

    private void Update()
    {
        if (IsEngaged)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = Mathf.MoveTowards(position.y, _targetY, Data.entrySpeed * Time.deltaTime);
        transform.position = position;

        if (Mathf.Approximately(position.y, _targetY))
        {
            IsEngaged = true;
        }
    }

    public void OnSegmentDestroyed(BossSegment segment)
    {
        _segmentsAlive--;
        AttackIntervalScale *= Data.rageIntervalScalePerKill;

        if (_segmentsAlive <= 0)
        {
            IsDefeated = true;
            IsEngaged = false;
            if (_laneStrike != null)
            {
                _laneStrike.Cancel();
            }
        }
    }

    public void ExecuteAttack(BossSegment segment)
    {
        BossSegmentDefinition definition = segment.Definition;
        Vector2 origin = segment.transform.position;

        switch (definition.attack)
        {
            case BossAttackKind.Summon:
                Summon(definition);
                break;

            case BossAttackKind.Volley:
                Volley(definition, origin);
                break;

            case BossAttackKind.Sweep:
                Sweep(definition);
                break;

            default:
                Aimed(definition, origin);
                break;
        }
    }

    private void Summon(BossSegmentDefinition definition)
    {
        if (definition.summon == null || _spawner == null)
        {
            return;
        }

        for (int i = 0; i < definition.summonCount; i++)
        {
            _spawner.SpawnReinforcement(definition.summon, Arena.Instance.RandomSpawnPoint());
        }
    }

    private void Volley(BossSegmentDefinition definition, Vector2 origin)
    {
        if (_spawner == null)
        {
            return;
        }

        int count = Mathf.Max(1, definition.volleyCount);
        const float spreadDegrees = 62f;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, t);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.down;

            _spawner.FireBossProjectile(origin, direction, definition.projectileSpeed, definition.attackDamage, definition.color);
        }
    }

    private void Sweep(BossSegmentDefinition definition)
    {
        PlayerController player = Battlefield.Player;
        if (player == null || _laneStrike == null)
        {
            return;
        }

        // Зона ставится туда, где игрок стоит сейчас: увернуться можно всегда,
        // но только если не стоять на месте.
        _laneStrike.Begin(player.transform.position.x, definition.sweepWidth, definition.attackDamage, telegraphDuration: 1.1f);
    }

    private void Aimed(BossSegmentDefinition definition, Vector2 origin)
    {
        PlayerController player = Battlefield.Player;
        if (player == null || _spawner == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)player.transform.position - origin).normalized;
        _spawner.FireBossProjectile(origin, direction, definition.projectileSpeed, definition.attackDamage, definition.color);
    }

    /// <summary>Снять босса с поля: между уровнями и при смерти игрока.</summary>
    public void Teardown()
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            if (_segments[i] != null)
            {
                _segments[i].Teardown();
            }
        }
        _segments.Clear();

        if (_laneStrike != null)
        {
            Destroy(_laneStrike.gameObject);
        }

        Destroy(gameObject);
    }
}
