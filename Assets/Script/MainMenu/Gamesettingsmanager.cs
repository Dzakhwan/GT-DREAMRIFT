using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Taruh script ini di sebuah GameObject kosong (misal nama "GameSettingsManager")
// di scene Main Menu. GameObject ini akan otomatis bertahan (tidak hancur)
// setiap kali pindah scene, jadi setting volume & brightness selalu konsisten.
public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    [Header("Audio Source (Slot untuk asset suara nanti)")]
    public AudioMixer audioMixer;
    public string exposedVolumeParam = "MasterVolume";
    public AudioSource musicSource;

    [Header("Brightness Control")]
    public float minLightIntensity = 0.1f;
    public float maxLightIntensity = 2f;

    [Header("Brightness Limit")]
    // Batas bawah brightness supaya tidak pernah full hitam (0 = full gelap, 1 = normal)
    [Range(0f, 1f)]
    public float minBrightnessFloor = 0.2f;

    private const string VOLUME_KEY = "SettingVolume";
    private const string BRIGHTNESS_KEY = "SettingBrightness";

    private float currentVolume;
    private float currentBrightness;

    void Awake()
    {
        // Pola Singleton: kalau sudah ada instance lain (misal karena balik lagi ke scene Main Menu),
        // hancurkan yang baru ini supaya tidak dobel
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Load nilai tersimpan (default volume 0.75, brightness 0.5)
        currentVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 0.75f);
        currentBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);

        ApplyVolume(currentVolume);
        ApplyBrightnessToCurrentScene();
    }

    // Dipanggil otomatis oleh Unity setiap kali scene baru selesai dimuat
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Setiap pindah scene, cari Directional Light di scene itu dan terapkan brightness tersimpan
        ApplyBrightnessToCurrentScene();
    }

    // ===================== VOLUME =====================

    public float GetVolume()
    {
        return currentVolume;
    }

    public void SetVolume(float normalizedValue)
    {
        currentVolume = Mathf.Clamp01(normalizedValue);
        ApplyVolume(currentVolume);
        PlayerPrefs.SetFloat(VOLUME_KEY, currentVolume);
    }

    private void ApplyVolume(float value)
    {
        if (audioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
            audioMixer.SetFloat(exposedVolumeParam, dB);
        }
        else if (musicSource != null)
        {
            musicSource.volume = value;
        }
        else
        {
            AudioListener.volume = value;
        }
    }

    // ===================== BRIGHTNESS =====================

    public float GetBrightness()
    {
        return currentBrightness;
    }

    public void SetBrightness(float normalizedValue)
    {
        currentBrightness = Mathf.Clamp01(normalizedValue);
        ApplyBrightnessToCurrentScene();
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, currentBrightness);
    }

    private void ApplyBrightnessToCurrentScene()
    {
        // Terapkan floor supaya brightness minimal tidak pernah sampai 0% (full hitam)
        float effectiveValue = Mathf.Lerp(minBrightnessFloor, 1f, currentBrightness);

        // Cari SEMUA Directional Light yang ada di scene yang lagi aktif sekarang
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in allLights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, effectiveValue);
            }
        }
    }
}