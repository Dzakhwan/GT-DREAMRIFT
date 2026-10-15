using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handler pemicu cutscene terpadu yang mendukung berbagai mode pemicuan:
/// - SceneStart: Otomatis berputar saat scene selesai dimuat.
/// - ZoneEnter: Berputar saat Player memasuki area Trigger.
/// - BossDefeat: Berputar otomatis saat EnemyHealth target boss bernilai 0 / mati.
/// - Interact: Player menekan tombol interaksi pada objek 3D.
/// </summary>
public class CutsceneTriggerHandler : MonoBehaviour, IInteractable
{
    [Header("Cutscene Data")]
    [Tooltip("Data cutscene yang akan diputar saat pemicu aktif")]
    [SerializeField] private CutsceneData cutsceneData;

    [Header("Trigger Configuration")]
    [Tooltip("Tipe pemicu yang digunakan untuk mengaktifkan cutscene")]
    [SerializeField] private CutsceneTriggerType triggerType = CutsceneTriggerType.Interact;

    [Tooltip("Target EnemyHealth jika tipe pemicu adalah BossDefeat")]
    [SerializeField] private EnemyHealth targetBoss;

    [Tooltip("Tag GameObject Player untuk pemicu ZoneEnter (default: Player)")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Jeda waktu (detik) sebelum cutscene dimulai setelah pemicu aktif")]
    [SerializeField] private float delayBeforeStart = 0f;

    [Header("Interact Mode Settings")]
    [Tooltip("Teks label tombol interaksi (hanya berlaku pada mode Interact)")]
    [SerializeField] private string interactLabel = "Periksa";

    [Tooltip("Jarak maksimal interaksi dalam unit Unity (hanya berlaku pada mode Interact)")]
    [SerializeField] private float interactRange = 2.5f;

    [Header("Behavior")]
    [Tooltip("Jika aktif, cutscene hanya terpicu 1 kali saja")]
    [SerializeField] private bool oneTimeOnly = true;

    [Tooltip("Jika true dan oneTimeOnly aktif, objek akan dinonaktifkan setelah cutscene selesai")]
    [SerializeField] private bool disableAfterUse = false;

    [Header("Events")]
    [Tooltip("Event yang dipanggil sesaat sebelum cutscene dimulai")]
    public UnityEvent onBeforeCutscene;

    [Tooltip("Event yang dipanggil saat cutscene selesai")]
    public UnityEvent onAfterCutscene;

    // State
    private bool hasBeenTriggered = false;

    // IInteractable Interface Properties
    public string InteractLabel => interactLabel;
    public float InteractRange => interactRange;

    public CutsceneTriggerType TriggerType => triggerType;
    public CutsceneData CutsceneData => cutsceneData;

    private void Start()
    {
        // Hubungkan listener untuk BossDefeat jika dikonfigurasi
        if (triggerType == CutsceneTriggerType.BossDefeat && targetBoss != null)
        {
            targetBoss.OnDeath += OnBossDefeated;
        }

        // Pemicu otomatis SceneStart
        if (triggerType == CutsceneTriggerType.SceneStart)
        {
            TriggerCutscene();
        }
    }

    private void OnDestroy()
    {
        if (targetBoss != null)
        {
            targetBoss.OnDeath -= OnBossDefeated;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerType != CutsceneTriggerType.ZoneEnter) return;
        if (hasBeenTriggered && oneTimeOnly) return;

        if (other.CompareTag(playerTag))
        {
            TriggerCutscene();
        }
    }

    private void OnBossDefeated()
    {
        if (triggerType != CutsceneTriggerType.BossDefeat) return;
        if (hasBeenTriggered && oneTimeOnly) return;

        TriggerCutscene();
    }

    public void OnInteract()
    {
        if (triggerType != CutsceneTriggerType.Interact) return;
        if (hasBeenTriggered && oneTimeOnly) return;

        TriggerCutscene();
    }

    /// <summary>
    /// Mulai proses memicu cutscene dengan opsional delay.
    /// </summary>
    public void TriggerCutscene()
    {
        if (hasBeenTriggered && oneTimeOnly) return;
        if (cutsceneData == null)
        {
            Debug.LogWarning($"CutsceneTriggerHandler [{gameObject.name}]: CutsceneData belum di-assign!", this);
            return;
        }

        if (InGameCutsceneManager.Instance == null)
        {
            Debug.LogError("CutsceneTriggerHandler: InGameCutsceneManager tidak ditemukan di scene!", this);
            return;
        }

        if (oneTimeOnly)
        {
            hasBeenTriggered = true;
        }

        if (delayBeforeStart > 0f)
        {
            StartCoroutine(DelayedStart());
        }
        else
        {
            StartCutsceneNow();
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(delayBeforeStart);
        StartCutsceneNow();
    }

    private void StartCutsceneNow()
    {
        onBeforeCutscene?.Invoke();

        InGameCutsceneManager.Instance.onCutsceneFinished.AddListener(HandleCutsceneFinished);
        InGameCutsceneManager.Instance.PlayCutscene(cutsceneData);
    }

    private void HandleCutsceneFinished()
    {
        if (InGameCutsceneManager.Instance != null)
        {
            InGameCutsceneManager.Instance.onCutsceneFinished.RemoveListener(HandleCutsceneFinished);
        }

        onAfterCutscene?.Invoke();

        if (oneTimeOnly && disableAfterUse)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Reset pemicu agar bisa dipicu kembali.
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerType == CutsceneTriggerType.Interact)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
        else if (triggerType == CutsceneTriggerType.ZoneEnter)
        {
            Gizmos.color = Color.cyan;
            var col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
