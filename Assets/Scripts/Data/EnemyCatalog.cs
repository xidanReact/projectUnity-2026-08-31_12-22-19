using UnityEngine;

/// <summary>
/// Ростер Биома 1 «Кровоток» (dev-plan.md, раздел «Биомы и враги»).
/// В Фазе 1 живёт в коде, чтобы прототип запускался без ассетов;
/// в Фазе 2 эти же значения переезжают в .asset-файлы через PrototypeAssetCreator.
/// </summary>
public static class EnemyCatalog
{
    private static EnemyData _neutrophil;
    private static EnemyData _macrophage;
    private static EnemyData _macrophageFragment;
    private static EnemyData _antibody;

    /// Нейтрофил — быстрый, ближний бой, мало здоровья.
    public static EnemyData Neutrophil
    {
        get
        {
            if (_neutrophil == null)
            {
                _neutrophil = EnemyData.Create("Нейтрофил", EnemyArchetype.Rusher,
                    maxHealth: 18f, moveSpeed: 2.6f, radius: 0.32f,
                    color: new Color(0.95f, 0.95f, 0.98f));
                _neutrophil.contactDamage = 8f;
                _neutrophil.attackInterval = 0.8f;
                _neutrophil.scoreValue = 1;
            }
            return _neutrophil;
        }
    }

    /// Осколок макрофага — то, на что он делится при смерти.
    public static EnemyData MacrophageFragment
    {
        get
        {
            if (_macrophageFragment == null)
            {
                _macrophageFragment = EnemyData.Create("Осколок макрофага", EnemyArchetype.Rusher,
                    maxHealth: 14f, moveSpeed: 2.2f, radius: 0.30f,
                    color: new Color(0.60f, 0.72f, 0.85f));
                _macrophageFragment.contactDamage = 6f;
                _macrophageFragment.attackInterval = 0.9f;
                _macrophageFragment.scoreValue = 1;
            }
            return _macrophageFragment;
        }
    }

    /// Макрофаг — медленный танк, при смерти делится на 2 слабых.
    public static EnemyData Macrophage
    {
        get
        {
            if (_macrophage == null)
            {
                _macrophage = EnemyData.Create("Макрофаг", EnemyArchetype.Tank,
                    maxHealth: 85f, moveSpeed: 1.1f, radius: 0.62f,
                    color: new Color(0.35f, 0.55f, 0.80f));
                _macrophage.contactDamage = 16f;
                _macrophage.attackInterval = 1.4f;
                _macrophage.scoreValue = 4;
                _macrophage.splitCount = 2;
                _macrophage.splitInto = MacrophageFragment;
            }
            return _macrophage;
        }
    }

    /// Антитело — стреляет издалека, держит дистанцию.
    public static EnemyData Antibody
    {
        get
        {
            if (_antibody == null)
            {
                _antibody = EnemyData.Create("Антитело", EnemyArchetype.Shooter,
                    maxHealth: 26f, moveSpeed: 1.6f, radius: 0.36f,
                    color: new Color(0.98f, 0.85f, 0.35f));
                _antibody.contactDamage = 5f;
                _antibody.attackInterval = 1.9f;
                // Строго меньше самой короткой дальности патогена (6.5 у бактерии):
                // иначе стрелок мог бы зависнуть вне досягаемости и подвесить уровень.
                _antibody.standoffDistance = 5.5f;
                _antibody.projectileSpeed = 7f;
                _antibody.projectileDamage = 7f;
                _antibody.scoreValue = 3;
            }
            return _antibody;
        }
    }
}
