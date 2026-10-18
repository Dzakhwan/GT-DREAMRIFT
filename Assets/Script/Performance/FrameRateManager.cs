using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Mengatur target frame rate dan VSync agar gameplay berjalan mulus dan stabil (bebas micro-stutter).
/// Otomatis aktif saat game mulai berjalan.
/// </summary>
public class FrameRateManager : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    [Tooltip("Target FPS game (rekomendasi: 60 untuk kestabilan gameplay).")]
    [SerializeField] private int targetFPS = 60;

    [Tooltip("VSync: 0 = Don't Sync (pakai targetFPS), 1 = Sync dengan monitor refresh.")]
    [SerializeField] private int vSyncCount = 0;

    private void Awake()
    {
        ApplyFrameRateSettings(targetFPS, vSyncCount);
    }

    /// <summary>
    /// Otomatis dieksekusi Unity sebelum scene manapun dimuat saat Play Mode.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitFrameRate()
    {
        ApplyFrameRateSettings(60, 0);
    }

    public static void ApplyFrameRateSettings(int fps, int vsync)
    {
        QualitySettings.vSyncCount = vsync;
        Application.targetFrameRate = fps;

        // Nonaktifkan runtime UI URP Debugger agar popup "Display Stats" tidak muncul di game/device simulator
        if (DebugManager.instance != null)
        {
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}
