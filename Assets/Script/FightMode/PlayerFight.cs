using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// Script gabungan: mengurus ANIMASI serangan (combo) + DAMAGE ke musuh.
/// Pasang script ini ke GameObject Player.
///
/// Cara kerja damage:
/// - Tambahkan Animation Event di Animator pada frame tepat pukulan mendarat
/// - Hubungkan Animation Event ke method DealDamage() di script ini
/// - Script akan otomatis cari musuh dalam jangkauan dan memberikan damage
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerFight : MonoBehaviour
{
    // ===================== COMBAT SETTINGS =====================
    [Header("Combo Settings")]
    [SerializeField, Range(1, 3)] private int maxComboSteps = 3;
    [SerializeField] private float comboWindowDuration = 0.6f;
    [SerializeField] private float preAttackDelay = 0.15f;
    [SerializeField] private float attackFailsafeDuration = 3f;

    // ===================== DAMAGE SETTINGS =====================
    [Header("Damage Settings")]
    [Tooltip("Base damage serangan. Tiap combo step memiliki multiplier-nya sendiri.")]
    [SerializeField] private int baseDamage = 20;

    [Tooltip("Multiplier damage per combo step (index 0 = serangan 1, dst)")]
    [SerializeField] private float[] comboDamageMultipliers = { 1f, 1.2f, 1.5f };

    [Tooltip("Jangkauan hitbox serangan (radius dari attackOrigin)")]
    [SerializeField] private float attackRange = 3.5f;

    [Tooltip("Pengali jangkauan hitbox per combo step (index 0 = tebasan 1, dst)")]
    [SerializeField] private float[] comboRangeMultipliers = { 1f, 1.1f, 1.25f };

    [Tooltip("Layer musuh untuk filter Physics. Jika tidak pakai layer, kosongkan dan aktifkan Use Tag.")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Aktifkan jika ingin filter musuh menggunakan Tag saja (tanpa Layer)")]
    [SerializeField] private bool useTagInstead = false;

    [Tooltip("Tag musuh jika Use Tag diaktifkan")]
    [SerializeField] private string enemyTag = "Enemy";

    [Tooltip("Titik pusat hitbox serangan. Jika kosong, pakai posisi Player.")]
    [SerializeField] private Transform attackOrigin;

    [Tooltip("Waktu delay otomatis untuk eksekusi damage jika animasi tidak memicu Animation Event (fallback).")]
    [SerializeField] private float autoDamageDelay = 0.28f;

    [Tooltip("Tampilkan log detail pertarungan di Console (matikan untuk performa optimal).")]
    [SerializeField] private bool showDebugLogs = false;

    // ===================== COMBAT VFX & KNOCKBACK =====================
    [Header("Combat VFX")]
    [Tooltip("Prefab efek benturan partikel darah (Blood Hit VFX) saat mengenai musuh")]
    [SerializeField] private GameObject hitImpactVFXPrefab;

    [Tooltip("Prefab efek garis tebasan slice darah (Blood Slash Slice VFX) pada tubuh musuh")]
    [SerializeField] private GameObject hitSlashSliceVFXPrefab;

    [Tooltip("Prefab efek busur tebasan ayunan pedang di udara")]
    [SerializeField] private GameObject slashArcVFXPrefab;

    [Tooltip("Aktifkan efek busur sabetan pedang di udara saat mengayun")]
    [SerializeField] private bool enableSlashArcVFX = false;

    [Header("Knockback Settings")]
    [Tooltip("Jarak dorongan knockback per combo step (meter)")]
    [SerializeField] private float[] comboKnockbackDistances = { 1.2f, 2.0f, 3.8f };

    [Tooltip("Durasi dorongan knockback per combo step (detik)")]
    [SerializeField] private float[] comboKnockbackDurations = { 0.15f, 0.20f, 0.30f };

    // ===================== WEAPON (STAFF) SETTINGS =====================
    [Header("Staff Weapon Settings")]
    [Tooltip("Prefab/model tongkat (FBX)")]
    [SerializeField] private GameObject staffPrefab;

    [Tooltip("Material URP Lit untuk tongkat")]
    [SerializeField] private Material staffMaterial;

    [Tooltip("Aktifkan auto-attach tongkat ke tangan kanan saat Start")]
    [SerializeField] private bool autoAttachStaff = true;

    [Tooltip("Offset posisi tongkat di telapak tangan kanan")]
    [SerializeField] private Vector3 staffGripPosition = new Vector3(0.0f, 0.075f, 0.005f);

    [Tooltip("Offset rotasi tongkat di telapak tangan kanan")]
    [SerializeField] private Vector3 staffGripRotation = new Vector3(0f, 0f, 90f);

    [Tooltip("Skala tongkat agar proporsional")]
    [SerializeField] private Vector3 staffGripScale = new Vector3(0.7f, 0.7f, 0.7f);

    [Tooltip("Offset posisi model tongkat di dalam socket (maju ke depan/bawah genggaman)")]
    [SerializeField] private Vector3 staffModelOffset = new Vector3(0f, 0.30f, 0f);

    // ===================== MOBILE BUTTON =====================
    [Header("Mobile Input")]
    [Tooltip("Drag & Drop tombol Attack dari Canvas UI untuk mobile")]
    [SerializeField] private Button attackButton;

    // ===================== REFERENCES =====================
    private Animator animator;
    private StarterAssetsInputs input;
    private ThirdPersonController tpc;

    // ===================== STATE =====================
    private int attackIndex = 0;
    private bool isAttacking;
    private bool comboWindowOpen;
    private float comboWindowTimer;
    private bool isPreAttacking;
    private float preAttackTimer;
    private float attackFailsafeTimer;
    private bool hasDealtDamageThisAttack = false;
    private Coroutine autoDamageCoroutine;

    private static readonly int AttackAStateHash = Animator.StringToHash("Charged_Upward_Slash");
    private static readonly int AttackBStateHash = Animator.StringToHash("Armature_Sword_Regular_B");
    private static readonly int AttackCStateHash = Animator.StringToHash("Armature_Sword_Regular_C");

    // ===================== UNITY LIFECYCLE =====================

    private void Awake()
    {
        maxComboSteps = Mathf.Clamp(maxComboSteps, 1, 3);
        animator = GetComponent<Animator>();
        input = GetComponent<StarterAssetsInputs>();
        if (input == null)
            input = GetComponentInParent<StarterAssetsInputs>();

        tpc = GetComponent<ThirdPersonController>();
        if (tpc == null)
            tpc = GetComponentInParent<ThirdPersonController>();
    }

    private void Start()
    {
        // Pasangkan tongkat ke tangan kanan jika belum ada
        EnsureStaffEquipped();

        // Gunakan posisi Player jika attackOrigin tidak di-assign
        if (attackOrigin == null)
            attackOrigin = transform;

        // Daftarkan tombol attack untuk mobile
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonPressed);

#if UNITY_EDITOR
        if (hitImpactVFXPrefab == null)
            hitImpactVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/BloodHit_VFX.prefab");
        if (hitSlashSliceVFXPrefab == null)
            hitSlashSliceVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/BloodSlashSlice_VFX.prefab");
        if (slashArcVFXPrefab == null)
            slashArcVFXPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/SlashArc_VFX.prefab");
#endif
    }

    private void EnsureStaffEquipped()
    {
        if (!autoAttachStaff) return;

        // Cari tulang tangan kanan humanoid yang aktif dan teranimasi
        Transform rightHand = null;
        if (animator != null)
        {
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
        if (rightHand == null)
        {
            rightHand = FindDeepChild(transform, "RightHand");
        }
        if (rightHand == null)
        {
            rightHand = FindDeepChild(transform, "Right_Hand");
        }
        if (rightHand == null) return;

        // Bersihkan socket lama yang mungkin salah terpasang di skeleton dummy/lainnya
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == "Staff_Socket" && t.parent != rightHand)
            {
                Destroy(t.gameObject);
            }
        }

        Transform existingSocket = rightHand.Find("Staff_Socket");
        if (existingSocket != null)
        {
            existingSocket.localPosition = staffGripPosition;
            existingSocket.localRotation = Quaternion.Euler(staffGripRotation);
            existingSocket.localScale = staffGripScale;

            Transform existingModel = existingSocket.Find("Meshy_Crystal_Crescent_Staff");
            if (existingModel != null)
            {
                existingModel.localPosition = staffModelOffset;
            }

            Transform tip = existingSocket.Find("StaffTip_AttackOrigin");
            if (tip != null)
            {
                tip.localPosition = new Vector3(0f, 0.95f + staffModelOffset.y, 0f);
                if (attackOrigin == null) attackOrigin = tip;
            }
            return;
        }

#if UNITY_EDITOR
        if (staffPrefab == null)
            staffPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture.fbx");
        if (staffMaterial == null)
            staffMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Materials/M_CrystalCrescentStaff.mat");
#endif

        if (staffPrefab == null) return;

        GameObject socketObj = new GameObject("Staff_Socket");
        socketObj.transform.SetParent(rightHand, false);
        socketObj.transform.localPosition = staffGripPosition;
        socketObj.transform.localRotation = Quaternion.Euler(staffGripRotation);
        socketObj.transform.localScale = staffGripScale;

        GameObject staffInst = Instantiate(staffPrefab, socketObj.transform);
        staffInst.name = "Meshy_Crystal_Crescent_Staff";
        staffInst.transform.localPosition = staffModelOffset;
        staffInst.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);

        if (staffMaterial != null)
        {
            foreach (Renderer r in staffInst.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = staffMaterial;
            }
        }

        GameObject tipObj = new GameObject("StaffTip_AttackOrigin");
        tipObj.transform.SetParent(socketObj.transform, false);
        tipObj.transform.localPosition = new Vector3(0f, 0.95f + staffModelOffset.y, 0f);
        attackOrigin = tipObj.transform;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private void Update()
    {
        if (isAttacking)
        {
            attackFailsafeTimer -= Time.deltaTime;
            if (attackFailsafeTimer <= 0f)
                ResetAttackState();
        }

        if (comboWindowTimer > 0f)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
            {
                comboWindowTimer = 0f;
                comboWindowOpen = false;
            }
        }

        if (isPreAttacking)
        {
            preAttackTimer -= Time.deltaTime;
            if (preAttackTimer <= 0f)
            {
                isPreAttacking = false;
                StartAttack(1);
            }
        }

        HandleAttackInput();
    }

    // ===================== INPUT HANDLING =====================

    private void HandleAttackInput()
    {
        bool attackPressed = false;

        // Input dari StarterAssets (Mobile UI Fire Button / Joystick)
        if (input != null && input.fire)
        {
            if (IsPointerOverUI())
            {
                input.fire = false;
                return;
            }
            attackPressed = true;
            input.fire = false;
        }

        // Klik Kiri Mouse (LMB) HANYA aktif pada PC / Windows / WebGL (bukan Mobile / Android)
        if (!attackPressed && !Application.isMobilePlatform && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return;
            attackPressed = true;
        }

        if (!attackPressed) return;

        TryAttack();
    }

    private bool IsPointerOverUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;

        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Dipanggil oleh tombol Attack di UI (mobile).
    /// </summary>
    private void OnAttackButtonPressed()
    {
        TryAttack();
    }

    // ===================== ATTACK LOGIC =====================

    private void TryAttack()
    {
        if (isAttacking)
        {
            if (!isPreAttacking && comboWindowOpen && comboWindowTimer > 0f &&
                attackIndex < maxComboSteps && !IsTransitioningOutOfAttack())
            {
                int nextIndex = attackIndex + 1;
                Debug.Log($"[PlayerFight] Combo accepted → attackIndex={nextIndex}");
                StartAttack(nextIndex);
            }
            else
            {
                Debug.Log($"[PlayerFight] TryAttack blocked: comboWindowTimer={comboWindowTimer:F3} expired");
            }
            return;
        }

        // Pre-attack: stop movement, biarkan rotasi, lalu StartAttack setelah delay
        isPreAttacking = true;
        isAttacking = true;
        preAttackTimer = preAttackDelay;
        attackFailsafeTimer = Mathf.Max(0f, preAttackDelay) + Mathf.Max(0.1f, attackFailsafeDuration);
        comboWindowTimer = 0f;

        if (tpc != null)
        {
            tpc._isAttacking = true;
            tpc._lockRotation = false; // Saat pre-attack player masih bisa rotasi ke arah musuh/input
        }

        if (showDebugLogs)
            Debug.Log($"[PlayerFight] Pre-attack dimulai, delay={preAttackDelay:F2}s");
    }

    // Memulai serangan dengan combo index tertentu.
    // Dipanggil dari TryAttack (serangan pertama) dan CloseAttack (combo lanjutan).
    private void StartAttack(int index = 1, bool setAttackTrigger = true)
    {
        isAttacking = true;
        isPreAttacking = false;
        attackIndex = index;
        comboWindowOpen = false;
        comboWindowTimer = 0f;
        attackFailsafeTimer = Mathf.Max(0.1f, attackFailsafeDuration);

        if (showDebugLogs)
            Debug.Log($"[PlayerFight] StartAttack → attackIndex={attackIndex}");

        if (tpc != null)
        {
            tpc._isAttacking = true;
            tpc._lockRotation = true; // Kunci rotasi & aktifkan root motion pada OnAnimatorMove
        }

        if (animator != null)
        {
            animator.SetInteger("ActionIndex", attackIndex);
            if (setAttackTrigger)
            {
                animator.ResetTrigger("Attack");
                animator.SetTrigger("Attack");
            }
        }

        // Spawn efek busur tebasan ayunan pedang di udara jika diaktifkan
        if (enableSlashArcVFX)
        {
            SpawnSlashArcVisual(attackIndex);
        }

        hasDealtDamageThisAttack = false;
        if (autoDamageCoroutine != null) StopCoroutine(autoDamageCoroutine);
        autoDamageCoroutine = StartCoroutine(AutoDamageFallbackRoutine());
    }

    private void SpawnSlashArcVisual(int comboStep)
    {
        if (slashArcVFXPrefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * 1.0f + Vector3.up * 0.9f;
        float rollAngle = comboStep == 1 ? 0f : (comboStep == 2 ? 45f : -45f);
        Quaternion rot = transform.rotation * Quaternion.Euler(0f, 0f, rollAngle);

        GameObject arc = Instantiate(slashArcVFXPrefab, spawnPos, rot);
        Destroy(arc, 0.5f);
    }

    private System.Collections.IEnumerator AutoDamageFallbackRoutine()
    {
        yield return new WaitForSeconds(autoDamageDelay);
        if (isAttacking && !hasDealtDamageThisAttack)
        {
            DealDamage();
        }
    }

    // ===================== ANIMATION EVENTS =====================
    // Method-method di bawah ini dipanggil dari Animation Event di Animator Unity.
    // Caranya: buka Animator window → pilih animasi → di timeline klik kanan → Add Event
    // → pilih fungsi yang sesuai.

    /// <summary>
    /// [ANIMATION EVENT] Panggil method ini di frame tepat pukulan mendarat di animasi.
    /// Script akan otomatis mendeteksi dan memberikan damage ke musuh yang kena.
    /// </summary>
    public void DealDamage()
    {
        if (hasDealtDamageThisAttack) return;
        hasDealtDamageThisAttack = true;

        if (autoDamageCoroutine != null)
        {
            StopCoroutine(autoDamageCoroutine);
            autoDamageCoroutine = null;
        }

        // Hitung damage final berdasarkan combo step saat ini
        int finalDamage = baseDamage;
        int comboIndex = Mathf.Clamp(attackIndex - 1, 0, comboDamageMultipliers.Length - 1);
        finalDamage = Mathf.RoundToInt(baseDamage * comboDamageMultipliers[comboIndex]);

        if (showDebugLogs)
            Debug.Log($"[PlayerFight] DealDamage dipanggil. Combo step {attackIndex}, damage: {finalDamage}");

        Vector3 origin = (attackOrigin != null && attackOrigin != transform)
            ? attackOrigin.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 0.6f;

        // Hitung jangkauan efektif dengan bonus combo step jika ada
        float effectiveRange = attackRange;
        if (comboRangeMultipliers != null && comboRangeMultipliers.Length > 0)
        {
            int rIdx = Mathf.Clamp(attackIndex - 1, 0, comboRangeMultipliers.Length - 1);
            effectiveRange = attackRange * comboRangeMultipliers[rIdx];
        }

        // Cari semua collider musuh dalam jangkauan hitbox
        Collider[] hits;

        if (useTagInstead)
        {
            // Mode Tag: ambil semua collider lalu filter by tag
            hits = Physics.OverlapSphere(origin, effectiveRange);
        }
        else
        {
            int mask = enemyLayer.value != 0 ? enemyLayer.value : LayerMask.GetMask("Enemy");
            if (mask == 0) mask = ~0;
            // Mode Layer: langsung filter by layer
            hits = Physics.OverlapSphere(origin, effectiveRange, mask);
        }

        bool hitAnything = false;
        var damagedSet = new System.Collections.Generic.HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            // Lewati diri sendiri
            if (hit.gameObject == gameObject || hit.transform.IsChildOf(transform)) continue;

            // Jika mode tag, filter di sini
            if (useTagInstead && !hit.CompareTag(enemyTag)) continue;

            // Berikan damage jika objek implementasi IDamageable (cek di collider atau parent-nya)
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                damageable = hit.GetComponent<IDamageable>();

            if (damageable != null && damagedSet.Add(damageable))
            {
                damageable.TakeDamage(finalDamage);
                if (showDebugLogs)
                    Debug.Log($"[PlayerFight] Mengenai {hit.name} sebesar {finalDamage} damage!");
                hitAnything = true;

                // Cek referensi EnemyHealth untuk custom VFX per tipe musuh (misal: Slime)
                EnemyHealth enemyHealth = hit.GetComponentInParent<EnemyHealth>();
                if (enemyHealth == null) enemyHealth = hit.GetComponent<EnemyHealth>();

                // 1. Hit Impact Blood / Splatter VFX
                Vector3 contactPoint = hit.ClosestPoint(origin);
                Vector3 hitNormal = (contactPoint - origin);
                if (hitNormal.sqrMagnitude < 0.001f) hitNormal = transform.forward;
                hitNormal.Normalize();

                GameObject impactPrefabToSpawn = (enemyHealth != null && enemyHealth.CustomHitVFXPrefab != null)
                    ? enemyHealth.CustomHitVFXPrefab
                    : hitImpactVFXPrefab;

                if (impactPrefabToSpawn != null)
                {
                    GameObject impact = Instantiate(impactPrefabToSpawn, contactPoint, Quaternion.LookRotation(hitNormal));
                    Destroy(impact, 1.2f);
                }

                // 2. Damage Hit Slash Slice Cut VFX
                GameObject slicePrefabToSpawn = (enemyHealth != null && enemyHealth.CustomSlashSliceVFXPrefab != null)
                    ? enemyHealth.CustomSlashSliceVFXPrefab
                    : hitSlashSliceVFXPrefab;

                if (slicePrefabToSpawn != null)
                {
                    float sliceAngle = attackIndex == 1 ? 0f : (attackIndex == 2 ? -35f : 65f);
                    Quaternion sliceRot = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0f, 0f, sliceAngle);
                    GameObject slice = Instantiate(slicePrefabToSpawn, contactPoint, sliceRot);
                    Destroy(slice, 1.2f);
                }

                // 3. Knockback Physics pada musuh
                Vector3 knockbackDir = (hit.transform.position - transform.position);
                knockbackDir.y = 0f;
                if (knockbackDir.sqrMagnitude < 0.001f) knockbackDir = transform.forward;
                knockbackDir.Normalize();

                int kIndex = Mathf.Clamp(attackIndex - 1, 0, comboKnockbackDistances.Length - 1);
                float kDist = comboKnockbackDistances[kIndex];
                float kDur = comboKnockbackDurations[kIndex];

                EnemyAIController enemyAI = hit.GetComponentInParent<EnemyAIController>();
                if (enemyAI == null) enemyAI = hit.GetComponent<EnemyAIController>();

                if (enemyAI != null)
                {
                    enemyAI.ApplyKnockback(knockbackDir, kDist, kDur);
                }
                else if (enemyHealth != null)
                {
                    enemyHealth.ApplyKnockback(knockbackDir, kDist, kDur);
                }
            }
        }

        if (!hitAnything && showDebugLogs)
            Debug.Log("[PlayerFight] Serangan tidak mengenai musuh apapun.");
    }

    /// <summary>
    /// [ANIMATION EVENT] Panggil di titik animasi dimana combo input bisa diterima.
    /// </summary>
    public void OpenComboWindow()
    {
        if (!isAttacking || isPreAttacking || attackIndex >= maxComboSteps)
            return;

        comboWindowOpen = true;
        comboWindowTimer = comboWindowDuration;
        if (showDebugLogs)
            Debug.Log($"[PlayerFight] OpenComboWindow → comboWindowTimer={comboWindowTimer:F3}");
    }

    /// <summary>
    /// [ANIMATION EVENT] Panggil di akhir animasi serangan untuk mereset state.
    /// </summary>
    public void CloseAttack()
    {
        int currentStateAttackIndex = GetAttackStateIndex(animator.GetCurrentAnimatorStateInfo(0));
        if (currentStateAttackIndex != 0 && currentStateAttackIndex != attackIndex)
        {
            Debug.Log($"[PlayerFight] Ignoring stale CloseAttack from step {currentStateAttackIndex}; current step is {attackIndex}");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[PlayerFight] CloseAttack called → attackIndex={attackIndex}");
        ResetAttackState();
    }

    private bool IsTransitioningOutOfAttack()
    {
        return animator != null && animator.IsInTransition(0) &&
               GetAttackStateIndex(animator.GetNextAnimatorStateInfo(0)) == 0;
    }

    private static int GetAttackStateIndex(AnimatorStateInfo state)
    {
        if (state.shortNameHash == AttackAStateHash) return 1;
        if (state.shortNameHash == AttackBStateHash) return 2;
        if (state.shortNameHash == AttackCStateHash) return 3;
        return 0;
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        isPreAttacking = false;
        comboWindowOpen = false;
        comboWindowTimer = 0f;
        attackIndex = 0;
        attackFailsafeTimer = 0f;

        Debug.Log("[PlayerFight] Attack state reset.");

        if (autoDamageCoroutine != null)
        {
            StopCoroutine(autoDamageCoroutine);
            autoDamageCoroutine = null;
        }
        hasDealtDamageThisAttack = false;

        if (tpc != null)
        {
            tpc._isAttacking = false;
            tpc._lockRotation = false;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetInteger("ActionIndex", 0);
        }
    }

    private void OnDisable()
    {
        ResetAttackState();
    }

    // ===================== DEBUG =====================

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = (attackOrigin != null && attackOrigin != transform)
            ? attackOrigin.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 0.6f;
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}
