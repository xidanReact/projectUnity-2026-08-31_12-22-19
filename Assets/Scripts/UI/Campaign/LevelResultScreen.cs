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
