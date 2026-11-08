using UnityEngine;

/// <summary>
/// Turret Spawner untuk Tahap 2 Tutorial (Dodge/Dash).
/// Menembakkan Projectile ke arah Player secara berkala.
/// </summary>
public class TutorialProjectileSpawner : MonoBehaviour
{
    [Header("Spawner References")]
    [Tooltip("Prefab Projectile (Sphere) yang memiliki script Projectile.cs")]
    [SerializeField] private Projectile projectilePrefab;

    [Tooltip("Titik keluarnya peluru (jika kosong, menggunakan posisi Spawner)")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Target Player (otomatis dicari berdasarkan Tag 'Player' jika kosong)")]
    [SerializeField] private Transform playerTarget;

    [Header("Projectile & Firing Settings")]
    [Tooltip("Kecepatan projectile yang ditembakkan (bisa di-adjust di Inspector!)")]
    public float projectileSpeed = 10f;

    [Tooltip("Jeda waktu antar tembakan (detik)")]
    [SerializeField] private float fireInterval = 2.0f;

    [Tooltip("Ketinggian bidikan pada tubuh Player (meter dari titik kaki)")]
    [SerializeField] private float targetHeightOffset = 0.9f;

    private float fireTimer;
    private bool isSpawningActive = false;

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTarget = playerObj.transform;
        }
    }

    private void Update()
    {
        if (!isSpawningActive) return;

        if (playerTarget != null)
        {
            Vector3 lookPos = playerTarget.position - transform.position;
            lookPos.y = 0f;
            if (lookPos.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos), Time.deltaTime * 8f);
            }
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            fireTimer = fireInterval;
        }
    }

    public void StartSpawning()
    {
        gameObject.SetActive(true);
        isSpawningActive = true;
        fireTimer = 0.5f; // Tembakan pertama keluar 0.5 detik setelah panel tutorial ditutup
    }

    public void StopSpawning()
    {
        isSpawningActive = false;
        gameObject.SetActive(false);
    }

    private void FireProjectile()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
        Vector3 targetPos = playerTarget != null
            ? playerTarget.position + Vector3.up * targetHeightOffset
            : spawnPos + transform.forward * 10f;

        Vector3 direction = (targetPos - spawnPos).normalized;

        Projectile projInstance;
        if (projectilePrefab != null)
        {
            projInstance = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        }
        else
        {
            // Fallback otomatis: buat Sphere Primitive jika prefab belum di-assign
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "TutorialProjectile_Runtime";
            sphere.transform.position = spawnPos;
            sphere.transform.localScale = Vector3.one * 0.45f;

            SphereCollider col = sphere.GetComponent<SphereCollider>();
            col.isTrigger = true;

            Rigidbody rb = sphere.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            projInstance = sphere.AddComponent<Projectile>();
        }

        projInstance.Launch(direction, projectileSpeed);
    }
}

