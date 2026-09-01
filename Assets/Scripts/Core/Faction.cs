/// <summary>
/// Кто кому враг. Infected существует ради механики вируса: заражённый враг
/// на несколько секунд перестаёт быть угрозой и бьёт своих.
/// </summary>
public enum Faction
{
    Pathogen,
    Immune,
    Infected
}

public static class FactionExtensions
{
    public static bool IsHostileTo(this Faction self, Faction other)
    {
        switch (self)
        {
            case Faction.Pathogen: return other == Faction.Immune;
            case Faction.Immune: return other == Faction.Pathogen || other == Faction.Infected;
            case Faction.Infected: return other == Faction.Immune;
            default: return false;
        }
    }
}
