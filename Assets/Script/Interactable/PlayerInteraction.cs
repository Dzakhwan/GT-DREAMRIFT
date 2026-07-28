using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pasang script ini ke GameObject Player.
/// Script ini mendeteksi semua objek yang mengimplementasikan IInteractable
/// (termasuk InteractableObject, CutsceneTrigger, dll.) dan menampilkan
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

    // Referensi ke objek interaktif (interface) yang saat ini paling dekat
    private IInteractable currentTarget;
    // Referensi ke MonoBehaviour dari currentTarget (untuk ambil transform/range)
    private MonoBehaviour currentTargetMB;

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
    /// Setiap frame, cari objek IInteractable terdekat dalam radius deteksi.
    /// Mendukung semua implementasi IInteractable: InteractableObject, CutsceneTrigger, dll.
    /// </summary>
    private void DetectNearbyInteractable()
    {
        // Cari semua collider dalam radius dan layer yang ditentukan
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayer);

        IInteractable closest = null;
        MonoBehaviour closestMB = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            // Cek apakah collider punya komponen yang mengimplementasikan IInteractable
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;

            // Ambil MonoBehaviour untuk mendapatkan InteractRange dan transform
            MonoBehaviour mb = hit.GetComponent<MonoBehaviour>();
            if (mb == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);

            // Cek jangkauan menggunakan InteractRange dari masing-masing implementasi
            float range = GetInteractRange(mb);
            if (dist < closestDistance && dist <= range)
            {
                closestDistance = dist;
                closest = interactable;
                closestMB = mb;
            }
        }

        // Update tombol berdasarkan hasil pencarian
        if (closest != currentTarget)
        {
            currentTarget = closest;
            currentTargetMB = closestMB;
            UpdateInteractButton();
        }
    }

    /// <summary>
    /// Ambil nilai InteractRange dari MonoBehaviour yang mengimplementasikan IInteractable.
    /// Mendukung InteractableObject dan CutsceneTrigger secara polymorphic.
    /// </summary>
    private float GetInteractRange(MonoBehaviour mb)
    {
        if (mb is InteractableObject io) return io.InteractRange;
        if (mb is CutsceneTrigger ct) return ct.InteractRange;
        if (mb is Dreamrift.QuestSystem.QuestTrigger qt) return qt.InteractRange;
        // Fallback: gunakan detectionRadius
        return detectionRadius;
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
