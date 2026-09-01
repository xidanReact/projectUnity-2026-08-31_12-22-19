# Оболочка приложения и кампания на карте — план реализации

> **Для агентов-исполнителей:** ОБЯЗАТЕЛЬНЫЙ САБ-СКИЛЛ: используйте
> superpowers:subagent-driven-development (рекомендуется) или
> superpowers:executing-plans для выполнения задача-за-задачей. Шаги размечены
> чекбоксами (`- [ ]`).

**Цель:** заменить экран выбора патогена и бесконечный цикл уровней полноценным
мета-слоем: сплэш, главный экран с персонажем, настройки, четыре раздела и
кампания в виде карты с поузловым прохождением, звёздами и наградами.

**Архитектура:** три слоя вместо слитых сегодня `GameState` и `GameHud`.
`AppFlow` держит состояние приложения и навигацию, `GameRunner` ужимается до боя
одного узла, `BiomeRun` хранит билд, живущий всю попытку биома. UI распадается на
экраны с общим базовым классом; тикается только активный.

**Стек:** Unity 6000.5.10f1, C#, uGUI на legacy `UnityEngine.UI.Text`, новый Input
System, NUnit EditMode-тесты, сборки `Pathogen.Runtime` / `Pathogen.Editor` /
`Pathogen.Tests`.

**Спек:** `docs/superpowers/specs/2026-09-01-shell-and-campaign-design.md`

## Глобальные ограничения

- Текст только `UnityEngine.UI.Text`, не TextMeshPro: TMP требует ручного импорта
  «TMP Essentials», без которого весь текст рендерится пустым.
- Модуль ввода только `InputSystemUIInputModule`: в проекте включён единственный
  новый Input System (`activeInputHandler: 1`), со `StandaloneInputModule` кнопки
  молча перестают нажиматься.
- Опорное разрешение макета `720×1280`, `matchWidthOrHeight = 1` (тянемся по
  высоте, игра портретная). Все координаты считаются в этом разрешении.
- `id` узлов кампании уходят в сейв игрока. Менять их после первого релиза нельзя.
- `BossData.battleOffsetFromLane` (сейчас 5.6) обязан быть меньше самой короткой
  дальности атаки среди патогенов (сейчас 6.5 у бактерии), иначе до сегментов не
  дотянуться и уровень зависает. Тест на это уже существует, не ломать.
- Множитель жадности из dev-plan.md — единственная точка калибровки экономики:
  `MetaProgression.GreedMultiplier = 0.65f`.
- Комментарии и текст интерфейса на русском, как во всём существующем коде.
- Ассеты биомов 2 и 3 не создаются: врагов для них не существует, рескином биома 1
  они не подделываются.

## Как запускать тесты

**Из редактора:** Window → General → Test Runner → вкладка EditMode → Run All.

**Из командной строки** (Unity должен быть закрыт — иначе проект залочен):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Unity.exe" -batchmode -quit `
  -runTests -projectPath "E:\projectUnity\My project" -testPlatform EditMode `
  -testResults "E:\projectUnity\My project\Logs\test-results.xml" -logFile -
```

Результат — в `Logs/test-results.xml`, атрибуты `total` / `passed` / `failed`
в корневом элементе `<test-run>`.

## О коммитах

Шаги «закоммитить» в этом плане намеренно отсутствуют: по глобальному соглашению
проекта коммиты делает человек, а не агент. Исполнитель оставляет изменения в
рабочем дереве и сообщает, что готово.

## Карта файлов

**Создаются:**

| Файл | Ответственность |
|---|---|
| `Assets/Scripts/Meta/GameSettings.cs` | громкости и имя игрока в сейве |
| `Assets/Scripts/Meta/NodeProgress.cs` | звёзды одного узла |
| `Assets/Scripts/Meta/CampaignProgress.cs` | узлы и открытые биомы |
| `Assets/Scripts/Meta/ProgressMigration.cs` | v1 → v2 |
| `Assets/Scripts/Meta/AudioService.cs` | применяет громкости |
| `Assets/Scripts/Data/Campaign/CampaignNode.cs` | узел карты |
| `Assets/Scripts/Data/Campaign/BiomeData.cs` | биом |
| `Assets/Scripts/Data/Campaign/CampaignMapData.cs` | вся кампания |
| `Assets/Scripts/Data/Campaign/CampaignBuilder.cs` | сборка кампании |
| `Assets/Scripts/Campaign/StarRating.cs` | парТайм и звёзды |
| `Assets/Scripts/Campaign/CampaignRewards.cs` | расчёт выплаты |
| `Assets/Scripts/Campaign/CampaignRules.cs` | открытие узлов и биомов |
| `Assets/Scripts/App/BiomeRun.cs` | билд попытки биома |
| `Assets/Scripts/App/AppState.cs` | состояния приложения |
| `Assets/Scripts/App/AppFlow.cs` | навигация и оркестрация |
| `Assets/Scripts/UI/Core/UiScreen.cs` | базовый экран |
| `Assets/Scripts/UI/Core/ScreenStack.cs` | стек экранов и модалок |
| `Assets/Scripts/UI/Shell/ShellChrome.cs` | шапка + таб-бар |
| `Assets/Scripts/UI/Shell/SplashScreen.cs` | заставка |
| `Assets/Scripts/UI/Shell/HomeScreen.cs` | карусель патогенов |
| `Assets/Scripts/UI/Shell/SettingsModal.cs` | настройки |
| `Assets/Scripts/UI/Shell/ConfirmModal.cs` | подтверждение сброса билда |
| `Assets/Scripts/UI/Shell/UpgradesScreen.cs` | магазин перков |
| `Assets/Scripts/UI/Shell/StubScreen.cs` | «Одежда» и «Битва» |
| `Assets/Scripts/UI/Campaign/CampaignMapScreen.cs` | карта |
| `Assets/Scripts/UI/Campaign/MapNodeView.cs` | вьюха узла |
| `Assets/Scripts/UI/Campaign/LevelBriefingModal.cs` | брифинг узла |
| `Assets/Scripts/UI/Campaign/LevelResultScreen.cs` | звёзды, награда, апгрейд |
| `Assets/Scripts/UI/Combat/CombatHud.cs` | боевой HUD |

**Изменяются:** `Meta/PlayerProgress.cs`, `Meta/JsonProgressStore.cs`,
`Meta/MetaProgression.cs`, `Data/BossData.cs`, `Core/GameRunner.cs`,
`Core/GameBootstrap.cs`, `UI/UiFactory.cs`.

**Удаляются:** `UI/GameHud.cs`, `UI/PrototypeHud.cs`, `Data/CampaignGenerator.cs`
(вместе с их `.meta`-файлами).

---

### Задача 1: Схема сейва версии 2 и миграция

Фундамент для всего остального: золото, настройки, прогресс кампании и последний
выбранный патоген. Миграция — единственное место в проекте, где можно молча
потерять чужой прогресс, поэтому она идёт первой и с тестами.

**Файлы:**
- Создать: `Assets/Scripts/Meta/GameSettings.cs`
- Создать: `Assets/Scripts/Meta/NodeProgress.cs`
- Создать: `Assets/Scripts/Meta/CampaignProgress.cs`
- Создать: `Assets/Scripts/Meta/ProgressMigration.cs`
- Изменить: `Assets/Scripts/Meta/PlayerProgress.cs`
- Изменить: `Assets/Scripts/Meta/JsonProgressStore.cs` (метод `Load`)
- Тест: `Assets/Tests/EditMode/ProgressMigrationTests.cs`

**Интерфейсы:**
- Использует: `PlayerProgress`, `IProgressStore`, `JsonProgressStore(string directory)`
- Отдаёт: `ProgressMigration.CurrentVersion` (int, = 2),
  `ProgressMigration.Migrate(PlayerProgress) → PlayerProgress`,
  `PlayerProgress.gold` (int), `PlayerProgress.settings` (`GameSettings`),
  `PlayerProgress.lastPathogen` (string), `PlayerProgress.campaign` (`CampaignProgress`),
  `CampaignProgress.StarsOf(string) → int`, `CampaignProgress.SetStars(string, int)`,
  `CampaignProgress.IsCleared(string) → bool`,
  `CampaignProgress.IsBiomeUnlocked(string) → bool`,
  `CampaignProgress.UnlockBiome(string)`,
  `GameSettings.masterVolume/musicVolume/sfxVolume` (float), `GameSettings.playerName` (string)

- [ ] **Шаг 1: Написать падающий тест миграции**

Создать `Assets/Tests/EditMode/ProgressMigrationTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Миграция сейва. Единственное место в проекте, где ошибка стирает прогресс
/// игрока безвозвратно, — поэтому проверяется отдельно от всей остальной меты.
/// </summary>
public class ProgressMigrationTests
{
    private string _dir;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "pathogen_migration_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private void WriteRawSave(string json)
    {
        File.WriteAllText(Path.Combine(_dir, "progress.json"), json);
    }

    [Test]
    public void Миграция_СохраняетБиомассуИПеркиИзВерсии1()
    {
        WriteRawSave("{\"version\":1,\"biomass\":320," +
                     "\"perks\":[{\"id\":\"perk_hp\",\"level\":3}]," +
                     "\"totalRuns\":9,\"bestLevelReached\":5,\"totalKills\":700,\"bossesDefeated\":2}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.AreEqual(320, progress.biomass, "Биомасса обязана пережить миграцию");
        Assert.AreEqual(3, progress.GetPerkLevel("perk_hp"), "Купленные перки обязаны пережить миграцию");
        Assert.AreEqual(9, progress.totalRuns);
        Assert.AreEqual(700, progress.totalKills);
        Assert.AreEqual(2, progress.bossesDefeated);
    }

    [Test]
    public void Миграция_ПоднимаетВерсиюИЗаполняетНовыеПоля()
    {
        WriteRawSave("{\"version\":1,\"biomass\":10,\"perks\":[]}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.AreEqual(ProgressMigration.CurrentVersion, progress.version);
        Assert.AreEqual(0, progress.gold, "Золота в версии 1 не было — начинаем с нуля");
        Assert.IsNotNull(progress.campaign);
        Assert.AreEqual(0, progress.campaign.nodes.Count, "Кампания не проходилась");
        Assert.IsNotNull(progress.settings);
        Assert.AreEqual(1f, progress.settings.masterVolume);
        Assert.AreEqual(string.Empty, progress.settings.playerName);
    }

    [Test]
    public void Миграция_НеТрогаетУжеАктуальныйСейв()
    {
        WriteRawSave("{\"version\":2,\"biomass\":5,\"gold\":77,\"perks\":[]," +
                     "\"settings\":{\"masterVolume\":0.3,\"musicVolume\":0.4,\"sfxVolume\":0.5,\"playerName\":\"мдв\"}," +
                     "\"lastPathogen\":\"Fungus\"," +
                     "\"campaign\":{\"nodes\":[{\"id\":\"b1_n1\",\"stars\":2}],\"biomesUnlocked\":[\"biome_bloodstream\"]}}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.AreEqual(77, progress.gold);
        Assert.AreEqual(0.3f, progress.settings.masterVolume);
        Assert.AreEqual("мдв", progress.settings.playerName);
        Assert.AreEqual("Fungus", progress.lastPathogen);
        Assert.AreEqual(2, progress.campaign.StarsOf("b1_n1"));
        Assert.IsTrue(progress.campaign.IsBiomeUnlocked("biome_bloodstream"));
    }

    [Test]
    public void Миграция_ПереживаетNullВСпискахПослеJsonUtility()
    {
        // JsonUtility кладёт null в список, если в JSON он записан как null.
        WriteRawSave("{\"version\":2,\"biomass\":1,\"perks\":null,\"campaign\":{\"nodes\":null,\"biomesUnlocked\":null}}");

        PlayerProgress progress = new JsonProgressStore(_dir).Load();

        Assert.IsNotNull(progress.perks);
        Assert.IsNotNull(progress.campaign.nodes);
        Assert.IsNotNull(progress.campaign.biomesUnlocked);
        Assert.DoesNotThrow(() => progress.campaign.SetStars("b1_n1", 1));
    }

    [Test]
    public void SetStars_ТолькоПовышаетРезультат()
    {
        var campaign = new CampaignProgress();

        campaign.SetStars("b1_n1", 3);
        campaign.SetStars("b1_n1", 1);

        Assert.AreEqual(3, campaign.StarsOf("b1_n1"), "Худший повтор не должен затирать лучший результат");
    }

    [Test]
    public void UnlockBiome_НеДублируетЗаписи()
    {
        var campaign = new CampaignProgress();

        campaign.UnlockBiome("biome_bloodstream");
        campaign.UnlockBiome("biome_bloodstream");

        Assert.AreEqual(1, campaign.biomesUnlocked.Count);
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Запустить EditMode-тесты. Ожидаемо: не компилируется —
`ProgressMigration`, `CampaignProgress`, `PlayerProgress.gold` не существуют.

- [ ] **Шаг 3: Создать `GameSettings.cs`**

```csharp
using System;

/// <summary>
/// Пользовательские настройки. Лежат в сейве, а не в PlayerPrefs: в Фазе 4
/// прогресс переезжает на бэкенд целиком, и настройки должны поехать вместе с ним.
/// </summary>
[Serializable]
public class GameSettings
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public string playerName = string.Empty;
}
```

- [ ] **Шаг 4: Создать `NodeProgress.cs`**

```csharp
using System;

/// <summary>Лучший результат по узлу кампании. Ноль звёзд — узел не пройден.</summary>
[Serializable]
public class NodeProgress
{
    public string id;
    public int stars;
}
```

- [ ] **Шаг 5: Создать `CampaignProgress.cs`**

```csharp
using System;
using System.Collections.Generic;

/// <summary>
/// Прогресс по кампании. Как и весь сейв, сериализуется через JsonUtility,
/// поэтому здесь только публичные поля и списки — словарей быть не может.
/// </summary>
[Serializable]
public class CampaignProgress
{
    public List<NodeProgress> nodes = new List<NodeProgress>();
    public List<string> biomesUnlocked = new List<string>();

    public int StarsOf(string nodeId)
    {
        if (nodes == null)
        {
            return 0;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == nodeId)
            {
                return nodes[i].stars;
            }
        }

        return 0;
    }

    public bool IsCleared(string nodeId) => StarsOf(nodeId) > 0;

    /// <summary>
    /// Записывает результат, только если он лучше прежнего: повторный проход
    /// на одну звезду не должен стирать заработанные три.
    /// </summary>
    public void SetStars(string nodeId, int stars)
    {
        if (nodes == null)
        {
            nodes = new List<NodeProgress>();
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].id == nodeId)
            {
                if (stars > nodes[i].stars)
                {
                    nodes[i].stars = stars;
                }
                return;
            }
        }

        nodes.Add(new NodeProgress { id = nodeId, stars = stars });
    }

    public bool IsBiomeUnlocked(string biomeId)
    {
        return biomesUnlocked != null && biomesUnlocked.Contains(biomeId);
    }

    public void UnlockBiome(string biomeId)
    {
        if (biomesUnlocked == null)
        {
            biomesUnlocked = new List<string>();
        }

        if (!biomesUnlocked.Contains(biomeId))
        {
            biomesUnlocked.Add(biomeId);
        }
    }
}
```

- [ ] **Шаг 6: Расширить `PlayerProgress.cs` до версии 2**

Заменить заголовок класса и поле версии. Было:

```csharp
    /// Версия схемы. Пригодится, когда формат изменится и старые сейвы надо будет мигрировать.
    public int version = 1;

    /// Мягкая валюта — «биомасса».
    public int biomass;
```

Стало:

```csharp
    /// Версия схемы. Старые сейвы поднимает ProgressMigration при загрузке.
    public int version = ProgressMigration.CurrentVersion;

    /// Мягкая валюта — «биомасса». Тратится на перманентные улучшения.
    public int biomass;

    /// Валюта косметики. Капает с узлов кампании вместе с биомассой.
    public int gold;

    public GameSettings settings = new GameSettings();

    /// Имя значения PathogenType, показываемое на главном экране. Строкой,
    /// а не индексом: перестановка значений enum не должна ломать сейвы.
    public string lastPathogen = "Virus";

    public CampaignProgress campaign = new CampaignProgress();
```

Ниже, к полю `bestLevelReached`, добавить комментарий:

```csharp
    public int totalRuns;

    /// Осталось от бесконечного забега и больше не обновляется: «дальше всех
    /// пройденный уровень» перестало быть определено вместе с самим забегом.
    /// Поле сохранено ради миграции; прогресс читается из campaign.nodes.
    public int bestLevelReached;
```

- [ ] **Шаг 7: Создать `ProgressMigration.cs`**

```csharp
using System.Collections.Generic;

/// <summary>
/// Подъём старых сейвов до актуальной схемы. Вызывается ровно в одном месте —
/// при загрузке в хранилище, — чтобы игровой код никогда не видел старый формат.
/// </summary>
public static class ProgressMigration
{
    public const int CurrentVersion = 2;

    /// <summary>
    /// Приводит прогресс к актуальной версии. Никогда не возвращает null и
    /// никогда не теряет то, что уже было: новые поля добавляются со значениями
    /// по умолчанию, старые не трогаются.
    /// </summary>
    public static PlayerProgress Migrate(PlayerProgress progress)
    {
        if (progress == null)
        {
            return new PlayerProgress();
        }

        // Версия 1 не знала про золото, настройки, кампанию и выбранного патогена.
        // Отдельной ветки не требуется: все новые поля заполняются ниже дефолтами.
        FillMissing(progress);
        progress.version = CurrentVersion;
        return progress;
    }

    /// <summary>
    /// JsonUtility оставляет null там, где в JSON поля не было или оно записано
    /// как null. Каждое такое место — потенциальный NullReference в рантайме.
    /// </summary>
    private static void FillMissing(PlayerProgress progress)
    {
        if (progress.perks == null)
        {
            progress.perks = new List<PerkLevel>();
        }

        if (progress.settings == null)
        {
            progress.settings = new GameSettings();
        }

        if (string.IsNullOrEmpty(progress.lastPathogen))
        {
            progress.lastPathogen = "Virus";
        }

        if (progress.campaign == null)
        {
            progress.campaign = new CampaignProgress();
        }

        if (progress.campaign.nodes == null)
        {
            progress.campaign.nodes = new List<NodeProgress>();
        }

        if (progress.campaign.biomesUnlocked == null)
        {
            progress.campaign.biomesUnlocked = new List<string>();
        }
    }
}
```

- [ ] **Шаг 8: Подключить миграцию в `JsonProgressStore.Load`**

В `Assets/Scripts/Meta/JsonProgressStore.cs` заменить блок, который сейчас
руками чинит только `perks`. Было:

```csharp
            if (progress.perks == null)
            {
                progress.perks = new System.Collections.Generic.List<PerkLevel>();
            }

            return progress;
```

Стало:

```csharp
            return ProgressMigration.Migrate(progress);
