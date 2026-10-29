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

        private int currentChapterIndex = 0;
        private QuestData currentSelectedQuest;
        private bool isTransitioning = false;
        private bool wasCursorLockedBeforeOpen = true;

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
            if (isTransitioning || bookPanelRoot == null) return;

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
            if (isTransitioning || bookPanelRoot == null || !IsOpen()) return;

            PlaySound(bookCloseSound);

            // Tutup instan tanpa animasi penutup
            bookPanelRoot.SetActive(false);
            isTransitioning = false;

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
            if (isTransitioning || chapters == null || chapters.Count == 0) return;

            int newIndex = Mathf.Clamp(currentChapterIndex + delta, 0, chapters.Count - 1);
            if (newIndex == currentChapterIndex) return;

            isTransitioning = true;
            PlaySound(pageTurnSound);

            // Sembunyikan konten halaman sebentar saat membalik buku
            if (pagesCanvasGroup != null)
            {
                pagesCanvasGroup.alpha = 0f;
                pagesCanvasGroup.interactable = false;
                pagesCanvasGroup.blocksRaycasts = false;
            }

            Action onPageTurnDone = () =>
            {
                currentChapterIndex = newIndex;
                currentSelectedQuest = null; // Reset pilihan saat pindah bab
                RefreshCurrentView();

                // Pastikan kembali ke frame buku terbuka penuh setelah animasi selesai
                if (frameAnimator != null && frameAnimator.TotalFrames > 0)
                {
                    frameAnimator.SetFrame(frameAnimator.TotalFrames - 1);
                }

                if (pagesCanvasGroup != null)
                {
                    pagesCanvasGroup.alpha = 1f;
                    pagesCanvasGroup.interactable = true;
                    pagesCanvasGroup.blocksRaycasts = true;
                }

                isTransitioning = false;
            };

            // Putar animasi membalik halaman khusus saat navigasi bab
            if (frameAnimator != null && frameAnimator.TotalFrames > 0)
            {
                frameAnimator.PlayForward(onPageTurnDone);
            }
            else
            {
                onPageTurnDone();
            }
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
