using UnityEngine;

/// <summary>
/// Телеграфированный удар по участку полосы игрока. Сначала на полосе загорается
/// зона, потом она бьёт — единственная атака босса, которая требует именно
/// движения, а не размена уроном, и ради которой патоген на босс-уровне
/// остаётся подвижным.
/// </summary>
public class LaneStrike : MonoBehaviour
{
    private enum Phase
    {
        Idle,
        Telegraph,
        Strike
    }

    private const float StrikeDuration = 0.18f;

    private SpriteRenderer _renderer;
    private Phase _phase = Phase.Idle;
    private float _timer;
    private float _telegraphDuration;
    private float _halfWidth;
    private float _damage;

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        SetVisible(false);
    }

    public void Begin(float centerX, float width, float damage, float telegraphDuration)
    {
        Arena arena = Arena.Instance;
        if (arena == null)
        {
            return;
        }

        _halfWidth = width * 0.5f;
        _damage = damage;
        _telegraphDuration = Mathf.Max(0.2f, telegraphDuration);
        _timer = _telegraphDuration;
        _phase = Phase.Telegraph;

        // Зона рисуется по всей высоте от полосы вниз — она и есть «пол» игрока.
        float height = arena.LaneY + arena.HalfHeight;
        transform.position = new Vector3(centerX, arena.LaneY - height * 0.5f, 0f);
        transform.localScale = new Vector3(width, height, 1f);

        SetVisible(true);
        ApplyColor(0f);
    }

    private void Update()
    {
        if (_phase == Phase.Idle)
        {
            return;
        }

        _timer -= Time.deltaTime;

        if (_phase == Phase.Telegraph)
        {
            ApplyColor(1f - Mathf.Clamp01(_timer / _telegraphDuration));

            if (_timer <= 0f)
            {
                _phase = Phase.Strike;
                _timer = StrikeDuration;
                Detonate();
            }
            return;
        }

        if (_timer <= 0f)
        {
            _phase = Phase.Idle;
            SetVisible(false);
        }
    }

    private void Detonate()
    {
        if (_renderer != null)
        {
            _renderer.color = new Color(1f, 0.35f, 0.3f, 0.85f);
        }

        PlayerController player = Battlefield.Player;
        if (player == null || !player.Health.IsAlive)
        {
            return;
        }

        if (Mathf.Abs(player.transform.position.x - transform.position.x) <= _halfWidth)
        {
            player.Health.TakeDamage(_damage);
        }
    }

    private void ApplyColor(float charge)
    {
        if (_renderer != null)
        {
            _renderer.color = new Color(0.95f, 0.25f, 0.25f, Mathf.Lerp(0.10f, 0.45f, charge));
        }
    }

    private void SetVisible(bool visible)
    {
        if (_renderer != null)
        {
            _renderer.enabled = visible;
        }
    }

    public void Cancel()
    {
        _phase = Phase.Idle;
        SetVisible(false);
    }
}
