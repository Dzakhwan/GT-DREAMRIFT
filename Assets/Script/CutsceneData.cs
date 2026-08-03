using UnityEngine;
using UnityEngine.Video;

public enum CutsceneType
{
    ImageSequence,
    Video
}

public enum CutsceneTriggerType
{
    Interact,
    SceneStart,
    ZoneEnter,
    BossDefeat
}

/// <summary>
/// ScriptableObject yang menyimpan data satu cutscene.
/// Cara membuat: klik kanan di Project Window → Create → DreamRift → Cutscene Data
/// </summary>
[CreateAssetMenu(fileName = "NewCutsceneData", menuName = "DreamRift/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    [Header("Cutscene Identity")]
    [Tooltip("Judul cutscene (opsional, tampil di UI)")]
    public string cutsceneTitle = "";

    [Header("Media Settings")]
    [Tooltip("Tipe media cutscene: Image Sequence (kumpulan gambar) atau Video")]
    public CutsceneType cutsceneType = CutsceneType.ImageSequence;

    [Tooltip("File video untuk cutscene (hanya berlaku jika cutsceneType = Video)")]
    public VideoClip videoClip;

    [Header("Frames / Gambar")]
    [Tooltip("Gambar-gambar cutscene, urut dari awal sampai akhir")]
    public Sprite[] frames;

    [Header("Timing")]
    [Tooltip("Durasi default tiap gambar tampil (detik). Hanya berlaku jika manualAdvance = false")]
    public float timePerFrame = 3f;

    [Tooltip("Durasi kustom tiap frame (detik). Jika bernilai 0/kosong pada indeks tertentu, akan memakai timePerFrame default.")]
    public float[] frameDurations;

    [Tooltip("Jika true, player harus klik tombol Next untuk lanjut ke gambar berikutnya")]
    public bool manualAdvance = false;

    [Header("Audio (Opsional)")]
    [Tooltip("Musik background selama cutscene berlangsung")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    public float bgmVolume = 0.7f;

    [Tooltip("Efek suara (SFX) per-frame. Diselaraskan dengan urutan frame gambar.")]
    public AudioClip[] frameSfx;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Skip & Post-Cutscene Rules")]
    [Tooltip("Jika true, tombol Skip/Lewati akan aktif sehingga player dapat melewati cutscene")]
    public bool allowSkip = true;

    [Tooltip("Nama Scene selanjutnya setelah cutscene selesai. Biarkan KOSONG (\"\") jika hanya ingin kembali ke gameplay di scene saat ini.")]
    public string loadSceneAfter = "";

    [Tooltip("Jika true, game di-pause (Time.timeScale=0) selama cutscene berlangsung")]
    public bool pauseGameDuringCutscene = true;

    [Header("Default Trigger Preset")]
    [Tooltip("Petunjuk preset pemicu default untuk Editor Tool")]
    public CutsceneTriggerType defaultTriggerType = CutsceneTriggerType.Interact;

    /// <summary>
    /// Mengambil durasi spesifik untuk frame pada index tertentu (dalam detik).
    /// </summary>
    public float GetFrameDuration(int index)
    {
        if (frameDurations != null && index >= 0 && index < frameDurations.Length && frameDurations[index] > 0f)
        {
            return frameDurations[index];
        }
        return timePerFrame > 0f ? timePerFrame : 3f;
    }

    /// <summary>
    /// Mengambil SFX AudioClip untuk frame pada index tertentu.
    /// </summary>
    public AudioClip GetFrameSFX(int index)
    {
        if (frameSfx != null && index >= 0 && index < frameSfx.Length)
        {
            return frameSfx[index];
        }
        return null;
    }
}

