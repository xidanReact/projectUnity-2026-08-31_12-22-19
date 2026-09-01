using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    /// Экран выбора патогена — вход в забег.
    PathogenSelect,

    /// Идёт бой.
    Playing,

    /// Уровень зачищен, игрок выбирает 1 из 3 апгрейдов (игра на паузе).
    UpgradeChoice,

    /// Патоген уничтожен.
    GameOver
}

/// <summary>
/// Машина состояний забега: выбор патогена → уровень → выбор апгрейда → следующий уровень.
/// Держит вместе всё остальное (арена, пулы, спавнер, сложность, апгрейды),
/// чтобы у систем не было ссылок друг на друга напрямую.
/// </summary>
public class GameRunner : MonoBehaviour
{
    [Header("Кампания")]
    [Tooltip("Сколько уровней Биома 1 генерировать. После последнего список идёт по кругу, но сложность продолжает расти.")]
    public int levelsInBiome = 8;

    public GameState State { get; private set; } = GameState.PathogenSelect;
    public PlayerController Player { get; private set; }
    public PlayerStats Stats { get; private set; }
    public LevelData CurrentLevel { get; private set; }

    /// Сквозной номер уровня в забеге (не сбрасывается при зацикливании списка).
    public int LevelNumber { get; private set; }

    public int TotalKills { get; private set; }

    /// Индекс босс-уровня в биоме — отладочный вход «сразу к боссу» из HUD.
    public int FirstBossLevelIndex { get; private set; }
    public IReadOnlyList<UpgradeDefinition> PendingUpgrades => _pendingUpgrades;

    private PoolHub _pools;
    private EnemySpawner _spawner;
    private DifficultyDirector _difficulty;
    private UpgradeSystem _upgrades;
    private MetaProgression _meta;

    /// Сколько боссов повержено за текущий забег — идёт в награду биомассой.
    private int _bossesDefeatedThisRun;

    private List<LevelData> _levels;
    private List<UpgradeDefinition> _pendingUpgrades = new List<UpgradeDefinition>();
    private GameObject _playerObject;

    public void Initialize(
        PoolHub pools,
        EnemySpawner spawner,
        DifficultyDirector difficulty,
        UpgradeSystem upgrades,
        MetaProgression meta)
    {
        _pools = pools;
        _spawner = spawner;
        _difficulty = difficulty;
        _upgrades = upgrades;
        _meta = meta;

        _levels = CampaignGenerator.BuildBloodstream(levelsInBiome);
        FirstBossLevelIndex = CampaignGenerator.FindFirstBossLevel(_levels);
        _spawner.Initialize(_difficulty);
        _spawner.LevelCleared += OnLevelCleared;

        State = GameState.PathogenSelect;
    }

    private void OnDestroy()
    {
        if (_spawner != null)
        {
            _spawner.LevelCleared -= OnLevelCleared;
        }
        Time.timeScale = 1f;
    }

    // --- Забег ---

    public void StartRun(PathogenType type)
    {
        StartRun(PathogenData.CreateDefault(type));
    }

    /// <param name="startLevel">С какого уровня биома начать. Не ноль — только для отладки.</param>
    public void StartRun(PathogenData data, int startLevel = 0)
    {
        Stats = new PlayerStats(data);

        // Перманентные улучшения ложатся на стартовые статы до создания игрока:
        // здоровье конфигурируется из Stats.MaxHealth и позже уже не пересчитывается.
        if (_meta != null)
        {
            _meta.ApplyTo(Stats);
        }

        _bossesDefeatedThisRun = 0;
        _difficulty.ResetRun();
        _upgrades.ResetRun();
        TotalKills = 0;
        LevelNumber = Mathf.Max(0, startLevel);

        SpawnPlayer(data);
        StartLevel();
    }

    public void RestartToSelect()
    {
        Time.timeScale = 1f;
        _spawner.StopLevel();
        _pools.ClearBattlefield();
        DestroyPlayer();
        _difficulty.SetRunning(false);
        State = GameState.PathogenSelect;
    }

