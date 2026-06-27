using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamrift.InventorySystem
{
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private InventoryUISlot slotPrefab;

        [Header("Mobile Controls")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private bool openOnStart;

        public event Action<int, InventorySlot> SlotSelected;

        private InventoryUISlot[] uiSlots;
        private int selectedIndex = -1;
        private bool initialized;

        private void Awake()
        {
            ResolveManager();
            BuildSlots();
            SetOpen(openOnStart);
        }

        private void OnEnable()
        {
            ResolveManager();

            if (inventoryManager != null)
            {
                inventoryManager.SlotChanged += HandleSlotChanged;
                inventoryManager.InventoryChanged += RefreshAll;
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            if (inventoryManager != null)
            {
                inventoryManager.SlotChanged -= HandleSlotChanged;
                inventoryManager.InventoryChanged -= RefreshAll;
            }

            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Toggle()
        {
            bool isOpen = panelRoot != null && panelRoot.activeSelf;
            SetOpen(!isOpen);
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void OnSlotPressed(int index)
        {
            if (inventoryManager == null || index < 0 || index >= inventoryManager.SlotCount)
            {
                return;
            }

            int previousIndex = selectedIndex;
            selectedIndex = index;

            RefreshSelection(previousIndex);
            RefreshSelection(selectedIndex);

            SlotSelected?.Invoke(index, inventoryManager.GetSlot(index));
        }

        public void RefreshAll()
        {
            if (inventoryManager == null)
            {
                return;
            }

            BuildSlots();

            for (int i = 0; i < uiSlots.Length; i++)
            {
                uiSlots[i].SetSlot(inventoryManager.GetSlot(i));
                uiSlots[i].SetSelected(i == selectedIndex);
            }
        }

        private void HandleSlotChanged(int index, InventorySlot slot)
        {
            if (uiSlots == null || index < 0 || index >= uiSlots.Length)
            {
                return;
            }

            uiSlots[index].SetSlot(slot);
            uiSlots[index].SetSelected(index == selectedIndex);
        }

        private void SetOpen(bool open)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(open);
            }
        }

        private void ResolveManager()
        {
            if (inventoryManager == null)
            {
                inventoryManager = InventoryManager.Instance;
            }
        }

        private void BuildSlots()
        {
            if (initialized || inventoryManager == null || slotContainer == null || slotPrefab == null)
            {
                return;
            }

            int slotCount = inventoryManager.SlotCount;
            uiSlots = new InventoryUISlot[slotCount];

            // Instantiate once, then reuse these views for every event-driven refresh.
            for (int i = 0; i < slotCount; i++)
            {
                InventoryUISlot uiSlot = Instantiate(slotPrefab, slotContainer);
                uiSlot.Initialize(this, i);
                uiSlot.gameObject.SetActive(true);
                uiSlots[i] = uiSlot;
            }

            initialized = true;
        }

        private void RefreshSelection(int index)
        {
            if (uiSlots == null || index < 0 || index >= uiSlots.Length)
            {
                return;
            }

            uiSlots[index].SetSelected(index == selectedIndex);
        }
    }
}
