using UnityEngine;

namespace Dreamrift.Currency
{
    /// <summary>
    /// Koin 3D yang dapat dipungut oleh Player di dalam game world.
    /// Berputar dan melayang secara anggun, otomatis menambahkan koin ke CurrencyManager saat disentuh
    /// atau otomatis tersedot (auto-collect magnet) saat Player berada di dalam radius tertentu.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CoinPickup : MonoBehaviour
    {
        [Header("Coin Value")]
        [Tooltip("Jumlah koin yang didapatkan saat dipungut")]
        [SerializeField, Min(1)] private int coinValue = 1;

        [Header("Animation")]
        [Tooltip("Kecepatan putaran koin (derajat/detik)")]
        [SerializeField] private float rotationSpeed = 200f;

        [Tooltip("Amplitudo melayang naik-turun (bobbing)")]
        [SerializeField] private float bobHeight = 0.12f;

        [Tooltip("Kecepatan melayang naik-turun")]
        [SerializeField] private float bobSpeed = 3.2f;

        [Tooltip("Kemiringan dinamis koin saat berputar (wobble tilt)")]
        [SerializeField] private float tiltAngle = 12f;
        [SerializeField] private float tiltSpeed = 2.4f;

        [Header("Spawn Pop Animation")]
        [Tooltip("Aktifkan animasi melontar melengkung saat koin pertama kali muncul")]
        [SerializeField] private bool playSpawnPopArc = true;
        [SerializeField] private float popHeight = 1.2f;
        [SerializeField] private float popDuration = 0.42f;

        [Header("Auto Collect / Magnet")]
        [Tooltip("Jarak deteksi otomatis pemain untuk mulai menarik koin secara magnet")]
        [SerializeField] private float autoCollectRadius = 3.5f;

        [Tooltip("Jeda waktu sebelum koin dapat ditarik magnet setelah muncul (agar animasi drop terlihat)")]
        [SerializeField] private float collectDelayAfterSpawn = 0.35f;

        [Tooltip("Kecepatan awal tarikan magnet koin")]
        [SerializeField] private float magnetSpeed = 9f;

        [Tooltip("Akselerasi kecepatan saat koin meluncur ke arah pemain")]
        [SerializeField] private float magnetAcceleration = 22f;

        [Tooltip("Offset ketinggian target pada tubuh pemain (dada/pinggang)")]
        [SerializeField] private float playerTargetHeight = 0.9f;

        [Header("Effects")]
        [Tooltip("Prefab partikel kilau emas saat koin dipungut (opsional)")]
        [SerializeField] private GameObject pickupVFXPrefab;

        private Vector3 startPosition;
        private Vector3 originalScale;
        private float phaseOffset;
        private float spawnTime;
        private bool isCollected = false;
        private bool isHoming = false;
        private float currentHomingSpeed;
        private float currentYaw;

        // Spawn arc state
        private bool isPopping = false;
        private float popTimer = 0f;
        private Vector3 popOrigin;
        private Vector3 popLandingPos;

        // Cache player transform
        private static Transform cachedPlayer;

        public float AutoCollectRadius => autoCollectRadius;

        private void Awake()
        {
            originalScale = transform.localScale;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            currentYaw = Random.Range(0f, 360f);
        }

        private void Start()
        {
            startPosition = transform.position;
            spawnTime = Time.time;
            currentHomingSpeed = magnetSpeed;

            // Pastikan collider adalah trigger
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            EnsurePlayerReference();

            if (playSpawnPopArc)
            {
                InitSpawnPopArc();
            }
        }

        private void InitSpawnPopArc()
        {
            isPopping = true;
            popTimer = 0f;
            popOrigin = transform.position;

            // Cari posisi mendarat di tanah dengan sedikit sebaran horizontal
            Vector2 randCircle = Random.insideUnitCircle * 0.75f;
            Vector3 targetXZ = popOrigin + new Vector3(randCircle.x, 0f, randCircle.y);

            // Raycast ke bawah untuk memastikan koin mendarat pas di atas permukaan tanah/NavMesh
            if (Physics.Raycast(targetXZ + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                popLandingPos = hit.point + Vector3.up * 0.45f;
            }
            else
            {
                popLandingPos = targetXZ;
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // 1. Fase Lontaran Awal (Spawn Pop Arc)
            if (isPopping)
            {
                popTimer += Time.deltaTime;
                float t = Mathf.Clamp01(popTimer / Mathf.Max(0.1f, popDuration));

                // Parabola: naik di awal, mendarat di akhir
                float arcY = 4f * popHeight * t * (1f - t);
                Vector3 currentPos = Vector3.Lerp(popOrigin, popLandingPos, t);
                currentPos.y += arcY;
                transform.position = currentPos;

                // Putar cepat selama melontar
                currentYaw += rotationSpeed * 1.5f * Time.deltaTime;
                ApplyCoinRotation(0f);

                if (t >= 1f)
                {
                    isPopping = false;
                    startPosition = popLandingPos;
                    transform.position = popLandingPos;
                }
                return;
            }

            EnsurePlayerReference();

            // 2. Cek Auto Collect Magnet
            float timeSinceSpawn = Time.time - spawnTime;
            if (cachedPlayer != null && timeSinceSpawn >= collectDelayAfterSpawn)
            {
                Vector3 playerTargetPos = cachedPlayer.position + Vector3.up * playerTargetHeight;
                float distToPlayer = Vector3.Distance(transform.position, playerTargetPos);

                if (!isHoming && distToPlayer <= autoCollectRadius)
                {
                    isHoming = true;
                }

                if (isHoming)
                {
                    currentHomingSpeed += magnetAcceleration * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, playerTargetPos, currentHomingSpeed * Time.deltaTime);

                    // Putar lebih lincah saat tersedot
                    currentYaw += rotationSpeed * 2.5f * Time.deltaTime;
                    ApplyCoinRotation(0f);

                    // Efek mengecil halus saat sudah sangat dekat dengan dada pemain (kesan terserap)
                    if (distToPlayer < 1.0f)
                    {
                        float scaleFactor = Mathf.Clamp01(distToPlayer / 1.0f);
                        transform.localScale = originalScale * Mathf.Max(0.2f, scaleFactor);
                    }

                    // Saat koin mencapai pemain (< 0.35m), otomatis ambil
                    if (distToPlayer <= 0.35f)
                    {
                        Collect();
                        return;
                    }
                    return;
                }
            }

            // 3. Fase Normal Idle Hover (Melayang, Berputar, & Miring Berkilau)
            currentYaw += rotationSpeed * Time.deltaTime;
            float tiltWave = Mathf.Sin((Time.time + phaseOffset) * tiltSpeed) * tiltAngle;
            ApplyCoinRotation(tiltWave);

            float bobOffset = Mathf.Sin((Time.time + phaseOffset) * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPosition.x, startPosition.y + bobOffset, startPosition.z);
        }

        private void ApplyCoinRotation(float tilt)
        {
            // Rotasi gabungan: Yaw putaran utama di sumbu Y + Tilt wobble di sumbu X/Z
            Quaternion yawRot = Quaternion.Euler(0f, currentYaw, 0f);
            Quaternion tiltRot = Quaternion.Euler(tilt, 0f, tilt * 0.45f);
            transform.rotation = yawRot * tiltRot;
        }

        private void EnsurePlayerReference()
        {
            if (cachedPlayer == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    cachedPlayer = playerGO.transform;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            // Abaikan jika baru saja spawn sebelum delay selesai
            if (Time.time - spawnTime < 0.15f) return;

            if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(coinValue);
            }
            else
            {
                Debug.LogWarning("[CoinPickup] CurrencyManager.Instance tidak ditemukan di scene!");
            }

            if (pickupVFXPrefab != null)
            {
                Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        public void SetCoinValue(int val)
        {
            coinValue = Mathf.Max(1, val);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, autoCollectRadius);
        }
    }
}
