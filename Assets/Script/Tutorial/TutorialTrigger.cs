using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Area trigger untuk memicu tutorial.
/// Pasang script ini ke GameObject dengan Collider (IsTrigger) di scene.
///
/// Cara setup:
///   1. Buat GameObject kosong di scene
///   2. Tambahkan Collider (BoxCollider / SphereCollider)
///   3. Centang IsTrigger
///   4. Atur posisi dan ukuran Collider sesuai area tutorial
///   5. Set Layer ke "Trigger" atau layer yang berinteraksi dengan Player
///   6. Tambahkan TutorialTrigger.cs
///   7. Isi Title dan Description di Inspector
///   8. (Opsional) Hubungkan onTutorialTriggered ke script lain
///      Contoh: monster chase → drag GameObject monster, pilih method StartChase()
///
/// Saat Player masuk area, TutorialPanel.Instance.Show() akan dipanggil
/// dan onTutorialTriggered akan di-fire.
///
/// Untuk skenario "baca tutorial lalu dikejar monster":
///   - Set oneTimeOnly = true
///   - di onTutorialTriggered, hubungkan ke method spawn/activate monster
///   - Monster chase akan mulai BERSAMAAN panel tutorial muncul
///   - Jika ingin monster mulai setelah panel ditutup, gunakan TutorialPanel.onClose
///     (tidak di-inspector, perlu dari script lain dengan Subscribe)
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    [Header("Tutorial Content")]
    [Tooltip("Judul yang tampil di panel tutorial")]
    [SerializeField] private string title = "Tutorial";

    [Tooltip("Isi instruksi tutorial")]
    [SerializeField] [TextArea(3, 8)] private string description = "";

    [Header("Behavior")]
    [Tooltip("Hanya trigger sekali. Set false jika ingin tiap masuk area muncul panel lagi")]
    [SerializeField] private bool oneTimeOnly = true;

    [Tooltip("Tag yang digunakan untuk mendeteksi Player. Kosongkan untuk trigger semua objek")]
    [SerializeField] private string playerTag = "Player";

    [Header("Events")]
    [Tooltip("Dipanggil saat player masuk area trigger (setelah panel ditampilkan). " +
             "Gunakan untuk start quest, aktifkan monster chase, dll.")]
    [SerializeField] private UnityEvent onTutorialTriggered;

    private bool hasBeenTriggered = false;

    // Cached reference agar tidak GetComponent tiap frame
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (oneTimeOnly && hasBeenTriggered) return;

        // Filter tag jika diisi
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        TriggerTutorial();
    }

    /// <summary>
    /// Jalankan tutorial: tampilkan panel + fire event.
    /// Method public agar bisa dipanggil dari script lain jika perlu.
    /// </summary>
    public void TriggerTutorial()
    {
        if (hasBeenTriggered) return;
        hasBeenTriggered = true;

        // Cari TutorialPanel di scene
        if (TutorialPanel.Instance != null)
        {
            TutorialPanel.Instance.Show(title, description);
        }
        else
        {
            Debug.LogWarning($"[TutorialTrigger] TutorialPanel.Instance tidak ditemukan. " +
                             $"Pastikan ada TutorialPanel di scene.", this);
        }

        onTutorialTriggered?.Invoke();

        Debug.Log($"[TutorialTrigger] Tutorial '{title}' ditampilkan.", this);
    }

    /// <summary>
    /// Reset trigger agar bisa dipicu lagi (panggil dari external script jika perlu).
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);

        if (triggerCollider is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (triggerCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
    }
}
