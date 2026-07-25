using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettingsController : MonoBehaviour
{
    [Header("Slider References")]
    // Slot untuk Drag & Drop Slider Volume (Tarik GameObject 'Volume' ke sini di Inspector)
    public Slider volumeSlider;

    // Slot untuk Drag & Drop Slider Brightness (Tarik GameObject 'Brightness slider' ke sini di Inspector)
    public Slider brightnessSlider;

    [Header("Audio Source (Slot untuk asset suara nanti)")]
    // OPSI 1: kalau nanti pakai AudioMixer, drag AudioMixer asset di sini
    public AudioMixer audioMixer;
    // Nama parameter yang sudah di-expose di AudioMixer (klik kanan parameter di Mixer > Expose)
    public string exposedVolumeParam = "MasterVolume";

    // OPSI 2: kalau belum pakai AudioMixer, cukup drag AudioSource musik/BGM di sini
    public AudioSource musicSource;

    [Header("Brightness Control")]
    // Drag Directional Light dari scene ke sini untuk kontrol brightness dunia game secara nyata
    public Light directionalLight;
    public float minLightIntensity = 0.1f;
    public float maxLightIntensity = 2f;

    // Drag GameObject 'BrightnessOverlay' ke sini (cukup drag GameObject-nya, tidak perlu setting apapun manual)
    public CanvasGroup brightnessOverlay;

    private const string VOLUME_KEY = "SettingVolume";
    private const string BRIGHTNESS_KEY = "SettingBrightness";

    void Awake()
    {
        // Paksa setup BrightnessOverlay otomatis, supaya tidak tergantung setting manual di Inspector
        SetupBrightnessOverlay();
    }

    void Start()
    {
        // ============================================================
        // PEMBERSIH SEKALI PAKAI:
        // Kalau slider/overlay nyangkut di value lama (misal full hitam terus),
        // uncomment baris di bawah ini, Play SEKALI, lalu Stop, lalu
        // comment lagi baris ini sebelum lanjut development seterusnya.
        // ============================================================
        // PlayerPrefs.DeleteAll();

        // Load nilai yang sudah disimpan sebelumnya (default 0.75 kalau belum pernah diset)
        float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 0.75f);
        float savedBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.75f);

        if (volumeSlider != null)
        {
            // Set posisi slider berdasarkan rentang Min/Max Value slider itu sendiri
            volumeSlider.value = Mathf.Lerp(volumeSlider.minValue, volumeSlider.maxValue, savedVolume);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.value = Mathf.Lerp(brightnessSlider.minValue, brightnessSlider.maxValue, savedBrightness);
        }

        // Terapkan langsung nilai awal saat menu dibuka
        ApplyVolume(savedVolume);
        ApplyBrightness(savedBrightness);
    }

    // Fungsi ini dipanggil dari OnValueChanged milik Slider Volume
    public void OnVolumeChanged(float value)
    {
        // Normalisasi value ke rentang 0-1, apapun Min/Max Value yang di-set di Inspector Slider
        float normalizedValue = volumeSlider != null
            ? Mathf.InverseLerp(volumeSlider.minValue, volumeSlider.maxValue, value)
            : value;

        ApplyVolume(normalizedValue);
        PlayerPrefs.SetFloat(VOLUME_KEY, normalizedValue);
    }

    // Fungsi ini dipanggil dari OnValueChanged milik Slider Brightness
    public void OnBrightnessChanged(float value)
    {
        // Normalisasi value ke rentang 0-1, apapun Min/Max Value yang di-set di Inspector Slider
        float normalizedValue = brightnessSlider != null
            ? Mathf.InverseLerp(brightnessSlider.minValue, brightnessSlider.maxValue, value)
            : value;

        ApplyBrightness(normalizedValue);
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, normalizedValue);
    }

    private void ApplyVolume(float value)
    {
        if (audioMixer != null)
        {
            // Volume dalam AudioMixer pakai skala desibel (dB), bukan linear 0-1
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(exposedVolumeParam, dB);
        }
        else if (musicSource != null)
        {
            musicSource.volume = value;
        }
        else
        {
            // Fallback kalau belum ada AudioMixer atau AudioSource yang di-assign
            AudioListener.volume = value;
        }
    }

    private void ApplyBrightness(float value)
    {
        // Kontrol nyata: intensity Directional Light (mempengaruhi seluruh scene 3D)
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, value);
        }

        // Overlay hitam untuk efek gelap di Main Menu (Canvas UI tidak terpengaruh Directional Light)
        if (brightnessOverlay != null)
        {
            // value tinggi = terang = overlay hampir transparan (alpha kecil)
            // value rendah = gelap = overlay hampir solid hitam (alpha besar)
            brightnessOverlay.alpha = 1f - value;
        }
    }

    // Memaksa GameObject BrightnessOverlay punya konfigurasi yang benar secara otomatis,
    // supaya tidak perlu lagi klak-klik manual di Inspector
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