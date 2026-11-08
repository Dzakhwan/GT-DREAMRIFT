using System;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;

/// <summary>
/// Mekanik serangan jarak jauh Mage Projectile pada karakter Player:
/// - Smart Auto-Aim menghadap musuh terdekat di depan
/// - Cast Strafe Glide (tetap bisa bergeser halus 35% speed saat cast) + Arcane Recoil Step (~0.30m)
/// - Cooldown singkat (default 1.8 detik) terintegrasi dengan MageSkillButtonUI
/// - Sinkronisasi penuh dengan tongkat kristal (PlayerFight.StaffTipOrigin), Dash-Cancel, & WeaponUpgradeData
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerProjectileAttack : MonoBehaviour
{
    [Header("Cooldown & Projectile Stats")]
    [Tooltip("Durasi cooldown sebelum bisa menembakkan proyektil lagi (detik)")]
    [SerializeField] private float projectileCooldown = 1.8f;

    [Tooltip("Damage dasar proyektil (ditambah WeaponUpgradeData.BonusDamage)")]
    [SerializeField] private int baseProjectileDamage = 22;

    [Tooltip("Kecepatan luncur proyektil (m/s)")]
    [SerializeField] private float projectileSpeed = 18f;

    [Tooltip("Jarak jangkauan maksimum proyektil (meter)")]
    [SerializeField] private float projectileMaxRange = 18f;

    [Header("Smart Auto-Aim")]
    [Tooltip("Jarak pencarian target musuh otomatis di depan karakter (meter)")]
    [SerializeField] private float autoAimRadius = 14f;

    [Tooltip("Setengah sudut kerucut auto-aim di depan karakter (derajat)")]
    [SerializeField, Range(15f, 120f)] private float autoAimHalfAngle = 85f;

    [Tooltip("Radius auto-aim jarak dekat 360 derajat jika musuh menempel di samping/belakang")]
    [SerializeField] private float closeAutoAimRadius = 7.5f;

    [Tooltip("Kecepatan putar karakter menghadap target saat cast (derajat/detik)")]
    [SerializeField] private float aimTurnSpeed = 720f;

    [Tooltip("Layer musuh opsional untuk pencarian auto-aim (kosong = semua layer dengan komponen EnemyHealth/IDamageable)")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Cast Movement & Recoil")]
    [Tooltip("Pengali kecepatan gerak kaki selama animasi cast berlangsung (Cast Strafe Glide)")]
    [SerializeField, Range(0f, 1f)] private float castMoveSpeedMultiplier = 0.35f;

    [Tooltip("Jarak dorongan mikro ke belakang saat proyektil dilepaskan (meter)")]
    [SerializeField] private float recoilDistance = 0.30f;

    [Tooltip("Durasi dorongan Arcane Recoil Step (detik)")]
    [SerializeField] private float recoilDuration = 0.12f;

    [Tooltip("Waktu fallback peluncuran proyektil jika Animation Event belum terpanggil (detik)")]
    [SerializeField] private float autoFireDelay = 0.22f;

    [Tooltip("Durasi maksimum state casting sebelum direset otomatis (detik)")]
    [SerializeField] private float castFailsafeDuration = 0.62f;

    [Header("VFX & Projectile Prefabs")]
    [Tooltip("Prefab proyektil utama (MageProjectile_VFX)")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Prefab kilatan lingkaran sihir di ujung tongkat saat cast (MageCastMuzzle_VFX)")]
    [SerializeField] private GameObject castMuzzleVFXPrefab;

    [Tooltip("Prefab ledakan energi saat proyektil menghantam musuh/rintangan (MageProjectileImpact_VFX)")]
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("Input & UI Optional Binding")]
    [Tooltip("Aktifkan shortcut tombol E pada keyboard PC/WebGL")]
    [SerializeField] private bool enableKeyboardShortcutE = true;

    [Tooltip("Opsional: referensi tombol UI langsung jika tidak memakai UICanvasControllerInput")]
    [SerializeField] private Button projectileSkillButton;

    // ===================== PUBLIC STATE FOR UI & OTHER SCRIPTS =====================
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);
    public float CooldownDuration => Mathf.Max(0.01f, projectileCooldown);
    public float CooldownNormalized => cooldownTimer > 0f ? Mathf.Clamp01(cooldownTimer / CooldownDuration) : 0f;
    public bool IsOnCooldown => cooldownTimer > 0.001f;
    public bool IsCasting => isCasting;

    public event Action OnProjectileFired;
    public event Action OnCooldownReady;

    // ===================== INTERNAL REFERENCES & STATE =====================
    private Animator animator;
    private CharacterController controller;
    private StarterAssetsInputs input;
    private ThirdPersonController tpc;
    private PlayerFight playerFight;
    private Camera mainCam;

    private float cooldownTimer = 0f;
    private bool isCasting = false;
    private bool hasFiredThisCast = false;
    private bool hasSpawnedChargeVFX = false;
    private float castElapsed = 0f;

    private Transform lockedTarget;
    private Vector3 castAimDirection = Vector3.forward;

    private float recoilTimer = 0f;
    private Vector3 recoilDirection = Vector3.back;

    private static readonly int AnimIDMageCast = Animator.StringToHash("MageCast");
    private static readonly Collider[] aimOverlapBuffer = new Collider[32];

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
        if (input == null) input = GetComponentInParent<StarterAssetsInputs>();

        tpc = GetComponent<ThirdPersonController>();
        if (tpc == null) tpc = GetComponentInParent<ThirdPersonController>();

        playerFight = GetComponent<PlayerFight>();
        mainCam = Camera.main;
    }

    private void Start()
    {
        if (projectileSkillButton != null)
        {
            projectileSkillButton.onClick.AddListener(OnSkillButtonClicked);
        }

#if UNITY_EDITOR
        EnsurePrefabReferences();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsurePrefabReferences();
    }

    private void Reset()
    {
        EnsurePrefabReferences();
    }

    private void EnsurePrefabReferences()
    {
        if (projectilePrefab == null)
            projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/MageProjectile_VFX.prefab");
        if (castMuzzleVFXPrefab == null)
            castMuzzleVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/MageCastMuzzle_VFX.prefab");
        if (impactVFXPrefab == null)
            impactVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/MageProjectileImpact_VFX.prefab");
    }
