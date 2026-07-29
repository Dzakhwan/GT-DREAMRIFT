using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pasang script ini ke objek 3D apapun yang ingin bisa di-interaksi Player.
/// Contoh penggunaan: Item di dunia, NPC, Chest, Lever, Pintu, dll.
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Interact Settings")]
    [Tooltip("Teks yang tampil di tombol interaksi. Contoh: 'Ambil', 'Bicara', 'Buka'")]
    [SerializeField] private string interactLabel = "Interaksi";

    [Tooltip("Jarak maksimal player agar tombol interaksi muncul (dalam unit Unity)")]
    [SerializeField] private float interactRange = 2.5f;

    [Header("Events")]
    [Tooltip("Event yang dipanggil saat Player berhasil berinteraksi")]
    [SerializeField] private UnityEvent onInteracted;

    [Header("Optional")]
    [Tooltip("Jika aktif, objek ini akan dinonaktifkan setelah di-interaksi")]
    [SerializeField] private bool disableAfterInteract = false;

    // Implementasi IInteractable
    public string InteractLabel => interactLabel;
    public float InteractRange => interactRange;

    /// <summary>
    /// Dipanggil oleh PlayerInteraction saat tombol Interact ditekan
    /// </summary>
    public void OnInteract()
    {
        // Jalankan semua event yang sudah di-assign di Inspector
        onInteracted?.Invoke();

        if (disableAfterInteract)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Visualisasi jangkauan interaksi di Scene View (hanya terlihat di Editor)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
