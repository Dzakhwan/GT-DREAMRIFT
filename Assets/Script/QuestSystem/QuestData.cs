using UnityEngine;

namespace Dreamrift.QuestSystem
{
    [CreateAssetMenu(fileName = "New Quest", menuName = "Dreamrift/Quest/Quest Data", order = 0)]
    public sealed class QuestData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questId = "";
        [SerializeField] private string displayName = "New Quest";
        [SerializeField, TextArea(2, 4)] private string description = "";

        public string QuestId => questId;
        public string DisplayName => displayName;
        public string Description => description;

        private void OnValidate()
        {
            // Keep a stable fallback id so runtime code never depends on display text.
            if (string.IsNullOrWhiteSpace(questId))
            {
                questId = name;
            }
        }
    }
}
