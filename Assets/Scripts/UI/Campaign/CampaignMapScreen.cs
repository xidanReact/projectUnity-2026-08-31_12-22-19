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
