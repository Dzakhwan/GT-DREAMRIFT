using UnityEngine;
using UnityEngine.Events;

namespace Dreamrift.InventorySystem
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [Header("Pickup")]
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private bool destroyWhenPickedUp = true;

        [Header("Filtering")]
        [SerializeField] private bool requirePlayerTag = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private InventoryManager inventoryManager;

        [Header("Events")]
        [SerializeField] private UnityEvent onPickedUp;
        [SerializeField] private UnityEvent onInventoryFull;

        private bool pickupConsumed;

        private void Reset()
        {
            Collider pickupCollider = GetComponent<Collider>();
            pickupCollider.isTrigger = true;
        }

        private void OnValidate()
        {
            quantity = Mathf.Max(1, quantity);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanPickupFrom(other))
            {
                return;
            }

            TryPickup();
        }

        public void TryPickup()
        {
            if (pickupConsumed || item == null || quantity <= 0)
            {
                return;
            }

            InventoryManager targetInventory = inventoryManager != null ? inventoryManager : InventoryManager.Instance;
            if (targetInventory == null)
            {
                Debug.LogWarning("ItemPickup could not find an InventoryManager.", this);
                return;
            }

            int remaining = targetInventory.AddItem(item, quantity);
            if (remaining <= 0)
            {
                pickupConsumed = true;
                onPickedUp?.Invoke();

                if (destroyWhenPickedUp)
                {
                    Destroy(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            // Partial pickups keep the leftover amount in the world.
            quantity = remaining;
            onInventoryFull?.Invoke();
        }

        private bool CanPickupFrom(Collider other)
        {
            if (!requirePlayerTag)
            {
                return true;
            }

            return !string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag);
        }
    }
}
