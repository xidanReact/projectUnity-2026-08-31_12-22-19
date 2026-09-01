using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Выгружает балансовые дефолты Фазы 1 в настоящие .asset-файлы.
/// Прототип работает и без них (значения зашиты в код), но как только
/// баланс начнут крутить руками — правки должны жить в ассетах, а не в C#.
/// </summary>
public static class PrototypeAssetCreator
{
    private const string RootFolder = "Assets/GameData";
    private const string PathogenFolder = RootFolder + "/Pathogens";
    private const string EnemyFolder = RootFolder + "/Enemies";
    private const string BossFolder = RootFolder + "/Bosses";

    [MenuItem("Pathogen/Создать балансовые ассеты (Фаза 1)")]
    public static void CreateAll()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(PathogenFolder);
        EnsureFolder(EnemyFolder);
        EnsureFolder(BossFolder);

        foreach (PathogenType type in System.Enum.GetValues(typeof(PathogenType)))
        {
            PathogenData data = PathogenData.CreateDefault(type);
            SaveAsset(data, $"{PathogenFolder}/{type}.asset");
        }

        SaveAsset(Clone(EnemyCatalog.Neutrophil), $"{EnemyFolder}/Neutrophil.asset");
        SaveAsset(Clone(EnemyCatalog.Antibody), $"{EnemyFolder}/Antibody.asset");

        // Осколок сохраняется первым: макрофаг должен ссылаться на ассет, а не на рантайм-копию.
        string fragmentPath = $"{EnemyFolder}/MacrophageFragment.asset";
        SaveAsset(Clone(EnemyCatalog.MacrophageFragment), fragmentPath);

        EnemyData macrophage = Clone(EnemyCatalog.Macrophage);
        macrophage.splitInto = AssetDatabase.LoadAssetAtPath<EnemyData>(fragmentPath);
        SaveAsset(macrophage, $"{EnemyFolder}/Macrophage.asset");

        SaveBoss();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Pathogen] Балансовые ассеты записаны в {RootFolder}.");
    }

    /// <summary>
    /// Босс ссылается на EnemyData подкреплений. Рантайм-каталог для этого не годится:
    /// ссылки надо перевесить на уже сохранённые ассеты, иначе в .asset попадёт пустота.
    /// </summary>
    private static void SaveBoss()
    {
        BossData boss = Object.Instantiate(BossCatalog.LymphNode);

        for (int i = 0; i < boss.segments.Count; i++)
        {
            BossSegmentDefinition segment = boss.segments[i];
            if (segment.summon == null)
            {
                continue;
            }

            string path = $"{EnemyFolder}/{FileNameFor(segment.summon)}.asset";
            EnemyData saved = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (saved != null)
            {
                segment.summon = saved;
            }
        }

        SaveAsset(boss, $"{BossFolder}/LymphNode.asset");
    }

    private static string FileNameFor(EnemyData enemy)
    {
        if (enemy == EnemyCatalog.Neutrophil) return "Neutrophil";
        if (enemy == EnemyCatalog.Antibody) return "Antibody";
        if (enemy == EnemyCatalog.Macrophage) return "Macrophage";
        return "MacrophageFragment";
    }

    private static EnemyData Clone(EnemyData source) => Object.Instantiate(source);

    private static void SaveAsset(ScriptableObject asset, string path)
    {
        // Перезапись существующего ассета сбросила бы ручные правки баланса.
        if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
        {
            Debug.Log($"[Pathogen] {path} уже существует — пропущен.");
            Object.DestroyImmediate(asset);
            return;
        }

        AssetDatabase.CreateAsset(asset, path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
