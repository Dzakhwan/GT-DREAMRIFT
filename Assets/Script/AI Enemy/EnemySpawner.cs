using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A modular spawner that populates enemies in a NavMesh area.
/// Automatically replenishes population up to maxEnemies when an enemy dies.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The Enemy prefab to spawn (must have EnemyHealth component).")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Maximum allowed active enemies spawned by this spawner.")]
    [SerializeField] private int maxEnemies = 5;

    [Tooltip("Radius around spawner to locate valid NavMesh positions.")]
    [SerializeField] private float spawnRadius = 10f;

    [Tooltip("Delay (seconds) before spawning a new enemy after one dies.")]
    [SerializeField] private float spawnInterval = 3f;

    [Header("Respawn Settings")]
    [Tooltip("Jika false, musuh yang mati TIDAK akan di-spawn ulang.")]
    [SerializeField] private bool respawnOnDeath = false;

    [Header("Events")]
    [Tooltip("Event yang dipanggil saat seluruh musuh dari spawner ini telah dieliminasi.")]
    public UnityEngine.Events.UnityEvent onAllEnemiesCleared;

    private readonly List<EnemyHealth> spawnedEnemies = new List<EnemyHealth>();
    private bool isSpawning = false;

    private void Start()
    {
        // Populate initially
        for (int i = 0; i < maxEnemies; i++)
        {
            SpawnEnemy();
        }
    }

    private void Update()
    {
        if (respawnOnDeath && spawnedEnemies.Count < maxEnemies && !isSpawning)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        yield return new WaitForSeconds(spawnInterval);

        if (respawnOnDeath && spawnedEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
        }
        isSpawning = false;
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[EnemySpawner] {gameObject.name} does not have an enemyPrefab assigned.");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        if (spawnPos != Vector3.zero)
        {
            GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            EnemyHealth enemyHealth = go.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                spawnedEnemies.Add(enemyHealth);
                // Listen for death to update active population
                enemyHealth.OnDeath += () => HandleEnemyDeath(enemyHealth);
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] Spawned prefab {go.name} is missing EnemyHealth component.");
            }
        }
    }

    private void HandleEnemyDeath(EnemyHealth enemy)
    {
        if (spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Remove(enemy);
            if (spawnedEnemies.Count == 0 && !isSpawning)
            {
                onAllEnemiesCleared?.Invoke();
            }
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
        randomPos += transform.position;
        if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
