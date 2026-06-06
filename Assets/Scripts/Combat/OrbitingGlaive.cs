using UnityEngine;
using System.Collections.Generic;

public class OrbitingGlaive : MonoBehaviour
{
    public float damage = 15f;
    public float orbitRadius = 2f;
    public float orbitSpeed = 180f;
    public float lifetime = 20f;
    public float knockbackForce = 3f;
    public Team ownerTeam;

    private Transform owner;
    private float angleOffset;
    private float spawnTime;
    private float lastDamageTime;
    private float damageInterval = 0.3f;
    private HashSet<HealthComponent> hitThisTick = new HashSet<HealthComponent>();

    public void Initialize(Transform owner, float angleOffsetDeg, float radius, float dmg, float duration, Team team)
    {
        this.owner = owner;
        this.angleOffset = angleOffsetDeg;
        this.orbitRadius = radius;
        this.damage = dmg;
        this.lifetime = duration;
        this.ownerTeam = team;
        spawnTime = Time.time;
        lastDamageTime = Time.time;
    }

    void Update()
    {
        if (owner == null || Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float angle = angleOffset + (Time.time - spawnTime) * orbitSpeed * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
        transform.position = owner.position + offset;

        // Damage enemies in range
        if (Time.time - lastDamageTime >= damageInterval)
        {
            lastDamageTime = Time.time;
            hitThisTick.Clear();
            DamageNearbyEnemies();
        }
    }

    void DamageNearbyEnemies()
    {
        float hitRadius = 1f;
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);
        foreach (Collider hit in hits)
        {
            HealthComponent health = hit.GetComponentInParent<HealthComponent>();
            if (health == null || health.CurrentTeam == ownerTeam) continue;
            if (hitThisTick.Contains(health)) continue;

            hitThisTick.Add(health);
            Vector3 knockDir = (health.transform.position - transform.position).normalized;
            health.TakeDamage(damage, knockDir * knockbackForce);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (owner != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.position, orbitRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}
