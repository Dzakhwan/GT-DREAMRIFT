using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Singleton Manager untuk cutscene overlay in-game.
/// Pasang script ini ke satu GameObject di scene (misalnya "CutsceneCanvas").
/// Canvas/Panel cutscene akan overlay di atas gameplay tanpa ganti scene.
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

    [Tooltip("RawImage komponen untuk menampilkan video cutscene (opsional, mode Video)")]
    [SerializeField] private RawImage videoRawImage;

    [Tooltip("VideoPlayer untuk memutar video cutscene (opsional, mode Video)")]
    [SerializeField] private VideoPlayer videoPlayer;

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

    [Tooltip("AudioSource khusus untuk memutar SFX cutscene (opsional, fallback ke audioSource utama)")]
    [SerializeField] private AudioSource sfxAudioSource;

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

    private void Update()
    {
        if (!isPlaying || !waitingForNext) return;

        // Deteksi klik mouse kiri / sentuhan layar / tombol Space & Enter
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            // Abaikan jika klik berada tepat di atas tombol Skip
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                skipButton != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == skipButton.gameObject)
            {
                return;
            }

            OnNextPressed();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mulai memutar cutscene berdasarkan CutsceneData yang diberikan.
    /// Panggil dari CutsceneTriggerHandler.OnInteract() atau event apapun.
    /// </summary>
    public void PlayCutscene(CutsceneData data)
    {
        if (isPlaying || data == null) return;

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

        // Sembunyikan tombol Next jika ada, karena layar bisa diklik langsung untuk next
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (skipButton != null)
            skipButton.gameObject.SetActive(data.allowSkip);

        // Putar BGM
        PlayBGM(data);

        // Cek tipe media
        if (data.cutsceneType == CutsceneType.Video)
        {
            playRoutine = StartCoroutine(PlayVideoSequence());
        }
        else
        {
            if (data.frames == null || data.frames.Length == 0)
            {
                Debug.LogWarning("InGameCutsceneManager: CutsceneData tidak punya frame gambar!");
                FinishCutscene();
                return;
            }
            playRoutine = StartCoroutine(PlayImageSequence());
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Coroutine Utama
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator PlayImageSequence()
    {
        if (videoRawImage != null) videoRawImage.gameObject.SetActive(false);
        if (frameImage != null) frameImage.gameObject.SetActive(true);

        // Tampilkan frame pertama
        yield return StartCoroutine(ShowFrame(currentData.frames[currentFrameIndex]));
        PlayFrameSFX(currentFrameIndex);

        while (currentFrameIndex < currentData.frames.Length)
        {
            waitingForNext = true;

            if (currentData.manualAdvance)
            {
                // Tunggu player klik layar / Next
                yield return new WaitUntil(() => !waitingForNext);
            }
            else
            {
                // Tunggu durasi frame spesifik atau sampai player klik layar
                float frameDuration = currentData.GetFrameDuration(currentFrameIndex);
                float timer = 0f;

                while (timer < frameDuration && waitingForNext)
                {
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            currentFrameIndex++;

            if (currentFrameIndex < currentData.frames.Length)
            {
                // Fade ke frame berikutnya & putar SFX frame baru
                yield return StartCoroutine(CrossfadeToFrame(currentData.frames[currentFrameIndex]));
                PlayFrameSFX(currentFrameIndex);
            }
        }

        // Semua frame selesai
        yield return StartCoroutine(FadeOutFrameImage());
        FinishCutscene();
    }

    private IEnumerator PlayVideoSequence()
    {
        if (frameImage != null) frameImage.gameObject.SetActive(false);
        if (videoRawImage != null) videoRawImage.gameObject.SetActive(true);

        if (videoPlayer == null || currentData.videoClip == null)
        {
            Debug.LogWarning("InGameCutsceneManager: VideoPlayer atau VideoClip belum di-assign!");
            yield return new WaitForSecondsRealtime(1f);
            FinishCutscene();
            yield break;
        }

        videoPlayer.clip = currentData.videoClip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        // Tunggu video selesai diputar
        while (videoPlayer.isPlaying || videoPlayer.time < videoPlayer.length - 0.1f)
        {
            yield return null;
        }

        videoPlayer.Stop();
        FinishCutscene();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Button Callbacks
    // ══════════════════════════════════════════════════════════════════════

    private void OnNextPressed()
    {
        waitingForNext = false;
    }

    private void OnSkipPressed()
    {
        if (!isPlaying) return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        waitingForNext = false;
        StartCoroutine(SkipSequence());
    }

    private IEnumerator SkipSequence()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        yield return StartCoroutine(FadeOutFrameImage());
        FinishCutscene();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Helper: Frame Display & Fading
    // ══════════════════════════════════════════════════════════════════════

    private IEnumerator ShowFrame(Sprite sprite)
    {
        if (frameImage == null) yield break;

        frameImage.sprite = sprite;
        yield return StartCoroutine(FadeImage(frameImage, 0f, 1f));
    }

    private IEnumerator CrossfadeToFrame(Sprite nextSprite)
    {
        if (frameImage == null) yield break;

        yield return StartCoroutine(FadeImage(frameImage, 1f, 0f));
        frameImage.sprite = nextSprite;
        yield return StartCoroutine(FadeImage(frameImage, 0f, 1f));
    }

    private IEnumerator FadeOutFrameImage()
    {
        if (frameImage == null || !frameImage.gameObject.activeInHierarchy) yield break;
        yield return StartCoroutine(FadeImage(frameImage, 1f, 0f));
    }

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
    // Helper: Audio & SFX
    // ══════════════════════════════════════════════════════════════════════

    private void PlayBGM(CutsceneData data)
    {
        if (audioSource == null || data.bgmClip == null) return;

        audioSource.clip = data.bgmClip;
        audioSource.volume = data.bgmVolume;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void PlayFrameSFX(int frameIndex)
    {
        if (currentData == null) return;
        AudioClip sfx = currentData.GetFrameSFX(frameIndex);
        if (sfx == null) return;

        float vol = currentData.sfxVolume;
        if (sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(sfx, vol);
        }
        else if (audioSource != null)
        {
            audioSource.PlayOneShot(sfx, vol);
        }
    }

    private void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (sfxAudioSource != null && sfxAudioSource.isPlaying)
            sfxAudioSource.Stop();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Cleanup
    // ══════════════════════════════════════════════════════════════════════

    private void FinishCutscene()
    {
        isPlaying = false;
        cutscenePanel?.SetActive(false);
        StopAudio();

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // Kembalikan timeScale
        if (currentData != null && currentData.pauseGameDuringCutscene)
            Time.timeScale = 1f;

        // Jalankan event callback
        onCutsceneFinished?.Invoke();

        // Load scene jika dikonfigurasi
        if (currentData != null && !string.IsNullOrEmpty(currentData.loadSceneAfter))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(currentData.loadSceneAfter);
        }

        currentData = null;
    }
}
