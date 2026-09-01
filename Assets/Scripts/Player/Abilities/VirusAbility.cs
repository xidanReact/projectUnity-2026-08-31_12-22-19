using UnityEngine;

/// <summary>
/// Вирус — «Вспышка». При убийстве врага есть шанс, что он заражается
/// и на несколько секунд атакует ближайших врагов вместо игрока.
/// </summary>
public class VirusAbility : PathogenAbility
{
    private int _infectedThisLevel;

    public override void OnLevelStarted()
    {
        _infectedThisLevel = 0;
    }

    public override bool TryConsumeKill(Enemy enemy)
    {
        // Базово заражается только то, что убил сам игрок: иначе заражённые
        // бесконечно поднимали бы друг друга и уровень никогда не кончался бы.
        // Мутация «Пандемия» разрешает цепочку, но с вдвое меньшим шансом на звено,
        // и каждое следующее звено слабее по здоровью — цепь затухает сама.
        bool fromPlayer = enemy.LastDamageSource == Faction.Pathogen;
        bool fromInfected = Stats.ChainInfection && enemy.LastDamageSource == Faction.Infected;

        if (!fromPlayer && !fromInfected)
        {
            return false;
        }

        float chance = fromPlayer ? Stats.InfectionChance : Stats.InfectionChance * 0.5f;
        if (Random.value > chance)
        {
            return false;
        }

        float healthFraction = fromPlayer ? Stats.InfectedHealthFraction : Stats.InfectedHealthFraction * 0.6f;
        enemy.BecomeInfected(Stats.InfectionDuration, healthFraction);
        _infectedThisLevel++;
        return true;
    }

    public override string StatusLine => $"Вспышка: {Stats.InfectionChance * 100f:0}% / {Stats.InfectionDuration:0.0}с · заражено {_infectedThisLevel}";
}
