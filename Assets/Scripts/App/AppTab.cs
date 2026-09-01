using System;

/// <summary>Разделы нижнего таб-бара, в порядке их показа слева направо.</summary>
public enum AppTab
{
    Upgrades,
    Wardrobe,
    Campaign,
    Battle
}

/// <summary>
/// Перелистывание патогенов на главном экране. Отдельно от экрана: заворачивание
/// на краях и разбор имени из сейва — единственная логика, которую тут можно
/// сломать так, что визуально это заметят не сразу.
/// </summary>
public static class PathogenCarousel
{
    public static readonly PathogenType[] Types =
    {
        PathogenType.Virus, PathogenType.Bacteria, PathogenType.Fungus, PathogenType.Parasite
    };

    public static int Shift(int index, int delta)
    {
        int count = Types.Length;
        return ((index + delta) % count + count) % count;
    }

    /// <summary>Индекс по имени значения из сейва. Мусор превращается в первого патогена.</summary>
    public static int IndexOf(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return 0;
        }

        for (int i = 0; i < Types.Length; i++)
        {
            if (string.Equals(Types[i].ToString(), typeName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return 0;
    }
}
