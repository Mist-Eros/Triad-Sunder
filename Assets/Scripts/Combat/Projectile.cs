using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    [Header("State (set by WeaponBase)")]
    public float damage;
    public float speed;
    public float lifetime;
    public float knockbackForce;
    public bool pierces;
    public bool returns;
    public Team ownerTeam;
    public float returnSpeed = 30f;

    private Vector3 direction;
    private float spawnTime;
    private HashSet<HealthComponent> hitTargets = new HashSet<HealthComponent>();
    private bool returning;
    private Transform returnTarget;
    private TrailRenderer trail;

    void Start()
    {
        spawnTime = Time.time;
        trail = GetComponent<TrailRenderer>();
        if (returns && trail == null)
            trail = gameObject.AddComponent<TrailRenderer>();
    }

    public void Launch(Vector3 dir, Transform owner = null)
    {
        direction = dir.normalized;
        returnTarget = owner;
        returning = false;
    }

    void Update()
    {
        if (returns && returning)
        {
            if (returnTarget == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = returnTarget.position - transform.position;
            float step = returnSpeed * Time.deltaTime;

            if (toTarget.magnitude < step * 1.5f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += toTarget.normalized * step;
            transform.forward = toTarget.normalized;
            return;
        }

        float elapsed = Time.time - spawnTime;
        if (elapsed >= lifetime)
        {
            if (returns && !returning)
            {
                returning = true;
                return;
            }
            Destroy(gameObject);
            return;
        }

        transform.position += direction * speed * Time.deltaTime;
        if (direction.sqrMagnitude > 0.001f)
            transform.forward = direction;
    }

    void OnTriggerEnter(Collider other)
    {
        if (returning) return;

        HealthComponent health = other.GetComponentInParent<HealthComponent>();
        if (health == null) return;
        if (health.CurrentTeam == ownerTeam) return;
        if (hitTargets.Contains(health)) return;

        hitTargets.Add(health);
        Vector3 knockDir = direction;
        health.TakeDamage(damage, knockDir * knockbackForce);

        if (!pierces)
        {
            if (returns)
                returning = true;
            else
                Destroy(gameObject);
        }
    }
}
