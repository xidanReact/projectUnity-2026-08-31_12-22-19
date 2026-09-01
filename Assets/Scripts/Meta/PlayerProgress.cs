using System;
using System.Collections.Generic;

/// <summary>
/// Всё, что переживает забег. Сериализуется через JsonUtility, поэтому здесь
/// только публичные поля и никаких словарей — уровни перков лежат списком пар.
/// </summary>
[Serializable]
public class PlayerProgress
{
    /// Версия схемы. Пригодится, когда формат изменится и старые сейвы надо будет мигрировать.
    public int version = 1;

    /// Мягкая валюта — «биомасса».
    public int biomass;

    /// Уровни перманентных улучшений.
    public List<PerkLevel> perks = new List<PerkLevel>();

    // --- Статистика для экрана результатов ---
    public int totalRuns;
    public int bestLevelReached;
    public int totalKills;
    public int bossesDefeated;

    public int GetPerkLevel(string id)
    {
        for (int i = 0; i < perks.Count; i++)
        {
            if (perks[i].id == id)
            {
                return perks[i].level;
            }
        }
        return 0;
    }

    public void SetPerkLevel(string id, int level)
    {
        for (int i = 0; i < perks.Count; i++)
        {
            if (perks[i].id == id)
            {
                perks[i].level = level;
                return;
            }
        }

        perks.Add(new PerkLevel { id = id, level = level });
    }
}

[Serializable]
public class PerkLevel
{
    public string id;
    public int level;
}
