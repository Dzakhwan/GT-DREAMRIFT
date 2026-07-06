using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Singleton Manager untuk cutscene overlay in-game.
/// Pasang script ini ke satu GameObject di scene (misalnya "CutsceneCanvas").
/// Canvas/Panel cutscene akan overlay di atas gameplay tanpa ganti scene.
/// 
/// Setup di Hierarchy:
///   [CutsceneCanvas] ← pasang script ini
///     ├── [CutscenePanel]     ← panel background (Image hitam)
///     │     ├── [FrameImage]  ← Image untuk menampilkan gambar cutscene
///     │     ├── [TitleText]   ← TextMeshPro untuk judul (opsional)
///     │     ├── [NextButton]  ← Tombol "Lanjut" (mode manual)
///     │     └── [SkipButton]  ← Tombol "Skip"
/// </summary>
public class InGameCutsceneManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static InGameCutsceneManager Instance { get; private set; }

    // ── Inspector References ───────────────────────────────────────────────
    [Header("UI References")]
    [Tooltip("Panel utama yang membungkus seluruh UI cutscene")]
    [SerializeField] private GameObject cutscenePanel;

    [Tooltip("Image komponen untuk menampilkan frame gambar cutscene")]
    [SerializeField] private Image frameImage;

    [Tooltip("(Opsional) TextMeshPro untuk judul cutscene")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Tombol 'Lanjut' — hanya aktif saat manualAdvance = true")]
    [SerializeField] private Button nextButton;

    [Tooltip("Tombol 'Lewati' untuk skip seluruh cutscene")]
    [SerializeField] private Button skipButton;

    [Header("Transition")]
    [Tooltip("Durasi fade in/out gambar (detik)")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Audio")]
    [Tooltip("AudioSource untuk memutar BGM cutscene")]
    [SerializeField] private AudioSource audioSource;

    // ── Events ─────────────────────────────────────────────────────────────
    [Header("Events")]
    [Tooltip("Dipanggil saat cutscene selesai (sebelum load scene / kembali ke game)")]
    public UnityEvent onCutsceneFinished;

    // ── Private State ──────────────────────────────────────────────────────
    private CutsceneData currentData;
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private bool waitingForNext = false;   // digunakan pada mode manual
    private Coroutine playRoutine;

    // ══════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        // Setup Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pastikan panel tersembunyi di awal
        if (cutscenePanel != null)
            cutscenePanel.SetActive(false);
    }

    private void Start()
    {
        // Daftarkan listener tombol
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextPressed);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipPressed);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mulai memutar cutscene berdasarkan CutsceneData yang diberikan.
    /// Panggil dari CutsceneTrigger.OnInteract() atau event apapun.
    /// </summary>
    public void PlayCutscene(CutsceneData data)
    {
        if (isPlaying || data == null) return;
        if (data.frames == null || data.frames.Length == 0)
        {
            Debug.LogWarning("InGameCutsceneManager: CutsceneData tidak punya frame gambar!");
            return;
        }

        currentData = data;
        currentFrameIndex = 0;
        isPlaying = true;

        // Pause game jika diminta
        if (currentData.pauseGameDuringCutscene)
            Time.timeScale = 0f;

        // Tampilkan panel
        cutscenePanel?.SetActive(true);

        // Set judul
        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(data.cutsceneTitle) ? "" : data.cutsceneTitle;

        // Tampilkan/sembunyikan tombol Next sesuai mode
        if (nextButton != null)
            nextButton.gameObject.SetActive(data.manualAdvance);

        // Putar BGM
        PlayBGM(data);

        // Mulai coroutine
        playRoutine = StartCoroutine(PlaySequence());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Coroutine Utama
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlaySequence()
    {
        // Tampilkan frame pertama
        yield return StartCoroutine(ShowFrame(currentData.frames[currentFrameIndex]));

        while (currentFrameIndex < currentData.frames.Length)
        {
            if (currentData.manualAdvance)
            {
                // Tunggu player klik Next
                waitingForNext = true;
                yield return new WaitUntil(() => !waitingForNext);
            }
            else
            {
                // Tunggu durasi frame (pakai WaitForSecondsRealtime agar tidak terpengaruh timeScale=0)
                yield return new WaitForSecondsRealtime(currentData.timePerFrame);
            }

            currentFrameIndex++;

            if (currentFrameIndex < currentData.frames.Length)
            {
                // Fade ke frame berikutnya
                yield return StartCoroutine(CrossfadeToFrame(currentData.frames[currentFrameIndex]));
            }
        }

        // Semua frame selesai
        yield return StartCoroutine(FadeOut());
        FinishCutscene();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Button Callbacks
    // ══════════════════════════════════════════════════════════════════════

    private void OnNextPressed()
    {
        // Sinyal bahwa player sudah klik Next
        waitingForNext = false;
    }

    private void OnSkipPressed()
    {
        if (!isPlaying) return;

        // Hentikan coroutine yang sedang berjalan
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        waitingForNext = false;
        StartCoroutine(SkipSequence());
    }

    private IEnumerator SkipSequence()
    {
        yield return StartCoroutine(FadeOut());
        FinishCutscene();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helper: Frame Display & Fading
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Langsung tampilkan frame tanpa transisi (untuk frame pertama).
    /// </summary>
    private IEnumerator ShowFrame(Sprite sprite)
    {
        if (frameImage == null) yield break;

        frameImage.sprite = sprite;
        yield return StartCoroutine(FadeImage(frameImage, 0f, 1f));
    }

    /// <summary>
    /// Fade out frame saat ini, ganti sprite, fade in frame baru.
    /// </summary>
    private IEnumerator CrossfadeToFrame(Sprite nextSprite)
    {
        if (frameImage == null) yield break;

        yield return StartCoroutine(FadeImage(frameImage, 1f, 0f));
        frameImage.sprite = nextSprite;
        yield return StartCoroutine(FadeImage(frameImage, 0f, 1f));
    }

    /// <summary>
    /// Fade out seluruh frame (akhir cutscene).
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (frameImage == null) yield break;
        yield return StartCoroutine(FadeImage(frameImage, 1f, 0f));
    }

    /// <summary>
    /// Animasi alpha dari startAlpha ke endAlpha pada Image target.
    /// Menggunakan WaitForSecondsRealtime agar berjalan meski timeScale = 0.
    /// </summary>
    private IEnumerator FadeImage(Image target, float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color c = target.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            target.color = c;
            yield return null;
        }

        c.a = endAlpha;
        target.color = c;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helper: Audio
    // ══════════════════════════════════════════════════════════════════════

    private void PlayBGM(CutsceneData data)
    {
        if (audioSource == null || data.bgmClip == null) return;

        audioSource.clip = data.bgmClip;
        audioSource.volume = data.bgmVolume;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopBGM()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Cleanup
    // ══════════════════════════════════════════════════════════════════════

    private void FinishCutscene()
    {
        isPlaying = false;
        cutscenePanel?.SetActive(false);
        StopBGM();

        // Kembalikan timeScale
        if (currentData != null && currentData.pauseGameDuringCutscene)
            Time.timeScale = 1f;

        // Jalankan event callback
        onCutsceneFinished?.Invoke();

        // Load scene jika dikonfigurasi
        if (currentData != null && !string.IsNullOrEmpty(currentData.loadSceneAfter))
        {
            Time.timeScale = 1f; // pastikan normal sebelum load
            SceneManager.LoadScene(currentData.loadSceneAfter);
        }

        currentData = null;
    }
}