```

Там же заменить оба возврата чистого прогресса, чтобы версия всегда была
актуальной, — `return new PlayerProgress();` превращается в
`return ProgressMigration.Migrate(new PlayerProgress());` в обеих ветках
(отсутствующий файл и повреждённый файл), и в `catch` — тоже.

- [ ] **Шаг 9: Запустить тесты и убедиться, что всё зелёное**

Ожидаемо: 64 существующих теста + 6 новых проходят. Если упал
`MetaProgressionTests.ResetProgress_ОбнуляетВсёИСохраняет` — значит,
`new PlayerProgress()` перестал заполнять что-то из новых полей.

---

### Задача 2: Данные кампании

Заменяет `CampaignGenerator` структурой из биомов и узлов. Узлы получают
стабильные `id`, позицию на карте и базовую награду.

**Файлы:**
- Создать: `Assets/Scripts/Data/Campaign/CampaignNode.cs`
- Создать: `Assets/Scripts/Data/Campaign/BiomeData.cs`
- Создать: `Assets/Scripts/Data/Campaign/CampaignMapData.cs`
- Создать: `Assets/Scripts/Data/Campaign/CampaignBuilder.cs`
- Изменить: `Assets/Scripts/Data/BossData.cs` (добавить `parTimeSeconds`)
- Удалить: `Assets/Scripts/Data/CampaignGenerator.cs` и его `.meta`
- Тест: `Assets/Tests/EditMode/CampaignBuilderTests.cs`

**Интерфейсы:**
- Использует: `LevelData`, `WaveDefinition`, `SpawnEntry`, `EnemyCatalog.Neutrophil`,
  `EnemyCatalog.Antibody`, `EnemyCatalog.Macrophage`, `BossCatalog.LymphNode`,
  `AdvanceType`
- Отдаёт: `CampaignNode` (поля `Id`, `Level`, `MapPosition`, `BaseGold`,
  `BaseBiomass`, `IsBoss`, `EnemyNames`), `BiomeData` (поля `Id`, `DisplayName`,
  `AccentColor`, `Playable`, `Nodes`), `CampaignMapData` (свойство `Biomes`,
  методы `FindNode(string) → CampaignNode`, `BiomeOf(CampaignNode) → BiomeData`,
  `IndexOf(BiomeData) → int`), `CampaignBuilder.Build() → CampaignMapData`,
  `BossData.parTimeSeconds` (float, по умолчанию 70)

- [ ] **Шаг 1: Написать падающий тест**

Создать `Assets/Tests/EditMode/CampaignBuilderTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Структура кампании. Идентификаторы узлов уходят в сейв игрока, поэтому
/// их форма зафиксирована тестом: молчаливое переименование сотрёт прогресс.
/// </summary>
public class CampaignBuilderTests
{
    private CampaignMapData _map;

    [SetUp]
    public void SetUp()
    {
        _map = CampaignBuilder.Build();
    }

    [Test]
    public void Build_ДаётТриБиомаИзКоторыхИграбеленПервый()
    {
        Assert.AreEqual(3, _map.Biomes.Count);
        Assert.IsTrue(_map.Biomes[0].Playable, "Биом «Кровоток» — единственный с существующими врагами");
        Assert.IsFalse(_map.Biomes[1].Playable);
        Assert.IsFalse(_map.Biomes[2].Playable);
    }

    [Test]
    public void ПервыйБиом_ВосемьУзловИПоследнийБосс()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual(8, nodes.Count);
        Assert.IsTrue(nodes[7].IsBoss);
        Assert.AreEqual(AdvanceType.Boss, nodes[7].Level.advanceType);
        Assert.IsNotNull(nodes[7].Level.bossData);

        for (int i = 0; i < 7; i++)
        {
            Assert.IsFalse(nodes[i].IsBoss, $"Узел {i} не должен быть боссом");
            Assert.Greater(nodes[i].Level.waves.Count, 0, $"У узла {i} обязаны быть волны");
        }
    }

    [Test]
    public void ИдентификаторыУзлов_СтабильныИУникальны()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual("b1_n1", nodes[0].Id);
        Assert.AreEqual("b1_n7", nodes[6].Id);
        Assert.AreEqual("b1_boss", nodes[7].Id);

        var seen = new HashSet<string>();
        foreach (BiomeData biome in _map.Biomes)
        {
            foreach (CampaignNode node in biome.Nodes)
            {
                Assert.IsTrue(seen.Add(node.Id), $"Дубликат идентификатора узла: {node.Id}");
            }
        }
    }

    [Test]
    public void FindNode_НаходитПоИдентификаторуИОтдаётNullНаЧужой()
    {
        Assert.AreEqual("b1_n3", _map.FindNode("b1_n3").Id);
        Assert.IsNull(_map.FindNode("нет_такого"));
    }

    [Test]
    public void BiomeOf_ВозвращаетБиомУзла()
    {
        CampaignNode node = _map.FindNode("b1_boss");

        Assert.AreSame(_map.Biomes[0], _map.BiomeOf(node));
        Assert.AreEqual(0, _map.IndexOf(_map.Biomes[0]));
    }

    [Test]
    public void Награда_РастётПоУзламИУтраиваетсяНаБоссе()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual(20, nodes[0].BaseGold, "20 + 6 * 0");
        Assert.AreEqual(15, nodes[0].BaseBiomass, "15 + 5 * 0");
        Assert.AreEqual(26, nodes[1].BaseGold, "20 + 6 * 1");
        Assert.Greater(nodes[7].BaseGold, nodes[6].BaseGold * 2, "Босс утраивает базу");
    }

    [Test]
    public void ТипНаступления_Чередуется()
    {
        IReadOnlyList<CampaignNode> nodes = _map.Biomes[0].Nodes;

        Assert.AreEqual(AdvanceType.Waves, nodes[0].Level.advanceType);
        Assert.AreEqual(AdvanceType.Segments, nodes[1].Level.advanceType);
        Assert.AreEqual(AdvanceType.Waves, nodes[2].Level.advanceType);
    }

    [Test]
    public void EnemyNames_ПеречисляетСоставБезПовторов()
    {
        CampaignNode late = _map.Biomes[0].Nodes[5];

        CollectionAssert.AllItemsAreUnique(late.EnemyNames);
        Assert.Contains(EnemyCatalog.Neutrophil.enemyName, (System.Collections.ICollection)late.EnemyNames);
    }

    [Test]
    public void ЗаблокированныеБиомы_ПустыИНеЛомаютОбход()
    {
        Assert.AreEqual(0, _map.Biomes[1].Nodes.Count, "Врагов для биома 2 не существует — узлов быть не может");
        Assert.AreEqual(0, _map.Biomes[2].Nodes.Count);
        Assert.IsNotEmpty(_map.Biomes[1].DisplayName);
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `CampaignBuilder`, `CampaignMapData`,
`CampaignNode`, `BiomeData` не существуют.

- [ ] **Шаг 3: Создать `CampaignNode.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Узел карты кампании: уровень плюс всё, что нужно карте и брифингу.
/// Не ScriptableObject — кампания собирается кодом, как и остальная сцена Фазы 2.
/// </summary>
public class CampaignNode
{
    /// <summary>
    /// Стабильный идентификатор вида «b1_n3». Уходит в сейв игрока —
    /// после первого релиза переименование стирает чужой прогресс.
    /// </summary>
    public readonly string Id;

    public readonly string DisplayName;
    public readonly LevelData Level;

    /// Порядковый номер узла внутри биома с нуля. От него растёт сложность.
    public readonly int IndexInBiome;

    /// Позиция на карте в координатах макета 720×1280.
    public readonly Vector2 MapPosition;

    public readonly int BaseGold;
    public readonly int BaseBiomass;
    public readonly bool IsBoss;

    /// Состав врагов для брифинга, без повторов и в порядке появления.
    public readonly IReadOnlyList<string> EnemyNames;

    public CampaignNode(
        string id,
        string displayName,
        LevelData level,
        int indexInBiome,
        Vector2 mapPosition,
        int baseGold,
        int baseBiomass)
    {
        Id = id;
        DisplayName = displayName;
        Level = level;
        IndexInBiome = indexInBiome;
        MapPosition = mapPosition;
        BaseGold = baseGold;
        BaseBiomass = baseBiomass;
        IsBoss = level != null && level.advanceType == AdvanceType.Boss;
        EnemyNames = CollectEnemyNames(level);
    }

    private static IReadOnlyList<string> CollectEnemyNames(LevelData level)
    {
        var names = new List<string>();
        if (level == null)
        {
            return names;
        }

        if (level.advanceType == AdvanceType.Boss)
        {
            if (level.bossData != null)
            {
                names.Add(level.bossData.bossName);
            }
            return names;
        }

        for (int w = 0; w < level.waves.Count; w++)
        {
            List<SpawnEntry> entries = level.waves[w].entries;
            for (int e = 0; e < entries.Count; e++)
            {
                if (entries[e] == null || entries[e].enemy == null)
                {
                    continue;
                }

                string name = entries[e].enemy.enemyName;
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }
}
```

- [ ] **Шаг 4: Создать `BiomeData.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Биом кампании. Playable отделяет настоящий контент от заглушек: биомы 2 и 3
/// нарисованы на карте, но врагов для них ещё не существует.
/// </summary>
public class BiomeData
{
    public readonly string Id;
    public readonly string DisplayName;
    public readonly Color AccentColor;
    public readonly bool Playable;
    public readonly IReadOnlyList<CampaignNode> Nodes;

    public BiomeData(string id, string displayName, Color accentColor, bool playable, IReadOnlyList<CampaignNode> nodes)
    {
        Id = id;
        DisplayName = displayName;
        AccentColor = accentColor;
        Playable = playable;
        Nodes = nodes ?? new List<CampaignNode>();
    }

    public CampaignNode BossNode => Nodes.Count > 0 && Nodes[Nodes.Count - 1].IsBoss
        ? Nodes[Nodes.Count - 1]
        : null;
}
```

- [ ] **Шаг 5: Создать `CampaignMapData.cs`**

```csharp
using System.Collections.Generic;

/// <summary>Вся кампания. Поиск по идентификатору нужен при восстановлении из сейва.</summary>
public class CampaignMapData
{
    public readonly IReadOnlyList<BiomeData> Biomes;

    public CampaignMapData(IReadOnlyList<BiomeData> biomes)
    {
        Biomes = biomes;
    }

    public CampaignNode FindNode(string id)
    {
        for (int b = 0; b < Biomes.Count; b++)
        {
            IReadOnlyList<CampaignNode> nodes = Biomes[b].Nodes;
            for (int n = 0; n < nodes.Count; n++)
            {
                if (nodes[n].Id == id)
                {
                    return nodes[n];
                }
            }
        }

        return null;
    }

    public BiomeData BiomeOf(CampaignNode node)
    {
        if (node == null)
        {
            return null;
        }

        for (int b = 0; b < Biomes.Count; b++)
        {
            IReadOnlyList<CampaignNode> nodes = Biomes[b].Nodes;
            for (int n = 0; n < nodes.Count; n++)
            {
                if (nodes[n] == node)
                {
                    return Biomes[b];
                }
            }
        }

        return null;
    }

    public int IndexOf(BiomeData biome)
    {
        for (int b = 0; b < Biomes.Count; b++)
        {
            if (Biomes[b] == biome)
            {
                return b;
            }
        }

        return -1;
    }
}
```

- [ ] **Шаг 6: Добавить `parTimeSeconds` в `BossData.cs`**

В `Assets/Scripts/Data/BossData.cs`, в класс `BossData`, после
`rageIntervalScalePerKill` добавить:

```csharp
    [Tooltip("Эталонное время боя в секундах. От него считаются звёзды: у босс-уровня нет волн, поэтому парТайм задаётся вручную, а не выводится из расписания спавна.")]
    public float parTimeSeconds = 70f;
```

- [ ] **Шаг 7: Создать `CampaignBuilder.cs`**

Логика построения волн переносится из `CampaignGenerator` без изменений — она
уже отражает dev-plan.md. Меняется обёртка вокруг неё.

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Собирает кампанию кодом. Заменяет CampaignGenerator: тот отдавал плоский
/// список уровней для бесконечного забега, здесь — биомы и узлы карты.
///
/// Правила состава волн перенесены из CampaignGenerator без изменений:
/// нечётные узлы — волны, чётные — сегменты, состав фиксирован для узла,
/// а тайминги рандомизируются уже в спавнере.
/// </summary>
public static class CampaignBuilder
{
    /// Сколько узлов в первом биоме, включая босса.
    public const int BloodstreamNodes = 8;

    public const string BloodstreamId = "biome_bloodstream";
    public const string LymphaticId = "biome_lymphatic";
    public const string MarrowId = "biome_marrow";

    /// Горизонтальный разброс дорожки на карте, в координатах макета.
    private const float MapSwing = 150f;

    /// Расстояние между узлами по вертикали.
    private const float MapStep = 152f;

    public static CampaignMapData Build()
    {
        var biomes = new List<BiomeData>
        {
            BuildBloodstream(),

            // Биомы 2 и 3 из dev-plan.md существуют на карте, но врагов и боссов
            // для них ещё нет — это Фаза 3. Подделывать их рескином биома 1 нельзя:
            // такой контент пришлось бы выбрасывать целиком.
            new BiomeData(LymphaticId, "Лимфатическая система",
                new Color(0.45f, 0.70f, 0.85f), playable: false, nodes: new List<CampaignNode>()),

            new BiomeData(MarrowId, "Костный мозг",
                new Color(0.85f, 0.72f, 0.45f), playable: false, nodes: new List<CampaignNode>())
        };

        return new CampaignMapData(biomes);
    }

    private static BiomeData BuildBloodstream()
    {
        var nodes = new List<CampaignNode>(BloodstreamNodes);

        for (int i = 0; i < BloodstreamNodes; i++)
        {
            int number = i + 1;
            bool isBoss = number == BloodstreamNodes;

            LevelData level = isBoss ? BuildBossLevel(number) : BuildBattleLevel(number, i);
            string id = isBoss ? "b1_boss" : $"b1_n{number}";

            int gold = 20 + 6 * i;
            int biomass = 15 + 5 * i;
            if (isBoss)
            {
                gold *= 3;
                biomass *= 3;
            }

            var position = new Vector2(i % 2 == 0 ? -MapSwing : MapSwing, i * MapStep);

            nodes.Add(new CampaignNode(id, level.levelName, level, i, position, gold, biomass));
        }

        return new BiomeData(BloodstreamId, "Кровоток", new Color(0.85f, 0.35f, 0.40f),
            playable: true, nodes: nodes);
    }

    private static LevelData BuildBattleLevel(int number, int index)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = (number % 2 == 0) ? AdvanceType.Segments : AdvanceType.Waves;
        level.levelName = $"Кровоток {number} · {(level.advanceType == AdvanceType.Waves ? "волны" : "сегменты")}";
        level.name = level.levelName;

        // Сегменты давят таймером, поэтому их спуск ускоряется медленнее,
        // чем растёт населённость волн.
        level.segmentDescendSpeed = 0.65f + 0.06f * index;
        level.segmentBreachDamage = 22f + 2f * index;

        int waveCount = Mathf.Clamp(2 + index / 2, 2, 5);
        for (int w = 0; w < waveCount; w++)
        {
            level.waves.Add(BuildWave(number, w));
        }

        return level;
    }

    private static LevelData BuildBossLevel(int number)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Boss;
        level.bossData = BossCatalog.LymphNode;
        level.levelName = $"Кровоток {number} · босс: {level.bossData.bossName}";
        level.name = level.levelName;
        return level;
    }

    private static WaveDefinition BuildWave(int levelNumber, int waveIndex)
    {
        var wave = new WaveDefinition();

        // Нейтрофилы — основа любой волны, их количество растёт быстрее всего.
        int rushers = 4 + levelNumber + waveIndex * 2;
        wave.entries.Add(new SpawnEntry(EnemyCatalog.Neutrophil, rushers));

        // Антитела появляются со 2-го узла: заставляют не стоять на месте.
        if (levelNumber >= 2)
        {
            wave.entries.Add(new SpawnEntry(EnemyCatalog.Antibody, 1 + (levelNumber - 2) / 2 + waveIndex / 2));
        }

        // Макрофаги — с 3-го, по одному на волну, плюс ещё один на поздних узлах.
        if (levelNumber >= 3)
        {
            wave.entries.Add(new SpawnEntry(EnemyCatalog.Macrophage, levelNumber >= 6 ? 2 : 1));
        }

        float fastest = Mathf.Max(0.18f, 0.45f - 0.03f * levelNumber);
        wave.spawnIntervalRange = new Vector2(fastest, fastest + 0.4f);
        wave.postWaveDelay = 1.6f;

        return wave;
    }
}
```

- [ ] **Шаг 8: Удалить `CampaignGenerator.cs`**

```powershell
Remove-Item "Assets\Scripts\Data\CampaignGenerator.cs", "Assets\Scripts\Data\CampaignGenerator.cs.meta"
```

Компиляция после этого сломается в `GameRunner.Initialize` (вызовы
`CampaignGenerator.BuildBloodstream` и `FindFirstBossLevel`) и в
`CampaignAndCombatTests`. Это ожидаемо и чинится в задаче 7 — до неё проект не
компилируется. Чтобы не оставлять сборку сломанной между задачами, временно
подставить в `GameRunner.Initialize`:

```csharp
        _levels = new List<LevelData>();
        foreach (CampaignNode node in CampaignBuilder.Build().Biomes[0].Nodes)
        {
            _levels.Add(node.Level);
        }
        FirstBossLevelIndex = _levels.Count - 1;
```

а в `CampaignAndCombatTests` заменить обращения к `CampaignGenerator.BuildBloodstream(8)`
на `CampaignBuilder.Build().Biomes[0].Nodes` с извлечением `.Level`. Обе временные
подпорки уходят в задаче 7.

- [ ] **Шаг 9: Запустить тесты**

Ожидаемо: все существующие тесты плюс 8 новых из `CampaignBuilderTests` проходят.

---

### Задача 3: Звёзды и парТайм

Порог звёзд не задаётся руками на каждый уровень, а считается из его же
содержимого: сумма расписания спавна — это физический минимум, быстрее которого
уровень пройти нельзя.

**Файлы:**
- Создать: `Assets/Scripts/Campaign/StarRating.cs`
- Тест: `Assets/Tests/EditMode/StarRatingTests.cs`

**Интерфейсы:**
- Использует: `CampaignNode`, `LevelData`, `WaveDefinition.TotalCount`,
  `WaveDefinition.spawnIntervalRange`, `WaveDefinition.postWaveDelay`,
  `BossData.parTimeSeconds`, `AdvanceType`
- Отдаёт: `StarRating.MaxStars` (int, = 3), `StarRating.ThreeStarFactor` (float, = 1.15),
  `StarRating.TwoStarFactor` (float, = 1.7),
  `StarRating.ParTime(CampaignNode) → float`,
  `StarRating.Evaluate(CampaignNode, float elapsedSeconds) → int`

- [ ] **Шаг 1: Написать падающий тест**

Создать `Assets/Tests/EditMode/StarRatingTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Оценка прохождения. Пороги считаются из расписания спавна самого уровня,
/// поэтому ошибка здесь перекашивает звёзды сразу по всей кампании.
/// </summary>
public class StarRatingTests
{
    /// <summary>Уровень с предсказуемым парТаймом: 10 врагов × 0.5с + 2с = 7с.</summary>
    private static CampaignNode MakeWaveNode()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        level.levelName = "Тестовый";

        var wave = new WaveDefinition
        {
            spawnIntervalRange = new Vector2(0.4f, 0.6f),
            postWaveDelay = 2f
        };
        wave.entries.Add(new SpawnEntry(EnemyCatalog.Neutrophil, 10));
        level.waves.Add(wave);

