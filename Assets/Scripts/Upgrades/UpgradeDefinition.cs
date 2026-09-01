using System;

/// <summary>
/// Апгрейд правит цифры, мутация меняет поведение.
/// Разделение нужно и для выдачи (мутации редкие и берутся один раз),
/// и для подачи в UI — игрок должен видеть, что это не очередные +15%.
/// </summary>
public enum UpgradeKind
{
    Upgrade,
    Mutation
}

/// <summary>
/// Один вариант апгрейда в выборе после уровня. Эффект — делегат над PlayerStats,
/// потому что в Фазе 1 апгрейды это чистая арифметика по статам; когда появятся
/// мутации со своим поведением (Фаза 2), сюда добавится ссылка на компонент.
/// </summary>
public class UpgradeDefinition
{
    public readonly string Id;
    public readonly string Title;
    public readonly string Description;

    /// Для какого патогена апгрейд. null — общий, доступен всем.
    public readonly PathogenType? RestrictedTo;

    /// Сколько раз апгрейд можно взять за забег.
    public readonly int MaxTakes;

    public readonly UpgradeKind Kind;

    public bool IsMutation => Kind == UpgradeKind.Mutation;

    private readonly Action<PlayerStats, PlayerController> _apply;

    public UpgradeDefinition(
        string id,
        string title,
        string description,
        Action<PlayerStats, PlayerController> apply,
        PathogenType? restrictedTo = null,
        int maxTakes = 99,
        UpgradeKind kind = UpgradeKind.Upgrade)
    {
        Kind = kind;
        Id = id;
        Title = title;
        Description = description;
        _apply = apply;
        RestrictedTo = restrictedTo;
        MaxTakes = maxTakes;
    }

    public void Apply(PlayerStats stats, PlayerController player)
    {
        _apply(stats, player);
    }
}
