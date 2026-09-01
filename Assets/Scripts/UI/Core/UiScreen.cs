using UnityEngine;

/// <summary>
/// Один экран интерфейса. Строится ровно один раз, дальше только включается
/// и выключается: пересборка иерархии на каждый показ — самый дорогой способ
/// сменить картинку.
/// </summary>
public abstract class UiScreen
{
    public RectTransform Root { get; private set; }

    public bool IsVisible => Root != null && Root.gameObject.activeSelf;

    public void Build(Transform parent)
    {
        if (Root != null)
        {
            return;
        }

        Root = UiFactory.CreateFullScreen(GetType().Name, parent);
        OnBuild();
        Root.gameObject.SetActive(false);
    }

    public void Show()
    {
        if (Root == null || IsVisible)
        {
            return;
        }

        Root.gameObject.SetActive(true);
        // Свежепоказанный экран поднимается наверх: модалки и экраны живут
        // в одном родителе, и порядок в иерархии решает, кто кого перекрывает.
        Root.SetAsLastSibling();
        OnShow();
    }

    public void Hide()
    {
        if (Root == null || !IsVisible)
        {
            return;
        }

        OnHide();
        Root.gameObject.SetActive(false);
    }

    /// <summary>Вызывается только для видимого экрана — см. ScreenStack.Tick.</summary>
    public void Tick()
    {
        if (IsVisible)
        {
            OnTick();
        }
    }

    protected abstract void OnBuild();

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }

    protected virtual void OnTick() { }
}
