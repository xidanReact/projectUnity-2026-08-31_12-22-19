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
