using UnityEngine;

/// <summary>
/// Таймер эскалации. Сложность растёт по двум осям сразу: номер уровня
/// (постоянный, «честный» рост) и время внутри забега (давит на затягивание —
/// без него выгодно кайтить толпу бесконечно).
/// </summary>
public class DifficultyDirector : MonoBehaviour
{
    [Header("Рост от номера уровня")]
    public float healthPerLevel = 0.15f;
    public float speedPerLevel = 0.05f;

    [Header("Рост от времени в забеге (за минуту)")]
    public float healthPerMinute = 0.25f;
    public float speedPerMinute = 0.06f;
    public float spawnRatePerMinute = 0.20f;

    [Header("Потолки")]
    public float maxSpeedMultiplier = 1.8f;
    public float minSpawnIntervalMultiplier = 0.45f;

    public float RunTime { get; private set; }
    public int LevelIndex { get; private set; }

    private bool _running;

    public float ElapsedMinutes => RunTime / 60f;

    /// Множитель здоровья врагов.
    public float HealthMultiplier => (1f + healthPerLevel * LevelIndex) * (1f + healthPerMinute * ElapsedMinutes);

    /// Множитель скорости врагов — с потолком, иначе поздние уровни становятся нечитаемыми.
    public float SpeedMultiplier => Mathf.Min(
        maxSpeedMultiplier,
        1f + speedPerLevel * LevelIndex + speedPerMinute * ElapsedMinutes);

    /// Множитель паузы между спавнами: чем дольше идёт забег, тем плотнее поток.
    public float SpawnIntervalMultiplier => Mathf.Max(
        minSpawnIntervalMultiplier,
        1f / (1f + spawnRatePerMinute * ElapsedMinutes));

    public void ResetRun()
    {
        RunTime = 0f;
        LevelIndex = 0;
        _running = false;
    }

    public void SetLevel(int levelIndex)
    {
        LevelIndex = levelIndex;
    }

    public void SetRunning(bool running)
    {
        _running = running;
    }

    private void Update()
    {
        if (_running)
        {
            RunTime += Time.deltaTime;
        }
    }
}
