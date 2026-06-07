using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "AI/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float angularSpeed = 360f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float loseInterestRange = 22f;

    [Header("Behavior")]
    public float idleDuration = 2f;
    public float attackCooldown = 1.2f;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float damage = 15f;
}