    private void StartLevel()
    {
        Time.timeScale = 1f;

        // Список уровней биома короткий — после последнего идём по кругу,
        // а рост сложности обеспечивает сквозной LevelNumber.
        CurrentLevel = _levels[LevelNumber % _levels.Count];

        _pools.ClearBattlefield();
        _difficulty.SetLevel(LevelNumber);
        _difficulty.SetRunning(true);

        Player.ResetToLane();
        Player.SetInputEnabled(true);
        Player.GetComponent<PlayerWeapon>().SetEnabled(true);
        Player.Ability.OnLevelStarted();

        _spawner.StartLevel(CurrentLevel);
        State = GameState.Playing;
    }

    private void OnLevelCleared()
    {
        if (State != GameState.Playing)
        {
            return;
        }

        TotalKills += _spawner.Kills;

        if (CurrentLevel != null && CurrentLevel.advanceType == AdvanceType.Boss)
        {
            _bossesDefeatedThisRun++;
        }

        _pools.ClearBattlefield();
        SetCombatActive(false);

        _pendingUpgrades = _upgrades.Roll(Stats, LevelNumber);
        if (_pendingUpgrades.Count == 0)
        {
            // Все апгрейды выбраны до потолка — просто идём дальше.
            LevelNumber++;
            StartLevel();
            return;
        }

        State = GameState.UpgradeChoice;
        Time.timeScale = 0f;
    }

    public void ChooseUpgrade(UpgradeDefinition upgrade)
    {
        if (State != GameState.UpgradeChoice)
        {
            return;
        }

        _upgrades.Take(upgrade, Stats, Player);
        _pendingUpgrades.Clear();

        LevelNumber++;
        StartLevel();
    }

    private void OnPlayerDied()
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        TotalKills += _spawner.Kills;
        _spawner.StopLevel();
        _pools.ClearBattlefield();
        SetCombatActive(false);
        _difficulty.SetRunning(false);

        // Награда начисляется и сохраняется здесь, до показа экрана результатов:
        // по dev-plan.md прогресс должен быть на диске раньше, чем игроку
        // предложат что-либо (в Фазе 4 — просмотр рекламы за удвоение).
        if (_meta != null)
        {
            _meta.AwardRun(TotalKills, LevelNumber, _bossesDefeatedThisRun);
        }

        State = GameState.GameOver;
    }

    private void SetCombatActive(bool active)
    {
        if (Player == null)
        {
            return;
        }

        Player.SetInputEnabled(active);
        var weapon = Player.GetComponent<PlayerWeapon>();
        if (weapon != null)
        {
            weapon.SetEnabled(active);
        }
    }

    // --- Игрок ---

    private void SpawnPlayer(PathogenData data)
    {
        DestroyPlayer();

        _playerObject = new GameObject("Pathogen");
        _playerObject.transform.SetParent(transform, false);
        PoolHub.AddSprite(_playerObject, PlaceholderArt.Circle, sortingOrder: 10);
        _playerObject.transform.localScale = Vector3.one * 0.84f;

        var health = _playerObject.AddComponent<Health>();
        var player = _playerObject.AddComponent<PlayerController>();
        var weapon = _playerObject.AddComponent<PlayerWeapon>();
        var mutations = _playerObject.AddComponent<PlayerMutations>();
        var reduction = _playerObject.AddComponent<DamageReduction>();
        PathogenAbility ability = AddAbility(_playerObject, data.type);

        reduction.Initialize(Stats);
        mutations.Initialize(Stats, health);
        ability.Initialize(Stats);
        player.Initialize(Stats, ability, mutations);
        weapon.Initialize(Stats, ability, mutations);
        health.Died += OnPlayerDied;

        Player = player;
        Battlefield.Player = player;
    }

    private void DestroyPlayer()
    {
        if (_playerObject == null)
        {
            return;
        }

        Destroy(_playerObject);
        _playerObject = null;
        Player = null;
        Battlefield.Player = null;
    }

    private static PathogenAbility AddAbility(GameObject target, PathogenType type)
    {
        switch (type)
        {
            case PathogenType.Bacteria: return target.AddComponent<BacteriaAbility>();
            case PathogenType.Fungus: return target.AddComponent<FungusAbility>();
            case PathogenType.Parasite: return target.AddComponent<ParasiteAbility>();
            default: return target.AddComponent<VirusAbility>();
        }
    }
}