        return new CampaignNode("test_n1", "Тестовый", level, 0, Vector2.zero, 10, 10);
    }

    private static CampaignNode MakeBossNode()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Boss;
        level.bossData = BossCatalog.LymphNode;
        level.levelName = "Босс";

        return new CampaignNode("test_boss", "Босс", level, 7, Vector2.zero, 30, 30);
    }

    [Test]
    public void ParTime_СуммируетРасписаниеСпавна()
    {
        Assert.AreEqual(7f, StarRating.ParTime(MakeWaveNode()), 0.001f,
            "10 врагов × средний интервал 0.5с + пауза 2с");
    }

    [Test]
    public void ParTime_ДляБоссаБерётсяИзBossData()
    {
        Assert.AreEqual(BossCatalog.LymphNode.parTimeSeconds, StarRating.ParTime(MakeBossNode()), 0.001f);
    }

    [Test]
    public void Evaluate_ТриЗвездыЗаВремяВнутриПорога()
    {
        CampaignNode node = MakeWaveNode();

        Assert.AreEqual(3, StarRating.Evaluate(node, 1f), "Быстрее порога — максимум");
        Assert.AreEqual(3, StarRating.Evaluate(node, 7f * 1.15f), "Ровно на пороге три звезды ещё дают");
    }

    [Test]
    public void Evaluate_ДвеЗвездыМеждуПорогами()
    {
        CampaignNode node = MakeWaveNode();

        Assert.AreEqual(2, StarRating.Evaluate(node, 7f * 1.15f + 0.01f), "Чуть медленнее — уже две");
        Assert.AreEqual(2, StarRating.Evaluate(node, 7f * 1.7f), "Ровно на втором пороге две звезды ещё дают");
    }

    [Test]
    public void Evaluate_ОднаЗвездаЗаЛюбоеПрохождение()
    {
        CampaignNode node = MakeWaveNode();

        Assert.AreEqual(1, StarRating.Evaluate(node, 7f * 1.7f + 0.01f));
        Assert.AreEqual(1, StarRating.Evaluate(node, 100000f), "Пройденный уровень никогда не даёт ноль");
    }

    [Test]
    public void ParTime_НикогдаНеНоль()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        var empty = new CampaignNode("test_empty", "Пустой", level, 0, Vector2.zero, 1, 1);

        Assert.Greater(StarRating.ParTime(empty), 0f,
            "Нулевой парТайм превратил бы пороги в деление на ноль и отдал бы одну звезду всегда");
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `StarRating` не существует.

- [ ] **Шаг 3: Создать `StarRating.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Звёзды за прохождение узла. Порог не задаётся руками для каждого уровня,
/// а выводится из его собственного расписания спавна: сумма интервалов — это
/// время, за которое враги физически успевают появиться, и быстрее уровень
/// пройти нельзя. Три звезды означают «убивал не медленнее, чем они выходили».
/// </summary>
public static class StarRating
{
    public const int MaxStars = 3;

    public const float ThreeStarFactor = 1.15f;
    public const float TwoStarFactor = 1.7f;

    /// Страховка от вырожденного уровня без волн: ноль превратил бы пороги
    /// в деление на ноль и выдавал бы одну звезду при любом результате.
    private const float MinimumParTime = 5f;

    public static float ParTime(CampaignNode node)
    {
        if (node == null || node.Level == null)
        {
            return MinimumParTime;
        }

        LevelData level = node.Level;

        // У босс-уровня нет расписания спавна, выводить парТайм не из чего.
        if (level.advanceType == AdvanceType.Boss)
        {
            return level.bossData != null
                ? Mathf.Max(MinimumParTime, level.bossData.parTimeSeconds)
                : MinimumParTime;
        }

        float total = 0f;
        for (int i = 0; i < level.waves.Count; i++)
        {
            WaveDefinition wave = level.waves[i];
            float meanInterval = (wave.spawnIntervalRange.x + wave.spawnIntervalRange.y) * 0.5f;
            total += wave.TotalCount * meanInterval + wave.postWaveDelay;
        }

        return Mathf.Max(MinimumParTime, total);
    }

    /// <summary>
    /// Оценка пройденного уровня. Вызывается только для победы: провал звёзд
    /// не даёт вообще, и ноль сюда не возвращается никогда.
    /// </summary>
    public static int Evaluate(CampaignNode node, float elapsedSeconds)
    {
        float par = ParTime(node);

        if (elapsedSeconds <= par * ThreeStarFactor)
        {
            return 3;
        }

        return elapsedSeconds <= par * TwoStarFactor ? 2 : 1;
    }
}
```

- [ ] **Шаг 4: Запустить тесты**

Ожидаемо: 6 новых тестов проходят, старые не сломаны.

---

### Задача 4: Награды и правила открытия

Экономика узла и доступность узлов и биомов. Здесь закрывается ферма первого
узла: повтор без улучшения платит треть.

**Файлы:**
- Создать: `Assets/Scripts/Campaign/CampaignRewards.cs`
- Создать: `Assets/Scripts/Campaign/CampaignRules.cs`
- Изменить: `Assets/Scripts/Meta/MetaProgression.cs`
- Тест: `Assets/Tests/EditMode/CampaignRewardsTests.cs`
- Тест: `Assets/Tests/EditMode/CampaignRulesTests.cs`

**Интерфейсы:**
- Использует: `CampaignNode`, `CampaignMapData`, `BiomeData`, `CampaignProgress`,
  `PlayerProgress`, `MetaProgression`
- Отдаёт: `Reward` (структура, поля `Gold` и `Biomass`, метод `Scale(float) → Reward`,
  оператор `-`), `CampaignRewards.RepeatFraction` (float, = 0.30),
  `CampaignRewards.StarMultiplier(int) → float`,
  `CampaignRewards.Full(CampaignNode, int stars) → Reward`,
  `CampaignRewards.Payout(CampaignNode, int previousStars, int newStars) → Reward`,
  `CampaignRules.IsNodeUnlocked(CampaignMapData, CampaignProgress, CampaignNode) → bool`,
  `CampaignRules.ApplyClear(CampaignMapData, CampaignProgress, CampaignNode, int stars)`,
  `MetaProgression.GreedMultiplier` (public const float),
  `MetaProgression.AwardNode(CampaignNode, int previousStars, int newStars) → Reward`,
  `MetaProgression.LastNodeReward` (Reward)

- [ ] **Шаг 1: Написать падающие тесты наград**

Создать `Assets/Tests/EditMode/CampaignRewardsTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Экономика узла. Ломается молча: игра продолжает работать, просто прогресс
/// становится либо непроходимым, либо фермой из одного уровня.
/// </summary>
public class CampaignRewardsTests
{
    private static CampaignNode Node(int gold = 100, int biomass = 100)
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        return new CampaignNode("test_n1", "Тестовый", level, 0, Vector2.zero, gold, biomass);
    }

    [Test]
    public void StarMultiplier_РастётПоЧетвертиЗаЗвезду()
    {
        Assert.AreEqual(1.00f, CampaignRewards.StarMultiplier(1), 0.001f);
        Assert.AreEqual(1.25f, CampaignRewards.StarMultiplier(2), 0.001f);
        Assert.AreEqual(1.50f, CampaignRewards.StarMultiplier(3), 0.001f);
    }

    [Test]
    public void Payout_ПервоеПрохождениеПлатитПолностью()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 0, newStars: 2);

        Assert.AreEqual(125, reward.Gold);
        Assert.AreEqual(125, reward.Biomass);
    }

    [Test]
    public void Payout_УлучшениеЗвёздПлатитТолькоРазницу()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 2, newStars: 3);

        Assert.AreEqual(25, reward.Gold, "150 за три звезды минус 125 уже полученных");
        Assert.AreEqual(25, reward.Biomass);
    }

    [Test]
    public void Payout_ПовторБезУлучшенияПлатитТреть()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 3, newStars: 3);

        Assert.AreEqual(45, reward.Gold, "30% от полной награды за три звезды");
    }

    [Test]
    public void Payout_ХудшийПовторНеОтнимаетНичего()
    {
        Reward reward = CampaignRewards.Payout(Node(), previousStars: 3, newStars: 1);

        Assert.GreaterOrEqual(reward.Gold, 0, "Отрицательная выплата отобрала бы у игрока валюту");
        Assert.AreEqual(45, reward.Gold, "Платится повтор по лучшему результату, а не по текущему");
    }

    [Test]
    public void AwardNode_НачисляетСоСрезомЖадностиИСохраняет()
    {
        var go = new GameObject("Meta");
        try
        {
            var meta = go.AddComponent<MetaProgression>();
            var store = new FakeStore();
            meta.Initialize(store);

            Reward reward = meta.AwardNode(Node(), previousStars: 0, newStars: 3);

            // 150 полной награды × 0.65 = 97.5 -> 98
            Assert.AreEqual(98, reward.Gold);
            Assert.AreEqual(98, meta.Progress.gold);
            Assert.AreEqual(98, meta.Progress.biomass);
            Assert.GreaterOrEqual(store.SaveCalls, 1, "Прогресс обязан лечь на диск до экрана результатов");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private class FakeStore : IProgressStore
    {
        public int SaveCalls;
        private PlayerProgress _stored = new PlayerProgress();

        public PlayerProgress Load() => _stored;

        public void Save(PlayerProgress progress)
        {
            SaveCalls++;
            _stored = progress;
        }
    }
}
```

- [ ] **Шаг 2: Написать падающие тесты правил**

Создать `Assets/Tests/EditMode/CampaignRulesTests.cs`:

```csharp
using NUnit.Framework;

/// <summary>
/// Доступность узлов и биомов. Ошибка здесь либо запирает игрока в первом узле,
/// либо открывает ему всю кампанию сразу.
/// </summary>
public class CampaignRulesTests
{
    private CampaignMapData _map;
    private CampaignProgress _progress;

    [SetUp]
    public void SetUp()
    {
        _map = CampaignBuilder.Build();
        _progress = new CampaignProgress();
        _progress.UnlockBiome(CampaignBuilder.BloodstreamId);
    }

    [Test]
    public void ПервыйУзелОткрытогоБиома_ДоступенСразу()
    {
        Assert.IsTrue(CampaignRules.IsNodeUnlocked(_map, _progress, _map.FindNode("b1_n1")));
    }

    [Test]
    public void ВторойУзел_ЗакрытПокаНеПройденПервый()
    {
        CampaignNode second = _map.FindNode("b1_n2");

        Assert.IsFalse(CampaignRules.IsNodeUnlocked(_map, _progress, second));

        _progress.SetStars("b1_n1", 1);

        Assert.IsTrue(CampaignRules.IsNodeUnlocked(_map, _progress, second),
            "Одной звезды достаточно: она и означает «пройден»");
    }

    [Test]
    public void УзлыЗакрытогоБиома_НедоступныДажеПоПорядку()
    {
        var fresh = new CampaignProgress();

        Assert.IsFalse(CampaignRules.IsNodeUnlocked(_map, fresh, _map.FindNode("b1_n1")),
            "Биом не открыт — узлы недоступны");
    }

    [Test]
    public void ApplyClear_ЗаписываетЗвёздыИОткрываетСледующийБиомТолькоЗаБосса()
    {
        CampaignRules.ApplyClear(_map, _progress, _map.FindNode("b1_n1"), 2);

        Assert.AreEqual(2, _progress.StarsOf("b1_n1"));
        Assert.IsFalse(_progress.IsBiomeUnlocked(CampaignBuilder.LymphaticId),
            "Обычный узел следующий биом не открывает");

        CampaignRules.ApplyClear(_map, _progress, _map.FindNode("b1_boss"), 1);

        Assert.IsTrue(_progress.IsBiomeUnlocked(CampaignBuilder.LymphaticId));
    }

    [Test]
    public void ApplyClear_НеПадаетНаБоссеПоследнегоБиома()
    {
        var progress = new CampaignProgress();
        progress.UnlockBiome(CampaignBuilder.MarrowId);
        BiomeData last = _map.Biomes[_map.Biomes.Count - 1];

        Assert.AreEqual(0, last.Nodes.Count, "Заглушка биома пуста — открывать нечего");
        Assert.DoesNotThrow(() => CampaignRules.EnsureFirstBiomeUnlocked(progress));
    }

    [Test]
    public void EnsureFirstBiomeUnlocked_ОткрываетКровотокНаЧистомПрогрессе()
    {
        var fresh = new CampaignProgress();

        CampaignRules.EnsureFirstBiomeUnlocked(fresh);

        Assert.IsTrue(fresh.IsBiomeUnlocked(CampaignBuilder.BloodstreamId));
    }
}
```

- [ ] **Шаг 3: Запустить тесты и убедиться, что они падают**

Ожидаемо: не компилируется — `Reward`, `CampaignRewards`, `CampaignRules`,
`MetaProgression.AwardNode` не существуют.

- [ ] **Шаг 4: Создать `CampaignRewards.cs`**

```csharp
using UnityEngine;

/// <summary>Пара валют. Структура, а не класс: живёт коротко и не должна мусорить.</summary>
public readonly struct Reward
{
    public readonly int Gold;
    public readonly int Biomass;

    public Reward(int gold, int biomass)
    {
        Gold = gold;
        Biomass = biomass;
    }

    public static readonly Reward Zero = new Reward(0, 0);

    public Reward Scale(float factor) => new Reward(
        Mathf.Max(0, Mathf.RoundToInt(Gold * factor)),
        Mathf.Max(0, Mathf.RoundToInt(Biomass * factor)));

    public static Reward operator -(Reward a, Reward b) => new Reward(
        Mathf.Max(0, a.Gold - b.Gold),
        Mathf.Max(0, a.Biomass - b.Biomass));
}

/// <summary>
/// Сколько платит узел. Повтор платит треть, а улучшение звёзд — только разницу:
/// без этого первый узел биома становится фермой, а звёзды теряют смысл.
/// </summary>
public static class CampaignRewards
{
    /// Доля награды за повторное прохождение без улучшения результата.
    public const float RepeatFraction = 0.30f;

    public static float StarMultiplier(int stars)
    {
        if (stars <= 1)
        {
            return 1f;
        }

        return stars == 2 ? 1.25f : 1.5f;
    }

    /// <summary>Полная награда узла за указанное число звёзд, до среза жадности.</summary>
    public static Reward Full(CampaignNode node, int stars)
    {
        if (node == null)
        {
            return Reward.Zero;
        }

        float multiplier = StarMultiplier(stars);
        return new Reward(
            Mathf.RoundToInt(node.BaseGold * multiplier),
            Mathf.RoundToInt(node.BaseBiomass * multiplier));
    }

    /// <summary>
    /// Что реально причитается за прохождение.
    /// </summary>
    /// <param name="previousStars">Лучший результат до этого захода, 0 — узел не пройден.</param>
    /// <param name="newStars">Результат текущего захода.</param>
    public static Reward Payout(CampaignNode node, int previousStars, int newStars)
    {
        if (node == null || newStars <= 0)
        {
            return Reward.Zero;
        }

        if (previousStars <= 0)
        {
            return Full(node, newStars);
        }

        if (newStars > previousStars)
        {
            return Full(node, newStars) - Full(node, previousStars);
        }

        // Повтор считается по лучшему результату, а не по текущему: сыграть хуже
        // и получить меньше — наказание, которого игрок не поймёт.
        return Full(node, previousStars).Scale(RepeatFraction);
    }
}
```

- [ ] **Шаг 5: Создать `CampaignRules.cs`**

```csharp
using System.Collections.Generic;

/// <summary>
/// Доступность узлов и биомов. Вынесено из UI намеренно: карта только рисует
/// то, что решили здесь, и правила можно проверить тестами без сцены.
/// </summary>
public static class CampaignRules
{
    /// <summary>Первый биом открыт всегда — иначе новому игроку некуда идти.</summary>
    public static void EnsureFirstBiomeUnlocked(CampaignProgress progress)
    {
        if (progress != null)
        {
            progress.UnlockBiome(CampaignBuilder.BloodstreamId);
        }
    }

    public static bool IsNodeUnlocked(CampaignMapData map, CampaignProgress progress, CampaignNode node)
    {
        if (map == null || progress == null || node == null)
        {
            return false;
        }

        BiomeData biome = map.BiomeOf(node);
        if (biome == null || !biome.Playable || !progress.IsBiomeUnlocked(biome.Id))
        {
            return false;
        }

        IReadOnlyList<CampaignNode> nodes = biome.Nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != node)
            {
                continue;
            }

            // Первый узел биома доступен сразу, остальные — по цепочке.
            return i == 0 || progress.IsCleared(nodes[i - 1].Id);
        }

        return false;
    }

    /// <summary>
    /// Записать результат прохождения. Звёзды только повышаются; босс открывает
    /// следующий биом.
    /// </summary>
    public static void ApplyClear(CampaignMapData map, CampaignProgress progress, CampaignNode node, int stars)
    {
        if (map == null || progress == null || node == null || stars <= 0)
        {
            return;
        }

        progress.SetStars(node.Id, stars);

        if (!node.IsBoss)
        {
            return;
        }

        BiomeData biome = map.BiomeOf(node);
        int index = map.IndexOf(biome);
        if (index >= 0 && index + 1 < map.Biomes.Count)
        {
            progress.UnlockBiome(map.Biomes[index + 1].Id);
        }
    }
}
```

- [ ] **Шаг 6: Добавить начисление за узел в `MetaProgression.cs`**

Сделать константу жадности публичной — на неё теперь ссылаются тесты и расчёт
награды за узел. Было:

```csharp
    private const float GreedMultiplier = 0.65f;
```

Стало:

```csharp
    public const float GreedMultiplier = 0.65f;
```

Удалить метод `AwardRun` целиком вместе с константами `BiomassPerKill`,
`BiomassPerLevel`, `BiomassPerBoss` и свойством `LastRunReward` — награда за
бесконечный забег больше не существует. На их место:

```csharp
    /// Что принёс последний пройденный узел — для экрана результата.
    public Reward LastNodeReward { get; private set; }

    /// <summary>
    /// Начислить награду за пройденный узел и сохранить прогресс.
    /// Сохранение идёт здесь, до показа результата: по dev-plan.md прогресс
    /// обязан быть на диске раньше, чем игроку что-либо предложат
    /// (в Фазе 4 — просмотр рекламы за удвоение).
    /// </summary>
    public Reward AwardNode(CampaignNode node, int previousStars, int newStars)
    {
        Reward payout = CampaignRewards.Payout(node, previousStars, newStars).Scale(GreedMultiplier);

        LastNodeReward = payout;
        Progress.gold += payout.Gold;
        Progress.biomass += payout.Biomass;

        if (node != null && node.IsBoss && previousStars <= 0 && newStars > 0)
        {
            Progress.bossesDefeated++;
        }

        Save();
        return payout;
    }

    /// <summary>Учесть завершённую попытку биома в статистике.</summary>
    public void RecordBiomeAttempt(int kills)
    {
        Progress.totalRuns++;
        Progress.totalKills += kills;
        Save();
    }
```

- [ ] **Шаг 7: Отключить старый UI, чтобы проект собирался**

Удаление `AwardRun` и `LastRunReward` ломает два места, которые всё равно
исчезнут в задачах 6 и 11. Отключить их сейчас, чтобы EditMode-тесты можно было
прогонять между задачами:

```powershell
Rename-Item "Assets\Scripts\UI\GameHud.cs" "GameHud.cs.disabled"
Rename-Item "Assets\Scripts\UI\GameHud.cs.meta" "GameHud.cs.disabled.meta"
Rename-Item "Assets\Scripts\UI\PrototypeHud.cs" "PrototypeHud.cs.disabled"
Rename-Item "Assets\Scripts\UI\PrototypeHud.cs.meta" "PrototypeHud.cs.disabled.meta"
```

В `Assets/Scripts/Core/GameBootstrap.cs` закомментировать весь блок
`if (useLegacyImguiHud) { ... } else { ... }` — до задачи 11 интерфейса не будет.

В `Assets/Scripts/Core/GameRunner.cs`, в `OnPlayerDied`, временно убрать вызов
исчезнувшего метода. Было:

```csharp
        if (_meta != null)
        {
            _meta.AwardRun(TotalKills, LevelNumber, _bossesDefeatedThisRun);
        }
```

