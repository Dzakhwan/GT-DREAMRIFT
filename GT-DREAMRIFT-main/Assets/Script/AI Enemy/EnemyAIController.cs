using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Unified enemy AI controller for top-down action games.
/// FSM: Spawn → Patrol → Chase → Attack → Dead
/// Works with any AttackPatternSO (Slime Dash, Melee, Ranged, etc).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAIController : MonoBehaviour
{
    public enum EnemyState
    {
        Spawn,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [Header("FSM")]
    [SerializeField] private EnemyState currentState = EnemyState.Spawn;

    [Header("Detection")]
    [Tooltip("Player must enter this radius before the enemy starts chasing.")]
    [SerializeField] private float detectionRadius = 8f;
    [Tooltip("Distance at which the enemy starts its attack pattern.")]
    [SerializeField] private float attackRange = 3f;
    [Tooltip("How often SetDestination is called while chasing (mobile CPU optimization).")]
    [SerializeField] private float chaseTickInterval = 0.15f;

    [Header("Patrol")]
    [Tooltip("Radius around spawn/home for random patrol points.")]
    [SerializeField] private float patrolRadius = 6f;
    [Tooltip("Seconds to wait after arriving at a patrol point.")]
    [SerializeField] private float patrolWaitTime = 1.5f;

    [Header("Spawn")]
    [Tooltip("Play pop-up scale animation on spawn (good for slime).")]
    [SerializeField] private bool useSpawnAnimation = true;
    [SerializeField] private float spawnDuration = 0.6f;
    [SerializeField] private UnityEvent onSpawnEvents;

    [Header("Visual Juice")]
    [Tooltip("Programmatic squish/stretch on spawn and attack telegraph (slime feel).")]
    [SerializeField] private bool useSquishAnimation = true;
    [SerializeField] private Animator animator;
    [SerializeField] private string attackAnimTrigger = "Attack";

    [Header("Swarm Positioning")]
    [Tooltip("Orbit player in a ring instead of walking straight at them.")]
    [SerializeField] private bool useSwarmPositioning = true;
    [SerializeField] private float swarmMinRadius = 2.5f;
    [SerializeField] private float swarmMaxRadius = 6.5f;
    [SerializeField] private float repositionInterval = 2.2f;
    [Tooltip("Keep enemies from stacking on top of each other.")]
    [SerializeField] private bool useSwarmSeparation = true;
    [SerializeField] private float separationDistance = 2f;

    [Header("Attack")]
    [Tooltip("ScriptableObject attack pattern (SlimeDash / Melee / Ranged).")]
    [SerializeField] private AttackPatternSO attackPattern;
    [SerializeField] private UnityEvent onAttackStartEvents;
    [SerializeField] private UnityEvent onAttackEndEvents;

    [Header("NavMesh Link")]
    [SerializeField] private float offMeshLinkJumpHeight = 2f;
    [SerializeField] private float offMeshLinkDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private Transform playerTarget;
    private Vector3 originalScale;
    private Vector3 homePosition;
    private Coroutine stateRoutine;
    private float chaseTickTimer;
    private float repositionTimer;
    private float cooldownTimer;
    private int enemyLayerMask;
    private readonly Collider[] separationResults = new Collider[8];
    private NavMeshPath pathCache;

    public EnemyState CurrentState => currentState;
    public Transform PlayerTarget => playerTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        originalScale = transform.localScale;
        homePosition = transform.position;
        enemyLayerMask = LayerMask.GetMask("Enemy");

        // IMPORTANT: NavMeshPath MUST be created in Awake, not as a field
        // initializer. Constructing it in the C# field initializer runs before
        // Unity's native NavMesh backend is ready, leaving the native pointer
        // null. Passing that path into NavMesh.CalculatePath / agent.CalculatePath
        // then throws NullReferenceException from inside Unity's native code.
        pathCache = new NavMeshPath();

        agent.autoBraking = true;
        agent.autoRepath = true;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }

        if (health != null)
            health.OnDeath += HandleDeath;

        if (attackPattern == null)
            Debug.LogWarning($"[EnemyAI] {name} has no AttackPatternSO assigned.");

        TransitionTo(EnemyState.Spawn);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void OnDisable()
    {
        if (stateRoutine != null)
        {
            StopCoroutine(stateRoutine);
            stateRoutine = null;
        }
    }

    private void TransitionTo(EnemyState next)
    {
        if (currentState == EnemyState.Dead && next != EnemyState.Dead)
            return;

        currentState = next;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        switch (currentState)
        {
            case EnemyState.Spawn:
                stateRoutine = StartCoroutine(SpawnState());
                break;
            case EnemyState.Patrol:
                stateRoutine = StartCoroutine(PatrolState());
                break;
            case EnemyState.Chase:
                stateRoutine = StartCoroutine(ChaseState());
                break;
            case EnemyState.Attack:
                stateRoutine = StartCoroutine(AttackState());
                break;
            case EnemyState.Dead:
                if (agent != null && agent.enabled)
                    agent.isStopped = true;
                break;
        }
    }

    // ===================== SPAWN =====================

    private IEnumerator SpawnState()
    {
        agent.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (useSpawnAnimation)
        {
            transform.localScale = Vector3.zero;
            onSpawnEvents?.Invoke();

            float t = 0f;
            while (t < spawnDuration)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / spawnDuration);
                float scale;
                if (n < 0.66f)
                    scale = Mathf.Lerp(0f, 1.3f, n / 0.66f);
                else
                    scale = Mathf.Lerp(1.3f, 1f, (n - 0.66f) / 0.34f);

                transform.localScale = originalScale * scale;
                yield return null;
            }
        }
        else
        {
            onSpawnEvents?.Invoke();
            yield return null;
        }

        transform.localScale = originalScale;
        if (col != null) col.enabled = true;
        agent.enabled = true;
        agent.isStopped = false;

        if (IsPlayerInDetectionRange())
            TransitionTo(EnemyState.Chase);
        else
            TransitionTo(EnemyState.Patrol);
    }

    // ===================== PATROL =====================

    private IEnumerator PatrolState()
    {
        while (true)
        {
            if (IsPlayerInDetectionRange())
            {
                TransitionTo(EnemyState.Chase);
                yield break;
            }

            if (!IsAgentReady())
            {
                TryRebindAgentToNavMesh();
                yield return null;
                continue;
            }

            if (TryGetRandomPatrolPoint(out Vector3 point))
                agent.SetDestination(point);

            while (IsAgentReady() && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
            {
                if (IsPlayerInDetectionRange())
                {
                    TransitionTo(EnemyState.Chase);
                    yield break;
                }

                if (agent.velocity.sqrMagnitude > 0.01f)
                    FaceDirection(agent.velocity);

                yield return null;
            }

            float wait = 0f;
            while (wait < patrolWaitTime)
            {
                if (IsPlayerInDetectionRange())
                {
                    TransitionTo(EnemyState.Chase);
                    yield break;
                }

                wait += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ===================== CHASE =====================

    private IEnumerator ChaseState()
    {
        chaseTickTimer = 0f;
        repositionTimer = 0f;

        while (true)
        {
            if (playerTarget == null)
            {
                TransitionTo(EnemyState.Patrol);
                yield break;
            }

            float dist = DistanceToPlayer();

            if (dist > detectionRadius)
            {
                TransitionTo(EnemyState.Patrol);
                yield break;
            }

            float effectiveAttackRange = attackPattern != null ? attackPattern.AttackRange : attackRange;
            if (dist <= effectiveAttackRange && cooldownTimer <= 0f)
            {
                TransitionTo(EnemyState.Attack);
                yield break;
            }

            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;

            // If the agent is not currently bound to the NavMesh, try to re-bind
            // it and skip pathing for this frame instead of throwing.
            if (!IsAgentReady())
            {
                TryRebindAgentToNavMesh();
                FaceTarget();
                yield return null;
                continue;
            }

            chaseTickTimer += Time.deltaTime;
            repositionTimer -= Time.deltaTime;

            bool shouldUpdatePath = chaseTickTimer >= chaseTickInterval;
            if (useSwarmPositioning)
            {
                bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
                shouldUpdatePath = repositionTimer <= 0f || arrived;
            }

            if (shouldUpdatePath)
            {
                chaseTickTimer = 0f;

                if (useSwarmPositioning)
                {
                    if (TryGetSwarmPosition(out Vector3 swarmPos))
                        agent.SetDestination(swarmPos);
                    repositionTimer = repositionInterval + Random.Range(-0.4f, 0.8f);
                }
                else
                {
                    agent.SetDestination(playerTarget.position);
                }
            }

            if (agent.velocity.sqrMagnitude > 0.01f)
                FaceDirection(agent.velocity);
            else
                FaceTarget();

            if (agent.isOnOffMeshLink)
                yield return StartCoroutine(TraverseOffMeshLink());

            yield return null;
        }
    }

    // ===================== ATTACK =====================

    private IEnumerator AttackState()
    {
        if (playerTarget == null || attackPattern == null)
        {
            TransitionTo(EnemyState.Chase);
            yield break;
        }

        agent.isStopped = true;
        agent.ResetPath();
        FaceTargetImmediate();

        onAttackStartEvents?.Invoke();

        if (useSquishAnimation)
            yield return StartCoroutine(SquishTelegraph(attackPattern.WindUpTime));
        else
        {
            if (animator != null && !string.IsNullOrEmpty(attackAnimTrigger))
                animator.SetTrigger(attackAnimTrigger);
            yield return new WaitForSeconds(attackPattern.WindUpTime);
        }

        // Direction is locked at wind-up start for dash-style attacks
        attackPattern.ExecuteAttack(this, playerTarget);

        // Wait while attack pattern may disable the agent (e.g. slime dash),
        // then wait until it is actually bound to the NavMesh again.
        float safety = 3f;
        while ((!agent.enabled || !agent.isOnNavMesh) && safety > 0f)
        {
            safety -= Time.deltaTime;
            yield return null;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (useSquishAnimation)
            yield return StartCoroutine(SquishRecovery(attackPattern.RecoveryTime));
        else
            yield return new WaitForSeconds(attackPattern.RecoveryTime);

        onAttackEndEvents?.Invoke();

        cooldownTimer = attackPattern.AttackCooldown;

        // Note: attack patterns (e.g. SlimeDashAttackSO) already snap the transform
        // back onto the NavMesh before re-enabling the agent. Do NOT write to
        // transform.position again here while the agent is enabled - doing so
        // desyncs the agent from the NavMesh (isOnNavMesh becomes false) and
        // causes NavMeshAgent.CalculatePath/SetDestination to throw NullReferenceException
        // on the next Chase tick. Use agent.Warp() instead if a re-snap is ever needed.
        if (agent.enabled)
        {
            agent.isStopped = false;
        }

        if (IsPlayerInDetectionRange())
            TransitionTo(EnemyState.Chase);
        else
            TransitionTo(EnemyState.Patrol);
    }

    // ===================== DEAD =====================

    private void HandleDeath()
    {
        TransitionTo(EnemyState.Dead);
        enabled = false;
    }

    // ===================== HELPERS =====================

    private bool IsAgentReady()
    {
        return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    }

    private bool TryRebindAgentToNavMesh()
    {
        if (agent == null) return false;

        if (!agent.enabled)
            agent.enabled = true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            // Warp keeps the agent bound to the NavMesh. Direct transform writes do not.
            agent.Warp(hit.position);
            return agent.isOnNavMesh;
        }

        return false;
    }

    private bool IsPlayerInDetectionRange()
    {
        return playerTarget != null && DistanceToPlayer() <= detectionRadius;
    }

    private float DistanceToPlayer()
    {
        if (playerTarget == null) return float.MaxValue;
        Vector3 a = transform.position;
        Vector3 b = playerTarget.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 10f);
    }

    private void FaceTarget()
    {
        if (playerTarget == null) return;
        FaceDirection(playerTarget.position - transform.position);
    }

    private void FaceTargetImmediate()
    {
        if (playerTarget == null) return;
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private bool TryGetRandomPatrolPoint(out Vector3 point)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = homePosition + Random.insideUnitSphere * patrolRadius;
            candidate.y = homePosition.y;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = transform.position;
        return false;
    }

    private bool TryGetSwarmPosition(out Vector3 point)
    {
        if (playerTarget == null || !IsAgentReady())
        {
            point = transform.position;
            return false;
        }

        for (int i = 0; i < 12; i++)
        {
            float radius = Random.Range(swarmMinRadius, swarmMaxRadius);
            Vector2 circle = Random.insideUnitCircle.normalized * radius;
            Vector3 candidate = playerTarget.position + new Vector3(circle.x, 0f, circle.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 7f, NavMesh.AllAreas))
                continue;

            // Use the static NavMesh.CalculatePath instead of agent.CalculatePath.
            // The agent-instance version can throw a NullReferenceException deep
            // inside Unity when the agent's internal path buffer is in a bad state
            // (e.g. right after being re-enabled). The static version is stateless
            // and safe to call as long as a NavMesh exists.
            if (!NavMesh.CalculatePath(agent.transform.position, hit.position, NavMesh.AllAreas, pathCache))
                continue;

            if (pathCache.status != NavMeshPathStatus.PathComplete)
                continue;

            if (useSwarmSeparation && IsTooCloseToOtherEnemies(hit.position))
                continue;

            point = hit.position;
            return true;
        }

        point = transform.position;
        return false;
    }

    private bool IsTooCloseToOtherEnemies(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(position, separationDistance, separationResults, enemyLayerMask);
        return count > 1;
    }

    private IEnumerator TraverseOffMeshLink()
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 start = agent.transform.position;
        Vector3 end = data.endPos;
        float t = 0f;

        agent.updatePosition = false;

        while (t < offMeshLinkDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / offMeshLinkDuration);
            float height = Mathf.Sin(n * Mathf.PI) * offMeshLinkJumpHeight;
            agent.transform.position = Vector3.Lerp(start, end, n) + Vector3.up * height;
            yield return null;
        }

        agent.transform.position = end;
        agent.updatePosition = true;
        agent.CompleteOffMeshLink();
    }

    private IEnumerator SquishTelegraph(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float x = Mathf.Lerp(1f, 1.4f, n);
            float y = Mathf.Lerp(1f, 0.5f, n);
            transform.localScale = new Vector3(originalScale.x * x, originalScale.y * y, originalScale.z * x);
            yield return null;
        }
    }

    private IEnumerator SquishRecovery(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float x = Mathf.Lerp(1.4f, 1f, n);
            float y = Mathf.Lerp(0.5f, 1f, n);
            transform.localScale = new Vector3(originalScale.x * x, originalScale.y * y, originalScale.z * x);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, patrolRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        float range = attackPattern != null ? attackPattern.AttackRange : attackRange;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        if (useSwarmPositioning)
        {
            Vector3 center = playerTarget != null ? playerTarget.position : transform.position;
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawWireSphere(center, swarmMinRadius);
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.35f);
            Gizmos.DrawWireSphere(center, swarmMaxRadius);
        }
    }
}
