using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Патоген. Движение только влево-вправо по фиксированной полосе (lane-defense).
/// Управление: тач/мышь — палец тянет патоген по X; A/D и стрелки — для отладки в редакторе.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    public Health Health { get; private set; }
    public PlayerStats Stats { get; private set; }
    public PathogenAbility Ability { get; private set; }
    public PlayerMutations Mutations { get; private set; }

    private SpriteRenderer _renderer;
    private Camera _camera;
    private float _targetX;
    private bool _inputEnabled = true;

    private void Awake()
    {
        Health = GetComponent<Health>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _camera = Camera.main;
    }

    public void Initialize(PlayerStats stats, PathogenAbility ability, PlayerMutations mutations)
    {
        Stats = stats;
        Ability = ability;
        Mutations = mutations;

        Health.Configure(stats.MaxHealth);
        Health.RefreshInterceptors();
        Health.Damaged += HandleDamaged;

        if (_renderer != null)
        {
            _renderer.color = stats.Source.bodyColor;
        }

        ResetToLane();
    }

    public void ResetToLane()
    {
        var arena = Arena.Instance;
        float y = arena != null ? arena.LaneY : transform.position.y;
        transform.position = new Vector3(0f, y, 0f);
        _targetX = 0f;
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    private void Update()
    {
        var arena = Arena.Instance;
        if (arena == null)
        {
            return;
        }

        if (_inputEnabled)
        {
            ReadInput(arena);
        }

        Vector3 position = transform.position;
        float clampedTarget = arena.ClampX(_targetX);
        float speed = Stats.MoveSpeed * (Mutations != null ? Mutations.SpeedMultiplier : 1f);
        position.x = Mathf.MoveTowards(position.x, clampedTarget, speed * Time.deltaTime);
        position.y = arena.LaneY;
        transform.position = position;
    }

    private void HandleDamaged(float amount, float remaining)
    {
        if (Mutations != null)
        {
            Mutations.OnDamaged();
        }
    }

    private void ReadInput(Arena arena)
    {
        // Клавиатура: удобно гонять прототип в редакторе, на устройстве просто отсутствует.
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float axis = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) axis -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) axis += 1f;

            if (!Mathf.Approximately(axis, 0f))
            {
                // Клавиатура ведёт цель сама, перебивая палец.
                _targetX = arena.ClampX(transform.position.x + axis * Stats.MoveSpeed * Time.deltaTime * 2f);
                return;
            }
        }

        if (!TryGetPointer(out Vector2 screenPoint))
        {
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera != null)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
            _targetX = arena.ClampX(world.x);
        }
    }

    private static bool TryGetPointer(out Vector2 screenPoint)
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
        {
            screenPoint = touch.primaryTouch.position.ReadValue();
            return true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            screenPoint = mouse.position.ReadValue();
            return true;
        }

        screenPoint = default;
        return false;
    }
}
