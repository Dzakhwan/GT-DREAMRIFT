using UnityEngine;
using UnityEngine.UI;

// Script ini HANYA mengurus tampilan UI Settings (slider) di Main Menu.
// Logika sesungguhnya (nyimpen & nerapin volume/brightness lintas scene)
// ada di GameSettingsManager.cs, supaya settingnya konsisten walau pindah scene.
public class AudioSettingsController : MonoBehaviour
{
    [Header("Slider References")]
    public Slider volumeSlider;
    public Slider brightnessSlider;

    // Overlay ini cuma efek visual lokal di Main Menu (dekorasi),
    // brightness sesungguhnya (Directional Light) diurus oleh GameSettingsManager
    public CanvasGroup brightnessOverlay;

    void Awake()
    {
        SetupBrightnessOverlay();
    }

    void Start()
    {
        if (GameSettingsManager.Instance == null)
        {
            Debug.LogError("GameSettingsManager belum ada di scene! Pasang script itu di GameObject persistent.");
            return;
        }

        float savedVolume = GameSettingsManager.Instance.GetVolume();
        float savedBrightness = GameSettingsManager.Instance.GetBrightness();

        if (volumeSlider != null)
        {
            volumeSlider.value = Mathf.Lerp(volumeSlider.minValue, volumeSlider.maxValue, savedVolume);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.value = Mathf.Lerp(brightnessSlider.minValue, brightnessSlider.maxValue, savedBrightness);
        }

        UpdateOverlayVisual(savedBrightness);
    }

    // Fungsi ini dipanggil dari OnValueChanged milik Slider Volume
    public void OnVolumeChanged(float value)
    {
        float normalizedValue = volumeSlider != null
            ? Mathf.InverseLerp(volumeSlider.minValue, volumeSlider.maxValue, value)
            : value;

        GameSettingsManager.Instance.SetVolume(normalizedValue);
    }

    // Fungsi ini dipanggil dari OnValueChanged milik Slider Brightness
    public void OnBrightnessChanged(float value)
    {
        float normalizedValue = brightnessSlider != null
            ? Mathf.InverseLerp(brightnessSlider.minValue, brightnessSlider.maxValue, value)
            : value;

        GameSettingsManager.Instance.SetBrightness(normalizedValue);
        UpdateOverlayVisual(normalizedValue);
    }

    // Update overlay hitam lokal di Main Menu (feedback visual instan saat slider digeser)
    private void UpdateOverlayVisual(float normalizedBrightness)
    {
        if (brightnessOverlay != null)
        {
            float floor = GameSettingsManager.Instance.minBrightnessFloor;
            float effectiveValue = Mathf.Lerp(floor, 1f, normalizedBrightness);
            brightnessOverlay.alpha = 1f - effectiveValue;
        }
    }

    // Memaksa GameObject BrightnessOverlay punya konfigurasi yang benar secara otomatis
    private void SetupBrightnessOverlay()
    {
        if (brightnessOverlay == null)
        {
            Debug.LogWarning("BrightnessOverlay belum di-drag ke slot AudioSettingsController!");
            return;
        }

        GameObject overlayObject = brightnessOverlay.gameObject;
        overlayObject.SetActive(true);

        brightnessOverlay.interactable = false;
        brightnessOverlay.blocksRaycasts = false;

        Image overlayImage = overlayObject.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = overlayObject.AddComponent<Image>();
        }
        overlayImage.sprite = null;
        overlayImage.type = Image.Type.Simple;
        overlayImage.color = Color.black;

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        overlayObject.transform.SetAsLastSibling();
    }
}