Стало (строка уходит целиком в задаче 6 вместе со всем методом):

```csharp
        // Награда за забег исчезла вместе с бесконечным циклом; начисление
        // за узел появится в задаче 6, когда GameRunner станет поузловым.
```

- [ ] **Шаг 8: Починить `MetaProgressionTests`**

Четыре теста в `Assets/Tests/EditMode/MetaProgressionTests.cs` обращаются к
удалённому `AwardRun`: `AwardRun_НачисляетПоФормулеСоСрезомЖадности`,
`AwardRun_ОбновляетСтатистикуИСохраняет`, `AwardRun_НеУхудшаетЛучшийРезультат` и
проверка `bestLevelReached`. Удалить эти три теста — их предмет исчез вместе с
бесконечным забегом, а начисление за узел покрыто `CampaignRewardsTests`.
Взамен добавить в тот же файл:

```csharp
    [Test]
    public void RecordBiomeAttempt_СчитаетПопыткиИУбийства()
    {
        _meta.RecordBiomeAttempt(kills: 40);
        _meta.RecordBiomeAttempt(kills: 15);

        Assert.AreEqual(2, _meta.Progress.totalRuns);
        Assert.AreEqual(55, _meta.Progress.totalKills);
    }
```

- [ ] **Шаг 9: Запустить тесты**

Ожидаемо: `CampaignRewardsTests` (6) и `CampaignRulesTests` (6) зелёные,
`MetaProgressionTests` компилируется и проходит.

---

### Задача 5: `BiomeRun` — билд, живущий всю попытку биома

Апгрейды копятся через все узлы биома. Здесь же закрывается эксплойт: повторное
прохождение узла в той же попытке апгрейда не даёт, иначе игрок фармит первый
узел и приходит к боссу с полным билдом.

**Файлы:**
- Создать: `Assets/Scripts/App/BiomeRun.cs`
- Тест: `Assets/Tests/EditMode/BiomeRunTests.cs`

**Интерфейсы:**
- Использует: `PathogenData.CreateDefault(PathogenType)`, `PlayerStats(PathogenData)`,
  `MetaProgression.ApplyTo(PlayerStats)`, `CampaignNode.Id`
- Отдаёт: `BiomeRun` — конструктор `BiomeRun(string biomeId, PathogenData pathogen, PlayerStats stats)`,
  фабрика `BiomeRun.Create(string biomeId, PathogenData pathogen, MetaProgression meta) → BiomeRun`,
  свойства `BiomeId` (string), `Pathogen` (PathogenData), `Stats` (PlayerStats),
  `TotalKills` (int), `NodesCleared` (int),
  методы `ShouldGrantUpgrade(string nodeId) → bool`,
  `MarkUpgradeGranted(string nodeId)`, `RegisterClear(string nodeId, int kills)`

- [ ] **Шаг 1: Написать падающий тест**

Создать `Assets/Tests/EditMode/BiomeRunTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Билд попытки биома. Правило «апгрейд только за первое прохождение узла»
/// живёт здесь: без него первый узел становится фермой апгрейдов и босс
/// перестаёт быть проверкой билда.
/// </summary>
public class BiomeRunTests
{
    private static BiomeRun MakeRun()
    {
        PathogenData data = PathogenData.CreateDefault(PathogenType.Virus);
        return new BiomeRun(CampaignBuilder.BloodstreamId, data, new PlayerStats(data));
    }

    [Test]
    public void НовыйЗабег_ДаётАпгрейдЗаПервоеПрохождениеУзла()
    {
        BiomeRun run = MakeRun();

        Assert.IsTrue(run.ShouldGrantUpgrade("b1_n1"));
    }

    [Test]
    public void ПовторУзлаВТойЖеПопытке_АпгрейдаНеДаёт()
    {
        BiomeRun run = MakeRun();

        run.MarkUpgradeGranted("b1_n1");

        Assert.IsFalse(run.ShouldGrantUpgrade("b1_n1"),
            "Иначе первый узел фармится до полного билда");
        Assert.IsTrue(run.ShouldGrantUpgrade("b1_n2"), "Другие узлы это не затрагивает");
    }

    [Test]
    public void НоваяПопытка_СбрасываетВыданныеАпгрейды()
    {
        BiomeRun first = MakeRun();
        first.MarkUpgradeGranted("b1_n1");

        BiomeRun second = MakeRun();

        Assert.IsTrue(second.ShouldGrantUpgrade("b1_n1"),
            "Билд сгорает вместе с попыткой — заход с нуля даёт апгрейды заново");
    }

    [Test]
    public void RegisterClear_КопитУбийстваИСчётПройденных()
    {
        BiomeRun run = MakeRun();

        run.RegisterClear("b1_n1", kills: 30);
        run.RegisterClear("b1_n2", kills: 12);
        run.RegisterClear("b1_n1", kills: 5);

        Assert.AreEqual(47, run.TotalKills, "Убийства считаются и за повторы");
        Assert.AreEqual(2, run.NodesCleared, "Уникальных пройденных узлов — два");
    }

    [Test]
    public void Create_НакладываетПерманентныеУлучшенияНаСтартовыеСтаты()
    {
        var go = new GameObject("Meta");
        try
        {
            var meta = go.AddComponent<MetaProgression>();
            meta.Initialize(new FakeStore());
            meta.Progress.SetPerkLevel("perk_hp", 2);

            PathogenData data = PathogenData.CreateDefault(PathogenType.Bacteria);
            BiomeRun run = BiomeRun.Create(CampaignBuilder.BloodstreamId, data, meta);

            Assert.AreEqual(data.maxHealth + 16f, run.Stats.MaxHealth, 0.001f,
                "+8 здоровья за уровень перка, куплено 2");
            Assert.AreEqual(PathogenType.Bacteria, run.Stats.Type);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private class FakeStore : IProgressStore
    {
        private PlayerProgress _stored = new PlayerProgress();
        public PlayerProgress Load() => _stored;
        public void Save(PlayerProgress progress) => _stored = progress;
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `BiomeRun` не существует.

- [ ] **Шаг 3: Создать `BiomeRun.cs`**

```csharp
using System.Collections.Generic;

/// <summary>
/// Активная попытка биома: билд, который копится через все его узлы и сгорает
/// при смерти или выходе из биома. Босс — проверка того, что игрок собрал,
/// поэтому апгрейды не сбрасываются между узлами.
///
/// Живёт только в памяти. Если ОС убьёт приложение посреди биома, билд пропадёт,
/// а пройденные узлы останутся — известное ограничение, чинится сериализацией
/// PlayerStats и состояния UpgradeSystem отдельной задачей.
/// </summary>
public class BiomeRun
{
    public readonly string BiomeId;
    public readonly PathogenData Pathogen;
    public readonly PlayerStats Stats;

    public int TotalKills { get; private set; }
    public int NodesCleared => _clearedNodes.Count;

    /// <summary>
    /// Узлы, за которые апгрейд уже выдан в этой попытке. Без этого множества
    /// игрок перепроходит первый узел и собирает полный билд, ни разу не
    /// столкнувшись с растущей сложностью.
    /// </summary>
    private readonly HashSet<string> _upgradedNodes = new HashSet<string>();

    private readonly HashSet<string> _clearedNodes = new HashSet<string>();

    public BiomeRun(string biomeId, PathogenData pathogen, PlayerStats stats)
    {
        BiomeId = biomeId;
        Pathogen = pathogen;
        Stats = stats;
    }

    /// <summary>
    /// Собрать попытку: стартовые статы патогена плюс купленные перманентные
    /// улучшения. Перки накладываются здесь, до создания игрока, — здоровье
    /// конфигурируется из Stats.MaxHealth и позже уже не пересчитывается.
    /// </summary>
    public static BiomeRun Create(string biomeId, PathogenData pathogen, MetaProgression meta)
    {
        var stats = new PlayerStats(pathogen);
        if (meta != null)
        {
            meta.ApplyTo(stats);
        }

        return new BiomeRun(biomeId, pathogen, stats);
    }

    public bool ShouldGrantUpgrade(string nodeId) => !_upgradedNodes.Contains(nodeId);

    public void MarkUpgradeGranted(string nodeId) => _upgradedNodes.Add(nodeId);

    public void RegisterClear(string nodeId, int kills)
    {
        TotalKills += kills;
        _clearedNodes.Add(nodeId);
    }
}
```

- [ ] **Шаг 4: Запустить тесты**

Ожидаемо: 5 новых тестов проходят.

---

### Задача 6: `GameRunner` — бой одного узла вместо бесконечного забега

Здесь исчезает зацикливание уровней и появляется победа. `GameRunner` перестаёт
знать про апгрейды и метапрогрессию: решение «давать ли апгрейд» зависит от
состояния попытки биома, а это дело `AppFlow`.

**Файлы:**
- Изменить: `Assets/Scripts/Core/GameRunner.cs` (переписывается почти целиком)
- Изменить: `Assets/Tests/EditMode/CampaignAndCombatTests.cs`
- Тест: `Assets/Tests/EditMode/NodeOutcomeTests.cs`

**Интерфейсы:**
- Использует: `PoolHub`, `EnemySpawner`, `DifficultyDirector`, `BiomeRun`,
  `CampaignNode`, `PlayerController`, `PlayerWeapon`, `PlayerMutations`,
  `DamageReduction`, `Health`, `Battlefield`, `PlaceholderArt`
- Отдаёт: `NodeOutcome` (структура: `Node`, `Cleared`, `ElapsedSeconds`, `Kills`),
  `GameRunner.Initialize(PoolHub, EnemySpawner, DifficultyDirector)`,
  `GameRunner.StartNode(CampaignNode, BiomeRun)`, `GameRunner.AbortNode()`,
  `GameRunner.NodeFinished` (event `Action<NodeOutcome>`),
  `GameRunner.IsRunning` (bool), `GameRunner.CurrentNode` (CampaignNode),
  `GameRunner.Player` (PlayerController), `GameRunner.Stats` (PlayerStats),
  `GameRunner.ElapsedSeconds` (float)

- [ ] **Шаг 1: Написать падающий тест исхода**

Создать `Assets/Tests/EditMode/NodeOutcomeTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Исход узла. Проверяется без запуска боя: NodeOutcome — это контракт между
/// GameRunner и экраном результата, и перепутанные поля тихо испортят звёзды.
/// </summary>
public class NodeOutcomeTests
{
    private static CampaignNode Node()
    {
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.advanceType = AdvanceType.Waves;
        return new CampaignNode("b1_n1", "Тестовый", level, 0, Vector2.zero, 20, 15);
    }

    [Test]
    public void Победа_НесётВремяИУбийства()
    {
        var outcome = new NodeOutcome(Node(), cleared: true, elapsedSeconds: 42.5f, kills: 61);

        Assert.IsTrue(outcome.Cleared);
        Assert.AreEqual(42.5f, outcome.ElapsedSeconds, 0.001f);
        Assert.AreEqual(61, outcome.Kills);
        Assert.AreEqual("b1_n1", outcome.Node.Id);
    }

    [Test]
    public void Провал_НеДаётЗвёзд()
    {
        var outcome = new NodeOutcome(Node(), cleared: false, elapsedSeconds: 8f, kills: 3);

        Assert.AreEqual(0, outcome.Stars, "Звёзды за провал не начисляются никогда");
    }

    [Test]
    public void Победа_ОцениваетсяПоПарТайму()
    {
        CampaignNode node = Node();
        float par = StarRating.ParTime(node);

        Assert.AreEqual(3, new NodeOutcome(node, true, par, 10).Stars);
        Assert.AreEqual(1, new NodeOutcome(node, true, par * 5f, 10).Stars);
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `NodeOutcome` не существует.

- [ ] **Шаг 3: Переписать `GameRunner.cs`**

Полностью заменить содержимое `Assets/Scripts/Core/GameRunner.cs`:

```csharp
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
```

- [ ] **Шаг 4: Починить `CampaignAndCombatTests.cs`**

Шесть тестов обращаются к `CampaignGenerator.BuildBloodstream(8)` и
`FindFirstBossLevel`. Заменить получение списка уровней. Было:

```csharp
        List<LevelData> levels = CampaignGenerator.BuildBloodstream(8);
```

Стало (во всех пяти местах):

```csharp
        List<LevelData> levels = LevelsOfFirstBiome();
```

и добавить в класс вспомогательный метод:

```csharp
    private static List<LevelData> LevelsOfFirstBiome()
    {
        var levels = new List<LevelData>();
        foreach (CampaignNode node in CampaignBuilder.Build().Biomes[0].Nodes)
        {
            levels.Add(node.Level);
        }
        return levels;
    }
```

Тест `Assert.AreEqual(7, CampaignGenerator.FindFirstBossLevel(levels));` заменить на:

```csharp
        Assert.AreEqual(7, CampaignBuilder.BloodstreamNodes - 1, "Босс — последний узел биома");
        Assert.IsTrue(levels[levels.Count - 1].advanceType == AdvanceType.Boss);
```

- [ ] **Шаг 5: Запустить тесты**

Ожидаемо: `NodeOutcomeTests` (3) зелёные, `CampaignAndCombatTests` компилируется
и проходит. Проект временно не собирается целиком — `GameHud` ссылается на
удалённые `GameRunner.State`, `StartRun`, `RestartToSelect`. Это чинится в
задаче 10; чтобы прогнать EditMode-тесты прямо сейчас, временно закомментировать
создание `GameHud` в `GameBootstrap` и переименовать `Assets/Scripts/UI/GameHud.cs`
в `GameHud.cs.disabled` вместе с его `.meta`.

---

### Задача 7: Ядро UI — экраны и стек

Каркас, в который втыкается всё остальное. Ключевое отличие от текущего
`GameHud`: тикается только видимый экран, а не все пять сразу.

**Файлы:**
- Создать: `Assets/Scripts/UI/Core/UiScreen.cs`
- Создать: `Assets/Scripts/UI/Core/ScreenStack.cs`
- Изменить: `Assets/Scripts/UI/UiFactory.cs` (добавить слайдер, поле ввода, помощники)
- Тест: `Assets/Tests/EditMode/ScreenStackTests.cs`

**Интерфейсы:**
- Использует: `UiFactory.CreateFullScreen`, `UiFactory.CreateImage`, `UiFactory.CreateText`
- Отдаёт: `UiScreen` (абстрактный: `Root`, `IsVisible`, `Build(Transform)`,
  `Show()`, `Hide()`, `Tick()`, защищённые `OnBuild()`, `OnShow()`, `OnHide()`, `OnTick()`),
  `ScreenStack` (`ScreenStack(Transform screenRoot, Transform modalRoot = null)`,
  `Register(UiScreen)`, `RegisterModal(UiScreen)`, `Show(UiScreen)`, `Current`,
  `PushModal(UiScreen)`, `PopModal()`, `ModalDepth` (int), `Tick()`),
  `UiFactory.CreateSlider(...) → Slider`, `UiFactory.CreateInputField(...) → InputField`,
  `UiFactory.Stretch(RectTransform)`, `UiFactory.StretchWithPadding(RectTransform, float, float)`,
  `UiFactory.BottomAnchored(RectTransform, float, float, float) → RectTransform`

- [ ] **Шаг 1: Написать падающий тест**

Создать `Assets/Tests/EditMode/ScreenStackTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Навигация. Проверяется именно то, что раньше делал GameHud вручную и
/// каждый кадр: кто видим и кого тикать.
/// </summary>
public class ScreenStackTests
{
    private GameObject _root;
    private ScreenStack _stack;

    private class ProbeScreen : UiScreen
    {
        public int Shows;
        public int Hides;
        public int Ticks;

        protected override void OnBuild() { }
        protected override void OnShow() => Shows++;
        protected override void OnHide() => Hides++;
        protected override void OnTick() => Ticks++;
    }

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Root", typeof(RectTransform));
        _stack = new ScreenStack(_root.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    private ProbeScreen Registered()
    {
        var screen = new ProbeScreen();
        _stack.Register(screen);
        return screen;
    }

    [Test]
    public void Register_СтроитЭкранИПрячетЕго()
    {
        ProbeScreen screen = Registered();

        Assert.IsNotNull(screen.Root, "Экран обязан построиться при регистрации");
        Assert.IsFalse(screen.IsVisible, "Свежий экран не должен перекрывать текущий");
    }

    [Test]
    public void Show_ПоказываетНовыйИПрячетПрежний()
    {
        ProbeScreen first = Registered();
        ProbeScreen second = Registered();

        _stack.Show(first);
        _stack.Show(second);

        Assert.IsFalse(first.IsVisible);
        Assert.IsTrue(second.IsVisible);
        Assert.AreEqual(1, first.Hides);
        Assert.AreEqual(1, second.Shows);
        Assert.AreSame(second, _stack.Current);
    }

    [Test]
    public void Show_ПовторныйПоказТогоЖеЭкранаНеДёргаетСобытия()
    {
        ProbeScreen screen = Registered();

        _stack.Show(screen);
        _stack.Show(screen);

        Assert.AreEqual(1, screen.Shows, "Повторный Show не должен перестраивать экран");
    }

    [Test]
    public void Tick_ИдётТолькоВВидимыйЭкран()
    {
        ProbeScreen visible = Registered();
        ProbeScreen hidden = Registered();
        _stack.Show(visible);

        _stack.Tick();

        Assert.AreEqual(1, visible.Ticks);
        Assert.AreEqual(0, hidden.Ticks, "Невидимый экран не должен тратить кадр");
    }

    [Test]
    public void Модалка_ПерекрываетЭкранНоНеПрячетЕго()
    {
        ProbeScreen screen = Registered();
        ProbeScreen modal = Registered();
        _stack.Show(screen);

        _stack.PushModal(modal);

        Assert.IsTrue(screen.IsVisible, "Экран под модалкой остаётся виден");
        Assert.IsTrue(modal.IsVisible);
        Assert.AreEqual(1, _stack.ModalDepth);
    }

    [Test]
    public void Tick_ПриОткрытойМодалкеИдётТолькоВНеё()
    {
        ProbeScreen screen = Registered();
        ProbeScreen modal = Registered();
        _stack.Show(screen);
        _stack.PushModal(modal);

        _stack.Tick();

        Assert.AreEqual(1, modal.Ticks);
        Assert.AreEqual(0, screen.Ticks, "Под модалкой экран не обновляется");
    }

    [Test]
    public void PopModal_ВозвращаетУправлениеЭкрану()
    {
        ProbeScreen screen = Registered();
        ProbeScreen modal = Registered();
        _stack.Show(screen);
        _stack.PushModal(modal);

        _stack.PopModal();
        _stack.Tick();

        Assert.IsFalse(modal.IsVisible);
        Assert.AreEqual(0, _stack.ModalDepth);
        Assert.AreEqual(1, screen.Ticks);
    }

    [Test]
    public void PopModal_НаПустомСтекеНеПадает()
    {
        Assert.DoesNotThrow(() => _stack.PopModal());
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `UiScreen` и `ScreenStack` не существуют.

- [ ] **Шаг 3: Создать `UiScreen.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Один экран интерфейса. Строится ровно один раз, дальше только включается
/// и выключается: пересборка иерархии на каждый показ — самый дорогой способ
/// сменить картинку.
/// </summary>
public abstract class UiScreen
{
    public RectTransform Root { get; private set; }

    public bool IsVisible => Root != null && Root.gameObject.activeSelf;

    public void Build(Transform parent)
    {
        if (Root != null)
        {
            return;
        }

        Root = UiFactory.CreateFullScreen(GetType().Name, parent);
        OnBuild();
        Root.gameObject.SetActive(false);
    }

    public void Show()
    {
        if (Root == null || IsVisible)
        {
            return;
        }

        Root.gameObject.SetActive(true);
        // Свежепоказанный экран поднимается наверх: модалки и экраны живут
        // в одном родителе, и порядок в иерархии решает, кто кого перекрывает.
        Root.SetAsLastSibling();
        OnShow();
    }

    public void Hide()
    {
        if (Root == null || !IsVisible)
        {
            return;
        }

        OnHide();
        Root.gameObject.SetActive(false);
    }

    /// <summary>Вызывается только для видимого экрана — см. ScreenStack.Tick.</summary>
    public void Tick()
    {
        if (IsVisible)
        {
            OnTick();
        }
    }

    protected abstract void OnBuild();

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }

    protected virtual void OnTick() { }
}
```

- [ ] **Шаг 4: Создать `ScreenStack.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Кто сейчас на экране. Модалки лежат отдельным стеком поверх текущего экрана:
/// настройки и подтверждения не должны выбивать игрока из того, что под ними.
/// </summary>
public class ScreenStack
{
    private readonly Transform _screenRoot;

