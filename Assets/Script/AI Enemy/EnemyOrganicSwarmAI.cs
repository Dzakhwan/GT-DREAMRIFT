using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyOrganicSwarmAI : MonoBehaviour
{
    [Header("Referensi Target")]
    [Tooltip("Target yang akan dikejar (biasanya Player)")]
    public Transform playerTarget;

    [Header("Organic Positioning (Pathfinding)")]
    [Tooltip("Jarak minimum musuh dari pemain")]
    public float minRadius = 2.5f;

    [Tooltip("Jarak maksimum musuh dari pemain")]
    public float maxRadius = 6.5f;

    [Tooltip("Seberapa sering musuh mencari posisi baru (dalam detik)")]
    public float repositionInterval = 2.2f;

    [Tooltip("Jarak berhenti dari tujuan (stopping distance)")]
    public float stoppingDistance = 1.0f;

    [Header("Separation (Antar Musuh)")]
    [Tooltip("Aktifkan pemisahan antar musuh agar tidak saling tumpang tindih")]
    public bool enableSeparation = true;

    [Tooltip("Jarak minimum antar musuh")]
    public float separationDistance = 2.0f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // ===================== INTERNAL =====================
    private NavMeshAgent agent;
    private float repositionTimer;
    private bool hasValidTarget = false;

    /// <summary>
    /// Flag ini bisa dikontrol dari script lain.
    /// Contoh: saat musuh sedang menyerang, set menjadi false agar tidak reposition.
    /// </summary>
    [HideInInspector] public bool canReposition = true;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
    }

    void Start()
    {
        // Jika playerTarget belum di-assign via Inspector, cari otomatis
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }

        hasValidTarget = playerTarget != null;

        // Acak timer awal agar tidak semua musuh bergerak bersamaan
        repositionTimer = Random.Range(0f, repositionInterval * 0.5f);
    }

    void Update()
    {
        if (!hasValidTarget || !canReposition) return;

        repositionTimer -= Time.deltaTime;

        // Reposition jika:
        // 1. Timer sudah habis, ATAU
        // 2. Sudah sampai dekat tujuan sebelumnya
        bool hasArrived = agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending;

        if (repositionTimer <= 0f || hasArrived)
        {
            if (TryGetValidOrganicPosition(out Vector3 newPosition))
            {
                agent.SetDestination(newPosition);
            }

            // Reset timer dengan sedikit variasi
            repositionTimer = repositionInterval + Random.Range(-0.6f, 1.4f);
        }
    }

    /// <summary>
    /// Mencari posisi valid di sekitar pemain dengan multiple attempt + validasi path.
    /// </summary>
    private bool TryGetValidOrganicPosition(out Vector3 validPosition)
    {
        const int maxAttempts = 12;

        for (int i = 0; i < maxAttempts; i++)
        {
            // Ambil posisi acak dalam lingkaran
            float radius = Random.Range(minRadius, maxRadius);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * radius;
            Vector3 candidate = playerTarget.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Sampling ke NavMesh
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 7f, NavMesh.AllAreas))
            {
                // Validasi apakah path benar-benar bisa dicapai
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    // Cek separation (jika diaktifkan)
                    if (!enableSeparation || !IsPositionTooCloseToOtherEnemies(hit.position))
                    {
                        validPosition = hit.position;
                        return true;
                    }
                }
            }
        }

        // Fallback: tetap di posisi saat ini
        validPosition = transform.position;
        return false;
    }

    /// <summary>
    /// Mengecek apakah posisi terlalu dekat dengan musuh lain.
    /// </summary>
    private bool IsPositionTooCloseToOtherEnemies(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, separationDistance, LayerMask.GetMask("Enemy"));
        return hits.Length > 1; // > 1 berarti ada musuh lain di area tersebut
    }

    // ===================== PUBLIC METHOD (untuk script lain) =====================

    /// <summary>
    /// Method ini bisa dipanggil dari script combat jika ingin memaksa musuh pindah posisi.
    /// </summary>
    public void ForceReposition()
    {
        if (TryGetValidOrganicPosition(out Vector3 newPos))
        {
            agent.SetDestination(newPos);
            repositionTimer = repositionInterval;
        }
    }

    // ===================== DEBUG =====================
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || playerTarget == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTarget.position, minRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(playerTarget.position, maxRadius);
    }
}