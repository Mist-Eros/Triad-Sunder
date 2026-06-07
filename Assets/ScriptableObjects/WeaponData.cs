using UnityEngine;

public enum WeaponType
{
    Melee,         // Sword, Cutlass - fast single target strikes
    MeleeCone,     // Axe, Hammer, Mace - wide cone AOE
    Ranged,        // Bow - single arrow projectile
    RangedPierce,  // Longbow - slow piercing arrow
    RangedRapid,   // Dual Crossbows - fast inaccurate shots
    Thrown,        // Glaive - throw and return
    Katana         // Special dash+slash+heal
}

public enum SecondaryAbility
{
    None,
    SpinHeal,          // Axe: spin AOE + heal based on damage
    SlamStun,          // Hammer: ground slam + stun
    SlamSlowRift,      // Mace: ground slam + slowing rift
    OrbitingGlaives,   // Glaive: 3 orbiting glaives for 20s
    Multishot,         // Bow: shoot 10 arrows at once
    PiercingShot,      // Longbow: big piercing arrow
    AccuracyBuff,      // Dual Crossbows: 100% accuracy, +20% dmg, +20% AS
    DashDR,            // Sword: long dash + 50% DR for 5s
    BackDashBuff,      // Cutlass: back dash + double dmg + 20% AS for 5s
    TeleportExecute    // Katana: teleport execute 3 closest enemies
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "Unnamed";
    public GameObject prefab;
    public Sprite icon;
    public WeaponType weaponType = WeaponType.Melee;
    public SecondaryAbility secondaryAbility = SecondaryAbility.None;

    [Header("Primary Combat")]
    public float damage = 25f;
    public float attackCooldown = 0.5f;
    public float attackDuration = 0.3f;
    public float attackRange = 1.8f;
    public float knockbackForce = 6f;
    public float swingAngle = 90f;

    [Header("Secondary Combat")]
    public float secondaryDamageMultiplier = 1f;
    public float secondaryCooldown = 5f;
    public float secondaryRange = 3f;
    public float secondaryDuration = 1f;

    [Header("Cone Attack (MeleeCone)")]
    public float coneAngle = 60f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 25f;
    public int projectileCount = 1;
    public float projectileSpread = 0f;
    public float projectileLifetime = 5f;
    public bool projectilePierces = false;
    public bool projectileReturns = false;

    [Header("Dash")]
    public float dashSpeed = 30f;
    public float dashDistance = 8f;
    public float dashDuration = 0.25f;

    [Header("Buff")]
    public float buffDuration = 5f;
    public float buffDamageMultiplier = 1f;
    public float buffAttackSpeedMultiplier = 1f;
    public float buffDamageReduction = 0f; // 0-1

    [Header("Debuff")]
    public float stunDuration = 0f;
    public float slowAmount = 0f;       // 0-1 (e.g. 0.7 = 70% slow)
    public float slowDuration = 0f;

    [Header("Spin (Axe secondary)")]
    public float spinRadius = 3f;
    public float spinDuration = 2f;
    public float healPercent = 0f;     // 0-1 percent of damage dealt healed

    [Header("Rift (Mace secondary)")]
    public float riftRadius = 5f;
    public float riftDuration = 5f;

    [Header("Orbiting Glaives (Glaive secondary)")]
    public int orbitingGlaiveCount = 3;
    public float orbitingGlaiveDuration = 20f;
    public float orbitingGlaiveRadius = 2f;
    public float orbitingGlaiveDamage = 15f;

    [Header("Teleport Execute (Katana secondary)")]
    public float teleportRange = 15f;
    public int teleportExecuteCount = 3;
}
