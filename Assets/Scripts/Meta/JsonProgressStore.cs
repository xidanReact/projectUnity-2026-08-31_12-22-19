using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Прогресс в JSON-файле рядом с игрой. Запись идёт через временный файл
/// и замену: если процесс убьют посреди сохранения (на мобиле это норма),
/// старый файл останется целым, а не превратится в обрезок.
/// </summary>
public class JsonProgressStore : IProgressStore
{
    private const string FileName = "progress.json";
    private const string TempFileName = "progress.json.tmp";

    private readonly string _path;
    private readonly string _tempPath;

    /// <param name="directory">
    /// Куда писать. По умолчанию — persistentDataPath; параметр нужен тестам,
    /// чтобы они не трогали настоящий файл прогресса игрока.
    /// </param>
    public JsonProgressStore(string directory = null)
    {
        string root = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;

        if (!Directory.Exists(root))
        {
            Directory.CreateDirectory(root);
        }

        _path = Path.Combine(root, FileName);
        _tempPath = Path.Combine(root, TempFileName);
    }

    /// Где лежит файл — нужно на плейтестах, чтобы его можно было найти и удалить.
    public string FilePath => _path;

    public PlayerProgress Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return ProgressMigration.Migrate(new PlayerProgress());
            }

            string json = File.ReadAllText(_path);
            PlayerProgress progress = JsonUtility.FromJson<PlayerProgress>(json);

            // FromJson отдаёт null на пустой или мусорной строке — молча сыпаться нельзя.
            if (progress == null)
            {
                Debug.LogWarning($"[Meta] Файл прогресса пуст или повреждён: {_path}. Начинаем с чистого.");
                return ProgressMigration.Migrate(new PlayerProgress());
            }

            return ProgressMigration.Migrate(progress);
        }
        catch (Exception e)
        {
            // Потерять прогресс молча хуже, чем начать заново с записью в лог.
            Debug.LogError($"[Meta] Не удалось прочитать прогресс ({_path}): {e.Message}. Начинаем с чистого.");
            return ProgressMigration.Migrate(new PlayerProgress());
        }
    }

    public void Save(PlayerProgress progress)
    {
        if (progress == null)
        {
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(progress, prettyPrint: true);
            File.WriteAllText(_tempPath, json);

            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(_tempPath, _path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Meta] Не удалось сохранить прогресс ({_path}): {e.Message}");
        }
    }
}
