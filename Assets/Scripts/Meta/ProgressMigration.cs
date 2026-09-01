using System.Collections.Generic;

/// <summary>
/// Подъём старых сейвов до актуальной схемы. Вызывается ровно в одном месте —
/// при загрузке в хранилище, — чтобы игровой код никогда не видел старый формат.
/// </summary>
public static class ProgressMigration
{
    public const int CurrentVersion = 2;

    /// <summary>
    /// Приводит прогресс к актуальной версии. Никогда не возвращает null и
    /// никогда не теряет то, что уже было: новые поля добавляются со значениями
    /// по умолчанию, старые не трогаются.
    /// </summary>
    public static PlayerProgress Migrate(PlayerProgress progress)
    {
        if (progress == null)
        {
            return new PlayerProgress();
        }

        // Версия 1 не знала про золото, настройки, кампанию и выбранного патогена.
        // Отдельной ветки не требуется: все новые поля заполняются ниже дефолтами.
        FillMissing(progress);
        progress.version = CurrentVersion;
        return progress;
    }

    /// <summary>
    /// JsonUtility оставляет null там, где в JSON поля не было или оно записано
    /// как null. Каждое такое место — потенциальный NullReference в рантайме.
    /// </summary>
    private static void FillMissing(PlayerProgress progress)
    {
        if (progress.perks == null)
        {
            progress.perks = new List<PerkLevel>();
        }

        if (progress.settings == null)
        {
            progress.settings = new GameSettings();
        }

        if (string.IsNullOrEmpty(progress.lastPathogen))
        {
            progress.lastPathogen = "Virus";
        }

        if (progress.campaign == null)
        {
            progress.campaign = new CampaignProgress();
        }

        if (progress.campaign.nodes == null)
        {
            progress.campaign.nodes = new List<NodeProgress>();
        }

        if (progress.campaign.biomesUnlocked == null)
        {
            progress.campaign.biomesUnlocked = new List<string>();
        }
    }
}