    /// <summary>
    /// Отдельный родитель для модалок. Между ним и экранами лежит шапка с
    /// таб-баром: экран должен быть под ней, а модалка — над.
    /// </summary>
    private readonly Transform _modalRoot;

    private readonly List<UiScreen> _registered = new List<UiScreen>();
    private readonly List<UiScreen> _modals = new List<UiScreen>();

    public ScreenStack(Transform screenRoot, Transform modalRoot = null)
    {
        _screenRoot = screenRoot;
        _modalRoot = modalRoot != null ? modalRoot : screenRoot;
    }

    public UiScreen Current { get; private set; }

    public int ModalDepth => _modals.Count;

    public void Register(UiScreen screen)
    {
        Register(screen, _screenRoot);
    }

    public void RegisterModal(UiScreen modal)
    {
        Register(modal, _modalRoot);
    }

    private void Register(UiScreen screen, Transform parent)
    {
        if (screen == null || _registered.Contains(screen))
        {
            return;
        }

        screen.Build(parent);
        _registered.Add(screen);
    }

    public void Show(UiScreen screen)
    {
        if (screen == null || Current == screen)
        {
            return;
        }

        Register(screen);

        if (Current != null)
        {
            Current.Hide();
        }

        Current = screen;
        screen.Show();
    }

    public void PushModal(UiScreen modal)
    {
        if (modal == null || _modals.Contains(modal))
        {
            return;
        }

        RegisterModal(modal);
        _modals.Add(modal);
        modal.Show();
    }

    public void PopModal()
    {
        if (_modals.Count == 0)
        {
            return;
        }

        UiScreen top = _modals[_modals.Count - 1];
        _modals.RemoveAt(_modals.Count - 1);
        top.Hide();
    }

    /// <summary>
    /// Обновляется только верхний видимый слой. В GameHud обновлялись все экраны
    /// каждый кадр, включая выключенные, — это и был основной холостой расход.
    /// </summary>
    public void Tick()
    {
        if (_modals.Count > 0)
        {
            _modals[_modals.Count - 1].Tick();
            return;
        }

        if (Current != null)
        {
            Current.Tick();
        }
    }
}
```

- [ ] **Шаг 5: Дополнить `UiFactory.cs`**

Добавить в конец класса `UiFactory` (перед закрывающей скобкой):

```csharp
    /// <summary>Растянуть элемент по всему родителю.</summary>
    public static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static RectTransform StretchWithPadding(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
        return rect;
    }

