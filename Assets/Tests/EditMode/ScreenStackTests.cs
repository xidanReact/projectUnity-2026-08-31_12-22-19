using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Навигация. Проверяется именно то, что раньше делал GameHud вручную и
/// каждый кадр: кто видим и кого тикать.
/// </summary>
public class ScreenStackTests
{
    private GameObject _root;
    private ScreenStack _stack;

    private class ProbeScreen : UiScreen
    {
        public int Shows;
        public int Hides;
        public int Ticks;

        protected override void OnBuild() { }
        protected override void OnShow() => Shows++;
        protected override void OnHide() => Hides++;
        protected override void OnTick() => Ticks++;
    }

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Root", typeof(RectTransform));
        _stack = new ScreenStack(_root.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
    }

    private ProbeScreen Registered()
    {
        var screen = new ProbeScreen();
        _stack.Register(screen);
        return screen;
    }

    [Test]
    public void Register_СтроитЭкранИПрячетЕго()
    {
        ProbeScreen screen = Registered();

        Assert.IsNotNull(screen.Root, "Экран обязан построиться при регистрации");
        Assert.IsFalse(screen.IsVisible, "Свежий экран не должен перекрывать текущий");
    }

    [Test]
    public void Show_ПоказываетНовыйИПрячетПрежний()
    {
        ProbeScreen first = Registered();
        ProbeScreen second = Registered();

        _stack.Show(first);
        _stack.Show(second);

        Assert.IsFalse(first.IsVisible);
        Assert.IsTrue(second.IsVisible);
        Assert.AreEqual(1, first.Hides);
        Assert.AreEqual(1, second.Shows);
        Assert.AreSame(second, _stack.Current);
    }

    [Test]
    public void Show_ПовторныйПоказТогоЖеЭкранаНеДёргаетСобытия()
    {
        ProbeScreen screen = Registered();

        _stack.Show(screen);
        _stack.Show(screen);

        Assert.AreEqual(1, screen.Shows, "Повторный Show не должен перестраивать экран");
    }

    [Test]
    public void Tick_ИдётТолькоВВидимыйЭкран()
    {
        ProbeScreen visible = Registered();
        ProbeScreen hidden = Registered();
        _stack.Show(visible);

        _stack.Tick();

        Assert.AreEqual(1, visible.Ticks);
        Assert.AreEqual(0, hidden.Ticks, "Невидимый экран не должен тратить кадр");
    }

    [Test]
    public void Модалка_ПерекрываетЭкранНоНеПрячетЕго()
    {
        ProbeScreen screen = Registered();
        ProbeScreen modal = Registered();
        _stack.Show(screen);

        _stack.PushModal(modal);

        Assert.IsTrue(screen.IsVisible, "Экран под модалкой остаётся виден");
        Assert.IsTrue(modal.IsVisible);
        Assert.AreEqual(1, _stack.ModalDepth);
    }

    [Test]
    public void Tick_ПриОткрытойМодалкеИдётТолькоВНеё()
    {
        ProbeScreen screen = Registered();
        ProbeScreen modal = Registered();
        _stack.Show(screen);
        _stack.PushModal(modal);

        _stack.Tick();

        Assert.AreEqual(1, modal.Ticks);
        Assert.AreEqual(0, screen.Ticks, "Под модалкой экран не обновляется");
    }

    [Test]
    public void PopModal_ВозвращаетУправлениеЭкрану()
    {
        ProbeScreen screen = Registered();
        ProbeScreen modal = Registered();
        _stack.Show(screen);
        _stack.PushModal(modal);

        _stack.PopModal();
        _stack.Tick();

        Assert.IsFalse(modal.IsVisible);
        Assert.AreEqual(0, _stack.ModalDepth);
        Assert.AreEqual(1, screen.Ticks);
    }

    [Test]
    public void PopModal_НаПустомСтекеНеПадает()
    {
        Assert.DoesNotThrow(() => _stack.PopModal());
    }
}
