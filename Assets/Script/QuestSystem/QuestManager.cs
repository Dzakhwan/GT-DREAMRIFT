using System;
using System.Collections.Generic;
using UnityEngine;
using Dreamrift.InventorySystem;

namespace Dreamrift.QuestSystem
{
    [DefaultExecutionOrder(-50)]
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private List<QuestData> registeredQuests = new List<QuestData>();

        private readonly Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> killProgress = new Dictionary<string, int>(StringComparer.Ordinal);

        public QuestData CurrentActiveQuest { get; private set; }
        public IReadOnlyList<QuestData> RegisteredQuests => registeredQuests;

        public event Action<QuestData, QuestState, QuestState> QuestStateChanged;
        public event Action<QuestData> QuestStarted;
        public event Action<QuestData> QuestCompleted;
        public event Action<QuestData, int, int> QuestProgressChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.InventoryChanged += OnInventoryChanged;
            }
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.InventoryChanged -= OnInventoryChanged;
            }
        }

        public QuestState GetQuestState(QuestData quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
            {
                return QuestState.NotStarted;
            }

            if (questStates.TryGetValue(quest.QuestId, out QuestState state))
            {
                return state;
            }

            // Check if prerequisites are satisfied
            if (quest.Prerequisites != null && quest.Prerequisites.Count > 0)
            {
                foreach (var prereq in quest.Prerequisites)
                {
                    if (prereq == null) continue;
                    if (GetQuestState(prereq) != QuestState.Complete)
                    {
                        return QuestState.Locked;
                    }
                }
                return QuestState.Available;
            }

            return QuestState.NotStarted;
        }

        public int GetQuestKillCount(QuestData quest)
        {
            if (quest == null) return 0;
            return killProgress.TryGetValue(quest.QuestId, out int count) ? count : 0;
        }

        public bool StartQuest(QuestData quest)
        {
            if (quest == null)
            {
                Debug.LogWarning("[QuestManager] Cannot start a null quest.");
                return false;
            }

            RegisterQuest(quest);

            QuestState currentState = GetQuestState(quest);
            if (currentState == QuestState.Locked)
            {
                Debug.LogWarning($"[QuestManager] Cannot start quest '{quest.DisplayName}' ({quest.QuestId}) because prerequisites are not yet completed!");
                return false;
            }
            if (currentState == QuestState.Active || currentState == QuestState.Complete)
            {
                Debug.LogWarning($"[QuestManager] Cannot start quest '{quest.DisplayName}' ({quest.QuestId}) because it is currently in state: {currentState}.");
                return false;
            }

            questStates[quest.QuestId] = QuestState.Active;
            killProgress[quest.QuestId] = 0;
            CurrentActiveQuest = quest;
            Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' ({quest.QuestId}) STARTED!");

            QuestStateChanged?.Invoke(quest, currentState, QuestState.Active);
            QuestStarted?.Invoke(quest);

            // Check immediately if it's a CollectItem quest and requirements are already satisfied
            if (quest.ObjectiveType == QuestObjectiveType.CollectItem)
            {
                CheckItemCollectionProgress(quest);
            }

            return true;
        }

        public bool CompleteQuest(QuestData quest)
        {
            if (quest == null)
            {
                Debug.LogWarning("[QuestManager] Cannot complete a null quest.");
                return false;
            }

            RegisterQuest(quest);

            QuestState currentState = GetQuestState(quest);
            if (currentState != QuestState.Active)
            {
                Debug.LogWarning($"[QuestManager] Cannot complete quest '{quest.DisplayName}' ({quest.QuestId}) because it is currently in state: {currentState}.");
                return false;
            }

            questStates[quest.QuestId] = QuestState.Complete;
            if (CurrentActiveQuest == quest)
            {
                CurrentActiveQuest = null;
            }

            Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' ({quest.QuestId}) COMPLETED!");

            // 🎁 Deliver Rewards to Player Inventory
            DeliverQuestRewards(quest);

            QuestStateChanged?.Invoke(quest, currentState, QuestState.Complete);
            QuestCompleted?.Invoke(quest);

            // Check next branching quests unlocked by completing this quest
            if (quest.NextQuestsOnComplete != null)
            {
                foreach (var nextQuest in quest.NextQuestsOnComplete)
                {
                    if (nextQuest == null) continue;
                    RegisterQuest(nextQuest);
                    QuestState nextState = GetQuestState(nextQuest);
                    Debug.Log($"[QuestManager] Branch quest '{nextQuest.DisplayName}' unlocked state: {nextState}");
                    QuestStateChanged?.Invoke(nextQuest, QuestState.Locked, nextState);
                }
            }

            return true;
        }

        public void RecordEnemyKill(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) return;

            foreach (var kvp in new Dictionary<string, QuestState>(questStates))
            {
                if (kvp.Value != QuestState.Active) continue;

                QuestData quest = FindRegisteredQuest(kvp.Key);
                if (quest == null || quest.ObjectiveType != QuestObjectiveType.DefeatEnemy) continue;

                if (string.Equals(quest.TargetEnemyId, enemyId, StringComparison.OrdinalIgnoreCase))
                {
                    int currentKills = GetQuestKillCount(quest) + 1;
                    killProgress[quest.QuestId] = currentKills;
                    Debug.Log($"[QuestManager] Enemy kill recorded ({enemyId}) for Quest '{quest.DisplayName}': {currentKills}/{quest.TargetKillCount}");

                    QuestProgressChanged?.Invoke(quest, currentKills, quest.TargetKillCount);

                    if (currentKills >= quest.TargetKillCount)
                    {
                        CompleteQuest(quest);
                    }
                }
            }
        }

        public void RecordNpcTalk(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId)) return;

            foreach (var kvp in new Dictionary<string, QuestState>(questStates))
            {
                if (kvp.Value != QuestState.Active) continue;

                QuestData quest = FindRegisteredQuest(kvp.Key);
                if (quest == null || quest.ObjectiveType != QuestObjectiveType.TalkToNPC) continue;

                if (string.Equals(quest.TargetNpcId, npcId, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[QuestManager] Talked to NPC ({npcId}) for Quest '{quest.DisplayName}'. Completing quest!");
                    CompleteQuest(quest);
                }
            }
        }

        public void ResetQuest(QuestData quest)
        {
            if (quest == null) return;

            QuestState currentState = GetQuestState(quest);
            questStates[quest.QuestId] = QuestState.NotStarted;
            killProgress[quest.QuestId] = 0;

            if (CurrentActiveQuest == quest)
            {
                CurrentActiveQuest = null;
            }

            Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' ({quest.QuestId}) reset.");
            QuestStateChanged?.Invoke(quest, currentState, QuestState.NotStarted);
        }

        public string GetQuestProgressStatus(QuestData quest)
        {
            if (quest == null) return "";
            QuestState state = GetQuestState(quest);

            if (state == QuestState.Locked) return "🔒 Terkunci (Selesaikan Prerequisite)";
            if (state == QuestState.Available) return "⭐ Siap Diambil";
            if (state == QuestState.NotStarted) return "Belum Dimulai";
            if (state == QuestState.Complete) return "✅ Selesai (Complete)";

            switch (quest.ObjectiveType)
            {
                case QuestObjectiveType.TalkToNPC:
                    return $"Bicara dengan NPC: {quest.TargetNpcId}";
                case QuestObjectiveType.ReachLocation:
                    return "Jelajahi / Capai Lokasi Target";
                case QuestObjectiveType.DefeatEnemy:
                    int kills = GetQuestKillCount(quest);
                    return $"Kalahkan {quest.TargetEnemyId}: {kills} / {quest.TargetKillCount}";
                case QuestObjectiveType.CollectItem:
                    int itemCount = (InventoryManager.Instance != null && quest.TargetItem != null)
                        ? InventoryManager.Instance.GetItemCount(quest.TargetItem) : 0;
                    return $"Kumpulkan {quest.TargetItem?.DisplayName ?? "Item"}: {itemCount} / {quest.TargetItemAmount}";
                default:
                    return "Aktif";
            }
        }

        private void OnInventoryChanged()
        {
            foreach (var kvp in new Dictionary<string, QuestState>(questStates))
            {
                if (kvp.Value != QuestState.Active) continue;

                QuestData quest = FindRegisteredQuest(kvp.Key);
                if (quest != null && quest.ObjectiveType == QuestObjectiveType.CollectItem)
                {
                    CheckItemCollectionProgress(quest);
                }
            }
        }

        private void CheckItemCollectionProgress(QuestData quest)
        {
            if (quest == null || InventoryManager.Instance == null || quest.TargetItem == null) return;

            int currentCount = InventoryManager.Instance.GetItemCount(quest.TargetItem);
            QuestProgressChanged?.Invoke(quest, currentCount, quest.TargetItemAmount);

            if (currentCount >= quest.TargetItemAmount)
            {
                Debug.Log($"[QuestManager] Item collection requirement met for '{quest.DisplayName}' ({currentCount}/{quest.TargetItemAmount}). Completing quest!");
                CompleteQuest(quest);
            }
        }

        private void DeliverQuestRewards(QuestData quest)
        {
            if (quest == null || quest.Rewards == null) return;

            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("[QuestManager] InventoryManager.Instance is null! Cannot deliver rewards.");
                return;
            }

            foreach (var reward in quest.Rewards)
            {
                if (reward.item != null && reward.amount > 0)
                {
                    int leftover = InventoryManager.Instance.AddItem(reward.item, reward.amount);
                    if (leftover > 0)
                    {
                        Debug.LogWarning($"[QuestManager] Inventory was full while delivering reward '{reward.item.DisplayName}'. Leftover: {leftover}");
                    }
                    else
                    {
                        Debug.Log($"[QuestManager] Successfully delivered reward: {reward.amount}x {reward.item.DisplayName}");
                    }
                }
            }
        }

        public void RegisterQuest(QuestData quest)
        {
            if (quest != null && !registeredQuests.Contains(quest))
            {
                registeredQuests.Add(quest);
            }
        }

        private QuestData FindRegisteredQuest(string questId)
        {
            return registeredQuests.Find(q => q != null && string.Equals(q.QuestId, questId, StringComparison.Ordinal));
        }
    }
}
