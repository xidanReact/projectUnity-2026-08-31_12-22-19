using UnityEngine;

/// <summary>
/// Плейсхолдер-графика Фазы 1: спрайты рисуются кодом, чтобы прототип
/// запускался без единого импортированного ассета. Всё это выбрасывается в Фазе 2.5,
/// когда появляется настоящий арт.
/// </summary>
public static class PlaceholderArt
{
    private const int TextureSize = 64;
    private const float PixelsPerUnit = 64f;

    private static Sprite _circle;
    private static Sprite _square;
    private static Sprite _ring;
    private static Sprite _triangle;
    private static Material _material;

    /// Универсальный круг — тела патогена и большинства врагов.
    public static Sprite Circle => _circle != null ? _circle : (_circle = Build(ShapeKind.Circle, "PlaceholderCircle"));

    /// Квадрат — снаряды, полоски здоровья, фон.
    public static Sprite Square => _square != null ? _square : (_square = Build(ShapeKind.Square, "PlaceholderSquare"));

    /// Кольцо — щит бактерии и радиусы зон.
    public static Sprite Ring => _ring != null ? _ring : (_ring = Build(ShapeKind.Ring, "PlaceholderRing"));

    /// Треугольник — стреляющие враги, чтобы силуэт отличался от «мяса».
    public static Sprite Triangle => _triangle != null ? _triangle : (_triangle = Build(ShapeKind.Triangle, "PlaceholderTriangle"));

    /// <summary>
    /// Общий unlit-материал. Задаётся явно: в URP 2D спрайт по умолчанию берёт
    /// lit-материал и без источника света рисуется чёрным — на плейсхолдерах это
    /// выглядит как «ничего не работает».
    /// </summary>
    public static Material SpriteMaterial
    {
        get
        {
            if (_material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return _material;
        }
    }

    public static Sprite ForArchetype(EnemyArchetype archetype)
    {
        switch (archetype)
        {
            case EnemyArchetype.Shooter: return Triangle;
            case EnemyArchetype.Tank: return Square;
            default: return Circle;
        }
    }

    private enum ShapeKind
    {
        Circle,
        Square,
        Ring,
        Triangle
    }

    private static Sprite Build(ShapeKind kind, string name)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[TextureSize * TextureSize];
        const float half = TextureSize * 0.5f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float nx = (x + 0.5f - half) / half; // -1..1
                float ny = (y + 0.5f - half) / half;
                float alpha = Coverage(kind, nx, ny);
                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f), PixelsPerUnit);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static float Coverage(ShapeKind kind, float nx, float ny)
    {
        // Мягкий край шириной в пару пикселей — иначе примитивы выглядят рвано.
        const float edge = 2f / TextureSize;

        switch (kind)
        {
            case ShapeKind.Circle:
            {
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                return Mathf.InverseLerp(1f, 1f - edge * 2f, d);
            }
            case ShapeKind.Ring:
            {
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float outer = Mathf.InverseLerp(1f, 1f - edge * 2f, d);
                float inner = Mathf.InverseLerp(0.78f - edge * 2f, 0.78f, d);
                return outer * inner;
            }
            case ShapeKind.Triangle:
            {
                // Равнобедренный треугольник вершиной вниз — «клюв» стрелка.
                float t = Mathf.InverseLerp(-1f, 1f, ny);
                float halfSpan = Mathf.Lerp(0.02f, 0.95f, t);
                float inside = Mathf.InverseLerp(halfSpan, halfSpan - edge * 3f, Mathf.Abs(nx));
                float top = Mathf.InverseLerp(0.95f, 0.95f - edge * 3f, ny);
                return inside * top;
            }
            default:
            {
                float ax = Mathf.InverseLerp(0.94f, 0.94f - edge * 3f, Mathf.Abs(nx));
                float ay = Mathf.InverseLerp(0.94f, 0.94f - edge * 3f, Mathf.Abs(ny));
                return ax * ay;
            }
        }
    }
}
