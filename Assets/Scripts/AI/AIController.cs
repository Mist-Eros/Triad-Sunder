using UnityEngine;
using UnityEngine.AI;

public enum AIState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Death
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(HealthComponent))]
public class AIController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyConfig config;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("References")]
    [SerializeField] private WeaponBase weapon;

    private AIState currentState;
    private NavMeshAgent agent;
    private HealthComponent health;
    private Transform player;

    private int patrolIndex;
    private float stateEnterTime;
    private float lastAttackTime;
    private bool isDead;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<HealthComponent>();

        agent.speed = config.moveSpeed;
        agent.angularSpeed = config.angularSpeed;
        agent.stoppingDistance = config.attackRange * 0.8f;
        agent.baseOffset = 1f;

        health.OnDeath.AddListener(HandleDeath);

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        float scaledHP = config.maxHealth;
        if (GameManager.Instance != null)
            scaledHP = GameManager.Instance.GetEnemyHealth(config.maxHealth);
        health.SetMaxHealth(scaledHP);

        EnterState(AIState.Idle);
    }

    void Update()
    {
        if (isDead) return;

        float distToPlayer = player != null
            ? Vector3.Distance(transform.position, player.position)
            : Mathf.Infinity;

        switch (currentState)
        {
            case AIState.Idle:   UpdateIdle(distToPlayer);   break;
            case AIState.Patrol: UpdatePatrol(distToPlayer); break;
            case AIState.Chase:  UpdateChase(distToPlayer);  break;
            case AIState.Attack: UpdateAttack(distToPlayer); break;
        }
    }

    void EnterState(AIState newState)
    {
        currentState = newState;
        stateEnterTime = Time.time;

        if (newState == AIState.Idle || newState == AIState.Attack || newState == AIState.Death)
            agent.isStopped = true;
        else
            agent.isStopped = false;

        if (newState == AIState.Patrol && patrolPoints.Length > 0)
            GoToNextPatrolPoint();
    }

    void UpdateIdle(float distToPlayer)
    {
        if (Time.time - stateEnterTime < config.idleDuration) return;

        if (PlayerInRange(distToPlayer, config.detectionRange) && CanSeePlayer())
        {
            EnterState(AIState.Chase);
            return;
        }

        if (patrolPoints.Length > 0)
            EnterState(AIState.Patrol);
        else
            EnterState(AIState.Idle); // restart idle timer
    }

    void UpdatePatrol(float distToPlayer)
    {
        if (PlayerInRange(distToPlayer, config.detectionRange) && CanSeePlayer())
        {
            EnterState(AIState.Chase);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            GoToNextPatrolPoint();
    }

    void UpdateChase(float distToPlayer)
    {
        if (PlayerInRange(distToPlayer, config.attackRange))
        {
            EnterState(AIState.Attack);
            return;
        }

        if (distToPlayer > config.loseInterestRange || !CanSeePlayer())
        {
            EnterState(AIState.Idle);
            return;
        }

        agent.SetDestination(player.position);
    }

    void UpdateAttack(float distToPlayer)
    {
        if (distToPlayer > config.attackRange + 1f)
        {
            EnterState(AIState.Chase);
            return;
        }

        FacePlayer();

        if (weapon != null && Time.time >= lastAttackTime + config.attackCooldown)
        {
            lastAttackTime = Time.time;
            weapon.Attack(Team.Enemy);
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(dir),
                config.angularSpeed * Time.deltaTime
            );
    }

    bool PlayerInRange(float dist, float range) => dist <= range;

    bool CanSeePlayer()
    {
        if (player == null) return false;
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > config.detectionRange) return false;

        Vector3 origin = transform.position + Vector3.up * 0.6f;
        Vector3 target = player.position + Vector3.up * 0.6f;
        Vector3 dir = (target - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist))
        {
            HealthComponent hc = hit.collider.GetComponentInParent<HealthComponent>();
            return hc != null && hc.CurrentTeam == Team.Player;
        }
        return false;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        if (patrolPoints[patrolIndex] != null)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    void HandleDeath()
    {
        isDead = true;
        EnterState(AIState.Death);
        agent.isStopped = true;
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        if (config == null) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, config.detectionRange);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, config.attackRange);
    }
}
