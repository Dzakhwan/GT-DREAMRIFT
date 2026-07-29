using UnityEngine;

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

    [Header("Frames / Gambar")]
    [Tooltip("Gambar-gambar cutscene, urut dari awal sampai akhir")]
    public Sprite[] frames;

    [Header("Timing")]
    [Tooltip("Durasi tiap gambar tampil (detik). Hanya berlaku jika manualAdvance = false")]
    public float timePerFrame = 3f;

    [Tooltip("Jika true, player harus klik tombol Next untuk lanjut ke gambar berikutnya")]
    public bool manualAdvance = false;

    [Header("Audio (Opsional)")]
    [Tooltip("Musik background selama cutscene berlangsung")]
    public AudioClip bgmClip;

    [Range(0f, 1f)]
    public float bgmVolume = 0.7f;

    [Header("Post-Cutscene")]
    [Tooltip("Jika diisi, akan load scene ini setelah cutscene selesai. Kosongkan jika hanya kembali ke game.")]
    public string loadSceneAfter = "";

    [Tooltip("Jika true, game di-pause (Time.timeScale=0) selama cutscene berlangsung")]
    public bool pauseGameDuringCutscene = true;
}
