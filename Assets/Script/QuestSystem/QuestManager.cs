using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamrift.QuestSystem
{
    [DefaultExecutionOrder(-50)]
    public sealed class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool persistAcrossScenes = true;

        private readonly Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>(StringComparer.Ordinal);

        public event Action<QuestData, QuestState, QuestState> QuestStateChanged;
        public event Action<QuestData> QuestStarted;
        public event Action<QuestData> QuestCompleted;

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

        public QuestState GetQuestState(QuestData quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
            {
                return QuestState.NotStarted;
            }

            return questStates.TryGetValue(quest.QuestId, out QuestState state) ? state : QuestState.NotStarted;
        }

        public bool StartQuest(QuestData quest)
        {
            if (quest == null)
            {
                Debug.LogWarning("[QuestManager] Cannot start a null quest.");
                return false;
            }

            QuestState currentState = GetQuestState(quest);
            if (currentState != QuestState.NotStarted)
            {
                Debug.LogWarning($"[QuestManager] Cannot start quest '{quest.DisplayName}' ({quest.QuestId}) because it is currently in state: {currentState}.");
                return false;
            }

            questStates[quest.QuestId] = QuestState.Active;
            Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' ({quest.QuestId}) changed state: {currentState} -> {QuestState.Active}");

            QuestStateChanged?.Invoke(quest, currentState, QuestState.Active);
            QuestStarted?.Invoke(quest);
            return true;
        }

        public bool CompleteQuest(QuestData quest)
        {
            if (quest == null)
            {
                Debug.LogWarning("[QuestManager] Cannot complete a null quest.");
                return false;
            }

            QuestState currentState = GetQuestState(quest);
            if (currentState != QuestState.Active)
            {
                Debug.LogWarning($"[QuestManager] Cannot complete quest '{quest.DisplayName}' ({quest.QuestId}) because it is currently in state: {currentState}.");
                return false;
            }

            questStates[quest.QuestId] = QuestState.Complete;
            Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' ({quest.QuestId}) changed state: {currentState} -> {QuestState.Complete}");

            QuestStateChanged?.Invoke(quest, currentState, QuestState.Complete);
            QuestCompleted?.Invoke(quest);
            return true;
        }

        public void ResetQuest(QuestData quest)
        {
            if (quest == null)
            {
                return;
            }

            QuestState currentState = GetQuestState(quest);
            if (currentState == QuestState.NotStarted)
            {
                return;
            }

            questStates[quest.QuestId] = QuestState.NotStarted;
            Debug.Log($"[QuestManager] Quest '{quest.DisplayName}' ({quest.QuestId}) reset state: {currentState} -> {QuestState.NotStarted}");

            QuestStateChanged?.Invoke(quest, currentState, QuestState.NotStarted);
        }
    }
}
