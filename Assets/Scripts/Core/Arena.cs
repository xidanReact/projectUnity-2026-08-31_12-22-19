using UnityEngine;

/// <summary>
/// Геометрия игрового поля. Патоген ходит только влево-вправо по фиксированной полосе
/// внизу экрана, угроза приходит сверху (lane-defense из dev-plan.md).
/// Значения считаются от ортографической камеры, чтобы поле совпадало с любым аспектом.
/// </summary>
public class Arena : MonoBehaviour
{
    public static Arena Instance { get; private set; }

    [Tooltip("Отступ от краёв экрана, в которых игрок ещё может стоять.")]
    public float sideMargin = 0.6f;

    [Tooltip("Насколько полоса игрока поднята над нижним краем экрана.")]
    public float laneOffsetFromBottom = 2.2f;

    [Tooltip("Насколько выше верхнего края экрана появляются враги.")]
    public float spawnMarginAboveTop = 1.5f;

    public float HalfWidth { get; private set; }
    public float HalfHeight { get; private set; }

    /// Y-координата полосы игрока.
    public float LaneY { get; private set; }

    /// Y-координата, на которой рождаются враги.
    public float SpawnY { get; private set; }

    /// Крайние X, между которыми зажат игрок.
    public float MinX => -HalfWidth + sideMargin;
    public float MaxX => HalfWidth - sideMargin;

    private Camera _camera;

    private void Awake()
    {
        Instance = this;
        _camera = Camera.main;
        Recalculate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        // Дёшево и надёжно: поворот экрана/смена аспекта на мобиле не должна ломать поле.
        Recalculate();
    }

    public void Recalculate()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
        if (_camera == null)
        {
            return;
        }

        HalfHeight = _camera.orthographicSize;
        HalfWidth = HalfHeight * _camera.aspect;
        LaneY = -HalfHeight + laneOffsetFromBottom;
        SpawnY = HalfHeight + spawnMarginAboveTop;
    }

    public float ClampX(float x)
    {
        return Mathf.Clamp(x, MinX, MaxX);
    }

    /// Случайная точка спавна по верхней кромке.
    public Vector2 RandomSpawnPoint()
    {
        return new Vector2(Random.Range(MinX, MaxX), SpawnY);
    }

    /// Объект уехал настолько далеко за экран, что его пора вернуть в пул.
    public bool IsFarOutside(Vector2 position, float slack = 3f)
    {
        return position.y > SpawnY + slack
            || position.y < -HalfHeight - slack
            || Mathf.Abs(position.x) > HalfWidth + slack;
    }
}
