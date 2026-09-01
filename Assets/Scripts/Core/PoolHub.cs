using UnityEngine;

/// <summary>
/// Владелец всех пулов боя. Врагов, снарядов и спор за забег создаются тысячи,
/// поэтому ни один из них не должен проходить через Instantiate/Destroy в рантайме.
/// Прототипы объектов собираются кодом — арт Фазы 1 всё равно плейсхолдерный.
/// </summary>
public class PoolHub : MonoBehaviour
{
    public static PoolHub Instance { get; private set; }

    [Header("Предзаполнение")]
    public int prewarmEnemies = 64;
    public int prewarmProjectiles = 96;
    public int prewarmSpores = 48;

    public ObjectPool<Enemy> Enemies { get; private set; }
    public ObjectPool<Projectile> Projectiles { get; private set; }
    public ObjectPool<Spore> Spores { get; private set; }

    private Transform _enemyRoot;
    private Transform _projectileRoot;
    private Transform _sporeRoot;

    private void Awake()
    {
        Instance = this;

        _enemyRoot = CreateRoot("Enemies");
        _projectileRoot = CreateRoot("Projectiles");
        _sporeRoot = CreateRoot("Spores");

        Enemies = new ObjectPool<Enemy>(CreateEnemy, _enemyRoot, prewarmEnemies);
        Projectiles = new ObjectPool<Projectile>(CreateProjectile, _projectileRoot, prewarmProjectiles);
        Spores = new ObjectPool<Spore>(CreateSpore, _sporeRoot, prewarmSpores);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Вернуть в пул всё, что летает и тлеет на поле. Между уровнями и при смерти.</summary>
    public void ClearBattlefield()
    {
        Enemies.ReleaseAll();
        Projectiles.ReleaseAll();
        Spores.ReleaseAll();
        Battlefield.Clear();
    }

    public Spore PlantSpore(Vector2 position, PlayerStats stats)
    {
        Spore spore = Spores.Get();
        spore.Plant(
            position,
            stats.SporeDamagePerTick,
            stats.SporeTickInterval,
            stats.SporeLifetime,
            stats.SporeRadius,
            new Color(0.95f, 0.70f, 0.25f, 0.55f),
            stats.SporeSynergyBonus,
            stats.SporeExplosionDamage);
        return spore;
    }

    private Transform CreateRoot(string rootName)
    {
        var root = new GameObject(rootName).transform;
        root.SetParent(transform, false);
        return root;
    }

    private Enemy CreateEnemy()
    {
        var go = new GameObject("Enemy");
        AddSprite(go, PlaceholderArt.Circle, sortingOrder: 5);
        go.AddComponent<Health>();
        return go.AddComponent<Enemy>();
    }

    private Projectile CreateProjectile()
    {
        var go = new GameObject("Projectile");
        AddSprite(go, PlaceholderArt.Circle, sortingOrder: 8);
        var projectile = go.AddComponent<Projectile>();
        projectile.ReleaseCallback = p => Projectiles.Release(p);
        return projectile;
    }

    private Spore CreateSpore()
    {
        var go = new GameObject("Spore");
        AddSprite(go, PlaceholderArt.Circle, sortingOrder: 2);
        var spore = go.AddComponent<Spore>();
        spore.ReleaseCallback = s => Spores.Release(s);
        return spore;
    }

    /// <summary>
    /// Спрайт живёт на дочернем объекте: масштаб тела задаётся на корне,
    /// а размер спрайта на плейсхолдерах всегда 1x1 — так проще считать радиусы.
    /// </summary>
    internal static SpriteRenderer AddSprite(GameObject parent, Sprite sprite, int sortingOrder)
    {
        var child = new GameObject("Sprite");
        child.transform.SetParent(parent.transform, false);

        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = PlaceholderArt.SpriteMaterial;
        return renderer;
    }
}
