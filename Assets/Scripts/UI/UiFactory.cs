using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Сборка uGUI-элементов кодом. Как и остальная сцена Фазы 2, интерфейс строится
/// в рантайме — но уже настоящими Canvas/Image/Text, а не IMGUI, поэтому его
/// видно на устройстве, он масштабируется и переживает перенос в авторскую сцену
/// (иерархия ровно та же, что получилась бы при сборке руками).
///
/// Текст — legacy UnityEngine.UI.Text, а не TextMeshPro: TMP требует однократного
/// импорта «TMP Essentials» в проект, без которого весь текст рендерится пустым.
/// Для плейсхолдерного UI это лишняя ловушка.
/// </summary>
public static class UiFactory
{
    /// Опорное разрешение макета. Все координаты ниже считаются в нём.
    public static readonly Vector2 ReferenceResolution = new Vector2(720f, 1280f);

    public const float Margin = 28f;
    public static float ContentWidth => ReferenceResolution.x - Margin * 2f;

    private static Font _font;

    public static Font Font
    {
        get
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (_font == null)
            {
                // Запасной путь на случай, если встроенный шрифт переименуют в будущей версии.
                _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }

            return _font;
        }
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>Панель во весь родитель — основа экрана.</summary>
    public static RectTransform CreateFullScreen(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>
    /// Элемент, прибитый к верхнему краю: макет считается сверху вниз,
    /// как в исходном IMGUI-варианте, поэтому позиции переносятся один в один.
    /// </summary>
    public static RectTransform TopAnchored(RectTransform rect, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
    {
        RectTransform rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : PlaceholderArt.Square;
        image.color = color;
        image.type = Image.Type.Simple;
        return image;
    }

    public static Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        TextAnchor anchor = TextAnchor.UpperLeft,
        FontStyle style = FontStyle.Normal)
    {
        RectTransform rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.fontStyle = style;
        text.color = new Color(0.94f, 0.94f, 0.96f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>Кнопка с подписью. Текст возвращается отдельно — его меняют чаще, чем саму кнопку.</summary>
    public static Button CreateButton(
        string name,
        Transform parent,
        string label,
        int fontSize,
        Color background,
        out Text labelText)
    {
        Image image = CreateImage(name, parent, background);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        button.colors = colors;

        labelText = CreateText(name + "Label", image.transform, label, fontSize, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 6f);
        labelRect.offsetMax = new Vector2(-14f, -6f);
        labelText.color = new Color(0.06f, 0.06f, 0.08f);

        return button;
    }

    /// <summary>
    /// Полоска-индикатор: фон плюс заполняемая часть.
    /// Заполнение через Image.fillAmount, а не через масштаб — не тянет спрайт.
    /// </summary>
    public static Image CreateBar(string name, Transform parent, Color fillColor, out Image background)
    {
        background = CreateImage(name + "Background", parent, new Color(0f, 0f, 0f, 0.55f));

        Image fill = CreateImage(name + "Fill", background.transform, fillColor);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        return fill;
    }

    /// <summary>Растянуть элемент по всему родителю.</summary>
    public static RectTransform Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    public static RectTransform StretchWithPadding(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
        return rect;
    }

    /// <summary>Элемент, прибитый к нижнему краю. Нужен таб-бару и кнопкам действий.</summary>
    public static RectTransform BottomAnchored(RectTransform rect, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    /// <summary>
    /// Слайдер без ручки: на телефоне полоса заполнения читается лучше, чем
    /// мелкий кружок, и по ней можно попасть пальцем не целясь.
    /// </summary>
    public static Slider CreateSlider(string name, Transform parent, float value, Color fillColor)
    {
        Image background = CreateImage(name + "Background", parent, new Color(0f, 0f, 0f, 0.55f));
        var slider = background.gameObject.AddComponent<Slider>();

        RectTransform fillArea = CreateRect(name + "FillArea", background.transform);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(4f, 4f);
        fillArea.offsetMax = new Vector2(-4f, -4f);

        Image fill = CreateImage(name + "Fill", fillArea, fillColor);
        Stretch(fill.rectTransform);

        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = background;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        return slider;
    }

    /// <summary>
    /// Поле ввода на legacy InputField — по той же причине, что и весь текст:
    /// TMP_InputField тянет за собой импорт TMP Essentials.
    /// </summary>
    public static InputField CreateInputField(string name, Transform parent, string value, string placeholder)
    {
        Image background = CreateImage(name + "Background", parent, new Color(0.92f, 0.93f, 0.96f));
        var field = background.gameObject.AddComponent<InputField>();

        Text text = CreateText(name + "Text", background.transform, value, 24, TextAnchor.MiddleLeft);
        text.color = new Color(0.06f, 0.06f, 0.08f);
        text.supportRichText = false;
        StretchWithPadding(text.rectTransform, 14f, 6f);

        Text hint = CreateText(name + "Placeholder", background.transform, placeholder, 24, TextAnchor.MiddleLeft);
        hint.color = new Color(0.40f, 0.40f, 0.45f);
        hint.fontStyle = FontStyle.Italic;
        StretchWithPadding(hint.rectTransform, 14f, 6f);

        field.textComponent = text;
        field.placeholder = hint;
        field.targetGraphic = background;
        field.characterLimit = 16;
        field.lineType = InputField.LineType.SingleLine;
        field.SetTextWithoutNotify(value);
        return field;
    }

    /// <summary>
    /// Вертикальный скролл. Маска на прозрачной картинке: она обязана оставаться
    /// raycastTarget, иначе ScrollRect не увидит перетаскивание пальцем.
    /// </summary>
    public static ScrollRect CreateScrollView(string name, Transform parent, out RectTransform content)
    {
        Image viewport = CreateImage(name + "Viewport", parent, new Color(0f, 0f, 0f, 0f));
        viewport.raycastTarget = true;

        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();

        content = CreateRect(name + "Content", viewport.transform);
        content.anchorMin = new Vector2(0.5f, 0f);
        content.anchorMax = new Vector2(0.5f, 0f);
        content.pivot = new Vector2(0.5f, 0f);
        content.anchoredPosition = Vector2.zero;

        scroll.content = content;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 28f;
        scroll.inertia = true;

        return scroll;
    }
}
