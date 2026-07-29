# Basic Enemy AI Scaffolding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a production-ready, mobile-optimized 3D Basic Enemy AI scaffolding with a modular state machine, health event integration, and a spawner on the NavMesh.

**Architecture:** Extend the existing `IDamageable`/`EnemyHealth` stack without breaking the active combat scripts. Define a modular Finite State Machine (`BasicEnemyAI`) using throttled destination updates and state actions, and build an `EnemySpawner` that manages enemy counts reactively.

**Tech Stack:** Unity C#, Unity Navigation (NavMesh), System.Action events, mobile CPU throttling techniques.

## Global Constraints

- Code must follow clean C# PascalCase/camelCase naming conventions.
- All configurable parameters must use `[SerializeField] private` fields.
- Integrate smoothly with `IDamageable` using `int` parameter types to match existing PlayerAttack/MeleeAttackSO scripts.
- No debug/extra comments unless explicitly requested.

---

### Task 1: Update IDamageable and EnemyHealth Events

**Files:**
- Modify: `Assets/Script/Health System/IDamageable.cs` (Ensure signature compatibility)
- Modify: `Assets/Script/Health System/EnemyHealth.cs`

**Interfaces:**
- Consumes: None.
- Produces: `EnemyHealth.OnDeath` C# Action event (`public event System.Action OnDeath;` or `public event System.Action<EnemyHealth> OnDeath;`).

- [ ] **Step 1: Read IDamageable.cs and EnemyHealth.cs to confirm structures**

- [ ] **Step 2: Add Action Event in EnemyHealth.cs**
  Add the C# event and raise it inside `Die()`:
  ```csharp
  public event System.Action OnDeath;
  ```

- [ ] **Step 3: Modify Die() method**
  Locate `Die()` in `EnemyHealth.cs` and trigger `OnDeath?.Invoke();` right after setting `isDead = true;`.

- [ ] **Step 4: Verify Compilation**
  Check that the project compiles cleanly in Unity.

- [ ] **Step 5: Commit**
  ```bash
  git add "Assets/Script/Health System/EnemyHealth.cs"
  git commit -m "feat(enemy-health): add System.Action OnDeath event trigger"
  ```

---

### Task 2: Create Basic Enemy AI FSM

**Files:**
- Create: `Assets/Script/AI Enemy/BasicEnemyAI.cs`

**Interfaces:**
- Consumes: `EnemyHealth`, `IDamageable`
- Produces: `BasicEnemyAI` script component with `EnemyState` [Patrol, Chase, Attack, Dead].

- [ ] **Step 1: Create BasicEnemyAI.cs script**
  Implement the enum-based FSM, NavMeshAgent updates, target-finding, throttling, attack checks, and visual gizmos:
  ```csharp
  using UnityEngine;
  using UnityEngine.AI;

  [RequireComponent(typeof(NavMeshAgent))]
  [RequireComponent(typeof(EnemyHealth))]
  public class BasicEnemyAI : MonoBehaviour
  {
      public enum EnemyState { Patrol, Chase, Attack, Dead }

      [Header("State Settings")]
      [SerializeField] private EnemyState currentState = EnemyState.Patrol;

      [Header("Detection Settings")]
      [SerializeField] private float detectionRadius = 10f;
      [SerializeField] private float attackRadius = 2f;
      [SerializeField] private LayerMask targetLayer;

      [Header("Movement Settings")]
      [SerializeField] private float patrolRadius = 8f;
      [SerializeField] private float minPatrolWaitTime = 1f;
      [SerializeField] private float maxPatrolWaitTime = 4f;

      [Header("Attack Settings")]
      [SerializeField] private int damageAmount = 10;
      [SerializeField] private float attackCooldown = 1.5f;

      [Header("Optimization Settings")]
      [SerializeField] private float pathUpdateInterval = 0.15f;

      private NavMeshAgent agent;
      private EnemyHealth health;
      private Transform currentTarget;
      private float pathUpdateTimer;
      private float patrolWaitTimer;
      private float attackCooldownTimer;
      private Vector3 patrolDestination;
      private bool hasPatrolDestination;

      private void Awake()
      {
          agent = GetComponent<NavMeshAgent>();
          health = GetComponent<EnemyHealth>();
      }

      private void OnEnable()
      {
          health.OnDeath += HandleDeath;
      }

      private void OnDisable()
      {
          health.OnDeath -= HandleDeath;
      }

      private void Start()
      {
          ChooseNextPatrolDestination();
      }

      private void Update()
      {
          if (currentState == EnemyState.Dead) return;

          UpdateCooldowns();
          UpdateStateTransitions();

          switch (currentState)
          {
              case EnemyState.Patrol:
                  UpdatePatrol();
                  break;
              case EnemyState.Chase:
                  UpdateChase();
                  break;
              case EnemyState.Attack:
                  UpdateAttack();
                  break;
          }
      }

      private void UpdateCooldowns()
      {
          if (attackCooldownTimer > 0)
              attackCooldownTimer -= Time.deltaTime;
      }

      private void UpdateStateTransitions()
      {
          Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);
          if (colliders.Length > 0)
          {
              currentTarget = colliders[0].transform;
              float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

              if (distanceToTarget <= attackRadius)
              {
                  currentState = EnemyState.Attack;
              }
              else
              {
                  currentState = EnemyState.Chase;
              }
          }
          else
          {
              currentTarget = null;
              if (currentState != EnemyState.Patrol)
              {
                  currentState = EnemyState.Patrol;
                  ChooseNextPatrolDestination();
              }
          }
      }

      private void UpdatePatrol()
      {
          if (!agent.isOnNavMesh) return;

          if (agent.remainingDistance <= agent.stoppingDistance)
          {
              patrolWaitTimer += Time.deltaTime;
              if (patrolWaitTimer >= Random.Range(minPatrolWaitTime, maxPatrolWaitTime))
              {
                  ChooseNextPatrolDestination();
              }
          }
      }

      private void ChooseNextPatrolDestination()
      {
          patrolWaitTimer = 0f;
          Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
          randomDirection += transform.position;

          if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
          {
              patrolDestination = hit.position;
              agent.SetDestination(patrolDestination);
          }
      }

      private void UpdateChase()
      {
          if (currentTarget == null) return;

          pathUpdateTimer += Time.deltaTime;
          if (pathUpdateTimer >= pathUpdateInterval)
          {
              pathUpdateTimer = 0f;
              if (agent.isOnNavMesh)
              {
                  agent.SetDestination(currentTarget.position);
              }
          }
      }

      private void UpdateAttack()
      {
          if (currentTarget == null) return;

          if (agent.isOnNavMesh)
          {
              agent.ResetPath();
          }

          Vector3 direction = (currentTarget.position - transform.position).normalized;
          direction.y = 0;
          if (direction != Vector3.zero)
          {
              transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
          }

          if (attackCooldownTimer <= 0)
          {
              PerformAttack();
          }
      }

      private void PerformAttack()
      {
          attackCooldownTimer = attackCooldown;
          if (currentTarget.TryGetComponent(out IDamageable damageable))
          {
              damageable.TakeDamage(damageAmount);
          }
      }

      private void HandleDeath()
      {
          currentState = EnemyState.Dead;
          if (agent.isOnNavMesh)
          {
              agent.isStopped = true;
              agent.enabled = false;
          }
          var col = GetComponent<Collider>();
          if (col != null) col.enabled = false;
      }

      private void OnDrawGizmosSelected()
      {
          Gizmos.color = Color.yellow;
          Gizmos.DrawWireSphere(transform.position, detectionRadius);

          Gizmos.color = Color.red;
          Gizmos.DrawWireSphere(transform.position, attackRadius);
      }
  }
  ```

