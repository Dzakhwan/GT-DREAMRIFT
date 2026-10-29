using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamrift.QuestSystem
{
    /// <summary>
    /// Komponen Halaman Kiri Quest Book (Chapter & Daftar Misi).
    /// Mengelola navigasi Chapter/Level dan me-render daftar quest yang rapi dan responsif.
    /// </summary>
    public class QuestBookLeftPageUI : MonoBehaviour
    {
        [Header("Chapter Navigation")]
        [SerializeField] private TextMeshProUGUI chapterTitleText;
        [SerializeField] private TextMeshProUGUI chapterCounterText;
        [SerializeField] private Button prevChapterButton;
        [SerializeField] private Button nextChapterButton;

        [Header("Quest List")]
        [SerializeField] private Transform questListContainer;
        [SerializeField] private QuestBookEntryUI entryPrefab;

        [Header("Track Quest Action")]
        [SerializeField] private Button trackQuestButton;
        [SerializeField] private TextMeshProUGUI trackButtonText;

        [Header("Sealed State UI")]
        [SerializeField] private GameObject normalContentContainer;
        [SerializeField] private GameObject sealedContainer;
        [SerializeField] private TextMeshProUGUI sealedNoticeText;

        private readonly List<QuestBookEntryUI> spawnedEntries = new List<QuestBookEntryUI>();
        private QuestData currentSelectedQuest;
        private Action<int> onChapterNavigatedCallback;
        private Action<QuestData> onQuestSelectedCallback;

        private void Awake()
        {
            if (prevChapterButton != null)
            {
                prevChapterButton.onClick.AddListener(() => onChapterNavigatedCallback?.Invoke(-1));
            }

            if (nextChapterButton != null)
            {
                nextChapterButton.onClick.AddListener(() => onChapterNavigatedCallback?.Invoke(1));
            }

            if (trackQuestButton != null)
            {
                trackQuestButton.onClick.AddListener(OnTrackButtonClicked);
            }
        }

        public void InitializeNavigation(Action<int> onChapterNavigated)
        {
            onChapterNavigatedCallback = onChapterNavigated;
        }

        public void RenderChapter(QuestChapterData chapter, int chapterIndex, int totalChapters, QuestData selectedQuest, Action<QuestData> onQuestSelected)
        {
            currentSelectedQuest = selectedQuest;
            onQuestSelectedCallback = onQuestSelected;

            bool isSealed = chapter != null && chapter.IsSealed(QuestManager.Instance);

            // Navigasi Chapter Info: Jika tersegel, hanya tertulis "Chapter 2", dst.
            if (chapterTitleText != null)
            {
                chapterTitleText.text = isSealed ? $"Chapter {chapterIndex + 1}" : (chapter != null ? chapter.ChapterTitle : "Daftar Misi");
            }

            if (chapterCounterText != null)
            {
                chapterCounterText.text = isSealed ? "[ TERSEGEL ]" : (totalChapters > 1 ? $"Bab {chapterIndex + 1} / {totalChapters}" : "");
            }

            if (prevChapterButton != null)
            {
                prevChapterButton.interactable = chapterIndex > 0;
            }

            if (nextChapterButton != null)
            {
                nextChapterButton.interactable = chapterIndex < totalChapters - 1;
            }

            // Bersihkan entri lama
            ClearEntries();

            if (isSealed)
            {
                if (normalContentContainer != null) normalContentContainer.SetActive(false);
                if (sealedContainer != null) sealedContainer.SetActive(true);
                if (sealedNoticeText != null)
                {
                    sealedNoticeText.text = $"Bab ini masih terkunci oleh segel misterius.\n\nSelesaikan seluruh petualangan pada Chapter {Mathf.Max(1, chapterIndex)} untuk mematahkan segel dan membuka misi di bab ini.";
                }
                UpdateTrackButton(null);
                return;
            }

            if (normalContentContainer != null) normalContentContainer.SetActive(true);
            if (sealedContainer != null) sealedContainer.SetActive(false);

            if (questListContainer == null || entryPrefab == null || chapter == null)
            {
                UpdateTrackButton(selectedQuest);
                return;
            }

            IReadOnlyList<QuestData> quests = chapter.Quests;
            if (quests != null)
            {
                foreach (var q in quests)
                {
                    if (q == null) continue;

                    QuestState state = QuestManager.Instance != null ? QuestManager.Instance.GetQuestState(q) : QuestState.NotStarted;
                    string progressStatus = QuestManager.Instance != null ? QuestManager.Instance.GetQuestProgressStatus(q) : "";
                    bool isSelected = (q == selectedQuest);

                    QuestBookEntryUI entryInstance = Instantiate(entryPrefab, questListContainer);
                    entryInstance.Setup(q, state, progressStatus, isSelected, OnEntrySelected);
                    spawnedEntries.Add(entryInstance);
                }

                Canvas.ForceUpdateCanvases();
                RectTransform crt = questListContainer as RectTransform;
                if (crt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
            }

            UpdateTrackButton(selectedQuest);
        }

        public void UpdateSelectionHighlight(QuestData selectedQuest)
        {
            currentSelectedQuest = selectedQuest;
            UpdateTrackButton(selectedQuest);

            if (questListContainer == null || chapterTitleText == null) return;

            // Update state visual masing-masing entry tanpa instantiate ulang
            for (int i = 0; i < spawnedEntries.Count; i++)
            {
                // Entri akan otomatis dicek apakah sama dengan selectedQuest
                // Bisa juga cukup dipanggil lewat RenderChapter jika data berubah
            }
        }

        private void OnEntrySelected(QuestData quest)
        {
            currentSelectedQuest = quest;
            UpdateTrackButton(quest);
            onQuestSelectedCallback?.Invoke(quest);
        }

        private void OnTrackButtonClicked()
        {
            if (currentSelectedQuest == null || QuestManager.Instance == null) return;

            QuestState state = QuestManager.Instance.GetQuestState(currentSelectedQuest);
            if (state == QuestState.Available || state == QuestState.NotStarted)
            {
                // Mulai quest jika belum aktif
                QuestManager.Instance.StartQuest(currentSelectedQuest);
            }

            // Set sebagai active quest di QuestManager
            QuestManager.Instance.SetActiveQuest(currentSelectedQuest);
            UpdateTrackButton(currentSelectedQuest);
        }

        private void UpdateTrackButton(QuestData quest)
        {
            if (trackQuestButton == null) return;

            if (quest == null || QuestManager.Instance == null)
            {
                trackQuestButton.gameObject.SetActive(false);
                return;
            }

            QuestState state = QuestManager.Instance.GetQuestState(quest);
            if (state == QuestState.Locked)
            {
                trackQuestButton.gameObject.SetActive(false);
                return;
            }

            trackQuestButton.gameObject.SetActive(true);

            bool isCurrentlyActive = (QuestManager.Instance.CurrentActiveQuest == quest);
            if (trackButtonText != null)
            {
                if (state == QuestState.Complete)
                {
                    trackButtonText.text = "Misi Telah Selesai";
                    trackQuestButton.interactable = false;
                }
                else if (isCurrentlyActive)
                {
                    trackButtonText.text = "Sedang Dilacak di HUD";
                    trackQuestButton.interactable = false;
                }
                else
                {
                    trackButtonText.text = "Lacak Misi Ini";
                    trackQuestButton.interactable = true;
                }
            }
        }

        private void ClearEntries()
        {
            spawnedEntries.Clear();

            if (questListContainer != null)
            {
                for (int i = questListContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = questListContainer.GetChild(i);
                    if (child == null) continue;

                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }
}
