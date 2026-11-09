using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Pengendali visual tombol UI Mage Projectile pada HUD:
/// - Radial360 cooldown sweep overlay gelap semi-transparan
/// - Hitung mundur detik (mis. "1.5") saat cooldown aktif
/// - Animasi denyutan skala singkat (Ready Pulse 1.14x -> 1.0x) saat skill siap kembali
/// </summary>
public class MageSkillButtonUI : MonoBehaviour, IPointerDownHandler
{
    [Header("References")]
    [Tooltip("Komponen PlayerProjectileAttack pada PlayerArmature (otomatis dicari jika kosong)")]
    [SerializeField] private PlayerProjectileAttack projectileAttack;

    [Tooltip("Image ikon bola komet sihir di tengah tombol")]
    [SerializeField] private Image iconImage;

    [Tooltip("Image Radial360 gelap untuk efek sapuan waktu cooldown")]
    [SerializeField] private Image cooldownOverlayImage;

    [Tooltip("Teks angka hitung mundur detik cooldown")]
    [SerializeField] private TMP_Text cooldownText;

    [Tooltip("RectTransform tombol yang berdenyut saat cooldown selesai")]
    [SerializeField] private RectTransform pulseTarget;

    [Header("Visual Style")]
    [SerializeField] private Color readyIconColor = new Color(1.00f, 1.00f, 1.00f, 0.86f);
    [SerializeField] private Color cooldownIconColor = new Color(1.00f, 1.00f, 1.00f, 0.35f);
    [SerializeField] private float readyPulseScaleMultiplier = 1.14f;
    [SerializeField] private float readyPulseDuration = 0.22f;

    private Vector3 baseScale = Vector3.one;
    private bool baseScaleCaptured = false;
    private bool wasOnCooldown = false;
    private float pulseTimer = 0f;
    private float nextAutoFindTime = 0f;

    private void Awake()
    {
        AutoBindChildElements();
        CaptureBaseScale();
    }

    private void Start()
    {
        ResolvePlayerReference();
        RefreshVisualImmediate();
    }

    private void AutoBindChildElements()
    {
        if (pulseTarget == null)
            pulseTarget = transform as RectTransform;

        if (iconImage == null)
        {
            Transform iconTr = transform.Find("Image_Icon");
            if (iconTr != null) iconImage = iconTr.GetComponent<Image>();
        }

        if (cooldownOverlayImage == null)
        {
            Transform cdTr = transform.Find("Cooldown_Overlay");
            if (cdTr != null) cooldownOverlayImage = cdTr.GetComponent<Image>();
        }

        if (cooldownText == null)
        {
            Transform txtTr = transform.Find("Cooldown_Text");
            if (txtTr != null) cooldownText = txtTr.GetComponent<TMP_Text>();
        }
    }

    private void CaptureBaseScale()
    {
        if (baseScaleCaptured || pulseTarget == null) return;
        baseScale = pulseTarget.localScale;
        if (baseScale.sqrMagnitude < 0.0001f)
            baseScale = new Vector3(0.6f, 0.6f, 1f);
        baseScaleCaptured = true;
    }

    private void ResolvePlayerReference()
    {
        if (projectileAttack != null) return;
        projectileAttack = FindFirstObjectByType<PlayerProjectileAttack>();
    }

    private void Update()
    {
        if (projectileAttack == null && Time.unscaledTime >= nextAutoFindTime)
        {
            nextAutoFindTime = Time.unscaledTime + 0.5f;
            ResolvePlayerReference();
        }

        bool onCooldown = projectileAttack != null && projectileAttack.IsOnCooldown;

        if (onCooldown)
        {
            ApplyCooldownState(projectileAttack.CooldownNormalized, projectileAttack.CooldownRemaining);
            wasOnCooldown = true;
        }
        else
        {
            if (wasOnCooldown)
            {
                wasOnCooldown = false;
                pulseTimer = readyPulseDuration;
            }

            ApplyReadyState();
        }

        UpdateReadyPulse();
    }

    private void ApplyCooldownState(float normalizedRemaining, float secondsRemaining)
    {
        if (cooldownOverlayImage != null)
        {
            if (!cooldownOverlayImage.enabled) cooldownOverlayImage.enabled = true;
            cooldownOverlayImage.fillAmount = Mathf.Clamp01(normalizedRemaining);
        }

        if (cooldownText != null)
        {
            if (!cooldownText.enabled) cooldownText.enabled = true;
            cooldownText.text = secondsRemaining >= 0.05f ? secondsRemaining.ToString("0.0") : string.Empty;
        }

        if (iconImage != null)
        {
            iconImage.color = cooldownIconColor;
        }
    }

    private void ApplyReadyState()
    {
        if (cooldownOverlayImage != null)
        {
            cooldownOverlayImage.fillAmount = 0f;
            if (cooldownOverlayImage.enabled) cooldownOverlayImage.enabled = false;
        }

        if (cooldownText != null)
        {
            if (!string.IsNullOrEmpty(cooldownText.text)) cooldownText.text = string.Empty;
            if (cooldownText.enabled) cooldownText.enabled = false;
        }

        if (iconImage != null)
        {
            iconImage.color = readyIconColor;
        }
    }

    private void UpdateReadyPulse()
    {
        if (pulseTarget == null) return;
        CaptureBaseScale();

        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.unscaledDeltaTime;
            if (pulseTimer <= 0f)
            {
                pulseTimer = 0f;
                pulseTarget.localScale = baseScale;
            }
            else
            {
                float progress = 1f - Mathf.Clamp01(pulseTimer / Mathf.Max(0.01f, readyPulseDuration));
                float factor = 1f + (readyPulseScaleMultiplier - 1f) * Mathf.Sin(progress * Mathf.PI);
                pulseTarget.localScale = baseScale * factor;
            }
        }
    }

    private void RefreshVisualImmediate()
    {
        if (projectileAttack != null && projectileAttack.IsOnCooldown)
        {
            ApplyCooldownState(projectileAttack.CooldownNormalized, projectileAttack.CooldownRemaining);
            wasOnCooldown = true;
        }
        else
        {
            ApplyReadyState();
            wasOnCooldown = false;
        }
    }

    /// <summary>
    /// Fallback langsung saat tombol disentuh/diklik agar selalu responsif di semua scene.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        ResolvePlayerReference();
        if (projectileAttack != null)
        {
            projectileAttack.TryCastProjectile();
        }
    }

    /// <summary>
    /// Utilitas preview status cooldown untuk verifikasi tampilan UI di Editor.
    /// </summary>
    public void SetEditorCooldownPreview(float normalizedRemaining, float secondsRemaining)
    {
        AutoBindChildElements();
        CaptureBaseScale();
        if (normalizedRemaining > 0.001f)
            ApplyCooldownState(normalizedRemaining, secondsRemaining);
        else
            ApplyReadyState();
    }
}

