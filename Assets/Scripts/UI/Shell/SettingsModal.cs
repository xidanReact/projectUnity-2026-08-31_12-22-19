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
