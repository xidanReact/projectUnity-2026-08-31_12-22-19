using System;
using UnityEngine;

/// <summary>
/// Результат прохождения узла. Звёзды считаются здесь, а не на экране:
/// правило «за провал ноль» не должно зависеть от того, кто рисует результат.
/// </summary>
public readonly struct NodeOutcome
{
    public readonly CampaignNode Node;
    public readonly bool Cleared;
    public readonly float ElapsedSeconds;
    public readonly int Kills;

    public NodeOutcome(CampaignNode node, bool cleared, float elapsedSeconds, int kills)
    {
        Node = node;
        Cleared = cleared;
        ElapsedSeconds = elapsedSeconds;
        Kills = kills;
    }

    public int Stars => Cleared ? StarRating.Evaluate(Node, ElapsedSeconds) : 0;
}

/// <summary>
/// Бой одного узла кампании: создать игрока, запустить уровень, дождаться
/// победы или смерти, отдать исход. Ничего не знает ни про карту, ни про
/// апгрейды, ни про метапрогрессию — этим управляет AppFlow.
/// </summary>
public class GameRunner : MonoBehaviour
{
    public event Action<NodeOutcome> NodeFinished;

    public bool IsRunning { get; private set; }
    public CampaignNode CurrentNode { get; private set; }
    public PlayerController Player { get; private set; }
    public PlayerStats Stats => _run != null ? _run.Stats : null;

    /// <summary>
    /// Время внутри узла. Копится по deltaTime, а не по Time.time: при паузе
    /// timeScale уходит в ноль, и разница таймстампов начислила бы игроку
    /// секунды, которые он не играл.
    /// </summary>
    public float ElapsedSeconds { get; private set; }

    private PoolHub _pools;
    private EnemySpawner _spawner;
    private DifficultyDirector _difficulty;
    private BiomeRun _run;
    private GameObject _playerObject;

    public void Initialize(PoolHub pools, EnemySpawner spawner, DifficultyDirector difficulty)
    {
        _pools = pools;
        _spawner = spawner;
        _difficulty = difficulty;

        _spawner.Initialize(_difficulty);
        _spawner.LevelCleared += OnLevelCleared;
    }

    private void OnDestroy()
    {
        if (_spawner != null)
        {
            _spawner.LevelCleared -= OnLevelCleared;
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (IsRunning)
        {
            ElapsedSeconds += Time.deltaTime;
        }
    }

    // --- Узел ---

    public void StartNode(CampaignNode node, BiomeRun run)
    {
        if (node == null || run == null)
        {
            return;
        }

        CurrentNode = node;
        _run = run;
        ElapsedSeconds = 0f;

        Time.timeScale = 1f;
        _pools.ClearBattlefield();

        // Сложность растёт с номером узла в биоме — сквозного счётчика забега
        // больше нет, и уровень давления теперь однозначно задан узлом карты.
        // Эскалация внутри узла начинается с нуля: узел — законченный бой,
        // а не отрезок бесконечного забега.
        _difficulty.ResetRun();
        _difficulty.SetLevel(node.IndexInBiome);
        _difficulty.SetRunning(true);

        SpawnPlayer();

        Player.ResetToLane();
        SetCombatActive(true);
        Player.Ability.OnLevelStarted();

        _spawner.StartLevel(node.Level);
        IsRunning = true;
    }

    /// <summary>Выйти из узла без исхода — используется при выходе из биома.</summary>
    public void AbortNode()
    {
        if (!IsRunning && _playerObject == null)
        {
            return;
        }

        IsRunning = false;
        Time.timeScale = 1f;
        _spawner.StopLevel();
        _pools.ClearBattlefield();
        _difficulty.SetRunning(false);
        DestroyPlayer();
        CurrentNode = null;
        _run = null;
    }

    private void OnLevelCleared()
    {
        Finish(cleared: true);
    }

    private void OnPlayerDied()
    {
        Finish(cleared: false);
    }

    private void Finish(bool cleared)
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;

        int kills = _spawner.Kills;
        CampaignNode node = CurrentNode;

        _spawner.StopLevel();
        _pools.ClearBattlefield();
        _difficulty.SetRunning(false);
        SetCombatActive(false);

        NodeFinished?.Invoke(new NodeOutcome(node, cleared, ElapsedSeconds, kills));
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

    /// <summary>
    /// Игрок пересоздаётся на каждый узел, но статы берутся из BiomeRun —
    /// поэтому апгрейды, взятые на прошлых узлах, остаются в силе.
    /// </summary>
    private void SpawnPlayer()
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
        PathogenAbility ability = AddAbility(_playerObject, _run.Pathogen.type);

        reduction.Initialize(_run.Stats);
        mutations.Initialize(_run.Stats, health);
        ability.Initialize(_run.Stats);
        player.Initialize(_run.Stats, ability, mutations);
        weapon.Initialize(_run.Stats, ability, mutations);
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
