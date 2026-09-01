using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Весь интерфейс игры на uGUI. Заменяет IMGUI-заглушку Фазы 1: тот вариант
/// не виден на устройстве нормально, не масштабируется и не переносится в сцену.
/// Здесь строится обычная Canvas-иерархия — та же, что получилась бы при сборке
/// руками, поэтому её можно будет вытащить в префаб без переписывания логики.
/// </summary>
public class GameHud : MonoBehaviour
{
    /// Сколько полосок сегментов босса держать наготове.
    private const int MaxBossBars = 8;

    private GameRunner _runner;
    private EnemySpawner _spawner;
    private UpgradeSystem _upgrades;
    private MetaProgression _meta;

    // --- Экраны ---
    private RectTransform _selectScreen;
    private RectTransform _shopScreen;
    private RectTransform _combatScreen;
    private RectTransform _upgradeScreen;
    private RectTransform _gameOverScreen;

    // --- Экран выбора ---
    private PathogenData[] _previews;
    private Text _selectBiomass;
    private Text _bossDebugLabel;
    private Image _bossDebugImage;
    private bool _startAtBoss;

    // --- Магазин ---
    private Text _shopBiomass;
    private Text _shopStats;
    private readonly List<Text> _shopRowLabels = new List<Text>();
    private readonly List<Image> _shopRowImages = new List<Image>();

    // --- Бой ---
    private Image _healthFill;
    private Text _healthText;
    private Text _infoText;
    private Text _abilityText;
    private Text _mutationsText;
    private Text _bossName;
    private readonly List<Image> _bossFills = new List<Image>();
    private readonly List<Image> _bossBackgrounds = new List<Image>();
    private readonly List<Text> _bossLabels = new List<Text>();

    // --- Выбор апгрейда ---
    private readonly List<Button> _upgradeButtons = new List<Button>();
    private readonly List<Text> _upgradeLabels = new List<Text>();
    private readonly List<Image> _upgradeImages = new List<Image>();

    // --- Смерть ---
    private Text _gameOverText;

    private static readonly PathogenType[] SelectableTypes =
    {
        PathogenType.Virus, PathogenType.Bacteria, PathogenType.Fungus, PathogenType.Parasite
    };

    private static readonly string[] SelectableHints =
    {
        "Вспышка — убитые враги заражаются и бьют своих",
        "Биоплёнка — щит поглощает один удар, затем восстанавливается",
        "Споры — попадания оставляют тлеющие зоны урона",
        "Прятки — смертельный удар превращается в 2с невидимости"
    };

    public void Initialize(GameRunner runner, EnemySpawner spawner, UpgradeSystem upgrades, MetaProgression meta)
    {
        _runner = runner;
        _spawner = spawner;
        _upgrades = upgrades;
        _meta = meta;

        EnsureEventSystem();
        Transform canvas = BuildCanvas();

        BuildSelectScreen(canvas);
        BuildShopScreen(canvas);
        BuildCombatScreen(canvas);
        BuildUpgradeScreen(canvas);
        BuildGameOverScreen(canvas);

        // Всё, кроме стартового экрана, гасится сразу: Update определяет видимость
        // магазина по его же activeSelf, и оставленный включённым магазин перекрыл бы
        // выбор патогена на первом кадре.
        _shopScreen.gameObject.SetActive(false);
        _combatScreen.gameObject.SetActive(false);
        _upgradeScreen.gameObject.SetActive(false);
        _gameOverScreen.gameObject.SetActive(false);
        _selectScreen.gameObject.SetActive(true);

        RefreshSelect();
    }

    // --- Каркас ---

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

    private Transform BuildCanvas()
    {
        var canvasObject = new GameObject("HudCanvas");
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
        return canvasObject.transform;
    }

    // --- Экран выбора патогена ---

