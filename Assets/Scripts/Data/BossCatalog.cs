using UnityEngine;

/// <summary>
/// Босс Биома 1 — «Лимфоузел» (dev-plan.md): неподвижен, несколько сегментов
/// урона со своей атакой каждый, периодически призывает подкрепления.
/// Как и EnemyCatalog, в Фазе 2 живёт в коде; выгружается в ассеты меню
/// «Pathogen → Создать балансовые ассеты».
/// </summary>
public static class BossCatalog
{
    private static BossData _lymphNode;

    public static BossData LymphNode
    {
        get
        {
            if (_lymphNode == null)
            {
                _lymphNode = ScriptableObject.CreateInstance<BossData>();
                _lymphNode.bossName = "Лимфоузел";
                _lymphNode.name = _lymphNode.bossName;
                _lymphNode.battleOffsetFromLane = 5.6f;
                _lymphNode.entrySpeed = 3.5f;
                _lymphNode.rageIntervalScalePerKill = 0.82f;

                // Два сосуда по краям призывают подкрепления, центр давит атаками.
                // Игрок сам выбирает, что душить первым: поток врагов или урон в лоб.
                _lymphNode.segments.Add(new BossSegmentDefinition
                {
                    segmentName = "Афферентный сосуд",
                    maxHealth = 180f,
                    radius = 0.75f,
                    color = new Color(0.55f, 0.75f, 0.95f),
                    attack = BossAttackKind.Summon,
                    attackInterval = 5.5f,
                    summon = EnemyCatalog.Neutrophil,
                    summonCount = 3,
                    offset = new Vector2(-2.6f, 0.3f)
                });

                _lymphNode.segments.Add(new BossSegmentDefinition
                {
                    segmentName = "Фолликул",
                    maxHealth = 260f,
                    radius = 0.9f,
                    color = new Color(0.95f, 0.80f, 0.35f),
                    attack = BossAttackKind.Volley,
                    attackInterval = 3.2f,
                    attackDamage = 8f,
                    projectileSpeed = 5.5f,
                    volleyCount = 5,
                    offset = new Vector2(-0.95f, -0.55f)
                });

                _lymphNode.segments.Add(new BossSegmentDefinition
                {
                    segmentName = "Синус",
                    maxHealth = 300f,
                    radius = 0.95f,
                    color = new Color(0.90f, 0.45f, 0.45f),
                    attack = BossAttackKind.Sweep,
                    attackInterval = 6f,
                    attackDamage = 22f,
                    sweepWidth = 3.5f,
                    offset = new Vector2(0.95f, -0.55f)
                });

                _lymphNode.segments.Add(new BossSegmentDefinition
                {
                    segmentName = "Эфферентный сосуд",
                    maxHealth = 180f,
                    radius = 0.75f,
                    color = new Color(0.55f, 0.75f, 0.95f),
                    attack = BossAttackKind.Summon,
                    attackInterval = 7f,
                    summon = EnemyCatalog.Antibody,
                    summonCount = 1,
                    offset = new Vector2(2.6f, 0.3f)
                });
            }

            return _lymphNode;
        }
    }
}
