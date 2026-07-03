using System;
using UnityEngine;

namespace Dreamrift.InventorySystem
{
    [DefaultExecutionOrder(-50)]
    public sealed class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory")]
        [SerializeField, Min(1)] private int capacity = 24;
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private InventorySlot[] slots;

        public event Action<int, InventorySlot> SlotChanged;
        public event Action InventoryChanged;
        public event Action<ItemData, int> ItemAdded;
        public event Action<ItemData, int> ItemRemoved;
        public event Action<ItemData, int> InventoryFull;

        public int Capacity => capacity;
        public int SlotCount => slots != null ? slots.Length : capacity;

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

            EnsureSlots();
        }

        private void OnValidate()
        {
            capacity = Mathf.Max(1, capacity);
            EnsureSlots();
        }

        public InventorySlot GetSlot(int index)
        {
            if (slots == null || index < 0 || index >= slots.Length)
            {
                return null;
            }

            return slots[index];
        }

        public int AddItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
            {
                return amount;
            }

            EnsureSlots();

            int remaining = amount;

            // Fill existing stacks first so Android UI stays compact and predictable.
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].CanStack(item))
                {
                    continue;
                }

                int added = slots[i].Add(item, remaining);
                if (added > 0)
                {
                    remaining -= added;
                    RaiseSlotChanged(i);
                }
            }

            // Then use empty slots for any leftover quantity.
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    continue;
                }

                int added = slots[i].Add(item, remaining);
                if (added > 0)
                {
                    remaining -= added;
                    RaiseSlotChanged(i);
                }
            }

            int accepted = amount - remaining;
            if (accepted > 0)
            {
                ItemAdded?.Invoke(item, accepted);
                InventoryChanged?.Invoke();
            }

            if (remaining > 0)
            {
                InventoryFull?.Invoke(item, remaining);
            }

            return remaining;
        }

        public bool TryAddItem(ItemData item, int amount)
        {
            return AddItem(item, amount) == 0;
        }

        public int RemoveItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0 || slots == null)
            {
                return 0;
            }

            int remaining = amount;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty || slot.Item != item)
                {
                    continue;
                }

                int removed = slot.Remove(remaining);
                if (removed > 0)
                {
                    remaining -= removed;
                    RaiseSlotChanged(i);
                }
            }

            int totalRemoved = amount - remaining;
            if (totalRemoved > 0)
            {
                ItemRemoved?.Invoke(item, totalRemoved);
                InventoryChanged?.Invoke();
            }

            return totalRemoved;
        }

        public bool HasItem(ItemData item, int amount)
        {
            return GetItemCount(item) >= amount;
        }

        public int GetItemCount(ItemData item)
        {
            if (item == null || slots == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.Item == item)
                {
                    count += slot.Quantity;
                }
            }

            return count;
        }

        public bool CanAddItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0 || slots == null)
            {
                return false;
            }

            int remaining = amount;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.CanStack(item))
                {
                    remaining -= slot.RemainingSpace;
                }
                else if (slot.IsEmpty)
                {
                    remaining -= item.MaxStack;
                }
            }

            return remaining <= 0;
        }

        public void SwapSlots(int firstIndex, int secondIndex)
        {
            if (slots == null ||
                firstIndex == secondIndex ||
                firstIndex < 0 || firstIndex >= slots.Length ||
                secondIndex < 0 || secondIndex >= slots.Length)
            {
                return;
            }

            InventorySlot temp = slots[firstIndex];
            slots[firstIndex] = slots[secondIndex];
            slots[secondIndex] = temp;

            RaiseSlotChanged(firstIndex);
            RaiseSlotChanged(secondIndex);
            InventoryChanged?.Invoke();
        }

        public void ClearInventory()
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    continue;
                }

                slots[i].Clear();
                RaiseSlotChanged(i);
            }

            InventoryChanged?.Invoke();
        }

        private void EnsureSlots()
        {
            if (slots == null || slots.Length != capacity)
            {
                InventorySlot[] resizedSlots = new InventorySlot[capacity];
                int copyCount = slots != null ? Mathf.Min(slots.Length, resizedSlots.Length) : 0;

                for (int i = 0; i < copyCount; i++)
                {
                    resizedSlots[i] = slots[i];
                }

                slots = resizedSlots;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = new InventorySlot();
                }
            }
        }

        private void RaiseSlotChanged(int index)
        {
            SlotChanged?.Invoke(index, slots[index]);
        }
    }
}
