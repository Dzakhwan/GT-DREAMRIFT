using System.Collections.Generic;
using UnityEngine;
using Dreamrift.InventorySystem;

namespace Dreamrift.QuestSystem
{
    public enum QuestObjectiveType
    {
        TalkToNPC = 0,
        ReachLocation = 1,
        DefeatEnemy = 2,
        CollectItem = 3
    }

    [System.Serializable]
    public struct QuestReward
    {
        public ItemData item;
        [Min(1)] public int amount;

        public QuestReward(ItemData item, int amount = 1)
        {
            this.item = item;
            this.amount = Mathf.Max(1, amount);
        }
    }

    [CreateAssetMenu(fileName = "New Quest", menuName = "Dreamrift/Quest/Quest Data", order = 0)]
    public sealed class QuestData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questId = "";
        [SerializeField] private string displayName = "New Quest";
        [SerializeField, TextArea(2, 4)] private string description = "";

        [Header("Branching & Prerequisites Chain")]
        [Tooltip("Quest yang harus diselesaikan lebih dulu sebelum quest ini dapat diambil/dimulai.")]
        [SerializeField] private List<QuestData> prerequisites = new List<QuestData>();
        [Tooltip("Quest cabang/kelanjutan yang otomatis terbuka atau dapat diambil setelah quest ini selesai.")]
        [SerializeField] private List<QuestData> nextQuestsOnComplete = new List<QuestData>();

        [Header("Objective Settings")]
        [SerializeField] private QuestObjectiveType objectiveType = QuestObjectiveType.ReachLocation;

        [Header("NPC Objective (TalkToNPC)")]
        [Tooltip("ID/Nama NPC yang harus diajak bicara")]
        [SerializeField] private string targetNpcId = "";

        [Header("Enemy Objective (DefeatEnemy)")]
        [Tooltip("ID/Nama musuh yang harus dikalahkan (misal: Slime, Goblin, Boss)")]
        [SerializeField] private string targetEnemyId = "Slime";
        [SerializeField, Min(1)] private int targetKillCount = 1;

        [Header("Item Objective (CollectItem)")]
        [Tooltip("ItemData yang harus dikumpulkan di inventaris")]
        [SerializeField] private ItemData targetItem;
        [SerializeField, Min(1)] private int targetItemAmount = 1;

        [Header("Rewards (Multiple Items Supported)")]
        [SerializeField] private QuestReward[] rewards;

        [Header("Visual Graph Position (Editor Window)")]
        [SerializeField] private Vector2 nodePosition = new Vector2(100f, 100f);

        public string QuestId => questId;
        public string DisplayName => displayName;
        public string Description => description;
        public List<QuestData> Prerequisites => prerequisites;
        public List<QuestData> NextQuestsOnComplete => nextQuestsOnComplete;
        public QuestObjectiveType ObjectiveType => objectiveType;
        public string TargetNpcId => targetNpcId;
        public string TargetEnemyId => targetEnemyId;
        public int TargetKillCount => targetKillCount;
        public ItemData TargetItem => targetItem;
        public int TargetItemAmount => targetItemAmount;
        public IReadOnlyList<QuestReward> Rewards => rewards ?? System.Array.Empty<QuestReward>();

        public Vector2 NodePosition
        {
            get => nodePosition;
            set => nodePosition = value;
        }

        private void OnValidate()
        {
            // Keep a stable fallback id so runtime code never depends on display text.
            if (string.IsNullOrWhiteSpace(questId))
            {
                questId = name;
            }
            targetKillCount = Mathf.Max(1, targetKillCount);
            targetItemAmount = Mathf.Max(1, targetItemAmount);

            // Clean self references
            prerequisites.RemoveAll(q => q == this);
            nextQuestsOnComplete.RemoveAll(q => q == this);
        }
    }
}
