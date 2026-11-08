using UnityEngine;

/// <summary>
/// Script Projectile untuk Tahap 2 Tutorial (Dodge/Dash).
/// Gunakan Sphere 3D Primitive sebagai placeholder dengan SphereCollider (IsTrigger = true) dan Rigidbody (IsKinematic = true).
/// Kecepatan (speed) dapat diatur langsung di Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings (Adjustable in Inspector)")]
    [Tooltip("Kecepatan gerak projectile (meter/detik). Bisa diubah langsung di Inspector!")]
    public float speed = 10f;

    [Tooltip("Damage yang diberikan jika mengenai Player")]
    [SerializeField] private int damage = 15;

    [Tooltip("Waktu hidup maksimum projectile sebelum hancur otomatis (detik)")]
    [SerializeField] private float lifeTime = 5f;

    private Vector3 moveDirection = Vector3.forward;
    private float spawnTimer;
    private bool hasHitPlayer = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Pastikan Rigidbody tidak jatuh karena gravitasi
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Inisialisasi arah gerak dan (opsional) override kecepatan dari Spawner.
    /// </summary>
    public void Launch(Vector3 direction, float customSpeed = -1f)
    {
        moveDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        if (customSpeed > 0f)
        {
            speed = customSpeed;
        }
        transform.rotation = Quaternion.LookRotation(moveDirection);
        spawnTimer = lifeTime;
    }

    private void Update()
    {
        // Bergerak maju sesuai variabel speed (otomatis berhenti saat Time.timeScale == 0)
        transform.position += moveDirection * (speed * Time.deltaTime);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Abaikan spawner atau sesama trigger
        if (other.GetComponent<TutorialProjectileSpawner>() != null || other.GetComponent<Projectile>() != null)
            return;

        if (other.CompareTag("Player"))
        {
            // Cek apakah Player sedang Dash / Invincible (I-Frame dari ThirdPersonController / PlayerHealth)
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            StarterAssets.ThirdPersonController tpc = other.GetComponentInParent<StarterAssets.ThirdPersonController>();

            bool isDodging = (tpc != null && tpc.IsDashing) || (playerHealth != null && playerHealth.IsInvincible);

            if (isDodging)
            {
                Debug.Log("[Projectile] Player berhasil melakukan Dash menembus Projectile (I-Frame aktif)!");
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.NotifySuccessfulDashDodge();
                }
                Destroy(gameObject);
                return;
            }

            // Jika tidak sedang Dash, berikan damage ke Player
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                hasHitPlayer = true;
                damageable.TakeDamage(damage);
                Debug.Log($"[Projectile] Mengenai Player sebesar {damage} damage! Gunakan tombol Dash untuk menghindar.");
            }

            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            // Hancur jika menabrak tembok/obstacle non-trigger
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Jika projectile hancur dan belum mengenai Player sementara Player sudah menekan Dash di fase 2
        if (!hasHitPlayer && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnProjectilePassedWithoutHit();
        }
    }
}

