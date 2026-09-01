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
