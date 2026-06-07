using UnityEngine;
using UnityEngine.Events;

public enum Team
{
    Player,
    Enemy,
    Neutral
}

public class HealthComponent : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Team team = Team.Enemy;
    [SerializeField] private float maxHealth = 100f;

    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent<float> OnDamageTaken;

    private float currentHealth;

    public Team CurrentTeam => team;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 knockback = default)
    {
        if (IsDead) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(amount);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && knockback != default)
            rb.AddForce(knockback, ForceMode.Impulse);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    public void SetMaxHealth(float newMax, bool fillToFull = true)
    {
        maxHealth = newMax;
        if (fillToFull)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        OnDeath?.Invoke();
    }
}