    private void BuildSelectScreen(Transform canvas)
    {
        _selectScreen = UiFactory.CreateFullScreen("SelectScreen", canvas);

        Text title = UiFactory.CreateText("Title", _selectScreen, "ПАТОГЕН vs ИММУННАЯ СИСТЕМА", 38,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 56f, UiFactory.ContentWidth, 64f);

        _selectBiomass = UiFactory.CreateText("Biomass", _selectScreen, "Биомасса: 0", 26, TextAnchor.MiddleLeft);
        RectTransform biomassRect = UiFactory.TopAnchored(_selectBiomass.rectTransform, 134f, 400f, 48f);
        biomassRect.anchoredPosition = new Vector2(-(UiFactory.ReferenceResolution.x * 0.5f) + UiFactory.Margin + 200f, -134f);

        Button shopButton = UiFactory.CreateButton("ShopButton", _selectScreen, "Улучшения", 26,
            new Color(0.55f, 0.60f, 0.75f), out _);
        RectTransform shopRect = UiFactory.TopAnchored((RectTransform)shopButton.transform, 130f, 220f, 58f);
        shopRect.anchoredPosition = new Vector2((UiFactory.ReferenceResolution.x * 0.5f) - UiFactory.Margin - 110f, -130f);
        shopButton.onClick.AddListener(() => SetScreen(_shopScreen));

        Text subtitle = UiFactory.CreateText("Subtitle", _selectScreen, "Биом «Кровоток». Выберите патоген:", 24);
        UiFactory.TopAnchored(subtitle.rectTransform, 200f, UiFactory.ContentWidth, 40f);

        _previews = new PathogenData[SelectableTypes.Length];
        float y = 250f;

        for (int i = 0; i < SelectableTypes.Length; i++)
        {
            _previews[i] = PathogenData.CreateDefault(SelectableTypes[i]);
            PathogenData preview = _previews[i];

            Button button = UiFactory.CreateButton($"Pathogen_{preview.pathogenName}", _selectScreen,
                $"{preview.pathogenName}\n{SelectableHints[i]}", 24, preview.bodyColor, out _);
            UiFactory.TopAnchored((RectTransform)button.transform, y, UiFactory.ContentWidth, 118f);

            button.onClick.AddListener(() => StartRun(preview));
            y += 134f;
        }

        Button bossButton = UiFactory.CreateButton("BossDebug", _selectScreen, string.Empty, 22,
            Color.white, out _bossDebugLabel);
        _bossDebugImage = bossButton.GetComponent<Image>();
        UiFactory.TopAnchored((RectTransform)bossButton.transform, y + 10f, UiFactory.ContentWidth, 74f);
        bossButton.onClick.AddListener(() =>
        {
            _startAtBoss = !_startAtBoss;
            RefreshBossDebugButton();
        });
        RefreshBossDebugButton();

        Text hint = UiFactory.CreateText("Hint", _selectScreen,
            "Управление: тяните палец (или ЛКМ) влево-вправо. Атака автоматическая.", 22, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(hint.rectTransform, y + 100f, UiFactory.ContentWidth, 70f);
    }

    private void StartRun(PathogenData preview)
    {
        _runner.StartRun(preview, _startAtBoss ? _runner.FirstBossLevelIndex : 0);
    }

    private void RefreshBossDebugButton()
    {
        _bossDebugLabel.text = _startAtBoss
            ? "Старт с босса: ВКЛ — выберите патогена выше"
            : "Старт с босса: ВЫКЛ — нажмите, чтобы пропустить биом";
        _bossDebugImage.color = _startAtBoss
            ? new Color(0.90f, 0.45f, 0.45f)
            : new Color(0.50f, 0.50f, 0.55f);
    }

    // --- Магазин перманентных улучшений ---

    private void BuildShopScreen(Transform canvas)
    {
        _shopScreen = UiFactory.CreateFullScreen("ShopScreen", canvas);

        Text title = UiFactory.CreateText("Title", _shopScreen, "ПЕРМАНЕНТНЫЕ УЛУЧШЕНИЯ", 34,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 56f, UiFactory.ContentWidth, 60f);

        _shopBiomass = UiFactory.CreateText("Biomass", _shopScreen, "Биомасса: 0", 26);
        UiFactory.TopAnchored(_shopBiomass.rectTransform, 132f, UiFactory.ContentWidth, 44f);

        float y = 192f;
        IReadOnlyList<PermanentUpgrade> upgradeList = _meta.Upgrades;

        for (int i = 0; i < upgradeList.Count; i++)
        {
            PermanentUpgrade upgrade = upgradeList[i];

            Button button = UiFactory.CreateButton($"Perk_{upgrade.Id}", _shopScreen, string.Empty, 23,
                Color.white, out Text label);
            UiFactory.TopAnchored((RectTransform)button.transform, y, UiFactory.ContentWidth, 92f);

            button.onClick.AddListener(() =>
            {
                _meta.TryPurchase(upgrade);
                RefreshShop();
            });

            _shopRowLabels.Add(label);
            _shopRowImages.Add(button.GetComponent<Image>());
            y += 104f;
        }

        Button back = UiFactory.CreateButton("Back", _shopScreen, "Назад", 28,
            new Color(0.70f, 0.72f, 0.78f), out _);
        UiFactory.TopAnchored((RectTransform)back.transform, y + 12f, UiFactory.ContentWidth, 78f);
        back.onClick.AddListener(() => SetScreen(_selectScreen));

        Button reset = UiFactory.CreateButton("Reset", _shopScreen, "Сбросить прогресс (отладка)", 20,
            new Color(0.60f, 0.45f, 0.45f), out _);
        UiFactory.TopAnchored((RectTransform)reset.transform, y + 104f, UiFactory.ContentWidth, 60f);
        reset.onClick.AddListener(() =>
        {
            // Нужно именно для плейтестов: иначе нельзя посмотреть, как игра
            // выглядит для человека, запустившего её впервые.
            _meta.ResetProgress();
            RefreshShop();
        });

        _shopStats = UiFactory.CreateText("Stats", _shopScreen, string.Empty, 20, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(_shopStats.rectTransform, y + 178f, UiFactory.ContentWidth, 60f);
    }

    private void RefreshShop()
    {
        PlayerProgress progress = _meta.Progress;
        _shopBiomass.text = $"Биомасса: {progress.biomass}";
        _shopStats.text = $"Забегов: {progress.totalRuns} · всего убито: {progress.totalKills} · боссов: {progress.bossesDefeated}";

        IReadOnlyList<PermanentUpgrade> upgradeList = _meta.Upgrades;
        for (int i = 0; i < upgradeList.Count && i < _shopRowLabels.Count; i++)
        {
            PermanentUpgrade upgrade = upgradeList[i];
            int level = _meta.LevelOf(upgrade);
            bool maxed = upgrade.IsMaxed(level);

            _shopRowLabels[i].text = maxed
                ? $"{upgrade.Title}  [{level}/{upgrade.MaxLevel}]\nмаксимум"
                : $"{upgrade.Title}  [{level}/{upgrade.MaxLevel}]\n{upgrade.PerLevelDescription} · цена {upgrade.CostForNextLevel(level)}";

            // Недоступное показывается, а не прячется: это цель следующего забега.
            _shopRowImages[i].color = maxed
                ? new Color(0.45f, 0.47f, 0.50f)
                : _meta.CanAfford(upgrade)
                    ? new Color(0.50f, 0.80f, 0.55f)
                    : new Color(0.62f, 0.48f, 0.48f);
        }
    }

    // --- Боевой HUD ---

    private void BuildCombatScreen(Transform canvas)
    {
        _combatScreen = UiFactory.CreateFullScreen("CombatScreen", canvas);

        _healthFill = UiFactory.CreateBar("Health", _combatScreen, new Color(0.35f, 0.85f, 0.45f), out Image background);
        UiFactory.TopAnchored(background.rectTransform, 22f, UiFactory.ContentWidth, 38f);

        _healthText = UiFactory.CreateText("HealthText", background.transform, string.Empty, 22, TextAnchor.MiddleCenter);
        RectTransform healthTextRect = _healthText.rectTransform;
        healthTextRect.anchorMin = Vector2.zero;
        healthTextRect.anchorMax = Vector2.one;
        healthTextRect.offsetMin = Vector2.zero;
        healthTextRect.offsetMax = Vector2.zero;

        _infoText = UiFactory.CreateText("Info", _combatScreen, string.Empty, 22);
        UiFactory.TopAnchored(_infoText.rectTransform, 70f, UiFactory.ContentWidth, 110f);

        _bossName = UiFactory.CreateText("BossName", _combatScreen, string.Empty, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
        UiFactory.TopAnchored(_bossName.rectTransform, 186f, UiFactory.ContentWidth, 32f);

        for (int i = 0; i < MaxBossBars; i++)
        {
            Image fill = UiFactory.CreateBar($"BossBar{i}", _combatScreen, Color.white, out Image barBackground);
            UiFactory.TopAnchored(barBackground.rectTransform, 222f + i * 34f, UiFactory.ContentWidth, 28f);

            Text label = UiFactory.CreateText($"BossBarLabel{i}", barBackground.transform, string.Empty, 19,
                TextAnchor.MiddleLeft);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.08f, 0.08f, 0.10f);

            _bossFills.Add(fill);
            _bossBackgrounds.Add(barBackground);
            _bossLabels.Add(label);
            barBackground.gameObject.SetActive(false);
        }

        _abilityText = UiFactory.CreateText("Ability", _combatScreen, string.Empty, 22);
        UiFactory.TopAnchored(_abilityText.rectTransform, UiFactory.ReferenceResolution.y - 128f,
            UiFactory.ContentWidth, 50f);

        _mutationsText = UiFactory.CreateText("Mutations", _combatScreen, string.Empty, 20);
        UiFactory.TopAnchored(_mutationsText.rectTransform, UiFactory.ReferenceResolution.y - 76f,
            UiFactory.ContentWidth, 50f);
        _mutationsText.color = new Color(0.85f, 0.55f, 0.90f);
    }

    // --- Выбор апгрейда ---

    private void BuildUpgradeScreen(Transform canvas)
    {
        _upgradeScreen = UiFactory.CreateFullScreen("UpgradeScreen", canvas);

        Image dim = UiFactory.CreateImage("Dim", _upgradeScreen, new Color(0f, 0f, 0f, 0.78f));
        RectTransform dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        Text title = UiFactory.CreateText("Title", _upgradeScreen, "УРОВЕНЬ ЗАЧИЩЕН", 38,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 230f, UiFactory.ContentWidth, 60f);

        Text subtitle = UiFactory.CreateText("Subtitle", _upgradeScreen, "Выберите одно улучшение:", 24,
            TextAnchor.MiddleCenter);
        UiFactory.TopAnchored(subtitle.rectTransform, 300f, UiFactory.ContentWidth, 40f);

        for (int i = 0; i < UpgradeSystem.ChoiceCount; i++)
        {
            int index = i;
            Button button = UiFactory.CreateButton($"Choice{i}", _upgradeScreen, string.Empty, 24,
                Color.white, out Text label);
            UiFactory.TopAnchored((RectTransform)button.transform, 360f + i * 152f, UiFactory.ContentWidth, 136f);

            button.onClick.AddListener(() => ChooseUpgrade(index));

            _upgradeButtons.Add(button);
            _upgradeLabels.Add(label);
            _upgradeImages.Add(button.GetComponent<Image>());
        }
    }

    private void ChooseUpgrade(int index)
    {
        IReadOnlyList<UpgradeDefinition> choices = _runner.PendingUpgrades;
        if (index >= 0 && index < choices.Count)
        {
            _runner.ChooseUpgrade(choices[index]);
        }
    }

    private void RefreshUpgradeChoices()
    {
        IReadOnlyList<UpgradeDefinition> choices = _runner.PendingUpgrades;

        for (int i = 0; i < _upgradeButtons.Count; i++)
        {
            bool has = i < choices.Count;
            _upgradeButtons[i].gameObject.SetActive(has);
            if (!has)
            {
                continue;
            }

            UpgradeDefinition upgrade = choices[i];
            int taken = _upgrades != null ? _upgrades.TakenCount(upgrade.Id) : 0;

            _upgradeLabels[i].text = upgrade.IsMutation
                ? $"МУТАЦИЯ · {upgrade.Title}\n{upgrade.Description}"
                : taken > 0
                    ? $"{upgrade.Title}  (взято {taken})\n{upgrade.Description}"
                    : $"{upgrade.Title}\n{upgrade.Description}";

            // Мутация должна читаться как другой класс выбора, а не как очередные +15%.
            _upgradeImages[i].color = upgrade.IsMutation
                ? new Color(0.85f, 0.50f, 0.92f)
                : new Color(0.86f, 0.88f, 0.92f);
        }
    }

    // --- Экран смерти ---

    private void BuildGameOverScreen(Transform canvas)
    {
        _gameOverScreen = UiFactory.CreateFullScreen("GameOverScreen", canvas);

        Image dim = UiFactory.CreateImage("Dim", _gameOverScreen, new Color(0f, 0f, 0f, 0.85f));
        RectTransform dimRect = dim.rectTransform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        Text title = UiFactory.CreateText("Title", _gameOverScreen, "ПАТОГЕН УНИЧТОЖЕН", 38,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 340f, UiFactory.ContentWidth, 60f);

        _gameOverText = UiFactory.CreateText("Results", _gameOverScreen, string.Empty, 26, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(_gameOverText.rectTransform, 420f, UiFactory.ContentWidth, 200f);

        Button restart = UiFactory.CreateButton("Restart", _gameOverScreen, "Заново", 30,
            new Color(0.55f, 0.80f, 0.60f), out _);
        UiFactory.TopAnchored((RectTransform)restart.transform, 660f, UiFactory.ContentWidth, 96f);
        restart.onClick.AddListener(() => _runner.RestartToSelect());
    }

    // --- Обновление ---

    private void SetScreen(RectTransform screen)
    {
        _selectScreen.gameObject.SetActive(screen == _selectScreen);
        _shopScreen.gameObject.SetActive(screen == _shopScreen);

        if (screen == _shopScreen)
        {
            RefreshShop();
        }
        else if (screen == _selectScreen)
        {
            RefreshSelect();
        }
    }

    private void RefreshSelect()
    {
        if (_meta != null && _meta.Progress != null)
        {
            _selectBiomass.text = $"Биомасса: {_meta.Progress.biomass}";
        }
    }

    private void Update()
    {
        if (_runner == null)
        {
            return;
        }

        bool inMenu = _runner.State == GameState.PathogenSelect;
        bool shopOpen = inMenu && _shopScreen.gameObject.activeSelf;

        _selectScreen.gameObject.SetActive(inMenu && !shopOpen);
        _shopScreen.gameObject.SetActive(shopOpen);
        _combatScreen.gameObject.SetActive(_runner.State == GameState.Playing || _runner.State == GameState.UpgradeChoice);
        _upgradeScreen.gameObject.SetActive(_runner.State == GameState.UpgradeChoice);
        _gameOverScreen.gameObject.SetActive(_runner.State == GameState.GameOver);

        if (inMenu && !shopOpen)
        {
            RefreshSelect();
            return;
        }

        if (_runner.State == GameState.GameOver)
        {
            RefreshGameOver();
            return;
        }

        if (_combatScreen.gameObject.activeSelf)
        {
            RefreshCombat();
        }

        if (_runner.State == GameState.UpgradeChoice)
        {
            RefreshUpgradeChoices();
        }
    }

    private void RefreshCombat()
    {
        PlayerController player = _runner.Player;
        if (player == null)
        {
            return;
        }

        _healthFill.fillAmount = player.Health.Normalized;
        _healthFill.color = Color.Lerp(new Color(0.85f, 0.20f, 0.20f), new Color(0.35f, 0.85f, 0.45f),
            player.Health.Normalized);
        _healthText.text = $"{Mathf.CeilToInt(player.Health.Current)} / {Mathf.CeilToInt(player.Health.Max)}";

        string level = _runner.CurrentLevel != null ? _runner.CurrentLevel.levelName : "-";
        int kills = _spawner != null ? _spawner.Kills : 0;
        _infoText.text = $"Уровень {_runner.LevelNumber + 1} · {level}\n" +
                         $"Убито за уровень: {kills} · всего: {_runner.TotalKills}\n" +
                         $"На поле: {Battlefield.ThreatCount}";

        _abilityText.text = player.Ability != null ? player.Ability.StatusLine : string.Empty;
        _mutationsText.text = _runner.Stats != null && _runner.Stats.TakenMutations.Count > 0
            ? "Мутации: " + string.Join(", ", _runner.Stats.TakenMutations)
            : string.Empty;

        RefreshBossBars();
    }

    /// <summary>
    /// Полоска на сегмент, а не одна общая: игрок должен видеть, что цель составная
    /// и что каждый снятый сегмент убирает конкретную атаку.
    /// </summary>
    private void RefreshBossBars()
    {
        Boss boss = _spawner != null ? _spawner.ActiveBoss : null;

        if (boss == null)
        {
            _bossName.text = string.Empty;
            for (int i = 0; i < _bossBackgrounds.Count; i++)
            {
                _bossBackgrounds[i].gameObject.SetActive(false);
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
                : $"{segment.Definition.segmentName} — уничтожен";
        }
    }

    private void RefreshGameOver()
    {
        string pathogen = _runner.Stats != null ? _runner.Stats.Source.pathogenName : "-";
        string reward = _meta != null && _meta.Progress != null
            ? $"\nПолучено биомассы: {_meta.LastRunReward} (всего {_meta.Progress.biomass})"
            : string.Empty;

        _gameOverText.text = $"Патоген: {pathogen}\n" +
                             $"Пройдено уровней: {_runner.LevelNumber}\n" +
                             $"Всего убито: {_runner.TotalKills}{reward}";
    }
}
