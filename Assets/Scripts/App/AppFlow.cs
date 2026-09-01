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
