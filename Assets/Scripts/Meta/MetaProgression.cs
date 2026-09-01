using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Метапрогрессия: мягкая валюта за забеги и перманентные улучшения между ними.
/// Знает про хранилище только через IProgressStore — в Фазе 4 сюда подставится
/// клиент Go-бэкенда, и ничего выше по стеку не изменится.
/// </summary>
public class MetaProgression : MonoBehaviour
{
    /// <summary>
    /// Калибровка «жадности» из dev-plan.md: дроп за забег снижен примерно на треть
    /// от щедрого базового значения (целевой уровень 6-7 из 10). Вынесено отдельной
    /// константой, потому что это первое, что будут крутить по данным софт-лонча.
    /// Публичная: на неё ссылается расчёт награды за узел.
    /// </summary>
    public const float GreedMultiplier = 0.65f;

    public PlayerProgress Progress { get; private set; }
    public IReadOnlyList<PermanentUpgrade> Upgrades => _upgrades;

    /// Что принёс последний пройденный узел — для экрана результата.
    public Reward LastNodeReward { get; private set; }

    private readonly List<PermanentUpgrade> _upgrades = new List<PermanentUpgrade>();
    private IProgressStore _store;

    private bool _built;

    private void Awake()
    {
        EnsureBuilt();
    }

    /// <summary>
    /// Список улучшений строится по требованию: вне режима игры Awake может не вызваться.
    /// </summary>
    private void EnsureBuilt()
    {
        if (_built)
        {
            return;
        }

        _built = true;
        BuildUpgrades();
    }

    public void Initialize(IProgressStore store)
    {
        EnsureBuilt();
        _store = store;
        Progress = _store.Load();
    }

    // --- Валюта ---

    /// <summary>
    /// Начислить награду за пройденный узел и сохранить прогресс.
    /// Сохранение идёт здесь, до показа результата: по dev-plan.md прогресс
    /// обязан быть на диске раньше, чем игроку что-либо предложат
    /// (в Фазе 4 — просмотр рекламы за удвоение).
    /// </summary>
    public Reward AwardNode(CampaignNode node, int previousStars, int newStars)
    {
        Reward payout = CampaignRewards.Payout(node, previousStars, newStars).Scale(GreedMultiplier);

        LastNodeReward = payout;
        Progress.gold += payout.Gold;
        Progress.biomass += payout.Biomass;

        if (node != null && node.IsBoss && previousStars <= 0 && newStars > 0)
        {
            Progress.bossesDefeated++;
        }

        Save();
        return payout;
    }

    /// <summary>Учесть завершённую попытку биома в статистике.</summary>
    public void RecordBiomeAttempt(int kills)
    {
        Progress.totalRuns++;
        Progress.totalKills += kills;
        Save();
    }

    public bool CanAfford(PermanentUpgrade upgrade)
    {
        int level = Progress.GetPerkLevel(upgrade.Id);
        return !upgrade.IsMaxed(level) && Progress.biomass >= upgrade.CostForNextLevel(level);
    }

    /// <summary>Купить следующий уровень улучшения. Возвращает false, если не хватило или уже максимум.</summary>
    public bool TryPurchase(PermanentUpgrade upgrade)
    {
        int level = Progress.GetPerkLevel(upgrade.Id);
        if (upgrade.IsMaxed(level))
        {
            return false;
        }

        int cost = upgrade.CostForNextLevel(level);
        if (Progress.biomass < cost)
        {
            return false;
        }

        Progress.biomass -= cost;
        Progress.SetPerkLevel(upgrade.Id, level + 1);
        Save();
        return true;
    }

    public int LevelOf(PermanentUpgrade upgrade) => Progress.GetPerkLevel(upgrade.Id);

    /// <summary>Наложить купленные улучшения на стартовые статы забега.</summary>
    public void ApplyTo(PlayerStats stats)
    {
        for (int i = 0; i < _upgrades.Count; i++)
        {
            _upgrades[i].Apply(stats, Progress.GetPerkLevel(_upgrades[i].Id));
        }
    }

    public void Save()
    {
        if (_store != null)
        {
            _store.Save(Progress);
        }
    }

    /// <summary>Полный сброс — нужен для плейтестов, чтобы смотреть первый запуск.</summary>
    public void ResetProgress()
    {
        Progress = new PlayerProgress();
        Save();
    }

    private void BuildUpgrades()
    {
        _upgrades.Clear();

        _upgrades.Add(new PermanentUpgrade("perk_hp", "Плотное ядро", "+8 к максимуму здоровья",
            maxLevel: 5, baseCost: 40,
            apply: (stats, level) => stats.MaxHealth += 8f * level));

        _upgrades.Add(new PermanentUpgrade("perk_damage", "Агрессивный штамм", "+5% урона",
            maxLevel: 5, baseCost: 50,
            apply: (stats, level) => stats.AttackDamage *= 1f + 0.05f * level));

        _upgrades.Add(new PermanentUpgrade("perk_rate", "Ускоренный метаболизм", "+4% скорости атаки",
            maxLevel: 5, baseCost: 50,
            apply: (stats, level) => stats.AttackRate *= 1f + 0.04f * level));

        _upgrades.Add(new PermanentUpgrade("perk_move", "Гибкая мембрана", "+4% скорости движения",
            maxLevel: 5, baseCost: 35,
            apply: (stats, level) => stats.MoveSpeed *= 1f + 0.04f * level));

        _upgrades.Add(new PermanentUpgrade("perk_range", "Дальнобойность", "+4% дальности атаки",
            maxLevel: 5, baseCost: 35,
            apply: (stats, level) => stats.AttackRange *= 1f + 0.04f * level));
    }
}