    /// <summary>Элемент, прибитый к нижнему краю. Нужен таб-бару и кнопкам действий.</summary>
    public static RectTransform BottomAnchored(RectTransform rect, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    /// <summary>
    /// Слайдер без ручки: на телефоне полоса заполнения читается лучше, чем
    /// мелкий кружок, и по ней можно попасть пальцем не целясь.
    /// </summary>
    public static Slider CreateSlider(string name, Transform parent, float value, Color fillColor)
    {
        Image background = CreateImage(name + "Background", parent, new Color(0f, 0f, 0f, 0.55f));
        var slider = background.gameObject.AddComponent<Slider>();

        RectTransform fillArea = CreateRect(name + "FillArea", background.transform);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(4f, 4f);
        fillArea.offsetMax = new Vector2(-4f, -4f);

        Image fill = CreateImage(name + "Fill", fillArea, fillColor);
        Stretch(fill.rectTransform);

        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = background;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        return slider;
    }

    /// <summary>
    /// Поле ввода на legacy InputField — по той же причине, что и весь текст:
    /// TMP_InputField тянет за собой импорт TMP Essentials.
    /// </summary>
    public static InputField CreateInputField(string name, Transform parent, string value, string placeholder)
    {
        Image background = CreateImage(name + "Background", parent, new Color(0.92f, 0.93f, 0.96f));
        var field = background.gameObject.AddComponent<InputField>();

        Text text = CreateText(name + "Text", background.transform, value, 24, TextAnchor.MiddleLeft);
        text.color = new Color(0.06f, 0.06f, 0.08f);
        text.supportRichText = false;
        StretchWithPadding(text.rectTransform, 14f, 6f);

        Text hint = CreateText(name + "Placeholder", background.transform, placeholder, 24, TextAnchor.MiddleLeft);
        hint.color = new Color(0.40f, 0.40f, 0.45f);
        hint.fontStyle = FontStyle.Italic;
        StretchWithPadding(hint.rectTransform, 14f, 6f);

        field.textComponent = text;
        field.placeholder = hint;
        field.targetGraphic = background;
        field.characterLimit = 16;
        field.lineType = InputField.LineType.SingleLine;
        field.SetTextWithoutNotify(value);
        return field;
    }
```

- [ ] **Шаг 6: Запустить тесты**

Ожидаемо: 8 тестов `ScreenStackTests` зелёные.

---

### Задача 8: Настройки, звук, сплэш и подтверждение

Три модалки и заставка. Звук — только проводка: слышать пока нечего.

**Файлы:**
- Создать: `Assets/Scripts/Meta/AudioService.cs`
- Создать: `Assets/Scripts/UI/Shell/SettingsModal.cs`
- Создать: `Assets/Scripts/UI/Shell/ConfirmModal.cs`
- Создать: `Assets/Scripts/UI/Shell/SplashScreen.cs`
- Тест: `Assets/Tests/EditMode/AudioServiceTests.cs`

**Интерфейсы:**
- Использует: `GameSettings`, `MetaProgression.Progress`, `MetaProgression.Save()`,
  `UiScreen`, `UiFactory`, `ScreenStack.PopModal()`
- Отдаёт: `AudioService.Apply(GameSettings)`, `AudioService.MusicVolume` (float),
  `AudioService.SfxVolume` (float), `AudioService.MasterVolume` (float);
  `SettingsModal(MetaProgression meta, ScreenStack stack)`;
  `ConfirmModal(ScreenStack stack)` с методом
  `Ask(string title, string body, string confirmLabel, Action onConfirm)`;
  `SplashScreen(Action onFinished)` со свойством `MinimumSeconds` (const float 1.2)

- [ ] **Шаг 1: Написать падающий тест**

Создать `Assets/Tests/EditMode/AudioServiceTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Проводка звука. Звуков в проекте ещё нет — проверяется ровно то, что
/// сохранённые значения доезжают до движка и не выходят за границы.
/// </summary>
public class AudioServiceTests
{
    [TearDown]
    public void TearDown()
    {
        AudioService.Apply(new GameSettings());
    }

    [Test]
    public void Apply_КладётОбщуюГромкостьВAudioListener()
    {
        AudioService.Apply(new GameSettings { masterVolume = 0.25f });

        Assert.AreEqual(0.25f, AudioListener.volume, 0.001f);
        Assert.AreEqual(0.25f, AudioService.MasterVolume, 0.001f);
    }

    [Test]
    public void Apply_ЗапоминаетГромкостиМузыкиИЭффектов()
    {
        AudioService.Apply(new GameSettings { musicVolume = 0.4f, sfxVolume = 0.7f });

        Assert.AreEqual(0.4f, AudioService.MusicVolume, 0.001f);
        Assert.AreEqual(0.7f, AudioService.SfxVolume, 0.001f);
    }

    [Test]
    public void Apply_ЗажимаетЗначенияВДиапазон()
    {
        AudioService.Apply(new GameSettings { masterVolume = 5f, musicVolume = -3f });

        Assert.AreEqual(1f, AudioService.MasterVolume, 0.001f);
        Assert.AreEqual(0f, AudioService.MusicVolume, 0.001f);
    }

    [Test]
    public void Apply_НаNullНеПадаетИСтавитЗначенияПоУмолчанию()
    {
        Assert.DoesNotThrow(() => AudioService.Apply(null));
        Assert.AreEqual(1f, AudioService.MasterVolume, 0.001f);
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `AudioService` не существует.

- [ ] **Шаг 3: Создать `AudioService.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Громкости из настроек. AudioMixer здесь не подходит: это ассет проекта,
/// его нельзя создать в рантайме, а сцена у нас собирается кодом. Поэтому общая
/// громкость идёт в AudioListener, а музыка и эффекты отдаются наружу
/// множителями — их будут читать источники звука, когда те появятся в Фазе 2.5.
/// </summary>
public static class AudioService
{
    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    public static void Apply(GameSettings settings)
    {
        if (settings == null)
        {
            settings = new GameSettings();
        }

        MasterVolume = Mathf.Clamp01(settings.masterVolume);
        MusicVolume = Mathf.Clamp01(settings.musicVolume);
        SfxVolume = Mathf.Clamp01(settings.sfxVolume);

        AudioListener.volume = MasterVolume;
    }
}
```

- [ ] **Шаг 4: Создать `SettingsModal.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Настройки поверх текущего экрана. Каждое изменение сразу применяется и
/// сохраняется: отдельная кнопка «Применить» на мобиле только теряет ввод.
/// </summary>
public class SettingsModal : UiScreen
{
    private readonly MetaProgression _meta;
    private readonly ScreenStack _stack;

    private Slider _master;
    private Slider _music;
    private Slider _sfx;
    private InputField _name;

    public SettingsModal(MetaProgression meta, ScreenStack stack)
    {
        _meta = meta;
        _stack = stack;
    }

    private GameSettings Settings => _meta.Progress.settings;

    protected override void OnBuild()
    {
        Image dim = UiFactory.CreateImage("Dim", Root, new Color(0f, 0f, 0f, 0.80f));
        UiFactory.Stretch(dim.rectTransform);
        // Затемнение должно ловить клики, иначе кнопки под модалкой останутся живыми.
        dim.raycastTarget = true;

        Image panel = UiFactory.CreateImage("Panel", Root, new Color(0.14f, 0.15f, 0.19f));
        UiFactory.TopAnchored(panel.rectTransform, 260f, UiFactory.ContentWidth, 660f);

        Text title = UiFactory.CreateText("Title", panel.transform, "НАСТРОЙКИ", 32,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 24f, UiFactory.ContentWidth - 40f, 50f);

        _master = BuildVolumeRow("Общая громкость", 100f, Settings.masterVolume,
            new Color(0.55f, 0.80f, 0.95f), panel.transform, v => { Settings.masterVolume = v; ApplyAndSave(); });

        _music = BuildVolumeRow("Музыка", 200f, Settings.musicVolume,
            new Color(0.65f, 0.60f, 0.95f), panel.transform, v => { Settings.musicVolume = v; ApplyAndSave(); });

        _sfx = BuildVolumeRow("Эффекты", 300f, Settings.sfxVolume,
            new Color(0.55f, 0.90f, 0.65f), panel.transform, v => { Settings.sfxVolume = v; ApplyAndSave(); });

        Text hint = UiFactory.CreateText("SoundHint", panel.transform,
            "Звуки появятся вместе с артом — сейчас настройка только запоминается.", 18,
            TextAnchor.UpperCenter);
        hint.color = new Color(0.65f, 0.66f, 0.72f);
        UiFactory.TopAnchored(hint.rectTransform, 372f, UiFactory.ContentWidth - 60f, 48f);

        Text nameLabel = UiFactory.CreateText("NameLabel", panel.transform, "Имя игрока", 24);
        UiFactory.TopAnchored(nameLabel.rectTransform, 434f, UiFactory.ContentWidth - 60f, 34f);

        _name = UiFactory.CreateInputField("PlayerName", panel.transform, Settings.playerName, "без имени");
        UiFactory.TopAnchored((RectTransform)_name.transform, 472f, UiFactory.ContentWidth - 60f, 62f);
        _name.onEndEdit.AddListener(value =>
        {
            Settings.playerName = value.Trim();
            _meta.Save();
        });

        Button close = UiFactory.CreateButton("Close", panel.transform, "Готово", 28,
            new Color(0.55f, 0.80f, 0.60f), out _);
        UiFactory.TopAnchored((RectTransform)close.transform, 556f, UiFactory.ContentWidth - 60f, 76f);
        close.onClick.AddListener(() => _stack.PopModal());
    }

    private Slider BuildVolumeRow(string label, float y, float value, Color fill, Transform parent,
        UnityEngine.Events.UnityAction<float> onChanged)
    {
        Text caption = UiFactory.CreateText(label + "Label", parent, label, 22);
        UiFactory.TopAnchored(caption.rectTransform, y, UiFactory.ContentWidth - 60f, 30f);

        Slider slider = UiFactory.CreateSlider(label + "Slider", parent, value, fill);
        UiFactory.TopAnchored((RectTransform)slider.transform, y + 34f, UiFactory.ContentWidth - 60f, 40f);
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    private void ApplyAndSave()
    {
        AudioService.Apply(Settings);
        _meta.Save();
    }

    protected override void OnShow()
    {
        // Значения могли поменяться сбросом прогресса — перечитываем при каждом показе.
        _master.SetValueWithoutNotify(Settings.masterVolume);
        _music.SetValueWithoutNotify(Settings.musicVolume);
        _sfx.SetValueWithoutNotify(Settings.sfxVolume);
        _name.SetTextWithoutNotify(Settings.playerName);
    }
}
```

- [ ] **Шаг 5: Создать `ConfirmModal.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Подтверждение необратимого действия. Одна переиспользуемая модалка на всё:
/// выход из биома со сгоранием билда и сброс прогресса на плейтестах.
/// </summary>
public class ConfirmModal : UiScreen
{
    private readonly ScreenStack _stack;

    private Text _title;
    private Text _body;
    private Text _confirmLabel;
    private Action _onConfirm;

    public ConfirmModal(ScreenStack stack)
    {
        _stack = stack;
    }

    public void Ask(string title, string body, string confirmLabel, Action onConfirm)
    {
        _onConfirm = onConfirm;
        _title.text = title;
        _body.text = body;
        _confirmLabel.text = confirmLabel;
        _stack.PushModal(this);
    }

    protected override void OnBuild()
    {
        Image dim = UiFactory.CreateImage("Dim", Root, new Color(0f, 0f, 0f, 0.82f));
        UiFactory.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image panel = UiFactory.CreateImage("Panel", Root, new Color(0.16f, 0.13f, 0.15f));
        UiFactory.TopAnchored(panel.rectTransform, 420f, UiFactory.ContentWidth, 400f);

        _title = UiFactory.CreateText("Title", panel.transform, string.Empty, 30,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(_title.rectTransform, 26f, UiFactory.ContentWidth - 40f, 50f);

        _body = UiFactory.CreateText("Body", panel.transform, string.Empty, 22, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(_body.rectTransform, 88f, UiFactory.ContentWidth - 60f, 120f);

        Button confirm = UiFactory.CreateButton("Confirm", panel.transform, string.Empty, 26,
            new Color(0.88f, 0.48f, 0.45f), out _confirmLabel);
        UiFactory.TopAnchored((RectTransform)confirm.transform, 218f, UiFactory.ContentWidth - 60f, 72f);
        confirm.onClick.AddListener(() =>
        {
            Action action = _onConfirm;
            _stack.PopModal();
            action?.Invoke();
        });

        Button cancel = UiFactory.CreateButton("Cancel", panel.transform, "Отмена", 26,
            new Color(0.62f, 0.64f, 0.70f), out _);
        UiFactory.TopAnchored((RectTransform)cancel.transform, 302f, UiFactory.ContentWidth - 60f, 72f);
        cancel.onClick.AddListener(() => _stack.PopModal());
    }
}
```

- [ ] **Шаг 6: Создать `SplashScreen.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Заставка при запуске. Картинки ещё нет — на её месте заливка и название,
/// но слот под спрайт сделан сразу: в Фазе 2.5 меняется только Sprite у Art.
///
/// Минимальное время показа нужно не для красоты: инициализация на быстром
/// устройстве заканчивается за один кадр, и экран мигнул бы, а не показался.
/// </summary>
public class SplashScreen : UiScreen
{
    public const float MinimumSeconds = 1.2f;

    private readonly Action _onFinished;

    private Image _progressFill;
    private float _elapsed;
    private bool _done;

    public SplashScreen(Action onFinished)
    {
        _onFinished = onFinished;
    }

    /// <summary>Место под будущую заставку: подставить спрайт и убрать заливку.</summary>
    public Image Art { get; private set; }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.10f, 0.04f, 0.06f));
        UiFactory.Stretch(backdrop.rectTransform);

        Art = UiFactory.CreateImage("Art", Root, new Color(0.55f, 0.16f, 0.22f));
        UiFactory.TopAnchored(Art.rectTransform, 300f, UiFactory.ContentWidth, 460f);

        Text title = UiFactory.CreateText("Title", Root, "ПАТОГЕН\nvs\nИММУННАЯ СИСТЕМА", 44,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 430f, UiFactory.ContentWidth, 200f);

        _progressFill = UiFactory.CreateBar("Progress", Root, new Color(0.85f, 0.35f, 0.40f),
            out Image background);
        UiFactory.BottomAnchored(background.rectTransform, 160f, UiFactory.ContentWidth, 22f);
        _progressFill.fillAmount = 0f;
    }

    protected override void OnShow()
    {
        _elapsed = 0f;
        _done = false;
        _progressFill.fillAmount = 0f;
    }

    protected override void OnTick()
    {
        if (_done)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;
        _progressFill.fillAmount = Mathf.Clamp01(_elapsed / MinimumSeconds);

        if (_elapsed >= MinimumSeconds)
        {
            _done = true;
            _onFinished?.Invoke();
        }
    }
}
```

- [ ] **Шаг 7: Запустить тесты**

Ожидаемо: 4 теста `AudioServiceTests` зелёные, остальные не сломаны.

---

### Задача 9: Оболочка — шапка, таб-бар, главный экран, улучшения, заглушки

Постоянная рамка приложения и три её страницы. Шапка и таб-бар живут в
отдельном слое между экранами и модалками: экран под ними, модалка над.

**Файлы:**
- Создать: `Assets/Scripts/App/AppTab.cs`
- Создать: `Assets/Scripts/UI/Shell/ShellChrome.cs`
- Создать: `Assets/Scripts/UI/Shell/HomeScreen.cs`
- Создать: `Assets/Scripts/UI/Shell/UpgradesScreen.cs`
- Создать: `Assets/Scripts/UI/Shell/StubScreen.cs`
- Тест: `Assets/Tests/EditMode/PathogenCarouselTests.cs`

**Интерфейсы:**
- Использует: `UiScreen`, `UiFactory`, `MetaProgression`, `PermanentUpgrade`,
  `PathogenData.CreateDefault`, `PathogenType`, `PlayerProgress.lastPathogen`
- Отдаёт: `AppTab` (enum: `Upgrades`, `Wardrobe`, `Campaign`, `Battle`),
  `PathogenCarousel.Types` (static readonly `PathogenType[]`),
  `PathogenCarousel.IndexOf(string) → int`, `PathogenCarousel.Shift(int index, int delta) → int`,
  `ShellChrome(Transform parent, MetaProgression meta, Action onHome, Action onSettings, Action<AppTab> onTab)`
  с методами `Refresh()`, `SetActiveTab(AppTab)`, `SetVisible(bool)` и статическим
  свойством `BottomInset` (float, = 104),
  `HomeScreen(MetaProgression meta, Action<PathogenType> onPathogenChanged)`
  со свойством `Selected` (PathogenType),
  `UpgradesScreen(MetaProgression meta, ConfirmModal confirm)`,
  `StubScreen(string title, string body)`

- [ ] **Шаг 1: Написать падающий тест карусели**

Создать `Assets/Tests/EditMode/PathogenCarouselTests.cs`:

```csharp
using NUnit.Framework;

/// <summary>
/// Перелистывание патогенов. Арифметика вынесена из экрана, потому что
/// заворачивание на краях — единственное, что здесь можно сломать незаметно.
/// </summary>
public class PathogenCarouselTests
{
    [Test]
    public void Types_СодержитВсеЧетыреПатогена()
    {
        Assert.AreEqual(4, PathogenCarousel.Types.Length);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Virus);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Bacteria);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Fungus);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Parasite);
    }

    [Test]
    public void Shift_ЛистаетВпередИНазад()
    {
        Assert.AreEqual(1, PathogenCarousel.Shift(0, 1));
        Assert.AreEqual(0, PathogenCarousel.Shift(1, -1));
    }

    [Test]
    public void Shift_ЗаворачиваетсяНаОбоихКраях()
    {
        int last = PathogenCarousel.Types.Length - 1;

        Assert.AreEqual(0, PathogenCarousel.Shift(last, 1), "С последнего вперёд — на первый");
        Assert.AreEqual(last, PathogenCarousel.Shift(0, -1), "С первого назад — на последний");
    }

    [Test]
    public void IndexOf_НаходитПоИмениИПадаетВНольНаМусоре()
    {
        Assert.AreEqual(0, PathogenCarousel.IndexOf("Virus"));
        Assert.AreEqual(2, PathogenCarousel.IndexOf("Fungus"));
        Assert.AreEqual(0, PathogenCarousel.IndexOf("нет_такого"),
            "Испорченный сейв не должен ронять главный экран");
        Assert.AreEqual(0, PathogenCarousel.IndexOf(null));
    }
}
```

- [ ] **Шаг 2: Запустить тест и убедиться, что он падает**

Ожидаемо: не компилируется — `PathogenCarousel` не существует.

- [ ] **Шаг 3: Создать `AppTab.cs` вместе с `PathogenCarousel`**

```csharp
using System;

/// <summary>Разделы нижнего таб-бара, в порядке их показа слева направо.</summary>
public enum AppTab
{
    Upgrades,
    Wardrobe,
    Campaign,
    Battle
}

/// <summary>
/// Перелистывание патогенов на главном экране. Отдельно от экрана: заворачивание
/// на краях и разбор имени из сейва — единственная логика, которую тут можно
/// сломать так, что визуально это заметят не сразу.
/// </summary>
public static class PathogenCarousel
{
    public static readonly PathogenType[] Types =
    {
        PathogenType.Virus, PathogenType.Bacteria, PathogenType.Fungus, PathogenType.Parasite
    };

    public static int Shift(int index, int delta)
    {
        int count = Types.Length;
        return ((index + delta) % count + count) % count;
    }

    /// <summary>Индекс по имени значения из сейва. Мусор превращается в первого патогена.</summary>
    public static int IndexOf(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return 0;
        }

        for (int i = 0; i < Types.Length; i++)
        {
            if (string.Equals(Types[i].ToString(), typeName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }
}
```

- [ ] **Шаг 4: Создать `ShellChrome.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Постоянная рамка приложения: шапка с валютами и шестерёнкой сверху,
/// четыре вкладки снизу. Не UiScreen — она переживает смену экранов и лежит
/// в собственном слое между экранами и модалками.
/// </summary>
public class ShellChrome
{
    private static readonly AppTab[] Tabs = { AppTab.Upgrades, AppTab.Wardrobe, AppTab.Campaign, AppTab.Battle };
    private static readonly string[] TabLabels = { "Улучшения", "Одежда", "Кампания", "Битва" };

    private const float TabBarHeight = 104f;

    private readonly MetaProgression _meta;
    private readonly RectTransform _root;
    private readonly Image[] _tabImages = new Image[4];

    private Text _gold;
    private Text _biomass;
    private AppTab _active = AppTab.Campaign;

    public ShellChrome(Transform parent, MetaProgression meta, Action onHome, Action onSettings, Action<AppTab> onTab)
    {
        _meta = meta;
        _root = UiFactory.CreateFullScreen("ShellChrome", parent);

        BuildHeader(onHome, onSettings);
        BuildTabBar(onTab);
        Refresh();
    }

    /// <summary>Высота таб-бара — экранам нужен нижний отступ, чтобы под него не залезать.</summary>
    public static float BottomInset => TabBarHeight;

    public void SetVisible(bool visible)
    {
        _root.gameObject.SetActive(visible);
    }

    public void Refresh()
    {
        PlayerProgress progress = _meta.Progress;
        _gold.text = $"Золото: {progress.gold}";
        _biomass.text = $"Биомасса: {progress.biomass}";
    }

    public void SetActiveTab(AppTab tab)
    {
        _active = tab;
        for (int i = 0; i < Tabs.Length; i++)
        {
            _tabImages[i].color = Tabs[i] == _active
                ? new Color(0.85f, 0.35f, 0.40f)
                : new Color(0.24f, 0.25f, 0.30f);
        }
    }

    private void BuildHeader(Action onHome, Action onSettings)
    {
        Image bar = UiFactory.CreateImage("Header", _root, new Color(0.10f, 0.11f, 0.14f, 0.95f));
        UiFactory.TopAnchored(bar.rectTransform, 0f, UiFactory.ReferenceResolution.x, 92f);

        Button settings = UiFactory.CreateButton("Settings", bar.transform, "⚙", 34,
            new Color(0.30f, 0.32f, 0.38f), out _);
        RectTransform settingsRect = UiFactory.TopAnchored((RectTransform)settings.transform, 14f, 64f, 64f);
        settingsRect.anchoredPosition = new Vector2(-(UiFactory.ReferenceResolution.x * 0.5f) + 52f, -14f);
        settings.onClick.AddListener(() => onSettings?.Invoke());

        Button home = UiFactory.CreateButton("Home", bar.transform, "⌂", 34,
            new Color(0.30f, 0.32f, 0.38f), out _);
        RectTransform homeRect = UiFactory.TopAnchored((RectTransform)home.transform, 14f, 64f, 64f);
        homeRect.anchoredPosition = new Vector2((UiFactory.ReferenceResolution.x * 0.5f) - 52f, -14f);
        home.onClick.AddListener(() => onHome?.Invoke());

        _gold = UiFactory.CreateText("Gold", bar.transform, string.Empty, 22, TextAnchor.MiddleCenter);
        RectTransform goldRect = UiFactory.TopAnchored(_gold.rectTransform, 14f, 240f, 30f);
        goldRect.anchoredPosition = new Vector2(0f, -14f);

        _biomass = UiFactory.CreateText("Biomass", bar.transform, string.Empty, 22, TextAnchor.MiddleCenter);
        RectTransform biomassRect = UiFactory.TopAnchored(_biomass.rectTransform, 48f, 240f, 30f);
        biomassRect.anchoredPosition = new Vector2(0f, -48f);
    }

    private void BuildTabBar(Action<AppTab> onTab)
    {
        Image bar = UiFactory.CreateImage("TabBar", _root, new Color(0.10f, 0.11f, 0.14f, 0.98f));
        UiFactory.BottomAnchored(bar.rectTransform, 0f, UiFactory.ReferenceResolution.x, TabBarHeight);

        float width = UiFactory.ReferenceResolution.x / Tabs.Length;

        for (int i = 0; i < Tabs.Length; i++)
        {
            AppTab tab = Tabs[i];

            Button button = UiFactory.CreateButton($"Tab_{tab}", bar.transform, TabLabels[i], 20,
                Color.white, out Text label);
            label.color = new Color(0.95f, 0.95f, 0.97f);

            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(i * width + 3f, 6f);
            rect.offsetMax = new Vector2(i * width + width - 3f, -6f);

            _tabImages[i] = button.GetComponent<Image>();
            button.onClick.AddListener(() => onTab?.Invoke(tab));
        }

        SetActiveTab(_active);
    }
}
```

- [ ] **Шаг 5: Создать `HomeScreen.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Главный экран: выбранный патоген крупно, стрелки по бокам. Выбор сразу
/// уходит в сейв — при следующем запуске игрок видит того же персонажа.
/// </summary>
public class HomeScreen : UiScreen
{
    private static readonly string[] Hints =
    {
        "Вспышка — убитые враги заражаются и бьют своих",
        "Биоплёнка — щит поглощает один удар, затем восстанавливается",
        "Споры — попадания оставляют тлеющие зоны урона",
        "Прятки — смертельный удар превращается в 2с невидимости"
    };

    private readonly MetaProgression _meta;
    private readonly Action<PathogenType> _onChanged;
    private readonly PathogenData[] _previews = new PathogenData[PathogenCarousel.Types.Length];

    private Image _body;
    private Text _name;
    private Text _hint;
    private Text _stats;
    private int _index;

    public HomeScreen(MetaProgression meta, Action<PathogenType> onChanged)
    {
        _meta = meta;
        _onChanged = onChanged;

        for (int i = 0; i < PathogenCarousel.Types.Length; i++)
        {
            _previews[i] = PathogenData.CreateDefault(PathogenCarousel.Types[i]);
        }
    }

    public PathogenType Selected => PathogenCarousel.Types[_index];

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.12f, 0.06f, 0.09f));
        UiFactory.Stretch(backdrop.rectTransform);

        Text caption = UiFactory.CreateText("Caption", Root, "ВАШ ПАТОГЕН", 26,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(caption.rectTransform, 130f, UiFactory.ContentWidth, 44f);

        _body = UiFactory.CreateImage("Body", Root, Color.white, PlaceholderArt.Circle);
        UiFactory.TopAnchored(_body.rectTransform, 200f, 300f, 300f);

        _name = UiFactory.CreateText("Name", Root, string.Empty, 36,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(_name.rectTransform, 520f, UiFactory.ContentWidth, 52f);

        _hint = UiFactory.CreateText("Hint", Root, string.Empty, 22, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(_hint.rectTransform, 578f, UiFactory.ContentWidth, 70f);

        _stats = UiFactory.CreateText("Stats", Root, string.Empty, 20, TextAnchor.UpperCenter);
        _stats.color = new Color(0.70f, 0.72f, 0.78f);
        UiFactory.TopAnchored(_stats.rectTransform, 650f, UiFactory.ContentWidth, 60f);

        BuildArrow("Prev", "◀", -260f, -1);
        BuildArrow("Next", "▶", 260f, 1);
    }

    private void BuildArrow(string name, string glyph, float x, int delta)
    {
        Button button = UiFactory.CreateButton(name, Root, glyph, 40,
            new Color(0.30f, 0.32f, 0.38f), out _);
        RectTransform rect = UiFactory.TopAnchored((RectTransform)button.transform, 300f, 84f, 100f);
        rect.anchoredPosition = new Vector2(x, -300f);

        button.onClick.AddListener(() =>
        {
            _index = PathogenCarousel.Shift(_index, delta);
            Persist();
            Refresh();
        });
    }

    protected override void OnShow()
    {
        _index = PathogenCarousel.IndexOf(_meta.Progress.lastPathogen);
        Refresh();
    }

    private void Persist()
    {
        _meta.Progress.lastPathogen = Selected.ToString();
        _meta.Save();
        _onChanged?.Invoke(Selected);
    }

    private void Refresh()
    {
        PathogenData preview = _previews[_index];

        _body.color = preview.bodyColor;
        _name.text = preview.pathogenName;
        _hint.text = Hints[_index];
        _stats.text = $"Здоровье {Mathf.RoundToInt(preview.maxHealth)} · " +
                      $"урон {Mathf.RoundToInt(preview.attackDamage)} · " +
                      $"дальность {preview.attackRange:0.0}";
    }
}
```

- [ ] **Шаг 6: Создать `UpgradesScreen.cs`**

Перенос магазина из `GameHud.BuildShopScreen` / `RefreshShop` без изменений
логики: тот же список перков, те же цвета доступности, та же кнопка сброса.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Витрина перманентных улучшений. Недоступное показывается, а не прячется:
/// это цель следующего забега.
/// </summary>
public class UpgradesScreen : UiScreen
{
    private readonly MetaProgression _meta;
    private readonly ConfirmModal _confirm;
    private readonly List<Text> _rowLabels = new List<Text>();
    private readonly List<Image> _rowImages = new List<Image>();

    private Text _biomass;
    private Text _stats;

    public UpgradesScreen(MetaProgression meta, ConfirmModal confirm)
    {
        _meta = meta;
        _confirm = confirm;
    }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.09f, 0.10f, 0.13f));
        UiFactory.Stretch(backdrop.rectTransform);

        Text title = UiFactory.CreateText("Title", Root, "ПЕРМАНЕНТНЫЕ УЛУЧШЕНИЯ", 30,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 112f, UiFactory.ContentWidth, 52f);

        _biomass = UiFactory.CreateText("Biomass", Root, string.Empty, 24, TextAnchor.MiddleCenter);
        UiFactory.TopAnchored(_biomass.rectTransform, 166f, UiFactory.ContentWidth, 38f);

        float y = 214f;
        IReadOnlyList<PermanentUpgrade> upgrades = _meta.Upgrades;

        for (int i = 0; i < upgrades.Count; i++)
        {
            PermanentUpgrade upgrade = upgrades[i];

            Button button = UiFactory.CreateButton($"Perk_{upgrade.Id}", Root, string.Empty, 22,
                Color.white, out Text label);
            UiFactory.TopAnchored((RectTransform)button.transform, y, UiFactory.ContentWidth, 88f);

            button.onClick.AddListener(() =>
            {
                _meta.TryPurchase(upgrade);
                Refresh();
            });

            _rowLabels.Add(label);
            _rowImages.Add(button.GetComponent<Image>());
            y += 98f;
        }

        _stats = UiFactory.CreateText("Stats", Root, string.Empty, 19, TextAnchor.UpperCenter);
        _stats.color = new Color(0.70f, 0.72f, 0.78f);
        UiFactory.TopAnchored(_stats.rectTransform, y + 8f, UiFactory.ContentWidth, 50f);

        Button reset = UiFactory.CreateButton("Reset", Root, "Сбросить прогресс (отладка)", 20,
            new Color(0.60f, 0.45f, 0.45f), out _);
        UiFactory.TopAnchored((RectTransform)reset.transform, y + 62f, UiFactory.ContentWidth, 58f);
        reset.onClick.AddListener(() => _confirm.Ask(
            "Сбросить прогресс?",
            "Пропадут биомасса, золото, купленные улучшения и все звёзды кампании.\n" +
            "Нужно для плейтестов: иначе не посмотреть, как игра выглядит при первом запуске.",
            "Стереть всё",
            () =>
            {
                _meta.ResetProgress();
                Refresh();
            }));
    }

    protected override void OnShow() => Refresh();

    private void Refresh()
    {
        PlayerProgress progress = _meta.Progress;
        _biomass.text = $"Биомасса: {progress.biomass}";
        _stats.text = $"Попыток биома: {progress.totalRuns} · всего убито: {progress.totalKills} · " +
                      $"боссов: {progress.bossesDefeated}";

        IReadOnlyList<PermanentUpgrade> upgrades = _meta.Upgrades;
        for (int i = 0; i < upgrades.Count && i < _rowLabels.Count; i++)
        {
            PermanentUpgrade upgrade = upgrades[i];
            int level = _meta.LevelOf(upgrade);
            bool maxed = upgrade.IsMaxed(level);

            _rowLabels[i].text = maxed
                ? $"{upgrade.Title}  [{level}/{upgrade.MaxLevel}]\nмаксимум"
                : $"{upgrade.Title}  [{level}/{upgrade.MaxLevel}]\n{upgrade.PerLevelDescription} · цена {upgrade.CostForNextLevel(level)}";

            _rowImages[i].color = maxed
                ? new Color(0.45f, 0.47f, 0.50f)
                : _meta.CanAfford(upgrade)
                    ? new Color(0.50f, 0.80f, 0.55f)
                    : new Color(0.62f, 0.48f, 0.48f);
        }
    }
}
```

- [ ] **Шаг 7: Создать `StubScreen.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Раздел, которого ещё нет. Честная заглушка лучше отключённой вкладки:
/// на плейтесте сразу видно, что раздел запланирован, а не сломан.
/// </summary>
public class StubScreen : UiScreen
{
    private readonly string _title;
    private readonly string _body;

    public StubScreen(string title, string body)
    {
        _title = title;
        _body = body;
    }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.09f, 0.10f, 0.13f));
        UiFactory.Stretch(backdrop.rectTransform);

        Text title = UiFactory.CreateText("Title", Root, _title, 32,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 420f, UiFactory.ContentWidth, 56f);

        Text body = UiFactory.CreateText("Body", Root, _body, 22, TextAnchor.UpperCenter);
        body.color = new Color(0.70f, 0.72f, 0.78f);
        UiFactory.TopAnchored(body.rectTransform, 486f, UiFactory.ContentWidth, 200f);
    }
}
```

- [ ] **Шаг 8: Запустить тесты**

Ожидаемо: 4 теста `PathogenCarouselTests` зелёные.

---

### Задача 10: Экраны кампании — карта, брифинг, результат

Карта с дорожками, модалка брифинга и экран результата, который совмещает
звёзды, награду и выбор апгрейда: два экрана подряд после каждого узла — лишний
тап на ровном месте.

**Файлы:**
- Создать: `Assets/Scripts/UI/Campaign/MapNodeView.cs`
- Создать: `Assets/Scripts/UI/Campaign/CampaignMapScreen.cs`
- Создать: `Assets/Scripts/UI/Campaign/LevelBriefingModal.cs`
- Создать: `Assets/Scripts/UI/Campaign/LevelResultScreen.cs`
- Изменить: `Assets/Scripts/UI/UiFactory.cs` (добавить `CreateScrollView`)

**Интерфейсы:**
- Использует: `UiScreen`, `UiFactory`, `ScreenStack`, `CampaignMapData`,
  `BiomeData`, `CampaignNode`, `CampaignProgress`, `CampaignRules`,
  `CampaignRewards`, `StarRating`, `NodeOutcome`, `Reward`, `MetaProgression`,
  `UpgradeDefinition`, `UpgradeSystem.ChoiceCount`, `ShellChrome.BottomInset`
- Отдаёт: `UiFactory.CreateScrollView(string, Transform, out RectTransform) → ScrollRect`,
  `MapNodeView(Transform parent, Action<CampaignNode> onPick)` с методом
  `Bind(CampaignNode node, int stars, bool unlocked)` и свойством `Root` (RectTransform),
  `CampaignMapScreen(CampaignMapData map, MetaProgression meta, Action<CampaignNode> onPick)`,
  `LevelBriefingModal(ScreenStack stack, MetaProgression meta)` с методом
  `Open(CampaignNode node, Action onStart)`,
  `LevelResultScreen(MetaProgression meta, Action<UpgradeDefinition> onUpgradePicked, Action onContinue)`
  с методом `Present(NodeOutcome outcome, Reward reward, IReadOnlyList<UpgradeDefinition> choices)`

- [ ] **Шаг 1: Добавить `CreateScrollView` в `UiFactory.cs`**

```csharp
    /// <summary>
    /// Вертикальный скролл. Маска на прозрачной картинке: она обязана оставаться
    /// raycastTarget, иначе ScrollRect не увидит перетаскивание пальцем.
    /// </summary>
    public static ScrollRect CreateScrollView(string name, Transform parent, out RectTransform content)
    {
        Image viewport = CreateImage(name + "Viewport", parent, new Color(0f, 0f, 0f, 0f));
        viewport.raycastTarget = true;

        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();

        content = CreateRect(name + "Content", viewport.transform);
        content.anchorMin = new Vector2(0.5f, 0f);
        content.anchorMax = new Vector2(0.5f, 0f);
        content.pivot = new Vector2(0.5f, 0f);
        content.anchoredPosition = Vector2.zero;

        scroll.content = content;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 28f;
        scroll.inertia = true;

        return scroll;
    }
