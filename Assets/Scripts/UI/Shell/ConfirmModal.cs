using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Подтверждение необратимого действия. Одна переиспользуемая модалка на всё:
/// выход из биома со сгоранием билда и сброс прогресса на плейтестах.
/// </summary>
public class ConfirmModal : UiScreen
{
    private readonly ScreenStack _stack;

    private Text _title;
    private Text _body;
    private Text _confirmLabel;
    private Action _onConfirm;

    public ConfirmModal(ScreenStack stack)
    {
        _stack = stack;
    }

    public void Ask(string title, string body, string confirmLabel, Action onConfirm)
    {
        _onConfirm = onConfirm;
        _title.text = title;
        _body.text = body;
        _confirmLabel.text = confirmLabel;
        _stack.PushModal(this);
    }

    protected override void OnBuild()
    {
        Image dim = UiFactory.CreateImage("Dim", Root, new Color(0f, 0f, 0f, 0.82f));
        UiFactory.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image panel = UiFactory.CreateImage("Panel", Root, new Color(0.16f, 0.13f, 0.15f));
        UiFactory.TopAnchored(panel.rectTransform, 420f, UiFactory.ContentWidth, 400f);

        _title = UiFactory.CreateText("Title", panel.transform, string.Empty, 30,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(_title.rectTransform, 26f, UiFactory.ContentWidth - 40f, 50f);

        _body = UiFactory.CreateText("Body", panel.transform, string.Empty, 22, TextAnchor.UpperCenter);
        UiFactory.TopAnchored(_body.rectTransform, 88f, UiFactory.ContentWidth - 60f, 120f);

        Button confirm = UiFactory.CreateButton("Confirm", panel.transform, string.Empty, 26,
            new Color(0.88f, 0.48f, 0.45f), out _confirmLabel);
        UiFactory.TopAnchored((RectTransform)confirm.transform, 218f, UiFactory.ContentWidth - 60f, 72f);
        confirm.onClick.AddListener(() =>
        {
            Action action = _onConfirm;
            _stack.PopModal();
            action?.Invoke();
        });

        Button cancel = UiFactory.CreateButton("Cancel", panel.transform, "Отмена", 26,
            new Color(0.62f, 0.64f, 0.70f), out _);
        UiFactory.TopAnchored((RectTransform)cancel.transform, 302f, UiFactory.ContentWidth - 60f, 72f);
        cancel.onClick.AddListener(() => _stack.PopModal());
    }
}
