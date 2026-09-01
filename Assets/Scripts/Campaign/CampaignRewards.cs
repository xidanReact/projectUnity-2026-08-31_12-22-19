using UnityEngine;

/// <summary>Пара валют. Структура, а не класс: живёт коротко и не должна мусорить.</summary>
public readonly struct Reward
{
    public readonly int Gold;
    public readonly int Biomass;

    public Reward(int gold, int biomass)
    {
        Gold = gold;
        Biomass = biomass;
    }

    public static readonly Reward Zero = new Reward(0, 0);

    public Reward Scale(float factor) => new Reward(
        Mathf.Max(0, Mathf.RoundToInt(Gold * factor)),
        Mathf.Max(0, Mathf.RoundToInt(Biomass * factor)));

    public static Reward operator -(Reward a, Reward b) => new Reward(
        Mathf.Max(0, a.Gold - b.Gold),
        Mathf.Max(0, a.Biomass - b.Biomass));
}

/// <summary>
/// Сколько платит узел. Повтор платит треть, а улучшение звёзд — только разницу:
/// без этого первый узел биома становится фермой, а звёзды теряют смысл.
/// </summary>
public static class CampaignRewards
{
    /// Доля награды за повторное прохождение без улучшения результата.
    public const float RepeatFraction = 0.30f;

    public static float StarMultiplier(int stars)
    {
        if (stars <= 1)
        {
            return 1f;
        }

        return stars == 2 ? 1.25f : 1.5f;
    }

    /// <summary>Полная награда узла за указанное число звёзд, до среза жадности.</summary>
    public static Reward Full(CampaignNode node, int stars)
    {
        if (node == null)
        {
            return Reward.Zero;
        }

        float multiplier = StarMultiplier(stars);
        return new Reward(
            Mathf.RoundToInt(node.BaseGold * multiplier),
            Mathf.RoundToInt(node.BaseBiomass * multiplier));
    }

    /// <summary>
    /// Что реально причитается за прохождение.
    /// </summary>
    /// <param name="previousStars">Лучший результат до этого захода, 0 — узел не пройден.</param>
    /// <param name="newStars">Результат текущего захода.</param>
    public static Reward Payout(CampaignNode node, int previousStars, int newStars)
    {
        if (node == null || newStars <= 0)
        {
            return Reward.Zero;
        }

        if (previousStars <= 0)
        {
            return Full(node, newStars);
        }

        if (newStars > previousStars)
        {
            return Full(node, newStars) - Full(node, previousStars);
        }

        // Повтор считается по лучшему результату, а не по текущему: сыграть хуже
        // и получить меньше — наказание, которого игрок не поймёт.
        return Full(node, previousStars).Scale(RepeatFraction);
    }
}
