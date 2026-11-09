using System;
using UnityEngine;

/// <summary>
/// Komponen fisika, soft-homing, deteksi tabrakan anti-tunneling, dan efek benturan
/// untuk proyektil sihir jarak jauh karakter Mage (Player).
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class PlayerMageProjectile : MonoBehaviour
{
    [Header("Flight Settings")]
    [Tooltip("Kecepatan terbang proyektil (meter/detik)")]
    [SerializeField] private float projectileSpeed = 18f;

    [Tooltip("Jarak tempuh maksimum proyektil sebelum lenyap (meter)")]
    [SerializeField] private float maxRange = 18f;

    [Tooltip("Radius SphereCast untuk deteksi benturan anti-tembus")]
    [SerializeField] private float collisionRadius = 0.32f;

    [Tooltip("Kecepatan belok soft-homing mengikuti target di medan miring (derajat/detik)")]
    [SerializeField] private float homingTurnSpeed = 90f;

    [Header("Knockback Settings")]
    [Tooltip("Jarak dorongan ringan saat proyektil menghantam musuh")]
    [SerializeField] private float knockbackDistance = 1.4f;

    [Tooltip("Durasi dorongan knockback proyektil (detik)")]
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Visual & Impact VFX")]
    [Tooltip("Prefab ledakan energi saat proyektil mengenai musuh atau dinding")]
    [SerializeField] private GameObject impactVFXPrefab;

    [Tooltip("Transform opsional cincin rune pada proyektil yang berputar selama terbang")]
    [SerializeField] private Transform spinningHaloTransform;

    [Tooltip("Kecepatan putar cincin rune selama terbang (derajat/detik)")]
    [SerializeField] private float haloSpinSpeed = 420f;

    private GameObject owner;
    private Vector3 moveDirection = Vector3.forward;
    private int damage = 22;
    private Transform homingTarget;
    private float traveledDistance = 0f;
    private bool isInitialized = false;
    private bool hasImpacted = false;

    private static readonly RaycastHit[] sphereCastBuffer = new RaycastHit[16];
    private static readonly Collider[] overlapBuffer = new Collider[12];

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDefaultReferences();
    }

    private void Reset()
    {
        EnsureDefaultReferences();
    }

    private void EnsureDefaultReferences()
    {
        if (impactVFXPrefab == null)
        {
            impactVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/VFX/MageProjectileImpact_VFX.prefab");
        }
    }
