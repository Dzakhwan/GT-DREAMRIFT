using System.Collections;
using UnityEngine;

/// <summary>
/// Dummy Enemy Target untuk Tahap 3 Tutorial (Melee Combat).
/// Mengimplementasikan IDamageable agar langsung terintegrasi dengan sistem serangan PlayerFight.cs yang sudah ada.
/// Gunakan Capsule/Cylinder 3D Primitive sebagai placeholder.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DummyTarget : MonoBehaviour, IDamageable
{
    [Header("Dummy Combat Settings")]
    [Tooltip("Jumlah pukulan (hit) yang dibutuhkan untuk menghancurkan Dummy (Set 1 untuk 1x hit, atau 3 untuk full combo 3x)")]
    [SerializeField, Min(1)] private int hitsToDestroy = 3;

    [Tooltip("ID Musuh untuk pencatatan otomatis ke QuestManager (DefeatEnemy)")]
    [SerializeField] private string dummyEnemyId = "Dummy";

    [Header("Visual Feedback")]
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.12f;

    private int currentHits = 0;
    private bool isDestroyed = false;
    private Renderer cachedRenderer;
    private Color originalColor = Color.white;
    private Vector3 initialPosition;

    private void Awake()
    {
        cachedRenderer = GetComponentInChildren<Renderer>();
        if (cachedRenderer != null && cachedRenderer.material != null && cachedRenderer.material.HasProperty("_BaseColor"))
        {
            originalColor = cachedRenderer.material.GetColor("_BaseColor");
        }
        else if (cachedRenderer != null && cachedRenderer.material != null && cachedRenderer.material.HasProperty("_Color"))
        {
            originalColor = cachedRenderer.material.color;
        }

        // Pastikan Collider bukan trigger agar terdeteksi Physics.OverlapSphere milik PlayerFight
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    private void OnEnable()
    {
        currentHits = 0;
        isDestroyed = false;
        initialPosition = transform.localPosition;
    }

    /// <summary>
    /// Dipanggil otomatis oleh PlayerFight.DealDamage() saat serangan melee Player mengenai Dummy.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHits++;
        Debug.Log($"[DummyTarget] Terkena serangan Melee! Hit: {currentHits}/{hitsToDestroy} (Damage diterima: {damage})");

        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(HitFeedbackRoutine());
        }

        if (currentHits >= hitsToDestroy)
        {
            DestroyDummy();
        }
    }

    private IEnumerator HitFeedbackRoutine()
    {
        if (cachedRenderer != null)
        {
            if (cachedRenderer.material.HasProperty("_BaseColor"))
                cachedRenderer.material.SetColor("_BaseColor", hitFlashColor);
            else if (cachedRenderer.material.HasProperty("_Color"))
                cachedRenderer.material.color = hitFlashColor;
        }

        // Efek getar (shake) singkat saat dipukul
        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = initialPosition + Random.insideUnitSphere * 0.08f;
            yield return null;
        }

        transform.localPosition = initialPosition;

        if (cachedRenderer != null)
        {
            if (cachedRenderer.material.HasProperty("_BaseColor"))
                cachedRenderer.material.SetColor("_BaseColor", originalColor);
            else if (cachedRenderer.material.HasProperty("_Color"))
                cachedRenderer.material.color = originalColor;
        }
    }

    private void DestroyDummy()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log("[DummyTarget] Dummy hancur! Tahap 3 (Melee Combat) selesai.");

        if (Dreamrift.QuestSystem.QuestManager.Instance != null)
        {
            Dreamrift.QuestSystem.QuestManager.Instance.RecordEnemyKill(dummyEnemyId);
        }

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnDummyDestroyed();
        }

        gameObject.SetActive(false);
    }
}

