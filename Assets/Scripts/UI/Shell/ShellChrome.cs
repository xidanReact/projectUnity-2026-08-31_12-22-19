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
