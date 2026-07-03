using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pasang script ini ke prefab Health Bar UI (1 Slider di dalam Canvas Screen Space Overlay).
/// Script ini mengurus tampilan dan posisi health bar di layar mengikuti enemy.
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider slider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Offset posisi bar di atas kepala enemy (dalam unit dunia)")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    // Enemy yang sedang di-track oleh health bar ini
    private Transform trackedEnemy;
    private Camera mainCam;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        mainCam = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void LateUpdate()
    {
        if (trackedEnemy == null)
        {
            Hide();
            return;
        }

        UpdateScreenPosition();
    }

    // ===================== PUBLIC METHODS =====================

    /// <summary>
    /// Kaitkan health bar ini ke enemy tertentu.
    /// Dipanggil oleh EnemyHealthBarPool saat enemy terkena damage.
    /// </summary>
    public void Attach(Transform enemy, int currentHP, int maxHP)
    {
        trackedEnemy = enemy;
        gameObject.SetActive(true);
        UpdateBar(currentHP, maxHP);
    }

    /// <summary>
    /// Update nilai health bar. Dipanggil dari EnemyHealth saat HP berubah.
    /// </summary>
    public void UpdateBar(int currentHP, int maxHP)
    {
        if (slider == null) return;
        slider.maxValue = maxHP;
        slider.value = currentHP;
    }

    /// <summary>
    /// Lepaskan health bar dari enemy (kembalikan ke pool).
    /// </summary>
    public void Detach()
    {
        trackedEnemy = null;
        Hide();
    }

    public Transform TrackedEnemy => trackedEnemy;

    // ===================== PRIVATE METHODS =====================

    private void UpdateScreenPosition()
    {
        // Konversi posisi dunia enemy ke posisi layar
        Vector3 worldPos = trackedEnemy.position + worldOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        // Sembunyikan jika enemy ada di belakang kamera
        if (screenPos.z < 0f)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = 1f;

        // Konversi screen position ke local position di dalam Canvas
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            screenPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCam,
            out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
