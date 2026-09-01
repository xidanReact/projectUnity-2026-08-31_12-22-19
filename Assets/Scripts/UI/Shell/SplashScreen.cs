using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Заставка при запуске. Картинки ещё нет — на её месте заливка и название,
/// но слот под спрайт сделан сразу: в Фазе 2.5 меняется только Sprite у Art.
///
/// Минимальное время показа нужно не для красоты: инициализация на быстром
/// устройстве заканчивается за один кадр, и экран мигнул бы, а не показался.
/// </summary>
public class SplashScreen : UiScreen
{
    public const float MinimumSeconds = 1.2f;

    private readonly Action _onFinished;

    private Image _progressFill;
    private float _elapsed;
    private bool _done;

    public SplashScreen(Action onFinished)
    {
        _onFinished = onFinished;
    }

    /// <summary>Место под будущую заставку: подставить спрайт и убрать заливку.</summary>
    public Image Art { get; private set; }

    protected override void OnBuild()
    {
        Image backdrop = UiFactory.CreateImage("Backdrop", Root, new Color(0.10f, 0.04f, 0.06f));
        UiFactory.Stretch(backdrop.rectTransform);

        Art = UiFactory.CreateImage("Art", Root, new Color(0.55f, 0.16f, 0.22f));
        UiFactory.TopAnchored(Art.rectTransform, 300f, UiFactory.ContentWidth, 460f);

        Text title = UiFactory.CreateText("Title", Root, "ПАТОГЕН\nvs\nИММУННАЯ СИСТЕМА", 44,
            TextAnchor.MiddleCenter, FontStyle.Bold);
        UiFactory.TopAnchored(title.rectTransform, 430f, UiFactory.ContentWidth, 200f);

        _progressFill = UiFactory.CreateBar("Progress", Root, new Color(0.85f, 0.35f, 0.40f),
            out Image background);
        UiFactory.BottomAnchored(background.rectTransform, 160f, UiFactory.ContentWidth, 22f);
        _progressFill.fillAmount = 0f;
    }

    protected override void OnShow()
    {
        _elapsed = 0f;
        _done = false;
        _progressFill.fillAmount = 0f;
    }

    protected override void OnTick()
    {
        if (_done)
        {
            return;
        }

        _elapsed += Time.unscaledDeltaTime;
        _progressFill.fillAmount = Mathf.Clamp01(_elapsed / MinimumSeconds);

        if (_elapsed >= MinimumSeconds)
        {
            _done = true;
            _onFinished?.Invoke();
        }
    }
}
