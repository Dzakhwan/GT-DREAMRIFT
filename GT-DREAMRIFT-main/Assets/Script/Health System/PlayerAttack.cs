using UnityEngine;
using System.Collections;

/// <summary>
/// Sistem serangan Player ke musuh.
/// Pasang script ini ke GameObject Player.
/// 
/// Cara kerja: saat Player menekan tombol Attack di layar (mobile),
/// script ini akan cek enemy dalam jangkauan menggunakan OverlapSphere
/// dan memanggil TakeDamage() pada musuh yang terkena.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage yang diberikan ke musuh per serangan")]
    [SerializeField] private int attackDamage = 20;

    [Tooltip("Jangkauan serangan Player (radius dalam unit Unity)")]
    [SerializeField] private float attackRange = 1.8f;

    [Tooltip("Cooldown antar serangan (dalam detik)")]
    [SerializeField] private float attackCooldown = 0.6f;

    [Tooltip("Layer musuh agar tidak mengenai objek lain")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Titik pusat serangan. Jika kosong, otomatis pakai posisi Player.")]
    [SerializeField] private Transform attackOrigin;

    [Header("Combo Settings")]
    [Tooltip("Aktifkan sistem combo (serangan beruntun yang meningkat)")]
    [SerializeField] private bool enableCombo = true;
    [Tooltip("Multiplier damage per serangan combo (contoh: 1 > 1.2 > 1.5)")]
    [SerializeField] private float[] comboDamageMultipliers = { 1f, 1.2f, 1.5f };
    [Tooltip("Waktu jendela combo (jika tidak serang dalam waktu ini, combo reset)")]
    [SerializeField] private float comboWindowTime = 1.2f;

    [Header("UI Reference")]
    [Tooltip("Drag & Drop tombol Attack dari Hierarchy")]
    [SerializeField] private UnityEngine.UI.Button attackButton;

    // State
    private float lastAttackTime = -999f;
    private int comboStep = 0;
    private float comboWindowTimer = 0f;
    private bool isOnCooldown = false;

    // ===================== UNITY LIFECYCLE =====================

    private void Start()
    {
        // Daftarkan tombol attack
        if (attackButton != null)
            attackButton.onClick.AddListener(TryAttack);

        // Gunakan posisi Player jika attackOrigin tidak di-assign
        if (attackOrigin == null)
            attackOrigin = transform;
    }

    private void Update()
    {
        // Reset combo jika tidak ada serangan dalam jendela waktu combo
        if (enableCombo && comboStep > 0)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0f)
            {
                comboStep = 0;
            }
        }
    }

    // ===================== PUBLIC METHOD =====================

    /// <summary>
    /// Dipanggil saat tombol Attack di layar ditekan.
    /// Juga bisa dipanggil langsung dari script lain.
    /// </summary>
    public void TryAttack()
    {
        // Cek cooldown
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        isOnCooldown = true;

        // Hitung damage berdasarkan combo step
        int finalDamage = attackDamage;
        if (enableCombo && comboDamageMultipliers.Length > 0)
        {
            int index = Mathf.Min(comboStep, comboDamageMultipliers.Length - 1);
            finalDamage = Mathf.RoundToInt(attackDamage * comboDamageMultipliers[index]);
            Debug.Log($"[Player] Combo Step {comboStep + 1}, damage: {finalDamage}");
        }

        // Eksekusi serangan
        PerformAttack(finalDamage);

        // Update combo
        if (enableCombo)
        {
            comboStep = (comboStep + 1) % (comboDamageMultipliers.Length > 0 ? comboDamageMultipliers.Length : 1);
            comboWindowTimer = comboWindowTime;
        }

        StartCoroutine(CooldownRoutine());
    }

    // ===================== PRIVATE METHODS =====================

    private void PerformAttack(int damage)
    {
        // Cari semua musuh dalam jangkauan serangan
        Collider[] hits = Physics.OverlapSphere(attackOrigin.position, attackRange, enemyLayer);

        if (hits.Length == 0)
        {
            Debug.Log("[Player] Serangan mengenai angin...");
            return;
        }

        foreach (Collider hit in hits)
        {
            // Coba ambil komponen IDamageable (EnemyHealth)
            if (hit.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
                Debug.Log($"[Player] Mengenai {hit.name} sebesar {damage} damage!");
            }
        }
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        isOnCooldown = false;
    }

    // ===================== DEBUG =====================

    /// <summary>
    /// Visualisasi jangkauan serangan di Scene View.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin.position, attackRange);
    }
}
