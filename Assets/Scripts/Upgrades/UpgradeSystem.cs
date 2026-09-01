using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Выбор из 3 апгрейдов после уровня. Пул состоит из общих улучшений
/// и улучшений, которые видит только «свой» патоген — это то, что должно
/// развести четырёх персонажей по ощущениям уже на плейсхолдерах.
/// </summary>
public class UpgradeSystem : MonoBehaviour
{
    public const int ChoiceCount = 3;

    /// С какого уровня в выборе могут появляться мутации (0 — первый).
    private const int MutationsFromLevel = 1;

    /// Шанс, что один из трёх вариантов окажется мутацией.
    private const float MutationChance = 0.45f;

    private readonly List<UpgradeDefinition> _pool = new List<UpgradeDefinition>();
    private readonly List<UpgradeDefinition> _mutations = new List<UpgradeDefinition>();
    private readonly Dictionary<string, int> _taken = new Dictionary<string, int>();
    private readonly List<UpgradeDefinition> _candidates = new List<UpgradeDefinition>();

    private bool _built;

    private void Awake()
    {
        EnsureBuilt();
    }

    /// <summary>
    /// Пулы строятся по требованию, а не только в Awake: вне режима игры
    /// (тесты, редакторские инструменты) Awake может не вызваться вообще.
    /// </summary>
    private void EnsureBuilt()
    {
        if (_built)
        {
            return;
        }

        _built = true;
        BuildPool();
        BuildMutations();
    }

    public void ResetRun()
    {
        _taken.Clear();
    }

    /// <summary>Сколько раз апгрейд уже взят за этот забег.</summary>
    public int TakenCount(string id) => _taken.TryGetValue(id, out int count) ? count : 0;

    /// <summary>
    /// Случайные N вариантов без повторов. Если подходящих меньше N —
    /// вернёт сколько есть; вызывающая сторона обязана это пережить.
    /// </summary>
    public List<UpgradeDefinition> Roll(PlayerStats stats, int levelNumber = 0, int count = ChoiceCount)
    {
        EnsureBuilt();
        var result = new List<UpgradeDefinition>(count);

        // Сначала решаем, будет ли в выборе мутация. Больше одной не бывает:
        // выбор «мутация против цифр» читается, а выбор из трёх мутаций — уже нет.
        if (levelNumber >= MutationsFromLevel && Random.value < MutationChance)
        {
            CollectCandidates(_mutations, stats);
            if (_candidates.Count > 0)
            {
                result.Add(_candidates[Random.Range(0, _candidates.Count)]);
            }
        }

        CollectCandidates(_pool, stats);
        while (result.Count < count && _candidates.Count > 0)
        {
            int index = Random.Range(0, _candidates.Count);
            result.Add(_candidates[index]);
            _candidates.RemoveAt(index);
        }

        return result;
    }

    private void CollectCandidates(List<UpgradeDefinition> source, PlayerStats stats)
    {
        _candidates.Clear();

        for (int i = 0; i < source.Count; i++)
        {
            UpgradeDefinition upgrade = source[i];

            if (upgrade.RestrictedTo.HasValue && upgrade.RestrictedTo.Value != stats.Type)
            {
                continue;
            }

            if (TakenCount(upgrade.Id) >= upgrade.MaxTakes)
            {
                continue;
            }

            _candidates.Add(upgrade);
        }
    }

    public void Take(UpgradeDefinition upgrade, PlayerStats stats, PlayerController player)
    {
        upgrade.Apply(stats, player);
        _taken[upgrade.Id] = TakenCount(upgrade.Id) + 1;
    }

