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
