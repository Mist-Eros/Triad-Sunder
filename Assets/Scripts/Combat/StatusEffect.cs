using UnityEngine;
using System.Collections;

public class StatusEffect : MonoBehaviour
{
    private HealthComponent health;
    private AIController ai;

    // State
    private float originalMoveSpeed;
    private float originalDamage;
    private float originalAttackCooldown;
    private float damageReduction;
    private bool isStunned;
    private bool isSlowed;

    void Awake()
    {
        health = GetComponent<HealthComponent>();
        ai = GetComponent<AIController>();

        // Try to get enemy weapon for damage/cooldown buffs
    }

    public void ApplyStun(float duration)
    {
        if (ai != null)
        {
            ai.enabled = false;
            isStunned = true;
            StopCoroutine(nameof(RemoveStunRoutine));
            StartCoroutine(RemoveStunRoutine(duration));
        }
    }

    IEnumerator RemoveStunRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ai != null && isStunned)
        {
            ai.enabled = true;
            isStunned = false;
        }
    }

    public void ApplySlow(float amount, float duration)
    {
        if (ai != null)
        {
            var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && !isSlowed)
            {
                originalMoveSpeed = agent.speed;
                agent.speed *= (1f - amount);
                isSlowed = true;
                StopCoroutine(nameof(RemoveSlowRoutine));
                StartCoroutine(RemoveSlowRoutine(duration));
            }
        }
    }

    IEnumerator RemoveSlowRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ai != null && isSlowed)
        {
            var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
                agent.speed = originalMoveSpeed;
            isSlowed = false;
        }
    }

    public void ApplyDamageReduction(float reduction, float duration)
    {
        // Store on the component — WeaponBase/CombatController checks this
        damageReduction = reduction;
        StopCoroutine(nameof(RemoveDRRoutine));
        StartCoroutine(RemoveDRRoutine(duration));
    }

    IEnumerator RemoveDRRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        damageReduction = 0f;
    }

    public float GetDamageReduction() => damageReduction;

    void OnDestroy()
    {
        if (ai != null && isStunned)
            ai.enabled = true;
        if (ai != null && isSlowed)
        {
            var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && originalMoveSpeed > 0)
                agent.speed = originalMoveSpeed;
        }
    }
}
