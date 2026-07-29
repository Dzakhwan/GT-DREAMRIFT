using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamrift.InventorySystem
{
    public sealed class InventoryUISlot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject emptyFrame;

        private InventoryUI owner;
        private int slotIndex = -1;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandlePressed);
            }
        }

        public void Initialize(InventoryUI inventoryUI, int index)
        {
            owner = inventoryUI;
            slotIndex = index;

            if (button != null)
            {
                button.onClick.RemoveListener(HandlePressed);
                button.onClick.AddListener(HandlePressed);
            }
        }

        public void SetSlot(InventorySlot slot)
        {
            bool isEmpty = slot == null || slot.IsEmpty;

            if (iconImage != null)
            {
                iconImage.enabled = !isEmpty;
                iconImage.sprite = isEmpty ? null : slot.Item.Icon;
            }

            if (quantityText != null)
            {
                if (!isEmpty && slot.Quantity > 1)
                {
                    // TMP SetText avoids the repeated string formatting pattern used by ToString.
                    quantityText.SetText("{0}", slot.Quantity);
                }
                else
                {
                    quantityText.SetText(string.Empty);
                }
            }

            if (emptyFrame != null)
            {
                emptyFrame.SetActive(isEmpty);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null)
            {
                selectedFrame.SetActive(selected);
            }
        }

        private void HandlePressed()
        {
            owner?.OnSlotPressed(slotIndex);
        }
    }
}
