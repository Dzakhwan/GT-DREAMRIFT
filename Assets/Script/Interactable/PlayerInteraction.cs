using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mode deteksi untuk menemukan objek interaktif di sekitar Player.
/// </summary>
public enum InteractionDetectionMode
{
    OverlapSphere = 0,
    RaycastPlayerForward = 1,
    RaycastMouseCursor = 2
}

/// <summary>
/// Pasang script ini ke GameObject Player.
/// Script ini mendeteksi semua objek yang mengimplementasikan IInteractable
/// (termasuk InteractableObject, CutsceneTrigger, QuestTrigger, ConversationStarter, ItemPickup, dll.) 
/// dan menampilkan tombol Interact di layar saat Player berada dalam jangkauan dan/atau menghadap objek.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Layer yang dipakai untuk mendeteksi objek interaktif")]
    [SerializeField] private LayerMask interactableLayer;

    [Tooltip("Radius pencarian objek interaktif di sekitar Player (atau jarak maksimal untuk Raycast)")]
    [SerializeField] private float detectionRadius = 3f;

    [Tooltip("Mode deteksi objek interaktif. Untuk game Isometric 3D dengan WASD/Joystick, disarankan RaycastPlayerForward.")]
    [SerializeField] private InteractionDetectionMode detectionMode = InteractionDetectionMode.RaycastPlayerForward;

    [Tooltip("Ketebalan sinar Raycast (0 = garis tipis, > 0 = SphereCast agar lebih mudah diposisikan)")]
    [SerializeField] private float raycastThickness = 0.5f;

    [Tooltip("Tinggi asal sinar dari posisi kaki Player (default 1.0 = tinggi dada)")]
    [SerializeField] private float raycastHeightOffset = 1.0f;

    [Header("UI References")]
    [Tooltip("Drag & Drop panel/GameObject tombol interaksi dari Hierarchy")]
    [SerializeField] private GameObject interactButtonPanel;

    [Tooltip("Drag & Drop komponen TextMeshPro untuk label tombol (contoh: teks 'Bicara')")]
    [SerializeField] private TextMeshProUGUI interactLabelText;

    [Tooltip("Drag & Drop komponen Button dari tombol interaksi")]
    [SerializeField] private Button interactButton;

    [Header("UI Positioning (World-To-Screen)")]
    [Tooltip("Jika true, tombol interaksi otomatis melayang di layar tepat di atas barang/NPC yang terdeteksi.")]
    [SerializeField] private bool followTargetOnScreen = true;

    [Tooltip("Ketinggian tambahan dari batas atas (top edge) collider objek 3D (default: 0.5 unit di atas objek)")]
    [SerializeField] private float uiWorldHeightOffset = 0.5f;

    [Tooltip("Offset piksel tambahan di layar (opsional)")]
    [SerializeField] private Vector2 uiScreenOffset = Vector2.zero;

    // Referensi ke objek interaktif (interface) yang saat ini paling dekat/terpilih
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

    private void LateUpdate()
    {
        UpdateInteractButtonPosition();
    }

    /// <summary>
    /// Setiap frame, cari objek IInteractable menggunakan mode deteksi yang dipilih.
    /// Mendukung semua implementasi IInteractable: InteractableObject, CutsceneTrigger, QuestTrigger, dll.
    /// </summary>
    private void DetectNearbyInteractable()
    {
        IInteractable closest = null;
        MonoBehaviour closestMB = null;

        switch (detectionMode)
        {
            case InteractionDetectionMode.OverlapSphere:
                DetectByOverlapSphere(out closest, out closestMB);
                break;
            case InteractionDetectionMode.RaycastPlayerForward:
                DetectByRaycastForward(out closest, out closestMB);
                break;
            case InteractionDetectionMode.RaycastMouseCursor:
                DetectByRaycastMouseCursor(out closest, out closestMB);
                break;
        }

        // Update tombol berdasarkan hasil pencarian
        if (closest != currentTarget)
        {
            currentTarget = closest;
            currentTargetMB = closestMB;
            UpdateInteractButton();
        }
    }

    private void DetectByOverlapSphere(out IInteractable closest, out MonoBehaviour closestMB)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayer);
        closest = null;
        closestMB = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            MonoBehaviour mb = hit.GetComponentInParent<MonoBehaviour>();
            if (mb == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            float range = GetInteractRange(mb);
            if (dist < closestDistance && dist <= range)
            {
                closestDistance = dist;
                closest = interactable;
                closestMB = mb;
            }
        }
    }

    private void DetectByRaycastForward(out IInteractable closest, out MonoBehaviour closestMB)
    {
        closest = null;
        closestMB = null;

        Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
        Vector3 direction = transform.forward;

        bool hitSomething;
        RaycastHit hit;

        if (raycastThickness > 0f)
        {
            hitSomething = Physics.SphereCast(origin, raycastThickness, direction, out hit, detectionRadius, interactableLayer);
        }
        else
        {
            hitSomething = Physics.Raycast(origin, direction, out hit, detectionRadius, interactableLayer);
        }

        if (hitSomething && hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            MonoBehaviour mb = hit.collider.GetComponentInParent<MonoBehaviour>();
            if (interactable != null && mb != null)
            {
                float dist = Vector3.Distance(transform.position, hit.collider.transform.position);
                float range = GetInteractRange(mb);
                if (dist <= range)
                {
                    closest = interactable;
                    closestMB = mb;
                }
            }
        }
    }

    private void DetectByRaycastMouseCursor(out IInteractable closest, out MonoBehaviour closestMB)
    {
        closest = null;
        closestMB = null;

        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            MonoBehaviour mb = hit.collider.GetComponentInParent<MonoBehaviour>();
            if (interactable != null && mb != null)
            {
                float dist = Vector3.Distance(transform.position, hit.collider.transform.position);
                float range = GetInteractRange(mb);
                if (dist <= range)
                {
                    closest = interactable;
                    closestMB = mb;
                }
            }
        }
    }

    /// <summary>
    /// Ambil nilai InteractRange dari MonoBehaviour yang mengimplementasikan IInteractable.
    /// Mendukung InteractableObject, CutsceneTrigger, QuestTrigger, ConversationStarter, dan ItemPickup.
    /// </summary>
    private float GetInteractRange(MonoBehaviour mb)
    {
        if (mb is InteractableObject io) return io.InteractRange;
        if (mb is CutsceneTrigger ct) return ct.InteractRange;
        if (mb is Dreamrift.QuestSystem.QuestTrigger qt) return qt.InteractRange;
        if (mb is ConversationStarter cs) return cs.InteractionRadius;
        if (mb is Dreamrift.InventorySystem.ItemPickup ip) return ip.InteractRange;
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

            // Segera update posisinya saat baru ditampilkan
            UpdateInteractButtonPosition();
        }
        else
        {
            // Sembunyikan tombol jika tidak ada target
            interactButtonPanel?.SetActive(false);
        }
    }

    /// <summary>
    /// Jika followTargetOnScreen aktif dan ada target, posisikan tombol UI
    /// tepat di atas barang/NPC tersebut menggunakan koordinat layar kamera.
    /// </summary>
    private void UpdateInteractButtonPosition()
    {
        if (!followTargetOnScreen || currentTarget == null || currentTargetMB == null || interactButtonPanel == null || Camera.main == null)
        {
            return;
        }

        RectTransform panelRect = interactButtonPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            Vector3 targetWorldPos = currentTargetMB.transform.position;

            // Cari batas atas dari Collider agar posisi tombol pas di atas objek kecil maupun NPC tinggi
            Collider col = currentTargetMB.GetComponent<Collider>();
            if (col == null)
                col = currentTargetMB.GetComponentInChildren<Collider>();
            if (col == null)
                col = currentTargetMB.GetComponentInParent<Collider>();

            if (col != null)
            {
                targetWorldPos = new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z);
            }

            Vector3 worldPos = targetWorldPos + Vector3.up * uiWorldHeightOffset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Pastikan objek ada di depan kamera (Z > 0)
            if (screenPos.z > 0)
            {
                panelRect.position = new Vector3(screenPos.x + uiScreenOffset.x, screenPos.y + uiScreenOffset.y, screenPos.z);
            }
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
    /// Visualisasi jangkauan dan arah deteksi di Scene View (hanya di Editor)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (detectionMode == InteractionDetectionMode.OverlapSphere)
        {
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
        else if (detectionMode == InteractionDetectionMode.RaycastPlayerForward)
        {
            Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
            Vector3 end = origin + transform.forward * detectionRadius;

            Gizmos.DrawLine(origin, end);
            if (raycastThickness > 0f)
            {
                Gizmos.DrawWireSphere(origin, raycastThickness);
                Gizmos.DrawWireSphere(end, raycastThickness);
            }
        }
    }
}