#endif

    private void Update()
    {
        // 1. Update Cooldown Timer
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                OnCooldownReady?.Invoke();
            }
        }

        // 2. Update Arcane Recoil Step (dorongan halus mundur saat tembakan lepas)
        UpdateRecoilStep();

        // 3. Update State Casting (rotasi auto-aim + timer fallback tembakan)
        if (isCasting)
        {
            castElapsed += Time.deltaTime;
            UpdateCastFacing();

            if (!hasFiredThisCast && castElapsed >= autoFireDelay)
            {
                SpawnProjectileNow();
            }

            if (castElapsed >= castFailsafeDuration)
            {
                EndCastState();
            }
        }

        // 4. Baca Input (UI Virtual Button / InputSystem MageFire / Shortcut E)
        HandleProjectileInput();
    }

    private void HandleProjectileInput()
    {
        bool castRequested = false;

        if (input != null && input.mageFire)
        {
            castRequested = true;
            input.mageFire = false;
        }

#if ENABLE_INPUT_SYSTEM
        if (!castRequested && enableKeyboardShortcutE && !Application.isMobilePlatform &&
            Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            castRequested = true;
        }
#endif

        if (castRequested)
        {
            TryCastProjectile();
        }
    }

    private void OnSkillButtonClicked()
    {
        TryCastProjectile();
    }

    /// <summary>
    /// Memulai cast Mage Projectile jika tidak sedang cooldown, dash, atau tebasan melee.
    /// Mengembalikan true jika cast berhasil dimulai.
    /// </summary>
    public bool TryCastProjectile()
    {
        if (Time.timeScale <= 0.0001f && Application.isPlaying) return false;
        if (isCasting || IsOnCooldown) return false;
        if (tpc != null && tpc.IsDashing) return false;
        if (playerFight != null && playerFight.IsAttacking) return false;

        // 1. Cari target musuh hidup terbaik (Smart Auto-Aim)
        lockedTarget = FindBestTarget();
        castAimDirection = ComputeInitialAimDirection(lockedTarget);

        // 2. Aktifkan state casting & mulai cooldown
        isCasting = true;
        hasFiredThisCast = false;
        hasSpawnedChargeVFX = false;
        castElapsed = 0f;
        cooldownTimer = projectileCooldown;

        if (tpc != null)
        {
            tpc._castMoveSpeedMultiplier = castMoveSpeedMultiplier;
            tpc._isCastingProjectile = true;
        }

        // Putar awal ke arah target agar responsif sejak frame pertama
        if (castAimDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(castAimDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, aimTurnSpeed * 0.04f);
        }

        // 3. Picu animasi Armature_Spell_Simple_Shoot melalui parameter Trigger 'MageCast'
        if (animator != null)
        {
            animator.ResetTrigger(AnimIDMageCast);
            animator.SetTrigger(AnimIDMageCast);
        }

        return true;
    }

    private void UpdateCastFacing()
    {
        if (lockedTarget != null)
        {
            EnemyHealth eh = lockedTarget.GetComponent<EnemyHealth>();
            if ((eh != null && eh.IsDead) || !lockedTarget.gameObject.activeInHierarchy)
            {
                lockedTarget = null;
            }
            else
            {
                Vector3 flatToTarget = lockedTarget.position - transform.position;
                flatToTarget.y = 0f;
                if (flatToTarget.sqrMagnitude > 0.001f)
                {
                    castAimDirection = flatToTarget.normalized;
                }
            }
        }

        if (castAimDirection.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(castAimDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRot,
                aimTurnSpeed * Time.deltaTime);
        }
    }

    private void UpdateRecoilStep()
    {
        if (recoilTimer <= 0f) return;

        if (tpc != null && tpc.IsDashing)
        {
            recoilTimer = 0f;
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, recoilTimer);
        recoilTimer -= Time.deltaTime;

        if (controller != null && controller.enabled && recoilDuration > 0.001f)
        {
            // Kurva meluruh halus (kuat di awal pelepasan energi, lembut di akhir)
            float normalizedLeft = Mathf.Clamp01(recoilTimer / recoilDuration);
            float speedFactor = 2f * normalizedLeft;
            float baseRecoilSpeed = recoilDistance / recoilDuration;
            Vector3 displacement = recoilDirection * (baseRecoilSpeed * speedFactor * dt);
            controller.Move(displacement);
        }
    }

    private Vector3 ComputeInitialAimDirection(Transform target)
    {
        if (target != null)
        {
            Vector3 toEnemy = target.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.001f)
            {
                return toEnemy.normalized;
            }
        }

        // Jika tidak ada musuh, bidik mengikuti arah input gerakan analog/WASD relatif kamera
        if (input != null && input.move.sqrMagnitude > 0.01f)
        {
            if (mainCam == null) mainCam = Camera.main;
            float camYaw = mainCam != null ? mainCam.transform.eulerAngles.y : transform.eulerAngles.y;
            Vector3 moveDir = new Vector3(input.move.x, 0f, input.move.y).normalized;
            float targetYaw = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg + camYaw;
            return (Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward).normalized;
        }

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        return flatForward.sqrMagnitude > 0.001f ? flatForward.normalized : Vector3.forward;
    }

    private Transform FindBestTarget()
    {
        int mask = enemyLayer.value != 0 ? enemyLayer.value : ~0;
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            autoAimRadius,
            aimOverlapBuffer,
            mask,
            QueryTriggerInteraction.Collide);

        Vector3 referenceForward = ComputeInitialAimDirection(null);

        Transform bestConeTarget = null;
        float bestConeScore = float.MaxValue;

        Transform bestCloseFallback = null;
        float bestCloseDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = aimOverlapBuffer[i];
            if (col == null) continue;
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;
            if (col.CompareTag("Player")) continue;

            EnemyHealth eh = col.GetComponentInParent<EnemyHealth>();
            if (eh == null) eh = col.GetComponent<EnemyHealth>();

            IDamageable dmg = eh != null ? (IDamageable)eh : col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;
            if (eh != null && eh.IsDead) continue;

            Transform candidate = eh != null ? eh.transform : ((Component)dmg).transform;
            if (!candidate.gameObject.activeInHierarchy) continue;

            Vector3 offset = candidate.position - transform.position;
            Vector3 flatOffset = new Vector3(offset.x, 0f, offset.z);
            float dist = flatOffset.magnitude;
            if (dist < 0.05f) dist = 0.05f;

            float angle = Vector3.Angle(referenceForward, flatOffset.normalized);

            if (angle <= autoAimHalfAngle)
            {
                // Prioritaskan musuh yang dekat sekaligus sejajar arah hadap
                float score = dist + (angle * 0.06f);
                if (score < bestConeScore)
                {
                    bestConeScore = score;
                    bestConeTarget = candidate;
                }
            }
            else if (dist <= closeAutoAimRadius && dist < bestCloseDist)
            {
                bestCloseDist = dist;
                bestCloseFallback = candidate;
            }
        }

        return bestConeTarget != null ? bestConeTarget : bestCloseFallback;
    }

    private Vector3 GetMuzzleSpawnPosition()
    {
        Transform staffTip = playerFight != null ? playerFight.GetOrInitStaffTipOrigin() : null;
        if (staffTip != null && staffTip != transform)
        {
            return staffTip.position;
        }

        return transform.position + castAimDirection * 0.85f + Vector3.up * 1.10f;
    }

    private void SpawnMuzzleFlashVFX(Vector3 spawnPos, Vector3 fireDir)
    {
        if (castMuzzleVFXPrefab == null) return;

        Quaternion rot = fireDir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(fireDir, Vector3.up)
            : transform.rotation;

        GameObject muzzle = Instantiate(castMuzzleVFXPrefab, spawnPos, rot);
        PlayerMageProjectile.ApplyWeaponTierTint(muzzle);
        Destroy(muzzle, 0.85f);
    }

    private void SpawnProjectileNow()
    {
        if (hasFiredThisCast) return;
        hasFiredThisCast = true;

        // Pastikan menghadap lurus ke arah bidikan saat proyektil lepas
        UpdateCastFacing();
        if (castAimDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(castAimDirection, Vector3.up);
        }

        Vector3 spawnPos = GetMuzzleSpawnPosition();
        Vector3 fireDir = castAimDirection;

        if (lockedTarget != null)
        {
            EnemyHealth eh = lockedTarget.GetComponent<EnemyHealth>();
            if (eh == null || !eh.IsDead)
            {
                Vector3 aimPoint = PlayerMageProjectile.GetTargetAimPoint(lockedTarget);
                Vector3 toAim = aimPoint - spawnPos;
                if (toAim.sqrMagnitude > 0.04f)
                {
                    Vector3 rawDir = toAim.normalized;
                    // Batasi sudut tukik vertikal agar proyektil tidak menukik tajam ke lantai saat musuh sangat dekat
                    rawDir.y = Mathf.Clamp(rawDir.y, -0.55f, 0.55f);
                    fireDir = rawDir.normalized;
                }
            }
        }

        if (!hasSpawnedChargeVFX)
        {
            SpawnMuzzleFlashVFX(spawnPos, fireDir);
            hasSpawnedChargeVFX = true;
        }

        if (projectilePrefab != null)
        {
            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(fireDir, Vector3.up));
            PlayerMageProjectile.ApplyWeaponTierTint(projObj);

            PlayerMageProjectile proj = projObj.GetComponent<PlayerMageProjectile>();
            if (proj == null) proj = projObj.AddComponent<PlayerMageProjectile>();

            int totalDamage = Mathf.Max(1, baseProjectileDamage + WeaponUpgradeData.BonusDamage);
            proj.Initialize(
                gameObject,
                fireDir,
                projectileSpeed,
                projectileMaxRange,
                totalDamage,
                lockedTarget,
                impactVFXPrefab);
        }

        // Picu Arcane Recoil Step (~0.30m ke belakang)
        if (recoilDistance > 0.001f && recoilDuration > 0.001f)
        {
            recoilDirection = -castAimDirection;
            recoilDirection.y = 0f;
            if (recoilDirection.sqrMagnitude < 0.001f) recoilDirection = -transform.forward;
            recoilDirection.Normalize();
            recoilTimer = recoilDuration;
        }

        OnProjectileFired?.Invoke();
    }

    // ===================== ANIMATION EVENTS =====================

    /// <summary>
    /// [ANIMATION EVENT] Memunculkan kilatan lingkaran energi di ujung tongkat saat awal ayunan cast.
    /// </summary>
    public void TriggerMageCastChargeVFX()
    {
        if (!isCasting || hasSpawnedChargeVFX) return;
        hasSpawnedChargeVFX = true;
        SpawnMuzzleFlashVFX(GetMuzzleSpawnPosition(), castAimDirection);
    }

    /// <summary>
    /// [ANIMATION EVENT] Melontarkan proyektil tepat pada frame pelepasan tongkat ke depan.
    /// </summary>
    public void FireMageProjectileEvent()
    {
        if (!isCasting) return;
        SpawnProjectileNow();
    }

    /// <summary>
    /// [ANIMATION EVENT] Mengakhiri state casting pada akhir animasi Armature_Spell_Simple_Shoot.
    /// </summary>
    public void CloseMageCast()
    {
        if (!isCasting) return;
        if (!hasFiredThisCast)
        {
            SpawnProjectileNow();
        }
        EndCastState();
    }

    /// <summary>
    /// Membatalkan cast secara instan saat karakter melakukan Dash / Dodge (Dash-Cancel).
    /// </summary>
    public void CancelCast()
    {
        if (!isCasting) return;

        // Jika dibatalkan sebelum proyektil sempat keluar, beri cooldown singkat 0.35s saja
        if (!hasFiredThisCast)
        {
            cooldownTimer = Mathf.Min(cooldownTimer, 0.35f);
        }

        recoilTimer = 0f;
        EndCastState();
    }

    private void EndCastState()
    {
        isCasting = false;
        lockedTarget = null;
        castElapsed = 0f;

        if (tpc != null)
        {
            tpc._isCastingProjectile = false;
        }

        if (animator != null)
        {
            animator.ResetTrigger(AnimIDMageCast);
        }
    }

    private void OnDisable()
    {
        recoilTimer = 0f;
        EndCastState();
    }
}

