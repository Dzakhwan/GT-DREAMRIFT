using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Sistem health untuk Enemy.
/// Pasang script ini ke setiap GameObject musuh.
/// Implementasi IDamageable agar bisa menerima damage dari serangan Player.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 50;

    [Header("Health Bar (Screen Space Pool)")]
    [Tooltip("Aktifkan agar pakai sistem pool (1 Canvas untuk semua enemy). Direkomendasikan.")]
    [SerializeField] private bool usePooledHealthBar = true;

    [Header("Hit Effect")]
    [Tooltip("Warna flash saat musuh terkena damage")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.15f;

    [Header("Death Settings")]
    [Tooltip("Delay sebelum musuh dihancurkan setelah mati (beri waktu animasi)")]
    [SerializeField, Min(0f)] private float deathDelay = 0.5f;
    [Tooltip("Prefab efek VFX yang di-spawn saat musuh mati (opsional)")]
    [SerializeField] private GameObject deathVFXPrefab;

    [Header("Loot (Opsional)")]
    [Tooltip("Prefab item yang di-drop saat musuh mati")]
    [SerializeField] private GameObject lootPrefab;
    [Tooltip("Offset posisi drop loot dari posisi musuh")]
    [SerializeField] private Vector3 lootOffset = new Vector3(0, 0.5f, 0);

    [Header("Events")]
    [SerializeField] private UnityEvent onDamaged;
    [SerializeField] private UnityEvent onDeath;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDeath;

    // State
    private int currentHealth;
    private bool isDead = false;

    // Cache untuk hit flash effect
    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    // Property publik
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    // ===================== UNITY LIFECYCLE =====================

    private void Awake()
    {
        // Cache semua renderer untuk hit flash effect
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ===================== IDamageable =====================

    /// <summary>
    /// Dipanggil oleh sistem serangan Player saat musuh terkena serangan.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"[Enemy] {gameObject.name} terkena {damage} damage. HP tersisa: {currentHealth}/{maxHealth}");

        UpdateHealthBar();
        onDamaged?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Tampilkan health bar lewat pool
        if (usePooledHealthBar && EnemyHealthBarPool.Instance != null)
            EnemyHealthBarPool.Instance.ShowHealthBar(transform, currentHealth, maxHealth);

        // Efek flash merah
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlash());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Paksa musuh mati. Bisa dipanggil dari ability khusus, cutscene, dll.
    /// </summary>
    public void InstantKill()
    {
        if (isDead) return;
        currentHealth = 0;
        UpdateHealthBar();
        Die();
    }

    // ===================== PRIVATE METHODS =====================

    [Header("Quest Integration")]
    [Tooltip("ID/Nama Tipe Musuh untuk sistem Quest (misal: Slime, Goblin, Boss)")]
    [SerializeField] private string enemyId = "Slime";

    public string EnemyId => enemyId;

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[Enemy] {gameObject.name} (enemyId: {enemyId}) mati!");
        onDeath?.Invoke();
        OnDeath?.Invoke();

        if (Dreamrift.QuestSystem.QuestManager.Instance != null)
        {
            Dreamrift.QuestSystem.QuestManager.Instance.RecordEnemyKill(enemyId);
        }

        // Kembalikan health bar ke pool
        if (usePooledHealthBar && EnemyHealthBarPool.Instance != null)
            EnemyHealthBarPool.Instance.HideHealthBar(transform);

        // Spawn VFX kematian jika ada
        if (deathVFXPrefab != null)
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);

        // Drop loot jika ada
        if (lootPrefab != null)
            Instantiate(lootPrefab, transform.position + lootOffset, Quaternion.identity);

        // Nonaktifkan komponen AI agar musuh berhenti bergerak
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null) aiController.enabled = false;

        // Note: New AI scripts should also subscribe to OnDeath and self-disable.

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;

        // Hancurkan gameobject setelah delay
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Nonaktifkan collider agar tidak bisa kena serangan lagi
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        // Set warna merah
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        // Kembalikan ke warna asli
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }

        flashCoroutine = null;
    }

    private void UpdateHealthBar()
    {
        // Health bar diurus oleh EnemyHealthBarPool, tidak perlu update manual
    }
}