```

- [ ] **Шаг 2: Создать `MapNodeView.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Узел на карте: кнопка, подпись, три звезды. Переиспользуется между входами
/// на карту — пересборка иерархии при каждом показе была бы самым дорогим
/// способом перерисовать восемь кружков.
/// </summary>
public class MapNodeView
{
    private static readonly Color LockedColor = new Color(0.30f, 0.31f, 0.35f);
    private static readonly Color OpenColor = new Color(0.55f, 0.80f, 0.60f);
    private static readonly Color BossColor = new Color(0.88f, 0.45f, 0.42f);
    private static readonly Color StarOn = new Color(0.98f, 0.82f, 0.35f);
    private static readonly Color StarOff = new Color(0.28f, 0.28f, 0.32f, 0.85f);

    private readonly Button _button;
    private readonly Image _image;
    private readonly Text _label;
    private readonly Text[] _stars = new Text[StarRating.MaxStars];
    private readonly Action<CampaignNode> _onPick;

    private CampaignNode _node;

    public MapNodeView(Transform parent, Action<CampaignNode> onPick)
    {
        _onPick = onPick;

        _button = UiFactory.CreateButton("MapNode", parent, string.Empty, 22, Color.white, out _label);
        Root = (RectTransform)_button.transform;
        Root.sizeDelta = new Vector2(190f, 110f);
        _image = _button.GetComponent<Image>();

        for (int i = 0; i < _stars.Length; i++)
        {
            Text star = UiFactory.CreateText($"Star{i}", Root, "★", 24, TextAnchor.MiddleCenter);
            RectTransform rect = star.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(30f, 30f);
            rect.anchoredPosition = new Vector2((i - 1) * 32f, -4f);
            _stars[i] = star;
        }

        _button.onClick.AddListener(() => _onPick?.Invoke(_node));
    }

    public RectTransform Root { get; }

    public void SetVisible(bool visible) => Root.gameObject.SetActive(visible);

    public void Bind(CampaignNode node, int stars, bool unlocked)
    {
        _node = node;

        Root.anchoredPosition = node.MapPosition;
        _label.text = node.IsBoss
            ? $"БОСС\n{node.DisplayName}"
            : $"{node.IndexInBiome + 1}. {(node.Level.advanceType == AdvanceType.Waves ? "Волны" : "Сегменты")}";

        _image.color = !unlocked ? LockedColor : node.IsBoss ? BossColor : OpenColor;
        _button.interactable = unlocked;

        for (int i = 0; i < _stars.Length; i++)
        {
            // Звёзды показываются и у закрытого узла — пустыми: игрок видит,
            // что оценка есть, ещё до того как туда доберётся.
            _stars[i].color = i < stars ? StarOn : StarOff;
        }

        SetVisible(true);
    }
}
```

- [ ] **Шаг 3: Создать `CampaignMapScreen.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Карта кампании: биомы сверху вниз, узлы дорожкой внутри биома.
/// Правила доступности сюда не заезжают — экран только рисует то, что решил
/// CampaignRules.
/// </summary>
public class CampaignMapScreen : UiScreen
{
    private const float BiomeHeaderHeight = 96f;
    private const float BiomeGap = 60f;

    private readonly CampaignMapData _map;
    private readonly MetaProgression _meta;
    private readonly Action<CampaignNode> _onPick;
    private readonly List<MapNodeView> _views = new List<MapNodeView>();
    private readonly List<Text> _headers = new List<Text>();

    private RectTransform _content;

    public CampaignMapScreen(CampaignMapData map, MetaProgression meta, Action<CampaignNode> onPick)
    {
        _map = map;
        _meta = meta;
        _onPick = onPick;
    }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.11f, 0.05f, 0.07f));
        UiFactory.Stretch(backdrop.rectTransform);

        ScrollRect scroll = UiFactory.CreateScrollView("Map", Root, out _content);
        RectTransform viewport = (RectTransform)scroll.transform;
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        // Сверху шапка, снизу таб-бар — карта не должна под них залезать.
        viewport.offsetMin = new Vector2(0f, ShellChrome.BottomInset);
        viewport.offsetMax = new Vector2(0f, -92f);

        BuildContent();
    }

    private void BuildContent()
    {
        float y = 40f;

        for (int b = 0; b < _map.Biomes.Count; b++)
        {
            BiomeData biome = _map.Biomes[b];

            Text header = UiFactory.CreateText($"Biome{b}", _content, string.Empty, 28,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            header.color = biome.AccentColor;
            RectTransform headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0.5f, 0f);
            headerRect.anchorMax = new Vector2(0.5f, 0f);
            headerRect.pivot = new Vector2(0.5f, 0f);
            headerRect.sizeDelta = new Vector2(UiFactory.ContentWidth, BiomeHeaderHeight);
            headerRect.anchoredPosition = new Vector2(0f, y);
            _headers.Add(header);

            y += BiomeHeaderHeight;

            for (int n = 0; n < biome.Nodes.Count; n++)
            {
                var view = new MapNodeView(_content, _onPick);
                // MapPosition задаёт горизонтальный зигзаг и шаг внутри биома,
                // а вертикальное смещение биома добавляется здесь.
                view.Root.anchorMin = new Vector2(0.5f, 0f);
                view.Root.anchorMax = new Vector2(0.5f, 0f);
                view.Root.pivot = new Vector2(0.5f, 0f);
                _views.Add(view);
            }

            y += biome.Nodes.Count > 0
                ? biome.Nodes[biome.Nodes.Count - 1].MapPosition.y + 150f
                : 40f;

            y += BiomeGap;
        }

        _content.sizeDelta = new Vector2(UiFactory.ReferenceResolution.x, y);
    }

    protected override void OnShow() => Refresh();

    private void Refresh()
    {
        CampaignProgress progress = _meta.Progress.campaign;
        CampaignRules.EnsureFirstBiomeUnlocked(progress);

        int viewIndex = 0;
        float y = 40f;

        for (int b = 0; b < _map.Biomes.Count; b++)
        {
            BiomeData biome = _map.Biomes[b];
            bool unlockedBiome = biome.Playable && progress.IsBiomeUnlocked(biome.Id);

            _headers[b].text = biome.Playable
                ? unlockedBiome ? biome.DisplayName : $"{biome.DisplayName} — закрыт"
                : $"{biome.DisplayName} — в разработке";

            y += BiomeHeaderHeight;

            for (int n = 0; n < biome.Nodes.Count; n++, viewIndex++)
            {
                CampaignNode node = biome.Nodes[n];
                MapNodeView view = _views[viewIndex];

                view.Bind(node, progress.StarsOf(node.Id),
                    CampaignRules.IsNodeUnlocked(_map, progress, node));

                view.Root.anchoredPosition = new Vector2(node.MapPosition.x, y + node.MapPosition.y);
            }

            y += biome.Nodes.Count > 0
                ? biome.Nodes[biome.Nodes.Count - 1].MapPosition.y + 150f
                : 40f;

            y += BiomeGap;
        }
    }
}
```

- [ ] **Шаг 4: Создать `LevelBriefingModal.cs`**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Что игрок узнаёт до входа в узел: состав врагов, награда и его лучший
/// результат. Награда показывается честно — с уже применённым срезом жадности
/// и с пометкой, что повтор платит меньше.
/// </summary>
public class LevelBriefingModal : UiScreen
{
    private readonly ScreenStack _stack;
    private readonly MetaProgression _meta;

    private Text _title;
    private Text _enemies;
    private Text _reward;
    private Text _best;
    private Action _onStart;

    public LevelBriefingModal(ScreenStack stack, MetaProgression meta)
    {
        _stack = stack;
        _meta = meta;
    }

    public void Open(CampaignNode node, Action onStart)
    {
        _onStart = onStart;

        int best = _meta.Progress.campaign.StarsOf(node.Id);

        _title.text = node.DisplayName;
        _enemies.text = node.EnemyNames.Count > 0
            ? "Противник: " + string.Join(", ", node.EnemyNames)
            : "Противник неизвестен";

        Reward payout = CampaignRewards
            .Payout(node, best, best > 0 ? best : StarRating.MaxStars)
            .Scale(MetaProgression.GreedMultiplier);

        _reward.text = best > 0
            ? $"Награда за повтор: {payout.Gold} золота, {payout.Biomass} биомассы\n" +
              "Полную награду даёт только улучшение результата"
            : $"Награда: до {payout.Gold} золота и {payout.Biomass} биомассы за три звезды";

        _best.text = best > 0
            ? $"Ваш результат: {new string('★', best)}{new string('☆', StarRating.MaxStars - best)}"
            : $"Узел не пройден · эталон {Mathf.RoundToInt(StarRating.ParTime(node))} с";

        _stack.PushModal(this);
    }

    protected override void OnBuild()
    {
        Image dim = UiFactory.CreateImage("Dim", Root, new Color(0f, 0f, 0f, 0.82f));
        UiFactory.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image panel = UiFactory.CreateImage("Panel", Root, new Color(0.15f, 0.12f, 0.14f));
        UiFactory.TopAnchored(panel.rectTransform, 340f, UiFactory.ContentWidth, 560f);

        _title = UiFactory.CreateText("Title", panel.transform, string.Empty, 28,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(_title.rectTransform, 24f, UiFactory.ContentWidth - 40f, 76f);

        _enemies = UiFactory.CreateText("Enemies", panel.transform, string.Empty, 22, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(_enemies.rectTransform, 110f, UiFactory.ContentWidth - 50f, 90f);

        _reward = UiFactory.CreateText("Reward", panel.transform, string.Empty, 22, TextAnchor.UpperCenter);
        _reward.color = new Color(0.95f, 0.85f, 0.45f);
        UiFactory.TopAnchored(_reward.rectTransform, 208f, UiFactory.ContentWidth - 50f, 90f);

        _best = UiFactory.CreateText("Best", panel.transform, string.Empty, 22, TextAnchor.UpperCenter);
        _best.color = new Color(0.72f, 0.74f, 0.80f);
        UiFactory.TopAnchored(_best.rectTransform, 300f, UiFactory.ContentWidth - 50f, 50f);

        Button start = UiFactory.CreateButton("Start", panel.transform, "В БОЙ", 30,
            new Color(0.55f, 0.80f, 0.60f), out _);
        UiFactory.TopAnchored((RectTransform)start.transform, 366f, UiFactory.ContentWidth - 60f, 82f);
        start.onClick.AddListener(() =>
        {
            Action action = _onStart;
            _stack.PopModal();
            action?.Invoke();
        });

        Button cancel = UiFactory.CreateButton("Cancel", panel.transform, "Назад", 24,
            new Color(0.62f, 0.64f, 0.70f), out _);
        UiFactory.TopAnchored((RectTransform)cancel.transform, 458f, UiFactory.ContentWidth - 60f, 66f);
        cancel.onClick.AddListener(() => _stack.PopModal());
    }
}
```

- [ ] **Шаг 5: Создать `LevelResultScreen.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Итог узла: звёзды, награда и выбор апгрейда на одном экране. Разносить их
/// по двум экранам — лишний тап после каждого боя; апгрейд здесь же и есть
/// продолжение боя, а не отдельное меню.
///
/// При провале и при повторном проходе узла в той же попытке биома карточки
/// апгрейдов не показываются вовсе — тогда вместо них кнопка «На карту».
/// </summary>
public class LevelResultScreen : UiScreen
{
    private readonly MetaProgression _meta;
    private readonly Action<UpgradeDefinition> _onUpgradePicked;
    private readonly Action _onContinue;

    private readonly List<Button> _choiceButtons = new List<Button>();
    private readonly List<Text> _choiceLabels = new List<Text>();
    private readonly List<Image> _choiceImages = new List<Image>();

    private Text _title;
    private Text _stars;
    private Text _summary;
    private Text _rewardText;
    private Text _choicePrompt;
    private Button _continueButton;

    private IReadOnlyList<UpgradeDefinition> _choices = new List<UpgradeDefinition>();

    public LevelResultScreen(MetaProgression meta, Action<UpgradeDefinition> onUpgradePicked, Action onContinue)
    {
        _meta = meta;
        _onUpgradePicked = onUpgradePicked;
        _onContinue = onContinue;
    }

    public void Present(NodeOutcome outcome, Reward reward, IReadOnlyList<UpgradeDefinition> choices)
    {
        _choices = choices ?? new List<UpgradeDefinition>();

        _title.text = outcome.Cleared ? "УЗЕЛ ЗАЧИЩЕН" : "ПАТОГЕН УНИЧТОЖЕН";
        _title.color = outcome.Cleared ? new Color(0.60f, 0.90f, 0.65f) : new Color(0.92f, 0.50f, 0.48f);

        int stars = outcome.Stars;
        _stars.text = new string('★', stars) + new string('☆', StarRating.MaxStars - stars);

        float par = StarRating.ParTime(outcome.Node);
        _summary.text = $"{outcome.Node.DisplayName}\n" +
                        $"Время: {outcome.ElapsedSeconds:0.0} с (эталон {par:0} с) · убито: {outcome.Kills}";

        _rewardText.text = outcome.Cleared
            ? $"Получено: {reward.Gold} золота, {reward.Biomass} биомассы"
            : "Награды нет. Билд биома сгорел.";

        bool hasChoices = _choices.Count > 0;
        _choicePrompt.gameObject.SetActive(hasChoices);

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            bool has = i < _choices.Count;
            _choiceButtons[i].gameObject.SetActive(has);
            if (!has)
            {
                continue;
            }

            UpgradeDefinition upgrade = _choices[i];
            _choiceLabels[i].text = upgrade.IsMutation
                ? $"МУТАЦИЯ · {upgrade.Title}\n{upgrade.Description}"
                : $"{upgrade.Title}\n{upgrade.Description}";

            // Мутация должна читаться как другой класс выбора, а не как очередные +15%.
            _choiceImages[i].color = upgrade.IsMutation
                ? new Color(0.85f, 0.50f, 0.92f)
                : new Color(0.86f, 0.88f, 0.92f);
        }

        _continueButton.gameObject.SetActive(!hasChoices);
    }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.06f, 0.05f, 0.07f, 0.97f));
        UiFactory.Stretch(backdrop.rectTransform);

        _title = UiFactory.CreateText("Title", Root, string.Empty, 36,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(_title.rectTransform, 90f, UiFactory.ContentWidth, 56f);

        _stars = UiFactory.CreateText("Stars", Root, string.Empty, 52, TextAnchor.MiddleCenter);
        _stars.color = new Color(0.98f, 0.82f, 0.35f);
        UiFactory.TopAnchored(_stars.rectTransform, 152f, UiFactory.ContentWidth, 76f);

        _summary = UiFactory.CreateText("Summary", Root, string.Empty, 22, TextAnchor.UpperCenter);
        _summary.color = new Color(0.78f, 0.80f, 0.85f);
        UiFactory.TopAnchored(_summary.rectTransform, 236f, UiFactory.ContentWidth, 80f);

        _rewardText = UiFactory.CreateText("Reward", Root, string.Empty, 24, TextAnchor.UpperCenter);
        _rewardText.color = new Color(0.95f, 0.85f, 0.45f);
        UiFactory.TopAnchored(_rewardText.rectTransform, 316f, UiFactory.ContentWidth, 50f);

        _choicePrompt = UiFactory.CreateText("Prompt", Root, "Выберите одно улучшение:", 24,
            TextAnchor.MiddleCenter);
        UiFactory.TopAnchored(_choicePrompt.rectTransform, 374f, UiFactory.ContentWidth, 40f);

        for (int i = 0; i < UpgradeSystem.ChoiceCount; i++)
        {
            int index = i;

            Button button = UiFactory.CreateButton($"Choice{i}", Root, string.Empty, 23,
                Color.white, out Text label);
            UiFactory.TopAnchored((RectTransform)button.transform, 424f + i * 146f,
                UiFactory.ContentWidth, 132f);

            button.onClick.AddListener(() =>
            {
                if (index < _choices.Count)
                {
                    _onUpgradePicked?.Invoke(_choices[index]);
                }
            });

            _choiceButtons.Add(button);
            _choiceLabels.Add(label);
            _choiceImages.Add(button.GetComponent<Image>());
        }

        _continueButton = UiFactory.CreateButton("Continue", Root, "На карту", 28,
            new Color(0.55f, 0.80f, 0.60f), out _);
        UiFactory.TopAnchored((RectTransform)_continueButton.transform, 430f,
            UiFactory.ContentWidth, 88f);
        _continueButton.onClick.AddListener(() => _onContinue?.Invoke());
    }
}
```

- [ ] **Шаг 6: Прогнать тесты**

Новых тестов в задаче нет: здесь только отрисовка, а вся логика (звёзды,
награды, доступность) уже покрыта задачами 3, 4 и 6. Ожидаемо: набор тестов
компилируется и остаётся зелёным.

---

### Задача 11: Боевой HUD, `AppFlow` и сборка

Последняя задача: `GameHud` распадается, появляется оркестратор, и всё это
поднимается из бутстрапа. После неё проект снова собирается и запускается.

**Файлы:**
- Создать: `Assets/Scripts/UI/Combat/CombatHud.cs`
- Создать: `Assets/Scripts/App/AppFlow.cs`
- Изменить: `Assets/Scripts/Core/GameBootstrap.cs`
- Удалить: `Assets/Scripts/UI/GameHud.cs` (или `GameHud.cs.disabled`) и `UI/PrototypeHud.cs` с их `.meta`

**Интерфейсы:**
- Использует: всё созданное в задачах 1-10
- Отдаёт: `CombatHud(GameRunner runner, EnemySpawner spawner)`,
  `AppFlow.Initialize(GameRunner, EnemySpawner, UpgradeSystem, MetaProgression)`

- [ ] **Шаг 1: Создать `CombatHud.cs`**

Перенос боевой части `GameHud` с одной содержательной правкой: строки собираются
`StringBuilder` и только при смене значений.

