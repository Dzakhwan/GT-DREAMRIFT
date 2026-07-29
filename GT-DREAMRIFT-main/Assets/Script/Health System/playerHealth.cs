using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro;

/// <summary>
/// Sistem health lengkap untuk Player.
/// Implementasi IDamageable agar bisa menerima damage dari sistem enemy yang sudah ada.
/// Pasang script ini ke GameObject Player.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField, Min(0f)] private float invincibilityDuration = 0.8f;

    [Header("UI References")]
    [Tooltip("Slider untuk Health Bar di HUD")]
    [SerializeField] private Slider healthBarSlider;
    [Tooltip("Text untuk menampilkan angka HP (opsional)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("Panel/Screen overlay merah saat terkena damage")]
    [SerializeField] private Image damageVignette;

    [Header("Death Settings")]
    [Tooltip("Scene yang diload saat Player mati (isi nama scene Game Over)")]
    [SerializeField] private string gameOverSceneName = "GameOver";
    [Tooltip("Delay sebelum pindah ke scene Game Over (dalam detik)")]
    [SerializeField, Min(0f)] private float deathDelay = 1.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDamaged;
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent onHealed;

    // State
    private int currentHealth;
    private bool isInvincible = false;
    private bool isDead = false;

    // Property publik untuk dibaca script lain
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public float HealthPercent => (float)currentHealth / maxHealth;

    // ===================== UNITY LIFECYCLE =====================

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Sembunyikan damage vignette di awal
        if (damageVignette != null)
        {
            Color c = damageVignette.color;
            c.a = 0f;
            damageVignette.color = c;
        }
    }

    // ===================== IDamageable =====================

    /// <summary>
    /// Dipanggil oleh MeleeAttackSO / EnemyProjectile saat musuh menyerang Player.
    /// </summary>
    public void TakeDamage(int damage)
    {
        // Abaikan damage jika sedang invincible atau sudah mati
        if (isInvincible || isDead) return;
        if (damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"[Player] Terkena {damage} damage. HP tersisa: {currentHealth}/{maxHealth}");

        UpdateHealthUI();
        onDamaged?.Invoke();

        // Tampilkan efek damage di layar
        StartCoroutine(ShowDamageVignette());

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Aktifkan invincibility frames agar tidak kena damage berkali-kali sekaligus
            StartCoroutine(ActivateInvincibility());
        }
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Tambah HP Player. Bisa dipanggil dari item heal, checkpoint, dll.
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"[Player] Heal {amount} HP. HP sekarang: {currentHealth}/{maxHealth}");

        UpdateHealthUI();
        onHealed?.Invoke();
    }

    /// <summary>
    /// Paksa Player mati. Bisa dipanggil dari trigger zona bahaya, dll.
    /// </summary>
    public void InstantKill()
    {
        if (isDead) return;
        currentHealth = 0;
        UpdateHealthUI();
        Die();
    }

    // ===================== PRIVATE METHODS =====================

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[Player] Player mati!");
        onDeath?.Invoke();

        // Nonaktifkan kontrol Player saat mati
        // Coba matikan script controller yang ada
        var controllers = GetComponents<MonoBehaviour>();
        foreach (var ctrl in controllers)
        {
            // Jangan matikan script ini sendiri
            if (ctrl == this) continue;
            // Matikan controller gerakan dan input
            if (ctrl.GetType().Name.Contains("Controller") || ctrl.GetType().Name.Contains("Input"))
                ctrl.enabled = false;
        }

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathDelay);

        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Nama scene Game Over belum diisi!");
        }
    }

    private IEnumerator ActivateInvincibility()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private IEnumerator ShowDamageVignette()
    {
        if (damageVignette == null) yield break;

        // Fade in merah
        float elapsed = 0f;
        float fadeDuration = 0.1f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = damageVignette.color;
            c.a = Mathf.Lerp(0f, 0.5f, elapsed / fadeDuration);
            damageVignette.color = c;
            yield return null;
        }

        // Fade out merah
        elapsed = 0f;
        fadeDuration = 0.4f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = damageVignette.color;
            c.a = Mathf.Lerp(0.5f, 0f, elapsed / fadeDuration);
            damageVignette.color = c;
            yield return null;
        }
    }

    private void UpdateHealthUI()
    {
        // Update Slider
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        // Update Teks Angka
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}