#endif

    /// <summary>
    /// Menginisialisasi parameter terbang proyektil saat ditembakkan dari ujung tongkat.
    /// </summary>
    public void Initialize(
        GameObject ownerObject,
        Vector3 direction,
        float speedOverride,
        float rangeOverride,
        int finalDamage,
        Transform target = null,
        GameObject customImpactPrefab = null)
    {
        owner = ownerObject;
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        if (speedOverride > 0.01f) projectileSpeed = speedOverride;
        if (rangeOverride > 0.1f) maxRange = rangeOverride;
        damage = Mathf.Max(1, finalDamage);
        homingTarget = target;
        if (customImpactPrefab != null) impactVFXPrefab = customImpactPrefab;

        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = collisionRadius;
        }

        if (spinningHaloTransform == null)
        {
            Transform halo = transform.Find("RuneHalo");
            if (halo != null) spinningHaloTransform = halo;
        }

        isInitialized = true;

        // Cek langsung jika musuh berada tepat di depan ujung tongkat saat spawn
        CheckInitialOverlap();
    }

    private void Update()
    {
        if (!isInitialized || hasImpacted) return;
        StepSimulation(Time.deltaTime);
    }

    /// <summary>
    /// Menjalankan satu langkah simulasi terbang & deteksi benturan proyektil.
    /// </summary>
    public void StepSimulation(float dt)
    {
        if (hasImpacted || dt <= 0f) return;

        // 1. Soft-homing halus agar bidikan tetap akurat pada lereng/bukit LV 1
        if (homingTarget != null)
        {
            EnemyHealth targetHealth = homingTarget.GetComponent<EnemyHealth>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                homingTarget = null;
            }
            else if (!homingTarget.gameObject.activeInHierarchy)
            {
                homingTarget = null;
            }
            else
            {
                Vector3 aimPoint = GetTargetAimPoint(homingTarget);
                Vector3 toTarget = aimPoint - transform.position;
                if (toTarget.sqrMagnitude > 0.04f)
                {
                    float maxRadians = homingTurnSpeed * Mathf.Deg2Rad * dt;
                    moveDirection = Vector3.RotateTowards(moveDirection, toTarget.normalized, maxRadians, 0f).normalized;
                    transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                }
            }
        }

        // Putar cincin rune pada sumbu terbang proyektil
        if (spinningHaloTransform != null)
        {
            spinningHaloTransform.Rotate(0f, 0f, haloSpinSpeed * dt, Space.Self);
        }

        // 2. SphereCast kontinu sepanjang langkah frame ini (anti-tunneling)
        float stepDistance = projectileSpeed * dt;
        int hitCount = Physics.SphereCastNonAlloc(
            transform.position,
            collisionRadius,
            moveDirection,
            sphereCastBuffer,
            stepDistance,
            ~0,
            QueryTriggerInteraction.Collide);

        if (hitCount > 0)
        {
            // Urutkan dari jarak terdekat agar mengenai target paling depan lebih dulu
            Array.Sort(sphereCastBuffer, 0, hitCount, RaycastHitDistanceComparer.Instance);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = sphereCastBuffer[i];
                if (hit.collider == null) continue;

                Vector3 hitPoint = hit.point.sqrMagnitude > 0.0001f
                    ? hit.point
                    : hit.collider.ClosestPoint(transform.position);
                Vector3 hitNormal = hit.normal.sqrMagnitude > 0.0001f
                    ? hit.normal
                    : -moveDirection;

                if (TryProcessHitCollider(hit.collider, hitPoint, hitNormal))
                {
                    return;
                }
            }
        }

        // 3. Majukan posisi proyektil
        transform.position += moveDirection * stepDistance;
        traveledDistance += stepDistance;

        if (traveledDistance >= maxRange)
        {
            ExpireAtMaxRange();
        }
    }

    private void CheckInitialOverlap()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            collisionRadius,
            overlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;

            Vector3 hitPoint = col.ClosestPoint(transform.position);
            if (TryProcessHitCollider(col, hitPoint, -moveDirection))
            {
                return;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isInitialized || hasImpacted || other == null) return;
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        TryProcessHitCollider(other, hitPoint, -moveDirection);
    }

    private bool TryProcessHitCollider(Collider col, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (col == null || hasImpacted) return false;

        // Abaikan proyektil sendiri & pemilik proyektil (Player)
        if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) return false;
        if (owner != null && (col.gameObject == owner || col.transform.IsChildOf(owner.transform))) return false;
        if (col.CompareTag("Player")) return false;
        if (col.GetComponentInParent<PlayerMageProjectile>() != null) return false;

        // 1. Cek apakah objek adalah entitas yang bisa menerima damage (Enemy / IDamageable)
        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null) damageable = col.GetComponent<IDamageable>();

        if (damageable != null)
        {
            // Jangan mengenai Player jika ada komponen IDamageable pada Player
            Component dmgComp = damageable as Component;
            if (dmgComp != null && (dmgComp.CompareTag("Player") || (owner != null && dmgComp.transform.IsChildOf(owner.transform))))
            {
                return false;
            }

            EnemyHealth enemyHealth = col.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = col.GetComponent<EnemyHealth>();

            // Abaikan musuh yang sudah mati agar proyektil menembus ke musuh hidup di belakangnya
            if (enemyHealth != null && enemyHealth.IsDead)
            {
                return false;
            }

            hasImpacted = true;
            damageable.TakeDamage(damage);

            // Terapkan knockback ringan searah laju proyektil
            Vector3 knockDir = moveDirection;
            knockDir.y = 0f;
            if (knockDir.sqrMagnitude < 0.001f) knockDir = transform.forward;
            knockDir.Normalize();

            EnemyAIController enemyAI = col.GetComponentInParent<EnemyAIController>();
            if (enemyAI == null) enemyAI = col.GetComponent<EnemyAIController>();

            if (enemyAI != null)
            {
                enemyAI.ApplyKnockback(knockDir, knockbackDistance, knockbackDuration);
            }
            else if (enemyHealth != null)
            {
                enemyHealth.ApplyKnockback(knockDir, knockbackDistance, knockbackDuration);
            }

            SpawnImpactVFX(hitPoint, hitNormal);
            CleanupAndDestroy();
            return true;
        }

        // 2. Abaikan semua Trigger volume non-musuh (QuestTrigger, TutorialTrigger, CoinPickup, Shrine, dsb.)
        if (col.isTrigger)
        {
            return false;
        }

        // 3. Collider padat (Tanah, Tebing, Pohon, Dinding) -> Hancurkan proyektil dengan Impact VFX
        hasImpacted = true;
        SpawnImpactVFX(hitPoint, hitNormal);
        CleanupAndDestroy();
        return true;
    }

    private void SpawnImpactVFX(Vector3 position, Vector3 normal)
    {
        if (impactVFXPrefab == null) return;

        Quaternion rot = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject impact = Instantiate(impactVFXPrefab, position, rot);
        ApplyWeaponTierTint(impact);
        Destroy(impact, 1.5f);
    }

    private void ExpireAtMaxRange()
    {
        if (hasImpacted) return;
        hasImpacted = true;
        CleanupAndDestroy();
    }

    private void CleanupAndDestroy()
    {
        // Lepaskan TrailRenderer agar pita cahaya memudar alami, bukan hilang mendadak
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>();
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer tr = trails[i];
            if (tr == null) continue;
            tr.emitting = false;
            tr.transform.SetParent(null, true);
            Destroy(tr.gameObject, Mathf.Max(0.15f, tr.time + 0.1f));
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Menghitung titik bidik tengah tubuh target menggunakan Collider.bounds.center.
    /// </summary>
    public static Vector3 GetTargetAimPoint(Transform target)
    {
        if (target == null) return Vector3.zero;

        Collider solidCol = null;
        Collider anyCol = null;
        Collider[] cols = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null || !cols[i].enabled) continue;
            if (anyCol == null) anyCol = cols[i];
            if (!cols[i].isTrigger)
            {
                solidCol = cols[i];
                break;
            }
        }

        Collider bestCol = solidCol != null ? solidCol : anyCol;
        if (bestCol != null)
        {
            Vector3 center = bestCol.bounds.center;
            float minHeight = target.position.y + 0.35f;
            if (center.y < minHeight) center.y = minHeight;
            return center;
        }

        return target.position + Vector3.up * 0.55f;
    }

    /// <summary>
    /// Mewarnai dan menskalakan instance VFX proyektil/muzzle/impact sesuai tier WeaponUpgradeData.
    /// </summary>
    public static void ApplyWeaponTierTint(GameObject vfxInstance)
    {
        if (vfxInstance == null) return;

        Color tierColor = WeaponUpgradeData.GetVfxColor();
        float tierScale = WeaponUpgradeData.GetVfxScale();

        ParticleSystem[] systems = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null) continue;

            var main = ps.main;
            Color baseColor = main.startColor.Evaluate(0.5f);
            bool isHotCore = ps.gameObject.name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0;
            Color tint = isHotCore ? Color.Lerp(tierColor, Color.white, 0.55f) : tierColor;

            Color finalColor = new Color(
                Mathf.Clamp01(baseColor.r * tint.r),
                Mathf.Clamp01(baseColor.g * tint.g),
                Mathf.Clamp01(baseColor.b * tint.b),
                baseColor.a);
            main.startColor = finalColor;
        }

        TrailRenderer[] trails = vfxInstance.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer tr = trails[i];
            if (tr == null) continue;

            Color startCol = Color.Lerp(tierColor, Color.white, 0.35f);
            startCol.a = tr.startColor.a;
            Color endCol = tierColor;
            endCol.a = tr.endColor.a;

            tr.startColor = startCol;
            tr.endColor = endCol;
            tr.widthMultiplier *= tierScale;
        }

        if (!Mathf.Approximately(tierScale, 1f))
        {
            vfxInstance.transform.localScale *= tierScale;
        }
    }

    private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }
}