    private void BuildPool()
    {
        _pool.Clear();

        // --- Общие ---
        Add("dmg", "Вирулентность", "Урон атаки +20%",
            (s, p) => s.AttackDamage *= 1.20f);

        Add("rate", "Репликация", "Скорость атаки +15%",
            (s, p) => s.AttackRate *= 1.15f);

        Add("range", "Тропизм", "Дальность атаки +15%",
            (s, p) => s.AttackRange *= 1.15f);

        Add("move", "Подвижность", "Скорость движения +10%",
            (s, p) => s.MoveSpeed *= 1.10f);

        Add("hp", "Плотная оболочка", "Максимум здоровья +25",
            (s, p) =>
            {
                s.MaxHealth += 25f;
                p.Health.AddMaxHealth(25f);
            });

        Add("heal", "Регенерация", "Восстановить 40% здоровья",
            (s, p) => p.Health.Heal(p.Health.Max * 0.4f));

        Add("pierce", "Пробивная атака", "Снаряд пробивает +1 цель",
            (s, p) => s.Pierce += 1, maxTakes: 3);

        Add("multishot", "Деление", "+1 снаряд за выстрел",
            (s, p) => s.ProjectileCount += 1, maxTakes: 3);

        Add("projspeed", "Ускоренный выброс", "Скорость снаряда +25%",
            (s, p) => s.ProjectileSpeed *= 1.25f);

        Add("crit_chance", "Точный штамм", "Шанс крита +8%",
            (s, p) => s.CritChance = Mathf.Min(0.75f, s.CritChance + 0.08f), maxTakes: 6);

        Add("crit_power", "Разрушительный удар", "Множитель крита +0.4",
            (s, p) => s.CritMultiplier += 0.4f, maxTakes: 3);

        Add("armor", "Толстая стенка", "Входящий урон -6%",
            (s, p) => s.DamageReduction += 0.06f, maxTakes: 4);

        // --- Вирус ---
        Add("virus_chance", "Заразность", "Шанс вспышки +7%",
            (s, p) => s.InfectionChance = Mathf.Min(0.9f, s.InfectionChance + 0.07f),
            PathogenType.Virus, maxTakes: 6);

        Add("virus_duration", "Инкубация", "Заражённый живёт +1.5с",
            (s, p) => s.InfectionDuration += 1.5f,
            PathogenType.Virus);

        Add("virus_carrier", "Стойкий носитель", "Заражённые получают +20% здоровья",
            (s, p) => s.InfectedHealthFraction += 0.2f,
            PathogenType.Virus, maxTakes: 3);

        // --- Бактерия ---
        Add("bact_cd", "Быстрая плёнка", "Щит восстанавливается на 1.5с быстрее",
            (s, p) => s.ShieldCooldown = Mathf.Max(2f, s.ShieldCooldown - 1.5f),
            PathogenType.Bacteria, maxTakes: 4);

        Add("bact_charges", "Слоистая плёнка", "+1 заряд щита",
            (s, p) => s.ShieldCharges += 1,
            PathogenType.Bacteria, maxTakes: 2);

        Add("bact_buffer", "Осмотический буфер", "Входящий урон -8%",
            (s, p) => s.DamageReduction += 0.08f,
            PathogenType.Bacteria, maxTakes: 3);

        // --- Грибок ---
        Add("fung_damage", "Едкие споры", "Урон споры за тик +1.5",
            (s, p) => s.SporeDamagePerTick += 1.5f,
            PathogenType.Fungus);

        Add("fung_radius", "Разрастание", "Радиус споры +0.25",
            (s, p) => s.SporeRadius += 0.25f,
            PathogenType.Fungus, maxTakes: 4);

        Add("fung_life", "Стойкий мицелий", "Спора живёт +1.5с",
            (s, p) => s.SporeLifetime += 1.5f,
            PathogenType.Fungus, maxTakes: 4);

        Add("fung_tick", "Частые выбросы", "Спора тикает на 0.15с чаще",
            (s, p) => s.SporeTickInterval = Mathf.Max(0.3f, s.SporeTickInterval - 0.15f),
            PathogenType.Fungus, maxTakes: 3);

        // --- Паразит ---
        Add("para_charges", "Двойное дно", "+1 использование пряток за уровень",
            (s, p) => s.InvincibilityChargesPerLevel += 1,
            PathogenType.Parasite, maxTakes: 2);

        Add("para_duration", "Глубокая маскировка", "Прятки длятся +1с",
            (s, p) => s.InvincibilityDuration += 1f,
            PathogenType.Parasite, maxTakes: 3);

        Add("para_slick", "Скользкая оболочка", "Входящий урон -7%",
            (s, p) => s.DamageReduction += 0.07f,
            PathogenType.Parasite, maxTakes: 3);
    }

    private void Add(
        string id,
        string title,
        string description,
        System.Action<PlayerStats, PlayerController> apply,
        PathogenType? restrictedTo = null,
        int maxTakes = 99)
    {
        _pool.Add(new UpgradeDefinition(id, title, description, apply, restrictedTo, maxTakes));
    }

    /// <summary>
    /// Мутация всегда берётся один раз: это включатель поведения,
    /// а не множитель, который имело бы смысл штабелировать.
    /// </summary>
    private void AddMutation(
        string id,
        string title,
        string description,
        System.Action<PlayerStats, PlayerController> apply,
        PathogenType? restrictedTo = null)
    {
        _mutations.Add(new UpgradeDefinition(
            id, title, description,
            (stats, player) =>
            {
                apply(stats, player);
                stats.TakenMutations.Add(title);
            },
            restrictedTo, maxTakes: 1, kind: UpgradeKind.Mutation));
    }

    private void BuildMutations()
    {
        _mutations.Clear();

        // --- Общие ---
        AddMutation("mut_explosive", "Разрывной снаряд",
            "Попадание бьёт по площади вокруг цели",
            (s, p) => s.ExplosiveRadius = 1.1f);

        AddMutation("mut_lifesteal", "Кровожадность",
            "Каждое убийство восстанавливает 1.5 здоровья",
            (s, p) => s.LifestealPerKill = 1.5f);

        AddMutation("mut_adrenaline", "Адреналин",
            "После полученного урона — +50% скорости на 2с",
            (s, p) => s.AdrenalineBonus = 0.5f);

        // --- Вирус ---
        AddMutation("mut_pandemic", "Пандемия",
            "Заражённые могут заражать дальше, половинным шансом",
            (s, p) => s.ChainInfection = true,
            PathogenType.Virus);

        AddMutation("mut_strain", "Вирулентный штамм",
            "Заражённые бьют вдвое сильнее",
            (s, p) => s.InfectedDamageFactor = 2f,
            PathogenType.Virus);

        // --- Бактерия ---
        AddMutation("mut_spikes", "Шипастая плёнка",
            "Щит при срабатывании наносит 25 урона вокруг",
            (s, p) => s.ShieldBurstDamage = 25f,
            PathogenType.Bacteria);

        // --- Грибок ---
        AddMutation("mut_mycelium", "Грибница",
            "Соседние споры усиливают друг друга: +0.8 урона за соседа",
            (s, p) => s.SporeSynergyBonus = 0.8f,
            PathogenType.Fungus);

        AddMutation("mut_burst_spores", "Взрывные споры",
            "Догоревшая спора взрывается на 12 урона",
            (s, p) => s.SporeExplosionDamage = 12f,
            PathogenType.Fungus);

        // --- Паразит ---
        AddMutation("mut_bloodsucker", "Кровосос",
            "Во время пряток восстанавливает 12 здоровья в секунду",
            (s, p) => s.CloakHealPerSecond = 12f,
            PathogenType.Parasite);

        AddMutation("mut_counter", "Контратака",
            "Уход в невидимость бьёт на 40 урона вокруг",
            (s, p) => s.CloakBurstDamage = 40f,
            PathogenType.Parasite);
    }
}
