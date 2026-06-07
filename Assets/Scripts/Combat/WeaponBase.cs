using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class WeaponBase : MonoBehaviour
{
    [SerializeField] private WeaponData data;

    // Cooldowns
    private float lastPrimaryTime = -99f;
    private float lastSecondaryTime = -99f;
    private bool isAttacking;
    private bool isSecondaryActive;
    private Team currentAttackerTeam;
    private HashSet<HealthComponent> hitTargets = new HashSet<HealthComponent>();
    private Quaternion originalLocalRotation;

    // Buff state
    private float buffDamageMultiplier = 1f;
    private float buffAttackSpeedMultiplier = 1f;
    private float buffDamageReduction;
    private float buffEndTime;

    // Projectile accuracy RNG (for crossbows)
    private System.Random accuracyRng = new System.Random();

    public WeaponData Data => data;
    public bool IsAttacking => isAttacking || isSecondaryActive;
    public float CurrentDamageMultiplier => buffDamageMultiplier;
    public float CurrentAttackSpeedMultiplier => buffAttackSpeedMultiplier;

    public bool CanPrimaryAttack => data != null && !isAttacking && !isSecondaryActive
        && Time.time >= lastPrimaryTime + (data.attackCooldown / buffAttackSpeedMultiplier);

    public bool CanSecondaryAttack => data != null && !isAttacking && !isSecondaryActive
        && data.secondaryAbility != SecondaryAbility.None
        && Time.time >= lastSecondaryTime + data.secondaryCooldown;

    void Awake()
    {
        originalLocalRotation = transform.localRotation;
    }

    void Update()
    {
        // Decay buffs
        if (Time.time >= buffEndTime)
        {
            buffDamageMultiplier = 1f;
            buffAttackSpeedMultiplier = 1f;
            buffDamageReduction = 0f;
        }

        // Apply damage reduction from StatusEffect if on player
        if (buffDamageReduction > 0f)
        {
            StatusEffect se = transform.root.GetComponent<StatusEffect>();
            if (se != null)
                buffDamageReduction = se.GetDamageReduction();
        }
    }

    // ============================================================
    // PUBLIC ATTACK ENTRY POINTS
    // ============================================================

    public void PrimaryAttack(Team team)
    {
        if (data == null || !CanPrimaryAttack) return;
        lastPrimaryTime = Time.time;
        currentAttackerTeam = team;
        hitTargets.Clear();

        switch (data.weaponType)
        {
            case WeaponType.Melee:
            case WeaponType.MeleeCone:
                StartCoroutine(MeleeSwingRoutine());
                break;
            case WeaponType.Ranged:
            case WeaponType.RangedPierce:
            case WeaponType.RangedRapid:
                RangedPrimary();
                break;
            case WeaponType.Thrown:
                ThrownPrimary();
                break;
            case WeaponType.Katana:
                StartCoroutine(KatanaPrimaryRoutine());
                break;
        }
    }

    public void SecondaryAttack(Team team)
    {
        if (data == null || !CanSecondaryAttack) return;
        lastSecondaryTime = Time.time;
        currentAttackerTeam = team;
        hitTargets.Clear();

        switch (data.secondaryAbility)
        {
            case SecondaryAbility.SpinHeal:
                StartCoroutine(SpinHealRoutine());
                break;
            case SecondaryAbility.SlamStun:
                StartCoroutine(SlamRoutine(true, false));
                break;
            case SecondaryAbility.SlamSlowRift:
                StartCoroutine(SlamRoutine(false, true));
                break;
            case SecondaryAbility.OrbitingGlaives:
                StartOrbitingGlaives();
                break;
            case SecondaryAbility.Multishot:
                Multishot();
                break;
            case SecondaryAbility.PiercingShot:
                PiercingShot();
                break;
            case SecondaryAbility.AccuracyBuff:
                ApplySelfBuff();
                break;
            case SecondaryAbility.DashDR:
                StartCoroutine(DashRoutine(true, false));
                break;
            case SecondaryAbility.BackDashBuff:
                StartCoroutine(DashRoutine(false, true));
                break;
            case SecondaryAbility.TeleportExecute:
                StartCoroutine(TeleportExecuteRoutine());
                break;
        }
    }

    // ============================================================
    // PRIMARY: MELEE / MELEE CONE SWING
    // ============================================================

    IEnumerator MeleeSwingRoutine()
    {
        isAttacking = true;
        float elapsed = 0f;
        float duration = data.attackDuration;
        bool hitChecked = false;
        float hitWindow = duration * 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float angle = Mathf.Sin(t * Mathf.PI) * data.swingAngle;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(-angle, 0f, 0f);

            if (!hitChecked && elapsed >= hitWindow)
            {
                hitChecked = true;
                if (data.weaponType == WeaponType.MeleeCone)
                    CheckConeHit();
                else
                    CheckMeleeHit();
            }

            yield return null;
        }

        transform.localRotation = originalLocalRotation;
        isAttacking = false;
    }

    void CheckMeleeHit()
    {
        Vector3 origin = transform.root.position + Vector3.up * 1.2f;
        Vector3 forward = transform.root.forward;
        float dmg = data.damage * buffDamageMultiplier;

        Collider[] hits = Physics.OverlapSphere(
            origin + forward * data.attackRange * 0.6f,
            data.attackRange * 0.5f
        );

        foreach (Collider hit in hits)
        {
            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || hitTargets.Contains(health)) continue;
            if (health.CurrentTeam == currentAttackerTeam) continue;

            hitTargets.Add(health);
            Vector3 knockDir = (health.transform.position - transform.root.position).normalized;
            health.TakeDamage(dmg, knockDir * data.knockbackForce);
        }
    }

    void CheckConeHit()
    {
        Vector3 origin = transform.root.position + Vector3.up * 1.2f;
        Vector3 forward = transform.root.forward;
        float dmg = data.damage * buffDamageMultiplier;

        Collider[] hits = Physics.OverlapSphere(origin, data.attackRange);

        foreach (Collider hit in hits)
        {
            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || hitTargets.Contains(health)) continue;
            if (health.CurrentTeam == currentAttackerTeam) continue;

            Vector3 dirToTarget = (health.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);
            if (angle > data.coneAngle * 0.5f) continue;

            hitTargets.Add(health);
            Vector3 knockDir = dirToTarget;
            health.TakeDamage(dmg, knockDir * data.knockbackForce);
        }
    }

    // ============================================================
    // PRIMARY: RANGED (Bow, Longbow, Dual Crossbows)
    // ============================================================

    void RangedPrimary()
    {
        int count = data.projectileCount;
        float spread = data.projectileSpread;

        // For RangedRapid, add spread; for RangedPierce it's single precise
        if (data.weaponType == WeaponType.Ranged)
            spread = 0f; // Bow single arrow is accurate
        else if (data.weaponType == WeaponType.RangedPierce)
            spread = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = GetProjectileDirection(spread);
            SpawnProjectile(dir, data.damage, data.projectileSpeed, data.projectileLifetime,
                data.weaponType == WeaponType.RangedPierce);
        }
    }

    void ThrownPrimary()
    {
        Vector3 dir = transform.root.forward;
        SpawnProjectile(dir, data.damage, data.projectileSpeed, data.projectileLifetime,
            false, true); // returns = true
    }

    // ============================================================
    // PRIMARY: KATANA (short dash + slash + heal)
    // ============================================================

    IEnumerator KatanaPrimaryRoutine()
    {
        isAttacking = true;
        float elapsed = 0f;
        float dur = data.attackDuration;
        Vector3 dashDir = transform.root.forward;
        float dashDist = 2.5f;
        Transform player = transform.root;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        bool hitChecked = false;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;

            // Quick dash forward
            if (rb != null)
            {
                float moveT = Mathf.Clamp01(t * 2f); // dash in first half
                rb.MovePosition(rb.position + dashDir * dashDist * Time.deltaTime * (1f - moveT) * 4f);
            }

            // Swing visual
            float angle = Mathf.Sin(t * Mathf.PI * 2f) * data.swingAngle * 0.7f;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(-angle, 0f, 0f);

            if (!hitChecked && t >= 0.25f)
            {
                hitChecked = true;
                CheckKatanaHit();
            }

            yield return null;
        }

        transform.localRotation = originalLocalRotation;
        isAttacking = false;
    }

    void CheckKatanaHit()
    {
        Vector3 origin = transform.root.position + Vector3.up * 1.2f;
        float dmg = data.damage * buffDamageMultiplier;
        float totalHeal = 0f;

        Collider[] hits = Physics.OverlapSphere(origin, data.attackRange);
        foreach (Collider hit in hits)
        {
            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || hitTargets.Contains(health)) continue;
            if (health.CurrentTeam == currentAttackerTeam) continue;

            hitTargets.Add(health);
            health.TakeDamage(dmg);
            totalHeal += dmg * data.healPercent;
        }

        // Heal player
        if (totalHeal > 0f)
        {
            HealthComponent playerHealth = transform.root.GetComponent<HealthComponent>();
            if (playerHealth != null)
                playerHealth.Heal(totalHeal);
        }
    }

    // ============================================================
    // SECONDARY: AXE - SPIN + HEAL
    // ============================================================

    IEnumerator SpinHealRoutine()
    {
        isSecondaryActive = true;
        float elapsed = 0f;
        float dur = data.spinDuration;
        Transform player = transform.root;
        float totalHeal = 0f;
        float lastDamageTime = -1f;
        float damageInterval = 0.2f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;

            // Spin the weapon
            transform.localRotation = originalLocalRotation * Quaternion.Euler(0f, elapsed * 720f, 0f);

            // Damage nearby enemies periodically
            if (Time.time - lastDamageTime >= damageInterval)
            {
                lastDamageTime = Time.time;
                Vector3 origin = player.position + Vector3.up * 1.2f;
                Collider[] hits = Physics.OverlapSphere(origin, data.spinRadius);

                foreach (Collider hit in hits)
                {
                    HealthComponent health = hit.GetComponentInParent<HealthComponent>();
                    if (health == null || hitTargets.Contains(health)) continue;
                    if (health.CurrentTeam == currentAttackerTeam) continue;

                    // Damage scales with proximity: closer = more damage, up to 2x
                    float dist = Vector3.Distance(origin, health.transform.position);
                    float proximityFactor = Mathf.Lerp(data.secondaryDamageMultiplier * 2f,
                        data.secondaryDamageMultiplier, dist / data.spinRadius);
                    proximityFactor = Mathf.Clamp(proximityFactor, data.secondaryDamageMultiplier,
                        data.secondaryDamageMultiplier * 2f);

                    float dmg = data.damage * proximityFactor;
                    hitTargets.Add(health);
                    health.TakeDamage(dmg);
                    totalHeal += dmg * data.healPercent;
                }
            }

            yield return null;
        }

        // Heal player
        if (totalHeal > 0f)
        {
            HealthComponent playerHealth = player.GetComponent<HealthComponent>();
            if (playerHealth != null)
                playerHealth.Heal(totalHeal);
        }

        transform.localRotation = originalLocalRotation;
        isSecondaryActive = false;
    }

    // ============================================================
    // SECONDARY: HAMMER / MACE - GROUND SLAM
    // ============================================================

    IEnumerator SlamRoutine(bool stun, bool spawnRift)
    {
        isSecondaryActive = true;
        float elapsed = 0f;
        float dur = 0.5f; // slam animation duration
        bool hitChecked = false;
        float hitTime = dur * 0.6f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;

            // Slam: weapon goes up then down fast
            float angle;
            if (t < 0.5f)
                angle = Mathf.Lerp(0f, -80f, t * 2f); // wind up
            else
                angle = Mathf.Lerp(-80f, 30f, (t - 0.5f) * 2f); // slam down
            transform.localRotation = originalLocalRotation * Quaternion.Euler(angle, 0f, 0f);

            if (!hitChecked && elapsed >= hitTime)
            {
                hitChecked = true;
                DoSlamHit(stun, spawnRift);
            }

            yield return null;
        }

        transform.localRotation = originalLocalRotation;
        isSecondaryActive = false;
    }

    void DoSlamHit(bool stun, bool spawnRift)
    {
        Vector3 origin = transform.root.position;
        float dmg = data.damage * data.secondaryDamageMultiplier;

        Collider[] hits = Physics.OverlapSphere(origin, data.secondaryRange);

        foreach (Collider hit in hits)
        {
            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health == null) continue;
            if (health.CurrentTeam == currentAttackerTeam) continue;

            health.TakeDamage(dmg, Vector3.up * data.knockbackForce * 0.5f);

            if (stun && data.stunDuration > 0f)
                GetOrAddStatusEffect(health).ApplyStun(data.stunDuration);

            if (spawnRift && data.slowDuration > 0f)
                GetOrAddStatusEffect(health).ApplySlow(data.slowAmount, data.slowDuration);
        }

        // Spawn ground rift
        if (spawnRift)
        {
            GameObject riftObj = new GameObject("GroundRift");
            riftObj.transform.position = transform.root.position;
            GroundRift rift = riftObj.AddComponent<GroundRift>();
            rift.Initialize(data.riftRadius, data.slowAmount, data.slowDuration,
                data.riftDuration, data.damage * 0.3f, currentAttackerTeam);
        }

        // Camera shake / visual feedback
        // Could add later
    }

    // ============================================================
    // SECONDARY: GLAIVE - ORBITING GLAIVES
    // ============================================================

    void StartOrbitingGlaives()
    {
        Transform player = transform.root;
        int count = data.orbitingGlaiveCount;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            GameObject glaiveObj = new GameObject($"OrbitingGlaive_{i}");
            glaiveObj.transform.position = player.position;

            // Add visual (small cube as placeholder)
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.transform.SetParent(glaiveObj.transform);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = new Vector3(0.15f, 0.05f, 0.4f);
            Destroy(vis.GetComponent<Collider>());
            MeshRenderer mr = vis.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.9f, 0.9f);
                mr.sharedMaterial = mat;
            }

            OrbitingGlaive orbit = glaiveObj.AddComponent<OrbitingGlaive>();
            orbit.Initialize(player, angleStep * i, data.orbitingGlaiveRadius,
                data.orbitingGlaiveDamage, data.orbitingGlaiveDuration, currentAttackerTeam);
        }
    }

    // ============================================================
    // SECONDARY: BOW - MULTISHOT
    // ============================================================

    void Multishot()
    {
        int count = 10;
        float totalSpread = 30f;
        float angleStep = totalSpread / (count - 1);
        float startAngle = -totalSpread * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.root.forward;
            SpawnProjectile(dir, data.damage * data.secondaryDamageMultiplier,
                data.projectileSpeed, data.projectileLifetime, false);
        }
    }

    // ============================================================
    // SECONDARY: LONGBOW - PIERCING SHOT
    // ============================================================

    void PiercingShot()
    {
        Vector3 dir = transform.root.forward;
        SpawnProjectile(dir, data.damage * data.secondaryDamageMultiplier,
            data.projectileSpeed * 1.2f, data.projectileLifetime, true); // pierces
    }

    // ============================================================
    // SECONDARY: DUAL CROSSBOWS - ACCURACY BUFF
    // ============================================================

    void ApplySelfBuff()
    {
        buffDamageMultiplier = 1f + data.buffDamageMultiplier;   // 1.2 = +20%
        buffAttackSpeedMultiplier = 1f + data.buffAttackSpeedMultiplier; // 1.2 = +20%
        buffEndTime = Time.time + data.buffDuration;

        // Also reduce spread to 0 for the duration
        // Handled in RangedPrimary by checking if buff is active
    }

    // ============================================================
    // SECONDARY: SWORD DASH (forward) / CUTLASS DASH (backward)
    // ============================================================

    IEnumerator DashRoutine(bool forward, bool applyDoubleDamage)
    {
        isSecondaryActive = true;
        Transform player = transform.root;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) { isSecondaryActive = false; yield break; }

        Vector3 dashDir = forward ? player.forward : -player.forward;
        float elapsed = 0f;
        float dur = data.dashDuration > 0f ? data.dashDuration : 0.25f;
        float dist = data.dashDistance;
        float speed = dist / dur;

        if (data.buffDamageReduction > 0f)
        {
            StatusEffect se = player.GetComponent<StatusEffect>();
            if (se == null) se = player.gameObject.AddComponent<StatusEffect>();
            se.ApplyDamageReduction(data.buffDamageReduction, data.buffDuration);
            buffDamageReduction = data.buffDamageReduction;
        }

        // Apply double damage buff (Cutlass)
        if (applyDoubleDamage)
        {
            buffDamageMultiplier = data.buffDamageMultiplier; // 2.0
            buffEndTime = Time.time + data.buffDuration;

            if (data.buffAttackSpeedMultiplier > 1f)
            {
                buffAttackSpeedMultiplier = data.buffAttackSpeedMultiplier; // 1.2
            }
        }

        HashSet<HealthComponent> dashedThrough = new HashSet<HealthComponent>();

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float moveAmount = speed * Time.deltaTime;
            rb.MovePosition(rb.position + dashDir * moveAmount);

            // Damage enemies we pass through
            if (data.damage > 0f)
            {
                Vector3 checkOrigin = player.position + Vector3.up * 1f;
                Collider[] hits = Physics.OverlapSphere(checkOrigin, 1.5f);
                foreach (Collider hit in hits)
                {
                    HealthComponent health = hit.GetComponentInParent<HealthComponent>();
                    if (health == null || health.CurrentTeam == currentAttackerTeam) continue;
                    if (dashedThrough.Contains(health)) continue;

                    dashedThrough.Add(health);
                    float dmg = data.damage * data.secondaryDamageMultiplier * buffDamageMultiplier;
                    health.TakeDamage(dmg, dashDir * data.knockbackForce);
                }
            }

            yield return null;
        }

        // Sword DR buff lasts beyond dash
        if (!applyDoubleDamage && data.buffDamageReduction > 0f)
        {
            buffEndTime = Time.time + data.buffDuration;
        }

        isSecondaryActive = false;
    }

    // ============================================================
    // SECONDARY: KATANA - TELEPORT EXECUTE
    // ============================================================

    IEnumerator TeleportExecuteRoutine()
    {
        isSecondaryActive = true;
        Transform player = transform.root;

        // Find all enemies
        HealthComponent[] allEnemies = FindObjectsOfType<HealthComponent>()
            .Where(h => h.CurrentTeam != currentAttackerTeam && !h.IsDead)
            .OrderBy(h => Vector3.Distance(player.position, h.transform.position))
            .Take(data.teleportExecuteCount)
            .ToArray();

        foreach (HealthComponent enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            // Teleport to enemy
            Vector3 targetPos = enemy.transform.position + (player.position - enemy.transform.position).normalized * 1f;
            targetPos.y = player.position.y;

            // Flash effect: briefly disable then re-enable
            player.position = targetPos;

            // Slash visual
            transform.localRotation = originalLocalRotation * Quaternion.Euler(-60f, 0f, 0f);
            yield return new WaitForSeconds(0.1f);
            transform.localRotation = originalLocalRotation;

            // Kill enemy (massive damage)
            enemy.TakeDamage(enemy.MaxHealth * 10f);

            // Brief pause between teleports
            yield return new WaitForSeconds(0.2f);
        }

        transform.localRotation = originalLocalRotation;
        isSecondaryActive = false;
    }

    // ============================================================
    // HELPERS
    // ============================================================

    StatusEffect GetOrAddStatusEffect(HealthComponent health)
    {
        StatusEffect se = health.GetComponent<StatusEffect>();
        if (se == null) se = health.gameObject.AddComponent<StatusEffect>();
        return se;
    }

    Vector3 GetProjectileDirection(float spread)
    {
        Vector3 baseDir = transform.root.forward;

        // If accuracy buff is active, no spread
        if (buffAttackSpeedMultiplier > 1.1f && data.secondaryAbility == SecondaryAbility.AccuracyBuff)
            spread = 0f;

        if (spread <= 0f) return baseDir;

        float hSpread = (float)(accuracyRng.NextDouble() * 2 - 1) * spread;
        float vSpread = (float)(accuracyRng.NextDouble() * 2 - 1) * spread * 0.3f;
        return Quaternion.Euler(vSpread, hSpread, 0f) * baseDir;
    }

    void SpawnProjectile(Vector3 direction, float dmg, float spd, float lifetime,
        bool pierces, bool returns = false)
    {
        GameObject projObj;
        Vector3 spawnPos = transform.root.position + transform.root.forward * 0.8f + Vector3.up * 1.2f;

        if (data.projectilePrefab != null)
        {
            projObj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        }
        else
        {
            // Create simple projectile visual
            projObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projObj.transform.position = spawnPos;
            projObj.transform.forward = direction;
            projObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            Destroy(projObj.GetComponent<Collider>());

            MeshRenderer mr = projObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = returns ? Color.cyan : (pierces ? Color.red : Color.yellow);
                mr.sharedMaterial = mat;
            }
        }

        // Add trigger collider
        BoxCollider col = projObj.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = projObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.3f, 0.6f);
        }
        col.isTrigger = true;

        // Ensure Rigidbody
        Rigidbody rb = projObj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = projObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj == null)
            proj = projObj.AddComponent<Projectile>();

        proj.damage = dmg;
        proj.speed = spd;
        proj.lifetime = lifetime;
        proj.knockbackForce = data.knockbackForce;
        proj.pierces = pierces;
        proj.returns = returns;
        proj.ownerTeam = currentAttackerTeam;

        proj.Launch(direction, returns ? transform.root : null);
    }

    // ============================================================
    // BACKWARDS COMPAT
    // ============================================================

    public void Attack(Team team)
    {
        PrimaryAttack(team);
    }

    public bool CanAttack => CanPrimaryAttack;
}
