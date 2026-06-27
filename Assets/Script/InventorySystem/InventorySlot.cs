using System;
using UnityEngine;

namespace Dreamrift.InventorySystem
{
    [Serializable]
    public sealed class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(0)] private int quantity;

        public ItemData Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;
        public int MaxStack => item != null ? item.MaxStack : 0;
        public int RemainingSpace => IsEmpty ? 0 : Mathf.Max(0, item.MaxStack - quantity);

        public bool CanStack(ItemData itemToCheck)
        {
            return !IsEmpty && item == itemToCheck && quantity < item.MaxStack;
        }

        public bool CanAccept(ItemData itemToCheck)
        {
            return itemToCheck != null && (IsEmpty || CanStack(itemToCheck));
        }

        public int Add(ItemData itemToAdd, int amount)
        {
            if (itemToAdd == null || amount <= 0)
            {
                return 0;
            }

            if (IsEmpty)
            {
                item = itemToAdd;
                int accepted = Mathf.Min(amount, itemToAdd.MaxStack);
                quantity = accepted;
                return accepted;
            }

            if (!CanStack(itemToAdd))
            {
                return 0;
            }

            int stackSpace = item.MaxStack - quantity;
            int added = Mathf.Min(amount, stackSpace);
            quantity += added;
            return added;
        }

        public int Remove(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return 0;
            }

            int removed = Mathf.Min(amount, quantity);
            quantity -= removed;

            if (quantity <= 0)
            {
                Clear();
            }

            return removed;
        }

        public void Set(ItemData newItem, int newQuantity)
        {
            if (newItem == null || newQuantity <= 0)
            {
                Clear();
                return;
            }

            item = newItem;
            quantity = Mathf.Clamp(newQuantity, 1, newItem.MaxStack);
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }
}
