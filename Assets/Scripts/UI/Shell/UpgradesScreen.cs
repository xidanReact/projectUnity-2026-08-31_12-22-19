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
