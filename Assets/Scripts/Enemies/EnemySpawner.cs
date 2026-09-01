using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Единственный источник врагов на поле. Отвечает за оба типа наступления:
/// волны (поток врагов, которые идут на игрока) и сегменты (строй, спускающийся
/// к полосе игрока — если дошёл, игрок получает крупный урон).
/// Состав уровня фиксирован, порядок и тайминги рандомизируются здесь.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    /// Расстояние между рядами строя в сегментном режиме.
    private const float SegmentRowSpacing = 1.1f;

    public event Action LevelCleared;
    public event Action<Enemy> EnemyKilled;

    public int WaveIndex { get; private set; }
    public int WaveCount { get; private set; }
    public int Kills { get; private set; }

    /// Босс текущего уровня, если уровень босс-типа. Иначе null.
    public Boss ActiveBoss { get; private set; }

    private LevelData _level;
    private DifficultyDirector _difficulty;
    private Coroutine _routine;
    private readonly List<EnemyData> _spawnOrder = new List<EnemyData>(64);

    public void Initialize(DifficultyDirector difficulty)
    {
        _difficulty = difficulty;
    }

    public void StartLevel(LevelData level)
    {
        StopLevel();

        _level = level;
        WaveIndex = 0;
        WaveCount = level.waves.Count;
        Kills = 0;
        _routine = StartCoroutine(RunLevel());
    }

    public void StopLevel()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        TeardownBoss();
    }

    private void TeardownBoss()
    {
        if (ActiveBoss != null)
        {
            ActiveBoss.Teardown();
            ActiveBoss = null;
        }
    }

    private IEnumerator RunLevel()
    {
        if (_level.advanceType == AdvanceType.Boss)
        {
            yield return RunBossLevel();
            _routine = null;
            LevelCleared?.Invoke();
            yield break;
        }

        for (int w = 0; w < _level.waves.Count; w++)
        {
            WaveIndex = w;
            WaveDefinition wave = _level.waves[w];

            if (_level.advanceType == AdvanceType.Waves)
            {
                yield return SpawnAsStream(wave);
            }
            else
            {
                SpawnAsFormation(wave);
            }

            // Волна считается пройденной, когда на поле не осталось живой угрозы.
            while (Battlefield.ThreatCount > 0)
            {
                yield return null;
            }

            if (w < _level.waves.Count - 1)
            {
                yield return new WaitForSeconds(wave.postWaveDelay);
            }
        }

        _routine = null;
        LevelCleared?.Invoke();
    }

    // --- Босс-уровень: волн нет, есть одна составная цель, которая сама себя ведёт ---

    private IEnumerator RunBossLevel()
    {
        var bossObject = new GameObject($"Boss_{_level.bossData.bossName}");
        bossObject.transform.SetParent(transform, false);

        Boss boss = bossObject.AddComponent<Boss>();
        boss.Configure(_level.bossData, this, _difficulty != null ? _difficulty.HealthMultiplier : 1f);
        ActiveBoss = boss;

        while (!boss.IsDefeated)
        {
            yield return null;
        }

        TeardownBoss();
    }

    /// <summary>Подкрепление, вызванное сегментом босса. Считается обычным врагом.</summary>
    public void SpawnReinforcement(EnemyData data, Vector2 position)
    {
        SpawnEnemy(data, position, descendSpeed: 0f);
    }

    public void FireBossProjectile(Vector2 origin, Vector2 direction, float speed, float damage, Color color)
    {
        Projectile projectile = PoolHub.Instance.Projectiles.Get();
        projectile.Launch(origin, direction, speed, damage, Faction.Immune, color,
            radius: 0.22f, pierce: 0, lifetime: 6f);
    }

    // --- Режим волн: враги сыплются потоком со случайными интервалами ---

    private IEnumerator SpawnAsStream(WaveDefinition wave)
    {
        BuildShuffledOrder(wave);
        float intervalScale = _difficulty != null ? _difficulty.SpawnIntervalMultiplier : 1f;

        for (int i = 0; i < _spawnOrder.Count; i++)
        {
            SpawnEnemy(_spawnOrder[i], Arena.Instance.RandomSpawnPoint(), descendSpeed: 0f);

            float interval = UnityEngine.Random.Range(wave.spawnIntervalRange.x, wave.spawnIntervalRange.y);
            yield return new WaitForSeconds(interval * intervalScale);
        }
    }

    // --- Режим сегментов: весь строй появляется разом и ползёт вниз ---

    private void SpawnAsFormation(WaveDefinition wave)
    {
        BuildShuffledOrder(wave);

        Arena arena = Arena.Instance;
        int perRow = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(_spawnOrder.Count) * 1.5f));
        float usableWidth = arena.MaxX - arena.MinX;

        for (int i = 0; i < _spawnOrder.Count; i++)
        {
            int row = i / perRow;
            int column = i % perRow;

            // Последний ряд может быть неполным — центрируем его, чтобы строй не «съезжал» влево.
            int itemsInRow = Mathf.Min(perRow, _spawnOrder.Count - row * perRow);
            float step = usableWidth / (itemsInRow + 1);
            float x = arena.MinX + step * (column + 1);
            float y = arena.SpawnY + row * SegmentRowSpacing;

            SpawnEnemy(_spawnOrder[i], new Vector2(x, y), _level.segmentDescendSpeed);
        }
    }

    private void BuildShuffledOrder(WaveDefinition wave)
    {
        _spawnOrder.Clear();

        for (int e = 0; e < wave.entries.Count; e++)
        {
            SpawnEntry entry = wave.entries[e];
            if (entry == null || entry.enemy == null)
            {
                continue;
            }

            for (int c = 0; c < entry.count; c++)
            {
                _spawnOrder.Add(entry.enemy);
            }
        }

        // Fisher-Yates: состав волны фиксирован планом, порядок — нет.
        for (int i = _spawnOrder.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_spawnOrder[i], _spawnOrder[j]) = (_spawnOrder[j], _spawnOrder[i]);
        }
    }

    private Enemy SpawnEnemy(EnemyData data, Vector2 position, float descendSpeed)
    {
        Enemy enemy = PoolHub.Instance.Enemies.Get();
        enemy.Configure(
            data,
            this,
            position,
            _difficulty != null ? _difficulty.HealthMultiplier : 1f,
            _difficulty != null ? _difficulty.SpeedMultiplier : 1f,
            descendSpeed,
            _level != null ? _level.segmentBreachDamage : 0f);
        return enemy;
    }

    // --- Обратные вызовы от врагов ---

    public void HandleEnemyDeath(Enemy enemy)
    {
        bool wasHostile = enemy.Faction == Faction.Immune;

        if (wasHostile)
        {
            Kills++;
            EnemyKilled?.Invoke(enemy);

            if (enemy.LastDamageSource == Faction.Pathogen
                && Battlefield.Player != null
                && Battlefield.Player.Mutations != null)
            {
                Battlefield.Player.Mutations.OnKill();
            }
        }

        PathogenAbility ability = Battlefield.Player != null ? Battlefield.Player.Ability : null;
        if (wasHostile && ability != null && ability.TryConsumeKill(enemy))
        {
            // Вирус поднял врага заражённым — в пул он не уходит.
            return;
        }

        if (wasHostile)
        {
            SpawnSplit(enemy);
        }

        PoolHub.Instance.Enemies.Release(enemy);
    }

    public void HandleSegmentBreach(Enemy enemy)
    {
        PoolHub.Instance.Enemies.Release(enemy);
    }

    public void HandleInfectionExpired(Enemy enemy)
    {
        PoolHub.Instance.Enemies.Release(enemy);
    }

    public void FireEnemyProjectile(Vector2 origin, Vector2 direction, EnemyData data)
    {
        Projectile projectile = PoolHub.Instance.Projectiles.Get();
        projectile.Launch(
            origin,
            direction,
            data.projectileSpeed,
            data.projectileDamage,
            Faction.Immune,
            data.bodyColor,
            radius: 0.14f,
            pierce: 0,
            lifetime: 5f);
    }

    private void SpawnSplit(Enemy enemy)
    {
        EnemyData data = enemy.Data;
        if (data.splitCount <= 0 || data.splitInto == null)
        {
            return;
        }

        Vector2 center = enemy.transform.position;
        for (int i = 0; i < data.splitCount; i++)
        {
            float angle = 360f / data.splitCount * i;
            Vector2 offset = (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector2.right) * (data.radius + 0.15f);

            // Осколки наследуют режим родителя: осколки сегмента продолжают спуск строем.
            SpawnEnemy(data.splitInto, center + offset, enemy.IsSegmentMember ? _level.segmentDescendSpeed : 0f);
        }
    }
}
