using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private int maxComboSteps = 3;
    [SerializeField] private float comboWindowDuration = 0.25f;

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

    // ===================== MOBILE BUTTON =====================
    [Header("Mobile Input")]
    [Tooltip("Drag & Drop tombol Attack dari Canvas UI untuk mobile")]
    [SerializeField] private Button attackButton;

    // ===================== REFERENCES =====================
    private Animator animator;
    private CharacterController characterController;
    private StarterAssetsInputs input;

    // ===================== STATE =====================
    private int attackIndex = 0;
    private bool isAttacking;
    private bool comboWindowOpen;
    private bool comboRequested;
    private float comboWindowTimer;

    // ===================== UNITY LIFECYCLE =====================

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        input = GetComponent<StarterAssetsInputs>();
        if (input == null)
            input = GetComponentInParent<StarterAssetsInputs>();
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
        if (comboWindowTimer > 0f)
            comboWindowTimer -= Time.deltaTime;

        HandleAttackInput();
    }

    // ===================== INPUT HANDLING =====================

    private void HandleAttackInput()
    {
        bool attackPressed = false;

        // Input dari StarterAssets (mobile joystick system)
        if (input != null && input.fire)
        {
            attackPressed = true;
            input.fire = false;
        }

        // Fallback: mouse click kiri (untuk testing di Editor)
        if (!attackPressed && Input.GetMouseButtonDown(0))
            attackPressed = true;

        if (!attackPressed) return;

        TryAttack();
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
            // Simpan request combo jika masih dalam jendela combo
            if (comboWindowTimer > 0f)
            {
                comboRequested = true;
                comboWindowOpen = true;
                Debug.Log("[PlayerFight] Combo requested");
            }
            return;
        }

        StartAttack();
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackIndex = 1;
        comboRequested = false;
        comboWindowTimer = 0f;

        Debug.Log($"[PlayerFight] StartAttack → attackIndex={attackIndex}");

        if (animator != null)
        {
            animator.SetInteger("ActionIndex", attackIndex);
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
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
        // Hitung damage final berdasarkan combo step saat ini
        int finalDamage = baseDamage;
        int comboIndex = Mathf.Clamp(attackIndex - 1, 0, comboDamageMultipliers.Length - 1);
        finalDamage = Mathf.RoundToInt(baseDamage * comboDamageMultipliers[comboIndex]);

        Debug.Log($"[PlayerFight] DealDamage dipanggil. Combo step {attackIndex}, damage: {finalDamage}");

        // Cari semua collider musuh dalam jangkauan hitbox
        Collider[] hits;

        if (useTagInstead)
        {
            // Mode Tag: ambil semua collider lalu filter by tag
            hits = Physics.OverlapSphere(attackOrigin.position, attackRange);
        }
        else
        {
            // Mode Layer: langsung filter by layer (lebih efisien)
            hits = Physics.OverlapSphere(attackOrigin.position, attackRange, enemyLayer);
        }

        bool hitAnything = false;
        foreach (Collider hit in hits)
        {
            // Lewati diri sendiri
            if (hit.gameObject == gameObject) continue;

            // Jika mode tag, filter di sini
            if (useTagInstead && !hit.CompareTag(enemyTag)) continue;

            // Berikan damage jika objek implementasi IDamageable
            if (hit.TryGetComponent(out IDamageable damageable))
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
        comboWindowTimer = comboWindowDuration;
        Debug.Log($"[PlayerFight] OpenComboWindow. comboRequested={comboRequested}");

        if (!comboRequested && !comboWindowOpen) return;

        comboRequested = false;
        comboWindowOpen = false;
        attackIndex++;

        if (attackIndex > maxComboSteps)
            attackIndex = 1;

        Debug.Log($"[PlayerFight] Combo lanjut → attackIndex={attackIndex}");

        if (animator != null)
        {
            animator.SetInteger("ActionIndex", attackIndex);
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Attack");
        }
    }

    /// <summary>
    /// [ANIMATION EVENT] Panggil di akhir animasi serangan untuk mereset state.
    /// </summary>
    public void CloseAttack()
    {
        isAttacking = false;
        comboRequested = false;
        comboWindowOpen = false;
        comboWindowTimer = 0f;
        attackIndex = 0;

        Debug.Log("[PlayerFight] CloseAttack → state reset.");

        if (animator != null)
            animator.SetInteger("ActionIndex", 0);
    }

    // ===================== ROOT MOTION =====================

    private void OnAnimatorMove()
    {
        if (animator == null || characterController == null) return;
        if (!isAttacking) return;

        characterController.Move(animator.deltaPosition);
        transform.rotation *= animator.deltaRotation;
    }

    // ===================== DEBUG =====================

    private void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
    }
}
