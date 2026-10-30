using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Dreamrift.QuestSystem
{
    /// <summary>
    /// Master Controller UI Quest Book (Buku Quest Interaktif).
    /// Mengelola animasi buka/tutup (UIFrameAnimation), kontrol kursor & pergerakan pemain,
    /// navigasi bab (Chapter/Level) di halaman kiri, dan tampilan narasi cerita di halaman kanan.
    /// </summary>
    public class QuestBookUI : MonoBehaviour
    {
        public static QuestBookUI Instance { get; private set; }

        [Header("Root & Canvas")]
        [SerializeField] private GameObject bookPanelRoot;
        [SerializeField] private CanvasGroup pagesCanvasGroup;
        [SerializeField] private UIFrameAnimation frameAnimator;

        [Header("Pages")]
        [SerializeField] private QuestBookLeftPageUI leftPage;
        [SerializeField] private QuestBookRightPageUI rightPage;

        [Header("Controls & Input")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button hudBookButton;
        [SerializeField] private KeyCode toggleShortcutKey = KeyCode.J;
        [SerializeField] private bool isOpenAtStart = false;
        [SerializeField] private bool pauseGameWhileOpen = false;

        [Header("Audio Effects (Opsional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip bookOpenSound;
        [SerializeField] private AudioClip bookCloseSound;
        [SerializeField] private AudioClip pageTurnSound;

        [Header("Chapters Data")]
        [Tooltip("Daftar Chapter yang akan ditampilkan di Quest Book.")]
        [SerializeField] private List<QuestChapterData> chapters = new List<QuestChapterData>();

        [Header("Page Turn Transition")]
        [Tooltip("Durasi efek Fade In dan Fade Out saat membalik halaman (dalam detik).")]
        [SerializeField, Range(0.05f, 0.5f)] private float pageFadeDuration = 0.15f;
        [Tooltip("Jika dicentang, arah animasi dibalik.")]
        [SerializeField] private bool invertFlipAnimationDirection = false;

        private int currentChapterIndex = 0;
        private QuestData currentSelectedQuest;
        private bool isTransitioning = false;
        private bool wasCursorLockedBeforeOpen = true;
        private Coroutine pageTurnCoroutine;

        // Cache player movement inputs
        private StarterAssets.StarterAssetsInputs cachedPlayerInputs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (leftPage != null)
            {
                leftPage.InitializeNavigation(OnChapterNavigated);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseBook);
            }

            if (hudBookButton != null)
            {
                hudBookButton.onClick.AddListener(ToggleBook);
            }

            // Sembunyikan panel di awal
            if (bookPanelRoot != null)
            {
                bookPanelRoot.SetActive(isOpenAtStart);
            }

            if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.alpha = isOpenAtStart ? 1f : 0f;
            }
        }

        private void Start()
        {
            ResolvePlayerInputs();

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStateChanged += OnQuestStateChanged;
                QuestManager.Instance.QuestProgressChanged += OnQuestProgressChanged;
            }

            // Jika belum ada chapter yang di-assign, buat fallback default chapter dari QuestManager
            EnsureChaptersAvailable();

            if (isOpenAtStart)
            {
                OpenBook();
            }
        }

        private void OnDestroy()
        {
            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
                pageTurnCoroutine = null;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStateChanged -= OnQuestStateChanged;
                QuestManager.Instance.QuestProgressChanged -= OnQuestProgressChanged;
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            {
                ToggleBook();
            }
            else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && IsOpen())
            {
                CloseBook();
            }
#else
            if (Input.GetKeyDown(toggleShortcutKey))
            {
                ToggleBook();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && IsOpen())
            {
                CloseBook();
            }
#endif
        }

        public bool IsOpen()
        {
            return bookPanelRoot != null && bookPanelRoot.activeSelf;
        }

        public void ToggleBook()
        {
            if (isTransitioning) return;

            if (IsOpen())
            {
                CloseBook();
            }
            else
            {
                OpenBook();
            }
        }

        public void OpenBook()
        {
            if (bookPanelRoot == null) return;
            isTransitioning = false;

            ResolvePlayerInputs();
            bookPanelRoot.SetActive(true);

            // Simpan status kursor & bebaskan kursor untuk klik UI
            wasCursorLockedBeforeOpen = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Nonaktifkan kontrol karakter pemain
            if (cachedPlayerInputs != null)
            {
                cachedPlayerInputs.cursorInputForLook = false;
                cachedPlayerInputs.cursorLocked = false;
                cachedPlayerInputs.move = Vector2.zero;
            }

            if (pauseGameWhileOpen)
            {
                Time.timeScale = 0f;
            }

            PlaySound(bookOpenSound);

            // Batalkan transisi aktif jika ada
            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
                pageTurnCoroutine = null;
            }

            // Buka instan tanpa animasi: langsung pasang frame buku terbuka (frame 18)
            if (frameAnimator != null && frameAnimator.TotalFrames > 0)
            {
                frameAnimator.SetFrame(frameAnimator.TotalFrames - 1);
            }

            // Tampilkan halaman kiri & kanan secara instan
            if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.alpha = 1f;
                pagesCanvasGroup.interactable = true;
                pagesCanvasGroup.blocksRaycasts = true;
            }

            isTransitioning = false;

            // Auto-pilih quest aktif jika ada
            if (currentSelectedQuest == null && QuestManager.Instance != null)
            {
                currentSelectedQuest = QuestManager.Instance.CurrentActiveQuest;
            }

            RefreshCurrentView();
        }

        public void CloseBook()
        {
            if (bookPanelRoot == null || !IsOpen()) return;

            PlaySound(bookCloseSound);

            // Batalkan transisi jika sedang berjalan
            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
                pageTurnCoroutine = null;
            }

            // Tutup instan tanpa animasi penutup
            bookPanelRoot.SetActive(false);
            isTransitioning = false;

            if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.alpha = 1f;
                pagesCanvasGroup.interactable = true;
                pagesCanvasGroup.blocksRaycasts = true;
            }

            if (pauseGameWhileOpen)
            {
                Time.timeScale = 1f;
            }

            // Kembalikan kursor ke status semula
            if (wasCursorLockedBeforeOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (cachedPlayerInputs != null)
            {
                cachedPlayerInputs.cursorInputForLook = true;
                cachedPlayerInputs.cursorLocked = wasCursorLockedBeforeOpen;
            }
        }

        public void SetChapters(List<QuestChapterData> newChapters)
        {
            chapters = newChapters ?? new List<QuestChapterData>();
            currentChapterIndex = 0;
            currentSelectedQuest = null;
            if (IsOpen())
            {
                RefreshCurrentView();
            }
        }

        public void SelectQuest(QuestData quest)
        {
            currentSelectedQuest = quest;
            QuestChapterData currentChapter = GetCurrentChapter();

            if (leftPage != null)
            {
                leftPage.UpdateSelectionHighlight(quest);
            }

            if (rightPage != null)
            {
                QuestState state = (quest != null && QuestManager.Instance != null)
                    ? QuestManager.Instance.GetQuestState(quest)
                    : QuestState.NotStarted;
                rightPage.DisplayStory(currentChapter, currentChapterIndex, quest, state);
            }
        }

        private void OnChapterNavigated(int delta)
        {
            if (!IsOpen() || isTransitioning || chapters == null || chapters.Count == 0) return;

            int newIndex = Mathf.Clamp(currentChapterIndex + delta, 0, chapters.Count - 1);
            if (newIndex == currentChapterIndex) return;

            if (pageTurnCoroutine != null)
            {
                StopCoroutine(pageTurnCoroutine);
            }
            pageTurnCoroutine = StartCoroutine(PageTurnSequence(delta, newIndex));
        }

        private System.Collections.IEnumerator PageTurnSequence(int delta, int newIndex)
        {
            isTransitioning = true;

            // Kunci interaksi tombol agar tidak bisa diklik berulang kali selama transisi
            if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.interactable = false;
                pagesCanvasGroup.blocksRaycasts = false;
            }

            // 1. FADE OUT (Teks dan konten memudar keluar: Alpha 1 -> 0)
            if (pagesCanvasGroup != null && pageFadeDuration > 0f)
            {
                float startAlpha = pagesCanvasGroup.alpha;
                float elapsed = 0f;
                while (elapsed < pageFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    pagesCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / pageFadeDuration);
                    yield return null;
                }
                pagesCanvasGroup.alpha = 0f;
            }
            else if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.alpha = 0f;
            }

            // 2. PUTAR ANIMASI LEMBARAN MEMBALIK
            PlaySound(pageTurnSound);

            bool isMovingForward = delta > 0;
            if (invertFlipAnimationDirection) isMovingForward = !isMovingForward;

            bool animFinished = false;
            Action onAnimationComplete = () => animFinished = true;

            if (frameAnimator != null && frameAnimator.TotalFrames > 0)
            {
                if (isMovingForward)
                {
                    // Chapter kecil ke besar: kanan ke kiri (PlayForward, frame 0 -> 18)
                    frameAnimator.PlayForward(onAnimationComplete);
                }
                else
                {
                    // Chapter besar ke kecil: kiri ke kanan (PlayReverse, frame 18 -> 0)
                    frameAnimator.PlayReverse(onAnimationComplete);
                }

                while (!animFinished)
                {
                    yield return null;
                }

                // Pastikan kembali ke frame buku terbuka penuh setelah animasi selesai
                frameAnimator.SetFrame(frameAnimator.TotalFrames - 1);
            }
            else
            {
                yield return null;
            }

            // 3. PERBARUI ISI CHAPTER BARU (Saat alpha masih 0)
            currentChapterIndex = newIndex;
            currentSelectedQuest = null; // Reset pilihan saat pindah bab
            RefreshCurrentView();

            // 4. FADE IN (Teks dan konten bab baru memudar masuk: Alpha 0 -> 1)
            if (pagesCanvasGroup != null && pageFadeDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < pageFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    pagesCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / pageFadeDuration);
                    yield return null;
                }
                pagesCanvasGroup.alpha = 1f;
            }
            else if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.alpha = 1f;
            }

            // Kembalikan interaksi
            if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.interactable = true;
                pagesCanvasGroup.blocksRaycasts = true;
            }

            isTransitioning = false;
            pageTurnCoroutine = null;
        }

        public void RefreshCurrentView()
        {
            EnsureChaptersAvailable();
            QuestChapterData currentChapter = GetCurrentChapter();

            // Auto-pilih quest pertama di chapter ini jika belum ada pilihan dan tidak tersegel
            bool isSealed = currentChapter != null && currentChapter.IsSealed(QuestManager.Instance);
            if (!isSealed && currentSelectedQuest == null && currentChapter != null && currentChapter.Quests.Count > 0)
            {
                currentSelectedQuest = currentChapter.Quests[0];
            }

            if (leftPage != null)
            {
                leftPage.RenderChapter(currentChapter, currentChapterIndex, chapters.Count, currentSelectedQuest, SelectQuest);
            }

            if (rightPage != null)
            {
                QuestState state = (currentSelectedQuest != null && QuestManager.Instance != null)
                    ? QuestManager.Instance.GetQuestState(currentSelectedQuest)
                    : QuestState.NotStarted;
                rightPage.DisplayStory(currentChapter, currentChapterIndex, currentSelectedQuest, state);
            }
        }

        private QuestChapterData GetCurrentChapter()
        {
            if (chapters == null || chapters.Count == 0) return null;
            currentChapterIndex = Mathf.Clamp(currentChapterIndex, 0, chapters.Count - 1);
            return chapters[currentChapterIndex];
        }

        private void EnsureChaptersAvailable()
        {
            if (chapters != null && chapters.Count > 0) return;

            // Fallback otomatis jika developer belum membuat asset chapter:
            // Buat chapter sementara yang memuat seluruh quest yang terdaftar di QuestManager
            QuestChapterData fallbackChapter = ScriptableObject.CreateInstance<QuestChapterData>();
            fallbackChapter.ChapterTitle = "Chapter 1: Petualangan Lembah";
            fallbackChapter.ChapterPrologue = "Selamat datang di Dreamrift. Catatan perjalanan petualang ini mencatat seluruh peristiwa, tugas, dan rahasia yang terungkap dalam perjalananmu.";

            if (QuestManager.Instance != null && QuestManager.Instance.RegisteredQuests != null)
            {
                foreach (var q in QuestManager.Instance.RegisteredQuests)
                {
                    if (q != null) fallbackChapter.AddQuest(q);
                }
            }

            chapters = new List<QuestChapterData> { fallbackChapter };
        }

        private void OnQuestStateChanged(QuestData quest, QuestState oldState, QuestState newState)
        {
            if (IsOpen())
            {
                RefreshCurrentView();
            }
        }

        private void OnQuestProgressChanged(QuestData quest, int current, int target)
        {
            if (IsOpen() && currentSelectedQuest == quest)
            {
                RefreshCurrentView();
            }
        }

        private void ResolvePlayerInputs()
        {
            if (cachedPlayerInputs == null)
            {
                cachedPlayerInputs = FindFirstObjectByType<StarterAssets.StarterAssetsInputs>();
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
