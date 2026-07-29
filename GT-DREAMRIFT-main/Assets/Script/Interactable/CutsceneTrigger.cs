using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pasang script ini ke objek 3D yang ingin memicu cutscene (buku, chest, papan tulis, dll.)
/// Script ini mengimplementasikan IInteractable sehingga langsung terdeteksi oleh PlayerInteraction.
/// 
/// Cara setup:
///   1. Pasang CutsceneTrigger ke GameObject objek (buku/chest/dll.)
///   2. Pastikan objek punya Collider (boleh IsTrigger atau tidak)
///   3. Set Layer ke layer yang didefinisikan di PlayerInteraction.interactableLayer
///   4. Assign CutsceneData di field 'cutsceneData'
///   5. Pastikan ada InGameCutsceneManager di scene
/// </summary>
public class CutsceneTrigger : MonoBehaviour, IInteractable
{
    [Header("Interact Settings")]
    [Tooltip("Teks yang tampil di tombol interaksi. Contoh: 'Baca', 'Buka', 'Periksa'")]
    [SerializeField] private string interactLabel = "Periksa";

    [Tooltip("Jarak maksimal player agar tombol interaksi muncul (dalam unit Unity)")]
    [SerializeField] private float interactRange = 2.5f;

    [Header("Cutscene")]
    [Tooltip("Data cutscene yang akan diputar saat objek ini di-interaksi")]
    [SerializeField] private CutsceneData cutsceneData;

    [Header("Behavior")]
    [Tooltip("Jika aktif, objek tidak bisa di-interaksi lagi setelah cutscene pertama selesai")]
    [SerializeField] private bool oneTimeOnly = true;

    [Tooltip("Jika true dan oneTimeOnly aktif, objek akan dinonaktifkan setelah digunakan")]
    [SerializeField] private bool disableAfterUse = false;

    [Header("Events")]
    [Tooltip("Event tambahan yang dipanggil sesaat sebelum cutscene dimulai")]
    [SerializeField] private UnityEvent onBeforeCutscene;

    [Tooltip("Event yang dipanggil saat cutscene selesai (diforward dari InGameCutsceneManager)")]
    [SerializeField] private UnityEvent onAfterCutscene;

    // ── State ──────────────────────────────────────────────────────────────
    private bool hasBeenUsed = false;

    // ── IInteractable Implementation ───────────────────────────────────────
    public string InteractLabel => interactLabel;
    public float InteractRange => interactRange;

    /// <summary>
    /// Dipanggil oleh PlayerInteraction saat tombol Interact ditekan.
    /// </summary>
    public void OnInteract()
    {
        // Cek apakah sudah pernah digunakan (oneTimeOnly)
        if (oneTimeOnly && hasBeenUsed) return;

        // Cek apakah CutsceneData sudah di-assign
        if (cutsceneData == null)
        {
            Debug.LogWarning($"CutsceneTrigger [{gameObject.name}]: CutsceneData belum di-assign!", this);
            return;
        }

        // Cek apakah InGameCutsceneManager tersedia di scene
        if (InGameCutsceneManager.Instance == null)
        {
            Debug.LogError("CutsceneTrigger: InGameCutsceneManager tidak ditemukan di scene! " +
                           "Pastikan ada GameObject dengan script InGameCutsceneManager.", this);
            return;
        }

        // Tandai sudah digunakan
        if (oneTimeOnly)
        {
            hasBeenUsed = true;
        }

        // Jalankan event sebelum cutscene
        onBeforeCutscene?.Invoke();

        // Daftarkan callback "setelah cutscene selesai" ke manager
        InGameCutsceneManager.Instance.onCutsceneFinished.AddListener(HandleCutsceneFinished);

        // Mulai cutscene!
        InGameCutsceneManager.Instance.PlayCutscene(cutsceneData);
    }

    /// <summary>
    /// Dipanggil oleh InGameCutsceneManager saat cutscene selesai.
    /// </summary>
    private void HandleCutsceneFinished()
    {
        // Hapus listener agar tidak terpanggil lagi oleh cutscene lain
        InGameCutsceneManager.Instance.onCutsceneFinished.RemoveListener(HandleCutsceneFinished);

        // Jalankan event setelah cutscene
        onAfterCutscene?.Invoke();

        // Nonaktifkan objek jika dikonfigurasi
        if (oneTimeOnly && disableAfterUse)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Reset status agar bisa digunakan lagi (panggil dari script lain jika perlu).
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenUsed = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Visualisasi jangkauan interaksi di Scene View (hanya di Editor).
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
