using UnityEngine;

namespace Dreamrift.InventorySystem
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Dreamrift/Inventory/Item Data", order = 0)]
    public sealed class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "";
        [SerializeField] private string displayName = "New Item";
        [SerializeField, TextArea(2, 4)] private string description = "";

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject worldPrefab;

        [Header("Inventory")]
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private bool consumable;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;
        public int MaxStack => maxStack;
        public bool IsStackable => maxStack > 1;
        public bool Consumable => consumable;

        private void OnValidate()
        {
            maxStack = Mathf.Max(1, maxStack);

            // Keep a stable fallback id so runtime code never depends on display text.
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = name;
            }
        }
    }
}
