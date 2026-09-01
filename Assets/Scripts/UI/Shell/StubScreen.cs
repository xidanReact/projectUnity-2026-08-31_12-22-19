using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Раздел, которого ещё нет. Честная заглушка лучше отключённой вкладки:
/// на плейтесте сразу видно, что раздел запланирован, а не сломан.
/// </summary>
public class StubScreen : UiScreen
{
    private readonly string _title;
    private readonly string _body;

    public StubScreen(string title, string body)
    {
        _title = title;
        _body = body;
    }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.09f, 0.10f, 0.13f));
        UiFactory.Stretch(backdrop.rectTransform);

        Text title = UiFactory.CreateText("Title", Root, _title, 32,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 420f, UiFactory.ContentWidth, 56f);

        Text body = UiFactory.CreateText("Body", Root, _body, 22, TextAnchor.UpperCenter);
        body.color = new Color(0.70f, 0.72f, 0.78f);
        UiFactory.TopAnchored(body.rectTransform, 486f, UiFactory.ContentWidth, 200f);
    }
}
