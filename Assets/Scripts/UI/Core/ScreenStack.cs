using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Кто сейчас на экране. Модалки лежат отдельным стеком поверх текущего экрана:
/// настройки и подтверждения не должны выбивать игрока из того, что под ними.
/// </summary>
public class ScreenStack
{
    private readonly Transform _screenRoot;

    /// <summary>
    /// Отдельный родитель для модалок. Между ним и экранами лежит шапка с
    /// таб-баром: экран должен быть под ней, а модалка — над.
    /// </summary>
    private readonly Transform _modalRoot;

    private readonly List<UiScreen> _registered = new List<UiScreen>();
    private readonly List<UiScreen> _modals = new List<UiScreen>();

    public ScreenStack(Transform screenRoot, Transform modalRoot = null)
    {
        _screenRoot = screenRoot;
        _modalRoot = modalRoot != null ? modalRoot : screenRoot;
    }

    public UiScreen Current { get; private set; }

    public int ModalDepth => _modals.Count;

    public void Register(UiScreen screen)
    {
        Register(screen, _screenRoot);
    }

    public void RegisterModal(UiScreen modal)
    {
        Register(modal, _modalRoot);
    }

    private void Register(UiScreen screen, Transform parent)
    {
        if (screen == null || _registered.Contains(screen))
        {
            return;
        }

        screen.Build(parent);
        _registered.Add(screen);
    }

    public void Show(UiScreen screen)
    {
        if (screen == null || Current == screen)
        {
            return;
        }

        Register(screen);

        if (Current != null)
        {
            Current.Hide();
        }

        Current = screen;
        screen.Show();
    }

    public void PushModal(UiScreen modal)
    {
        if (modal == null || _modals.Contains(modal))
        {
            return;
        }

        RegisterModal(modal);
        _modals.Add(modal);
        modal.Show();
    }

    public void PopModal()
    {
        if (_modals.Count == 0)
        {
            return;
        }

        UiScreen top = _modals[_modals.Count - 1];
        _modals.RemoveAt(_modals.Count - 1);
        top.Hide();
    }

    /// <summary>
    /// Обновляется только верхний видимый слой. В GameHud обновлялись все экраны
    /// каждый кадр, включая выключенные, — это и был основной холостой расход.
    /// </summary>
    public void Tick()
    {
        if (_modals.Count > 0)
        {
            _modals[_modals.Count - 1].Tick();
            return;
        }

        if (Current != null)
        {
            Current.Tick();
        }
    }
}
