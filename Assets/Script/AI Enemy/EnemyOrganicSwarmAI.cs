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

    [Header("Attack Settings")]
    [Tooltip("Pola serangan (ScriptableObject) yang digunakan oleh musuh ini")]
    [SerializeField] private AttackPatternSO attackPattern;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    // ===================== STATE MACHINE =====================
    private enum EnemyState
    {
        Moving,
        WindUp,     // Bersiap menyerang (diam sejenak)
        Recovery    // Pemulihan setelah menyerang (diam sejenak)
    }

    private EnemyState currentState = EnemyState.Moving;
    private float stateTimer = 0f;
    private float cooldownTimer = 0f;

    // ===================== INTERNAL =====================
    private NavMeshAgent agent;
    private float repositionTimer;
    private bool hasValidTarget = false;

    // Variabel cache untuk optimasi Garbage Collection (GC)
    private int enemyLayerMask;
    private readonly Collider[] separationResults = new Collider[8];

    /// <summary>
    /// Flag ini bisa dikontrol dari script lain.
    /// Contoh: saat musuh sedang terkena stun, set menjadi false agar tidak reposition.
    /// </summary>
    [HideInInspector] public bool canReposition = true;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;

        // Cache LayerMask sekali saja saat Awake untuk optimasi performa
        enemyLayerMask = LayerMask.GetMask("Enemy");
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

        if (attackPattern == null)
        {
            Debug.LogWarning($"[Combat] {gameObject.name} belum memiliki AttackPatternSO yang ditentukan di Inspector!");
        }
    }

    void Update()
    {
        if (!hasValidTarget) return;

        // Kurangi cooldown serangan jika aktif
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Jalankan State Machine untuk pergerakan dan serangan
        switch (currentState)
        {
            case EnemyState.Moving:
                HandleMovingState();
                break;
            case EnemyState.WindUp:
                HandleWindUpState();
                break;
            case EnemyState.Recovery:
                HandleRecoveryState();
                break;
        }
    }

    private void HandleMovingState()
    {
        if (!canReposition) return;

        // Periksa apakah siap menyerang (jarak mencukupi & cooldown selesai)
        if (attackPattern != null && cooldownTimer <= 0f)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer <= attackPattern.AttackRange)
            {
                // Masuk ke fase bersiap menyerang (berhenti diam)
                currentState = EnemyState.WindUp;
                stateTimer = attackPattern.WindUpTime;
                agent.isStopped = true;
                agent.ResetPath(); // Batalkan path pergerakan saat ini
                return;
            }
        }

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

    private void HandleWindUpState()
    {
        stateTimer -= Time.deltaTime;

        // Buat musuh tetap menghadap ke player secara halus selama bersiap menyerang
        LookAtTarget();

        if (stateTimer <= 0f)
        {
            // Eksekusi serangan setelah waktu bersiap habis
            if (attackPattern != null)
            {
                attackPattern.ExecuteAttack(this, playerTarget);
            }

            // Pindah ke recovery state (jeda pemulihan)
            currentState = EnemyState.Recovery;
            stateTimer = attackPattern.RecoveryTime;
        }
    }

    private void HandleRecoveryState()
    {
        stateTimer -= Time.deltaTime;

        // Tetap menghadap player saat recovery
        LookAtTarget();

        if (stateTimer <= 0f)
        {
            // Kembali bergerak dan set cooldown serangan baru
            currentState = EnemyState.Moving;
            cooldownTimer = attackPattern.AttackCooldown;
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// Memaksa rotasi musuh agar menghadap ke target secara halus (hanya sumbu Y).
    /// </summary>
    private void LookAtTarget()
    {
        if (playerTarget == null) return;
        Vector3 direction = (playerTarget.position - transform.position);
        direction.y = 0; // Kunci sumbu Y agar musuh tidak mendongak ke atas/bawah
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
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
    /// Mengecek apakah posisi terlalu dekat dengan musuh lain dengan optimasi performa.
    /// </summary>
    private bool IsPositionTooCloseToOtherEnemies(Vector3 position)
    {
        // Menggunakan OverlapSphereNonAlloc untuk menghindari alokasi memori berkala
        int count = Physics.OverlapSphereNonAlloc(position, separationDistance, separationResults, enemyLayerMask);
        return count > 1; // > 1 berarti ada musuh lain selain objek musuh saat ini
    }

    // ===================== PUBLIC METHOD (untuk script lain) =====================

    /// <summary>
    /// Method ini bisa dipanggil dari script combat jika ingin memaksa musuh pindah posisi.
    /// </summary>
    public void ForceReposition()
    {
        if (currentState != EnemyState.Moving) return; // Jangan memaksa pindah jika sedang menyerang

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

        // Debug visual range serangan jika attackPattern di-assign
        if (attackPattern != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackPattern.AttackRange);
        }
    }
}