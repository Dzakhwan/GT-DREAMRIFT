using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pasang script ini ke GameObject Player.
/// Script ini mendeteksi objek InteractableObject terdekat dan menampilkan
/// tombol Interact di layar saat Player berada dalam jangkauan.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Layer yang dipakai untuk mendeteksi objek interaktif")]
    [SerializeField] private LayerMask interactableLayer;

    [Tooltip("Radius pencarian objek interaktif di sekitar Player")]
    [SerializeField] private float detectionRadius = 3f;

    [Header("UI References")]
    [Tooltip("Drag & Drop panel/GameObject tombol interaksi dari Hierarchy")]
    [SerializeField] private GameObject interactButtonPanel;

    [Tooltip("Drag & Drop komponen TextMeshPro untuk label tombol (contoh: teks 'Bicara')")]
    [SerializeField] private TextMeshProUGUI interactLabelText;

    [Tooltip("Drag & Drop komponen Button dari tombol interaksi")]
    [SerializeField] private Button interactButton;

    // Referensi ke objek interaktif yang saat ini paling dekat
    private InteractableObject currentTarget;

    private void Start()
    {
        // Pastikan tombol tersembunyi di awal
        if (interactButtonPanel != null)
            interactButtonPanel.SetActive(false);

        // Daftarkan fungsi OnInteractButtonPressed ke tombol
        if (interactButton != null)
            interactButton.onClick.AddListener(OnInteractButtonPressed);
    }

    private void Update()
    {
        DetectNearbyInteractable();
    }

    /// <summary>
    /// Setiap frame, cari objek InteractableObject terdekat dalam radius deteksi.
    /// </summary>
    private void DetectNearbyInteractable()
    {
        // Cari semua collider dalam radius dan layer yang ditentukan
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayer);

        InteractableObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // Cek apakah collider punya komponen InteractableObject
            InteractableObject interactable = hit.GetComponent<InteractableObject>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);

            // Simpan yang paling dekat dan masih dalam jangkauan objek itu
            if (dist < closestDistance && dist <= interactable.InteractRange)
            {
                closestDistance = dist;
                closest = interactable;
            }
        }

        // Update tombol berdasarkan hasil pencarian
        if (closest != currentTarget)
        {
            currentTarget = closest;
            UpdateInteractButton();
        }
    }

    /// <summary>
    /// Tampilkan atau sembunyikan tombol berdasarkan apakah ada target yang terdeteksi.
    /// </summary>
    private void UpdateInteractButton()
    {
        if (currentTarget != null)
        {
            // Tampilkan tombol dan update labelnya
            interactButtonPanel?.SetActive(true);

            if (interactLabelText != null)
                interactLabelText.text = currentTarget.InteractLabel;
        }
        else
        {
            // Sembunyikan tombol jika tidak ada target
            interactButtonPanel?.SetActive(false);
        }
    }

    /// <summary>
    /// Dipanggil saat tombol Interact di layar ditekan.
    /// </summary>
    private void OnInteractButtonPressed()
    {
        if (currentTarget != null)
        {
            currentTarget.OnInteract();
        }
    }

    /// <summary>
    /// Visualisasi radius deteksi di Scene View (hanya di Editor)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
