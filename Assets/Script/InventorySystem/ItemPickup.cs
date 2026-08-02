using UnityEngine;
using UnityEngine.Events;

namespace Dreamrift.InventorySystem
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Pickup")]
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int quantity = 1;
        [SerializeField] private bool destroyWhenPickedUp = true;

        [Header("Interaction (IInteractable)")]
        [Tooltip("Jika true, pemain harus menekan tombol interaksi UI untuk memungut barang. Jika false, barang otomatis terambil saat collider diinjak.")]
        [SerializeField] private bool pickupViaInteractButton = true;

        [Tooltip("Teks tombol UI yang muncul di layar saat dekat item ini")]
        [SerializeField] private string interactLabel = "Ambil Item";

        [Tooltip("Jarak maksimal pemain dari item untuk memunculkan tombol interaksi")]
        [SerializeField] private float interactRange = 2.5f;

        [Header("Filtering")]
        [SerializeField] private bool requirePlayerTag = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private InventoryManager inventoryManager;

        [Header("Audio & Visual Feedback")]
        [SerializeField] private AudioClip pickupSFX;
        [SerializeField] private GameObject pickupVFXPrefab;

        [Header("Events")]
        [SerializeField] private UnityEvent onPickedUp;
        [SerializeField] private UnityEvent onInventoryFull;

        private bool pickupConsumed;

        public string InteractLabel
        {
            get
            {
                if (!string.IsNullOrEmpty(interactLabel))
                    return interactLabel;

                return item != null ? $"Ambil {item.DisplayName}" : "Ambil Item";
            }
        }

        public float InteractRange => interactRange;

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
            if (pickupViaInteractButton)
            {
                // Jika diatur lewat tombol interaksi, jangan ambil secara otomatis
                return;
            }

            if (!CanPickupFrom(other))
            {
                return;
            }

            TryPickup();
        }

        /// <summary>
        /// Implementasi IInteractable. Dipanggil saat tombol interaksi di layar ditekan.
        /// </summary>
        public void OnInteract()
        {
            if (pickupViaInteractButton)
            {
                TryPickup();
            }
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

                if (pickupSFX != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
                }

                if (pickupVFXPrefab != null)
                {
                    Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);
                }

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
