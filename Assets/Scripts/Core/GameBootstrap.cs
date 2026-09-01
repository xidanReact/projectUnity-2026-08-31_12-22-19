using UnityEngine;

/// <summary>
/// Собирает всю сцену прототипа кодом. Так Фаза 1 не требует ни одного
/// префаба и ни одной ручной привязки в инспекторе: открыл проект — нажал Play.
/// В Фазе 2, когда появятся настоящие ассеты и несколько сцен, это заменяется
/// нормальной сценой с сериализованными ссылками.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameBootstrap : MonoBehaviour
{
    private const float CameraSize = 10f;

    /// Ставится в false, если бутстрап уже положен в сцену руками.
    public static bool AutoBootEnabled = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        if (!AutoBootEnabled || FindAnyObjectByType<GameBootstrap>() != null)
        {
            return;
        }

        new GameObject("[Bootstrap]").AddComponent<GameBootstrap>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        SetUpCamera();

        var arena = gameObject.AddComponent<Arena>();
        arena.Recalculate();

        BuildBackdrop(arena);

        var poolsObject = new GameObject("Pools");
        poolsObject.transform.SetParent(transform, false);
        PoolHub pools = poolsObject.AddComponent<PoolHub>();

        var gameObjectRoot = new GameObject("Game");
        gameObjectRoot.transform.SetParent(transform, false);

        var difficulty = gameObjectRoot.AddComponent<DifficultyDirector>();
        var upgrades = gameObjectRoot.AddComponent<UpgradeSystem>();
        var spawner = gameObjectRoot.AddComponent<EnemySpawner>();
        var meta = gameObjectRoot.AddComponent<MetaProgression>();
        var runner = gameObjectRoot.AddComponent<GameRunner>();

        // Единственное место, где выбирается хранилище прогресса.
        // В Фазе 4 здесь появится клиент Go-бэкенда вместо JSON-файла.
        var store = new JsonProgressStore();
        Debug.Log($"[Meta] Файл прогресса: {store.FilePath}");
        meta.Initialize(store);

        runner.Initialize(pools, spawner, difficulty);

        var app = gameObjectRoot.AddComponent<AppFlow>();
        app.Initialize(runner, spawner, upgrades, meta);
    }

    private static void SetUpCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = CameraSize;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.05f, 0.08f);
    }

    /// <summary>
    /// Минимальные ориентиры поля: полоса игрока и линия, за которой сегмент
    /// считается прорвавшимся. Без них lane-defense читается как каша.
    /// </summary>
    private void BuildBackdrop(Arena arena)
    {
        var backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(transform, false);

        var laneObject = new GameObject("LaneLine");
        laneObject.transform.SetParent(backdrop.transform, false);
        SpriteRenderer lane = PoolHub.AddSprite(laneObject, PlaceholderArt.Square, sortingOrder: -10);
        lane.color = new Color(0.85f, 0.35f, 0.40f, 0.35f);
        laneObject.transform.position = new Vector3(0f, arena.LaneY, 0f);
        laneObject.transform.localScale = new Vector3(arena.HalfWidth * 2f, 0.12f, 1f);

        var floorObject = new GameObject("LaneFloor");
        floorObject.transform.SetParent(backdrop.transform, false);
        SpriteRenderer floor = PoolHub.AddSprite(floorObject, PlaceholderArt.Square, sortingOrder: -11);
        floor.color = new Color(0.30f, 0.08f, 0.12f, 0.55f);
        float floorHeight = arena.LaneY + arena.HalfHeight;
        floorObject.transform.position = new Vector3(0f, arena.LaneY - floorHeight * 0.5f, 0f);
        floorObject.transform.localScale = new Vector3(arena.HalfWidth * 2f, floorHeight, 1f);
    }
}