- [ ] **Step 2: Verify Compilation**
  Check that the project compiles cleanly.

- [ ] **Step 3: Commit**
  ```bash
  git add "Assets/Script/AI Enemy/BasicEnemyAI.cs"
  git commit -m "feat(ai): implement BasicEnemyAI FSM with throttled NavMesh updates"
  ```

---

### Task 3: Create Enemy Spawner

**Files:**
- Create: `Assets/Script/AI Enemy/EnemySpawner.cs`

**Interfaces:**
- Consumes: `EnemyHealth`
- Produces: `EnemySpawner` script component that manages counts and listens to `EnemyHealth.OnDeath`.

- [ ] **Step 1: Create EnemySpawner.cs script**
  Implement spawner rules, NavMesh sampling, prefab instantiation, and reactive count management:
  ```csharp
  using UnityEngine;
  using UnityEngine.AI;
  using System.Collections.Generic;

  public class EnemySpawner : MonoBehaviour
  {
      [Header("Prefab Settings")]
      [SerializeField] private GameObject enemyPrefab;

      [Header("Spawning Rules")]
      [SerializeField] private int maxActiveEnemies = 5;
      [SerializeField] private float spawnInterval = 3f;
      [SerializeField] private float spawnRadius = 15f;

      private List<EnemyHealth> activeEnemies = new List<EnemyHealth>();
      private float spawnTimer;

      private void Update()
      {
          spawnTimer += Time.deltaTime;
          if (spawnTimer >= spawnInterval)
          {
              spawnTimer = 0f;
              if (activeEnemies.Count < maxActiveEnemies)
              {
                  SpawnEnemy();
              }
          }
      }

      private void SpawnEnemy()
      {
          Vector3 randomPoint = Random.insideUnitSphere * spawnRadius;
          randomPoint += transform.position;

          if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
          {
              GameObject spawnedObj = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
              if (spawnedObj.TryGetComponent(out EnemyHealth enemyHealth))
              {
                  activeEnemies.Add(enemyHealth);
                  enemyHealth.OnDeath += () => HandleEnemyDeath(enemyHealth);
              }
          }
      }

      private void HandleEnemyDeath(EnemyHealth enemyHealth)
      {
          if (activeEnemies.Contains(enemyHealth))
          {
              activeEnemies.Remove(enemyHealth);
          }
      }

      private void OnDrawGizmosSelected()
      {
          Gizmos.color = Color.cyan;
          Gizmos.DrawWireSphere(transform.position, spawnRadius);
      }
  }
  ```

- [ ] **Step 2: Verify Compilation**
  Check that the project compiles cleanly.

- [ ] **Step 3: Commit**
  ```bash
  git add "Assets/Script/AI Enemy/EnemySpawner.cs"
  git commit -m "feat(ai): implement EnemySpawner with reactive count tracking"
  ```
