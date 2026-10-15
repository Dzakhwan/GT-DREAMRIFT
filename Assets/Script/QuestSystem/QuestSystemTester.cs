using UnityEngine;
using Dreamrift.InventorySystem;

namespace Dreamrift.QuestSystem
{
    public sealed class QuestSystemTester : MonoBehaviour
    {
        [Header("Test Target")]
        [SerializeField] private QuestData testQuest;

        [Header("Simulation Settings")]
        [SerializeField] private string testEnemyId = "Slime";
        [SerializeField] private string testNpcId = "Elder";

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

        [ContextMenu("Test - Record Enemy Kill (Simulate Defeat Enemy)")]
        public void TestKillEnemy()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] QuestManager instance is not found.");
                return;
            }

            Debug.Log($"[QuestSystemTester] Simulating enemy kill for: '{testEnemyId}'");
            QuestManager.Instance.RecordEnemyKill(testEnemyId);
        }

        [ContextMenu("Test - Record NPC Talk (Simulate Talk to NPC)")]
        public void TestTalkToNpc()
        {
            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] QuestManager instance is not found.");
                return;
            }

            Debug.Log($"[QuestSystemTester] Simulating NPC conversation for: '{testNpcId}'");
            QuestManager.Instance.RecordNpcTalk(testNpcId);
        }

        [ContextMenu("Test - Complete Quest & Deliver Rewards")]
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

        [ContextMenu("Test - Verify Inventory Rewards Received")]
        public void VerifyInventoryRewards()
        {
            if (testQuest == null)
            {
                Debug.LogWarning("[QuestSystemTester] No Test Quest assigned in Inspector.");
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[QuestSystemTester] InventoryManager instance is not found.");
                return;
            }

            if (testQuest.Rewards == null || testQuest.Rewards.Count == 0)
            {
                Debug.Log($"[QuestSystemTester] Quest '{testQuest.DisplayName}' has no rewards assigned.");
                return;
            }

            foreach (var reward in testQuest.Rewards)
            {
                if (reward.item != null)
                {
                    int count = InventoryManager.Instance.GetItemCount(reward.item);
                    Debug.Log($"[QuestSystemTester] Inventory check: '{reward.item.DisplayName}' count = {count} (Requested reward amount: {reward.amount})");
                }
            }
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

        [ContextMenu("Test - Log Current Quest State & Progress")]
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
            string status = QuestManager.Instance.GetQuestProgressStatus(testQuest);
            Debug.Log($"[QuestSystemTester] Quest '{testQuest.DisplayName}' ({testQuest.QuestId}) | State: {state} | Progress: {status}");
        }
    }
}