```csharp
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Боевой HUD: здоровье, счётчики, полоски сегментов босса.
///
/// Строки собираются в StringBuilder и перестраиваются только при смене
/// значений. В GameHud три интерполированные строки собирались каждый кадр —
/// около трёх аллокаций на кадр в бою, ровно там, где на экране сотни объектов.
/// </summary>
public class CombatHud : UiScreen
{
    /// Сколько полосок сегментов босса держать наготове.
    private const int MaxBossBars = 8;

    private readonly GameRunner _runner;
    private readonly EnemySpawner _spawner;
    private readonly StringBuilder _builder = new StringBuilder(128);

    private readonly List<Image> _bossFills = new List<Image>();
    private readonly List<Image> _bossBackgrounds = new List<Image>();
    private readonly List<Text> _bossLabels = new List<Text>();

    private Image _healthFill;
    private Text _healthText;
    private Text _infoText;
    private Text _abilityText;
    private Text _mutationsText;
    private Text _bossName;

    // Последние показанные значения — чтобы не пересобирать строки впустую.
    private int _shownHealth = -1;
    private int _shownKills = -1;
    private int _shownThreats = -1;
    private string _shownAbility = string.Empty;

    public CombatHud(GameRunner runner, EnemySpawner spawner)
    {
        _runner = runner;
        _spawner = spawner;
    }

    protected override void OnBuild()
    {
        _healthFill = UiFactory.CreateBar("Health", Root, new Color(0.35f, 0.85f, 0.45f), out Image background);
        UiFactory.TopAnchored(background.rectTransform, 22f, UiFactory.ContentWidth, 38f);

        _healthText = UiFactory.CreateText("HealthText", background.transform, string.Empty, 22,
            TextAnchor.MiddleCenter);
        UiFactory.Stretch(_healthText.rectTransform);

        _infoText = UiFactory.CreateText("Info", Root, string.Empty, 22);
        UiFactory.TopAnchored(_infoText.rectTransform, 70f, UiFactory.ContentWidth, 110f);

        _bossName = UiFactory.CreateText("BossName", Root, string.Empty, 24,
            TextAnchor.MiddleLeft, FontStyle.Bold);
        UiFactory.TopAnchored(_bossName.rectTransform, 186f, UiFactory.ContentWidth, 32f);

        for (int i = 0; i < MaxBossBars; i++)
        {
            Image fill = UiFactory.CreateBar($"BossBar{i}", Root, Color.white, out Image barBackground);
            UiFactory.TopAnchored(barBackground.rectTransform, 222f + i * 34f, UiFactory.ContentWidth, 28f);

            Text label = UiFactory.CreateText($"BossBarLabel{i}", barBackground.transform, string.Empty, 19,
                TextAnchor.MiddleLeft);
            UiFactory.StretchWithPadding(label.rectTransform, 10f, 0f);
            label.color = new Color(0.08f, 0.08f, 0.10f);

            _bossFills.Add(fill);
            _bossBackgrounds.Add(barBackground);
            _bossLabels.Add(label);
            barBackground.gameObject.SetActive(false);
        }

        _abilityText = UiFactory.CreateText("Ability", Root, string.Empty, 22);
        UiFactory.TopAnchored(_abilityText.rectTransform, UiFactory.ReferenceResolution.y - 128f,
            UiFactory.ContentWidth, 50f);

        _mutationsText = UiFactory.CreateText("Mutations", Root, string.Empty, 20);
        UiFactory.TopAnchored(_mutationsText.rectTransform, UiFactory.ReferenceResolution.y - 76f,
            UiFactory.ContentWidth, 50f);
        _mutationsText.color = new Color(0.85f, 0.55f, 0.90f);
    }

    protected override void OnShow()
    {
        // Сброс кэша: значения прошлого узла не должны подавлять первое обновление.
        _shownHealth = -1;
        _shownKills = -1;
        _shownThreats = -1;
        _shownAbility = string.Empty;

        RefreshMutations();
    }

    protected override void OnTick()
    {
        PlayerController player = _runner.Player;
        if (player == null)
        {
            return;
        }

        RefreshHealth(player);
        RefreshInfo();
        RefreshAbility(player);
        RefreshBossBars();
    }

    private void RefreshHealth(PlayerController player)
    {
        float normalized = player.Health.Normalized;
        _healthFill.fillAmount = normalized;
        _healthFill.color = Color.Lerp(new Color(0.85f, 0.20f, 0.20f), new Color(0.35f, 0.85f, 0.45f), normalized);

        int current = Mathf.CeilToInt(player.Health.Current);
        if (current == _shownHealth)
        {
            return;
        }

        _shownHealth = current;
        _builder.Clear();
        _builder.Append(current).Append(" / ").Append(Mathf.CeilToInt(player.Health.Max));
        _healthText.text = _builder.ToString();
    }

    private void RefreshInfo()
    {
        int kills = _spawner != null ? _spawner.Kills : 0;
        int threats = Battlefield.ThreatCount;

        if (kills == _shownKills && threats == _shownThreats)
        {
            return;
        }

        _shownKills = kills;
        _shownThreats = threats;

        _builder.Clear();
        _builder.Append(_runner.CurrentNode != null ? _runner.CurrentNode.DisplayName : "-").Append('\n');
        _builder.Append("Убито: ").Append(kills).Append('\n');
        _builder.Append("На поле: ").Append(threats);
        _infoText.text = _builder.ToString();
    }

    private void RefreshAbility(PlayerController player)
    {
        string status = player.Ability != null ? player.Ability.StatusLine : string.Empty;
        if (status == _shownAbility)
        {
            return;
        }

        _shownAbility = status;
        _abilityText.text = status;
    }

    private void RefreshMutations()
    {
        PlayerStats stats = _runner.Stats;
        _mutationsText.text = stats != null && stats.TakenMutations.Count > 0
            ? "Мутации: " + string.Join(", ", stats.TakenMutations)
            : string.Empty;
    }

    /// <summary>
    /// Полоска на сегмент, а не одна общая: игрок должен видеть, что цель
    /// составная и что каждый снятый сегмент убирает конкретную атаку.
    /// </summary>
    private void RefreshBossBars()
    {
        Boss boss = _spawner != null ? _spawner.ActiveBoss : null;

        if (boss == null)
        {
            if (_bossName.text.Length > 0)
            {
                _bossName.text = string.Empty;
                for (int i = 0; i < _bossBackgrounds.Count; i++)
                {
                    _bossBackgrounds[i].gameObject.SetActive(false);
                }
            }
            return;
        }

        _bossName.text = boss.Data.bossName;
        IReadOnlyList<BossSegment> segments = boss.Segments;

        for (int i = 0; i < _bossBackgrounds.Count; i++)
        {
            bool has = i < segments.Count && segments[i] != null;
            _bossBackgrounds[i].gameObject.SetActive(has);
            if (!has)
            {
                continue;
            }

            BossSegment segment = segments[i];
            float fraction = segment.Health.Normalized;

            _bossFills[i].fillAmount = fraction;
            _bossFills[i].color = segment.Definition.color;
            _bossLabels[i].text = fraction > 0f
                ? segment.Definition.segmentName
                : segment.Definition.segmentName + " — уничтожен";
        }
    }

    /// <summary>Позвать после взятия мутации, чтобы строка обновилась вне тика.</summary>
    public void NotifyMutationsChanged() => RefreshMutations();
}
```

- [ ] **Шаг 2: Создать `AppFlow.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Состояние приложения и навигация. Держит вместе оболочку, карту и бой,
/// чтобы у экранов не было ссылок друг на друга напрямую.
///
/// Решение «давать ли апгрейд за узел» живёт здесь, а не в GameRunner: оно
/// зависит от состояния попытки биома, про которую бой ничего не знает.
/// </summary>
[DefaultExecutionOrder(-50)]
public class AppFlow : MonoBehaviour
{
    private GameRunner _runner;
    private EnemySpawner _spawner;
    private UpgradeSystem _upgrades;
    private MetaProgression _meta;

    private CampaignMapData _map;
    private ScreenStack _stack;
    private ShellChrome _chrome;
    private BiomeRun _run;

    private SplashScreen _splash;
    private HomeScreen _home;
    private UpgradesScreen _upgradesScreen;
    private StubScreen _wardrobe;
    private StubScreen _battle;
    private CampaignMapScreen _mapScreen;
    private CombatHud _combat;
    private LevelResultScreen _result;

    private SettingsModal _settings;
    private ConfirmModal _confirm;
    private LevelBriefingModal _briefing;

    public void Initialize(GameRunner runner, EnemySpawner spawner, UpgradeSystem upgrades, MetaProgression meta)
    {
        _runner = runner;
        _spawner = spawner;
        _upgrades = upgrades;
        _meta = meta;

        _map = CampaignBuilder.Build();
        CampaignRules.EnsureFirstBiomeUnlocked(_meta.Progress.campaign);
        AudioService.Apply(_meta.Progress.settings);

        EnsureEventSystem();
        BuildUi();

        _runner.NodeFinished += OnNodeFinished;

        _chrome.SetVisible(false);
        _stack.Show(_splash);
    }

    private void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.NodeFinished -= OnNodeFinished;
        }
    }

    private void Update() => _stack.Tick();

    // --- Сборка ---

    /// <summary>
    /// Без EventSystem uGUI не получает ввод вообще. Модуль обязательно
    /// InputSystemUIInputModule: в проекте включён только новый Input System,
    /// со StandaloneInputModule кнопки молча перестали бы нажиматься.
    /// </summary>
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("AppCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiFactory.ReferenceResolution;
        // Тянемся по высоте: игра портретная, и вертикальный макет важнее ширины.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // Три слоя в порядке перекрытия: экраны, рамка, модалки.
        RectTransform screens = UiFactory.CreateFullScreen("Screens", canvasObject.transform);
        RectTransform chrome = UiFactory.CreateFullScreen("Chrome", canvasObject.transform);
        RectTransform modals = UiFactory.CreateFullScreen("Modals", canvasObject.transform);

        _stack = new ScreenStack(screens, modals);

        _confirm = new ConfirmModal(_stack);
        _settings = new SettingsModal(_meta, _stack);
        _briefing = new LevelBriefingModal(_stack, _meta);
        _stack.RegisterModal(_confirm);
        _stack.RegisterModal(_settings);
        _stack.RegisterModal(_briefing);

        _splash = new SplashScreen(OnSplashFinished);
        // Смена патогена ничего не оповещает: до карусели нельзя добраться,
        // не пройдя через GoHome, а тот уже спросил подтверждение на сжигание
        // билда. Билд привязан к патогену и пересадке не подлежит.
        _home = new HomeScreen(_meta, _ => { });
        _upgradesScreen = new UpgradesScreen(_meta, _confirm);
        _wardrobe = new StubScreen("ОДЕЖДА",
            "Косметика и бафы от неё появятся отдельным разделом.\nСейчас здесь пусто — это честная заглушка, а не ошибка.");
        _battle = new StubScreen("БИТВА",
            "Бои с боссами вне кампании появятся, когда боссов станет больше одного.\nСейчас единственный босс живёт в конце «Кровотока».");
        _mapScreen = new CampaignMapScreen(_map, _meta, OnNodePicked);
        _combat = new CombatHud(_runner, _spawner);
        _result = new LevelResultScreen(_meta, OnUpgradePicked, GoToMap);

        _stack.Register(_splash);
        _stack.Register(_home);
        _stack.Register(_upgradesScreen);
        _stack.Register(_wardrobe);
        _stack.Register(_battle);
        _stack.Register(_mapScreen);
        _stack.Register(_combat);
        _stack.Register(_result);

        _chrome = new ShellChrome(chrome, _meta, GoHome, OpenSettings, OnTabPicked);
    }

    // --- Навигация ---

    private void OnSplashFinished()
    {
        _chrome.SetVisible(true);
        GoHome();
    }

    private void OpenSettings() => _stack.PushModal(_settings);

    private void GoHome()
    {
        LeaveBiomeIfNeeded(() =>
        {
            _chrome.SetVisible(true);
            _chrome.Refresh();
            _stack.Show(_home);
        });
    }

    private void GoToMap()
    {
        _chrome.SetVisible(true);
        _chrome.Refresh();
        _chrome.SetActiveTab(AppTab.Campaign);
        _stack.Show(_mapScreen);
    }

    private void OnTabPicked(AppTab tab)
    {
        LeaveBiomeIfNeeded(() =>
        {
            _chrome.SetVisible(true);
            _chrome.SetActiveTab(tab);
            _chrome.Refresh();

            switch (tab)
            {
                case AppTab.Upgrades: _stack.Show(_upgradesScreen); break;
                case AppTab.Wardrobe: _stack.Show(_wardrobe); break;
                case AppTab.Battle: _stack.Show(_battle); break;
                default: _stack.Show(_mapScreen); break;
            }
        });
    }

    /// <summary>
    /// Уход из биома сжигает билд, поэтому спрашивается подтверждение.
    /// Пока идёт бой, уходить нельзя вовсе — исход узла должен состояться.
    /// </summary>
    private void LeaveBiomeIfNeeded(System.Action then)
    {
        if (_run == null || _runner.IsRunning)
        {
            then();
            return;
        }

        _confirm.Ask(
            "Выйти из биома?",
            "Собранные апгрейды и мутации пропадут. Пройденные узлы и звёзды останутся.",
            "Выйти",
            () =>
            {
                DiscardRun();
                then();
            });
    }

    private void DiscardRun()
    {
        if (_run != null)
        {
            _meta.RecordBiomeAttempt(_run.TotalKills);
            _run = null;
        }

        _runner.AbortNode();
    }

    // --- Кампания ---

    private void OnNodePicked(CampaignNode node)
    {
        if (node == null)
        {
            return;
        }

        _briefing.Open(node, () => StartNode(node));
    }

    private void StartNode(CampaignNode node)
    {
        BiomeData biome = _map.BiomeOf(node);

        // Билд живёт ровно один биом: заход в другой биом начинает попытку заново.
        if (_run != null && _run.BiomeId != biome.Id)
        {
            DiscardRun();
        }

        if (_run == null)
        {
            PathogenType type = PathogenCarousel.Types[PathogenCarousel.IndexOf(_meta.Progress.lastPathogen)];
            _upgrades.ResetRun();
            _run = BiomeRun.Create(biome.Id, PathogenData.CreateDefault(type), _meta);
        }

        _chrome.SetVisible(false);
        _stack.Show(_combat);
        _runner.StartNode(node, _run);
    }

    private void OnNodeFinished(NodeOutcome outcome)
    {
        CampaignProgress progress = _meta.Progress.campaign;
        int previousStars = progress.StarsOf(outcome.Node.Id);
        int stars = outcome.Stars;

        Reward reward = Reward.Zero;
        List<UpgradeDefinition> choices = null;

        if (outcome.Cleared)
        {
            CampaignRules.ApplyClear(_map, progress, outcome.Node, stars);
            reward = _meta.AwardNode(outcome.Node, previousStars, stars);
            _run.RegisterClear(outcome.Node.Id, outcome.Kills);

            // Апгрейд — только за первое прохождение узла в этой попытке биома.
            // Иначе первый узел фармится до полного билда, и босс перестаёт быть
            // проверкой того, что игрок собрал.
            if (_run.ShouldGrantUpgrade(outcome.Node.Id))
            {
                choices = _upgrades.Roll(_run.Stats, _run.NodesCleared);
                if (choices.Count == 0)
                {
                    choices = null;
                }
            }
        }
        else
        {
            DiscardRun();
        }

        _stack.Show(_result);
        _result.Present(outcome, reward, choices);
    }

    private void OnUpgradePicked(UpgradeDefinition upgrade)
    {
        if (_run != null && _runner.CurrentNode != null)
        {
            _upgrades.Take(upgrade, _run.Stats, _runner.Player);
            _run.MarkUpgradeGranted(_runner.CurrentNode.Id);
            _combat.NotifyMutationsChanged();
        }

        GoToMap();
    }
}
```

- [ ] **Шаг 3: Переписать хвост `GameBootstrap.Awake`**

В `Assets/Scripts/Core/GameBootstrap.cs` заменить блок от создания систем
до конца метода. Было:

```csharp
        var difficulty = gameObjectRoot.AddComponent<DifficultyDirector>();
        var upgrades = gameObjectRoot.AddComponent<UpgradeSystem>();
        var spawner = gameObjectRoot.AddComponent<EnemySpawner>();
        var meta = gameObjectRoot.AddComponent<MetaProgression>();
        var runner = gameObjectRoot.AddComponent<GameRunner>();

        var store = new JsonProgressStore();
        Debug.Log($"[Meta] Файл прогресса: {store.FilePath}");
        meta.Initialize(store);

        runner.Initialize(pools, spawner, difficulty, upgrades, meta);

        if (useLegacyImguiHud)
        {
            var legacy = gameObjectRoot.AddComponent<PrototypeHud>();
            legacy.runner = runner;
            legacy.spawner = spawner;
            legacy.upgrades = upgrades;
            legacy.meta = meta;
        }
        else
        {
            var hud = gameObjectRoot.AddComponent<GameHud>();
            hud.Initialize(runner, spawner, upgrades, meta);
        }
```

Стало:

```csharp
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
```

Там же удалить поле аварийного отката — второй копии интерфейса больше нет:

```csharp
    [Tooltip("Аварийный откат на IMGUI-заглушку Фазы 1, если с uGUI что-то не так.")]
    public bool useLegacyImguiHud;
```

- [ ] **Шаг 4: Удалить старый UI**

```powershell
Remove-Item "Assets\Scripts\UI\GameHud.cs","Assets\Scripts\UI\GameHud.cs.meta" -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\UI\GameHud.cs.disabled","Assets\Scripts\UI\GameHud.cs.disabled.meta" -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\UI\PrototypeHud.cs","Assets\Scripts\UI\PrototypeHud.cs.meta"
```

- [ ] **Шаг 5: Прогнать весь набор тестов**

Ожидаемо: проект компилируется целиком, все EditMode-тесты зелёные.
Ориентир по количеству: 61 прежний (64 минус три удалённых `AwardRun`) плюс
6 миграции, 8 кампании, 6 звёзд, 6 наград, 6 правил, 5 `BiomeRun`,
3 исхода, 8 стека экранов, 4 звука, 4 карусели — **117**.

- [ ] **Шаг 6: Ручная проверка в редакторе**

Открыть `Assets/Scenes/SampleScene.unity`, нажать Play и пройти список:

1. Показывается заставка с полосой прогресса, примерно через секунду сама
   уходит на главный экран.
2. На главном виден патоген, стрелки листают всех четырёх по кругу.
3. Шестерёнка слева вверху открывает настройки поверх экрана; слайдеры
   двигаются, имя вводится, «Готово» закрывает.
4. Перезапуск Play: показан тот же патоген и те же настройки — значит, сейв
   версии 2 пишется и читается.
5. Вкладка «Улучшения» показывает пять перков и статистику; «Одежда» и «Битва» —
   заглушки; «Кампания» — карта.
6. На карте открыт только первый узел «Кровотока», остальные серые. Биомы 2 и 3
   подписаны «в разработке».
7. Тап по узлу открывает брифинг с составом врагов, наградой и эталонным
   временем. «В бой» запускает уровень, рамка оболочки исчезает.
8. Уровень проходится, показывается результат со звёздами, наградой и выбором
   из трёх улучшений. Выбор возвращает на карту, второй узел открылся.
9. Повторный заход в первый узел: награда меньше, карточек улучшений нет,
   вместо них кнопка «На карту».
10. Смерть на узле: звёзд нет, награды нет, на карте узел остался пройденным,
    а следующий заход начинается с чистым билдом.
11. Переход на другую вкладку с живым билдом спрашивает подтверждение.
12. Дойти до восьмого узла и убить Лимфоузел: биом 2 меняет подпись с «закрыт»
    на «в разработке» — правило разблокировки сработало.

Пункты 4, 9, 10 и 12 — те, ради которых написаны тесты миграции, выплат и
правил; если они расходятся с поведением в редакторе, ошибка в проводке, а не
в логике.

---

## Что остаётся после плана

- Перенос собранной кодом иерархии в авторскую сцену с префабами — оставшийся
  пункт Фазы 2 из `Assets/Scripts/README.md`.
- Плейтест на 3-5 незнакомых людях — критерий готовности Фазы 2.
- `BiomeRun` не переживает убийство приложения: билд живёт только в памяти.
- Одежда и Битва — заглушки. Биомы 2 и 3 ждут врагов из Фазы 3.
- Обновить `Assets/Scripts/README.md` под новую структуру экранов.
