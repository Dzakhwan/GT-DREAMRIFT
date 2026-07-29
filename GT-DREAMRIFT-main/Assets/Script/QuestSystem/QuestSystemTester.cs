using UnityEngine;

namespace Dreamrift.QuestSystem
{
    public sealed class QuestSystemTester : MonoBehaviour
    {
        [Header("Test Target")]
        [SerializeField] private QuestData testQuest;

        [ContextMenu("Test - Start Quest")]
        public void StartTestQuest()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] QuestManager instance is not found in the scene.");
                return;
            }

            if (testQuest == null)
            {
                Debug.LogWarning("[QuestSystemTester] No Test Quest assigned in Inspector.");
                return;
            }

            QuestManager.Instance.StartQuest(testQuest);
        }

        [ContextMenu("Test - Complete Quest")]
        public void CompleteTestQuest()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] QuestManager instance is not found in the scene.");
                return;
            }

            if (testQuest == null)
            {
                Debug.LogWarning("[QuestSystemTester] No Test Quest assigned in Inspector.");
                return;
            }

            QuestManager.Instance.CompleteQuest(testQuest);
        }

        [ContextMenu("Test - Reset Quest")]
        public void ResetTestQuest()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] QuestManager instance is not found in the scene.");
                return;
            }

            if (testQuest == null)
            {
                Debug.LogWarning("[QuestSystemTester] No Test Quest assigned in Inspector.");
                return;
            }

            QuestManager.Instance.ResetQuest(testQuest);
        }

        [ContextMenu("Test - Log Current Quest State")]
        public void LogCurrentQuestState()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] QuestManager instance is not found in the scene.");
                return;
            }

            if (testQuest == null)
            {
                Debug.LogWarning("[QuestSystemTester] No Test Quest assigned in Inspector.");
                return;
            }

            QuestState state = QuestManager.Instance.GetQuestState(testQuest);
            Debug.Log($"[QuestSystemTester] Current state of '{testQuest.DisplayName}' ({testQuest.QuestId}): {state}");
        }
    }
}
