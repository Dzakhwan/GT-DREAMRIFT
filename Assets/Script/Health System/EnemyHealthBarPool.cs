using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton pool manager untuk semua health bar enemy di layar.
/// Taruh 1 GameObject kosong di scene dengan script ini.
/// Script ini mengelola sejumlah health bar UI yang dibagi-pakai ke semua enemy.
/// </summary>
public class EnemyHealthBarPool : MonoBehaviour
{
    public static EnemyHealthBarPool Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Prefab Health Bar UI (Slider di dalam RectTransform)")]
    [SerializeField] private EnemyHealthBarUI healthBarPrefab;

    [Tooltip("Jumlah maksimum health bar yang bisa tampil sekaligus")]
    [SerializeField] private int poolSize = 10;

    [Header("Canvas Reference")]
    [Tooltip("Drag Canvas (HUD Canvas yang sudah ada, bisa dipakai bareng UI lain)")]
    [SerializeField] private Canvas screenCanvas;

    [Tooltip("(Opsional) Parent khusus di dalam Canvas untuk health bar enemy. Buat Empty di dalam Canvas, rename 'EnemyHealthBarsParent'. Jika kosong, langsung ditaruh di Canvas.")]
    [SerializeField] private RectTransform healthBarsParent;

    // Pool: daftar semua health bar yang tersedia
    private List<EnemyHealthBarUI> pool = new List<EnemyHealthBarUI>();

    // Dictionary: enemy → health bar yang sedang diklaim
    private Dictionary<Transform, EnemyHealthBarUI> activeMap = new Dictionary<Transform, EnemyHealthBarUI>();

    // ===================== UNITY LIFECYCLE =====================

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Pastikan healthBarsParent memiliki Canvas mandiri (Sub-Canvas)
        // agar pergerakan floating health bar tidak memicu re-batch pada Canvas HUD utama!
        if (healthBarsParent != null && !healthBarsParent.TryGetComponent(out Canvas _))
        {
            Canvas subCanvas = healthBarsParent.gameObject.AddComponent<Canvas>();
            subCanvas.overrideSorting = true;
            if (screenCanvas != null)
                subCanvas.sortingOrder = screenCanvas.sortingOrder + 1;
        }

        // Tentukan parent: pakai healthBarsParent jika ada, fallback ke Canvas
        Transform parent = healthBarsParent != null
            ? healthBarsParent
            : (screenCanvas != null ? screenCanvas.transform : transform);

        // Buat health bar sebanyak poolSize di awal
        if (healthBarPrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                EnemyHealthBarUI bar = Instantiate(healthBarPrefab, parent);
                bar.gameObject.SetActive(false);
                pool.Add(bar);
            }
        }
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Tampilkan health bar untuk enemy tertentu.
    /// Dipanggil dari EnemyHealth saat spawn atau saat TakeDamage() dipanggil.
    /// </summary>
    public void ShowHealthBar(Transform enemy, int currentHP, int maxHP, Vector3? customOffset = null)
    {
        if (enemy == null) return;

        // Jika enemy sudah punya health bar aktif, update saja
        if (activeMap.TryGetValue(enemy, out EnemyHealthBarUI existingBar))
        {
            if (existingBar != null)
            {
                existingBar.UpdateBar(currentHP, maxHP);
                return;
            }
            activeMap.Remove(enemy);
        }

        // Cari health bar yang sedang bebas dari pool
        EnemyHealthBarUI freeBar = GetFreeBar();
        if (freeBar == null)
        {
            Debug.LogWarning("[HealthBarPool] Gagal mendapatkan health bar untuk " + enemy.name);
            return;
        }

        // Klaim health bar untuk enemy ini
        freeBar.Attach(enemy, currentHP, maxHP, customOffset);
        activeMap[enemy] = freeBar;
    }

    /// <summary>
    /// Kembalikan health bar enemy ke pool (saat enemy mati atau keluar layar).
    /// Dipanggil dari EnemyHealth saat Die() atau OnDisable dipanggil.
    /// </summary>
    public void HideHealthBar(Transform enemy)
    {
        if (enemy == null) return;

        if (activeMap.TryGetValue(enemy, out EnemyHealthBarUI bar))
        {
            if (bar != null)
                bar.Detach();
            activeMap.Remove(enemy);
        }
    }

    // ===================== PRIVATE METHODS =====================

    private EnemyHealthBarUI GetFreeBar()
    {
        foreach (var bar in pool)
        {
            // Bar bebas = tidak aktif atau tidak sedang tracking enemy
            if (bar != null && (!bar.gameObject.activeSelf || bar.TrackedEnemy == null))
                return bar;
        }

        // Auto-expand pool jika jumlah musuh melebihi poolSize awal
        if (healthBarPrefab != null)
        {
            Transform parent = healthBarsParent != null
                ? healthBarsParent
                : (screenCanvas != null ? screenCanvas.transform : transform);

            EnemyHealthBarUI newBar = Instantiate(healthBarPrefab, parent);
            newBar.gameObject.SetActive(false);
            pool.Add(newBar);
            return newBar;
        }

        return null;
    }
}
