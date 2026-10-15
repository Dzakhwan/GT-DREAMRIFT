using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Dreamrift.QuestSystem
{
    public class QuestLogUI : MonoBehaviour
    {
        public static QuestLogUI Instance { get; private set; }

        [Header("Panel & Controls")]
        [SerializeField] private GameObject questLogPanel;
        [SerializeField] private KeyCode toggleShortcutKey = KeyCode.J;
        [SerializeField] private bool isOpenAtStart = false;

        [Header("Quest List View")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private QuestLogEntryUI entryPrefab;

        [Header("Quest Detail View")]
        [SerializeField] private TextMeshProUGUI detailTitleText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailObjectiveText;
        [SerializeField] private TextMeshProUGUI detailStatusText;
        [SerializeField] private TextMeshProUGUI detailRewardsText;

        private readonly List<QuestLogEntryUI> activeEntries = new List<QuestLogEntryUI>();
        private QuestData selectedQuest;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (questLogPanel != null)
            {
                questLogPanel.SetActive(isOpenAtStart);
            }
        }

        private void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStateChanged += OnQuestStateChanged;
                QuestManager.Instance.QuestProgressChanged += OnQuestProgressChanged;
            }

            RefreshList();
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
                ToggleQuestLog();
            }
#else
            if (Input.GetKeyDown(toggleShortcutKey))
            {
                ToggleQuestLog();
            }
#endif
        }

        public void ToggleQuestLog()
        {
            if (questLogPanel == null) return;

            bool active = !questLogPanel.activeSelf;
            questLogPanel.SetActive(active);

            if (active)
            {
                RefreshList();
            }
        }

        public void RefreshList()
        {
            // Clear old entries
            foreach (var entry in activeEntries)
            {
                if (entry != null) Destroy(entry.gameObject);
            }
            activeEntries.Clear();

            if (QuestManager.Instance == null || listContainer == null || entryPrefab == null)
            {
                return;
            }

            IReadOnlyList<QuestData> quests = QuestManager.Instance.RegisteredQuests;
            foreach (var quest in quests)
            {
                if (quest == null) continue;

                QuestState state = QuestManager.Instance.GetQuestState(quest);
                QuestLogEntryUI entryInstance = Instantiate(entryPrefab, listContainer);
                entryInstance.Setup(quest, state, SelectQuest);
                activeEntries.Add(entryInstance);
            }

            // Auto-select active quest or first quest in list
            if (selectedQuest == null && quests.Count > 0)
            {
                SelectQuest(QuestManager.Instance.CurrentActiveQuest ?? quests[0]);
            }
            else
            {
                DisplayQuestDetails(selectedQuest);
            }
        }

        public void SelectQuest(QuestData quest)
        {
            selectedQuest = quest;
            DisplayQuestDetails(quest);
        }

        private void DisplayQuestDetails(QuestData quest)
        {
            if (quest == null)
            {
                if (detailTitleText != null) detailTitleText.text = "Pilih Quest";
                if (detailDescriptionText != null) detailDescriptionText.text = "";
                if (detailObjectiveText != null) detailObjectiveText.text = "";
                if (detailStatusText != null) detailStatusText.text = "";
                if (detailRewardsText != null) detailRewardsText.text = "";
                return;
            }

            QuestState state = QuestManager.Instance != null ? QuestManager.Instance.GetQuestState(quest) : QuestState.NotStarted;

            if (detailTitleText != null) detailTitleText.text = quest.DisplayName;
            if (detailDescriptionText != null) detailDescriptionText.text = quest.Description;
            if (detailStatusText != null) detailStatusText.text = $"Status: {state}";

            if (detailObjectiveText != null)
            {
                string objStatus = QuestManager.Instance != null ? QuestManager.Instance.GetQuestProgressStatus(quest) : "";
                detailObjectiveText.text = $"Objektif: {objStatus}";
            }

            if (detailRewardsText != null)
            {
                if (quest.Rewards == null || quest.Rewards.Count == 0)
                {
                    detailRewardsText.text = "Hadiah: Tidak ada";
                }
                else
                {
                    List<string> rewardStrings = new List<string>();
                    foreach (var reward in quest.Rewards)
                    {
                        if (reward.item != null)
                        {
                            rewardStrings.Add($"{reward.amount}x {reward.item.DisplayName}");
                        }
                    }
                    detailRewardsText.text = "Hadiah: " + string.Join(", ", rewardStrings);
                }
            }
        }

        private void OnQuestStateChanged(QuestData quest, QuestState oldState, QuestState newState)
        {
            RefreshList();
        }

        private void OnQuestProgressChanged(QuestData quest, int currentProgress, int targetProgress)
        {
            if (selectedQuest == quest)
            {
                DisplayQuestDetails(quest);
            }
        }
    }
}
