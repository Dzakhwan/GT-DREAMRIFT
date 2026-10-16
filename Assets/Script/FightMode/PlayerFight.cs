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
    [SerializeField] private float attackRange = 1.8f;

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
        // Gunakan posisi Player jika attackOrigin tidak di-assign
        if (attackOrigin == null)
            attackOrigin = transform;

        // Daftarkan tombol attack untuk mobile
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonPressed);
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
            tpc._lockRotation = false;
        }

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

        Debug.Log($"[PlayerFight] StartAttack → attackIndex={attackIndex}");

        if (tpc != null)
        {
            tpc._isAttacking = true;
            tpc._lockRotation = true;
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

        hasDealtDamageThisAttack = false;
        if (autoDamageCoroutine != null) StopCoroutine(autoDamageCoroutine);
        autoDamageCoroutine = StartCoroutine(AutoDamageFallbackRoutine());
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

        Debug.Log($"[PlayerFight] DealDamage dipanggil. Combo step {attackIndex}, damage: {finalDamage}");

        Vector3 origin = (attackOrigin != null && attackOrigin != transform)
            ? attackOrigin.position
            : transform.position + transform.forward * 0.9f + Vector3.up * 0.6f;

        // Cari semua collider musuh dalam jangkauan hitbox
        Collider[] hits;

        if (useTagInstead)
        {
            // Mode Tag: ambil semua collider lalu filter by tag
            hits = Physics.OverlapSphere(origin, attackRange);
        }
        else
        {
            int mask = enemyLayer.value != 0 ? enemyLayer.value : LayerMask.GetMask("Enemy");
            if (mask == 0) mask = ~0;
            // Mode Layer: langsung filter by layer
            hits = Physics.OverlapSphere(origin, attackRange, mask);
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
                Debug.Log($"[PlayerFight] Mengenai {hit.name} sebesar {finalDamage} damage!");
                hitAnything = true;
            }
        }

        if (!hitAnything)
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
        if (attackOrigin == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
    }
}
