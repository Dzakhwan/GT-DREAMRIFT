using UnityEngine;
using UnityEngine.Events;

namespace Dreamrift.QuestSystem
{
    public enum QuestActionType
    {
        StartQuest = 0,
        CompleteQuest = 1,
        LogState = 2
    }

    /// <summary>
    /// Komponen serbaguna untuk memicu StartQuest, CompleteQuest, atau mengecek status Quest.
    /// Bisa digunakan lewat:
    ///   1. Interaksi Player (IInteractable) -> tombol interaksi
    ///   2. Trigger Collider (OnTriggerEnter) -> saat Player masuk area
    ///   3. UnityEvent (panggil public method StartAssignedQuest / CompleteAssignedQuest dari event lain)
    /// </summary>
    public class QuestTrigger : MonoBehaviour, IInteractable
    {
        [Header("Quest Target")]
        [SerializeField] private QuestData questData;
        [SerializeField] private QuestActionType actionType = QuestActionType.StartQuest;

        [Header("Interact Settings (IInteractable)")]
        [Tooltip("Aktifkan jika ingin dipicu melalui tombol interaksi Player")]
        [SerializeField] private bool interactable = true;
        [Tooltip("Teks yang tampil di tombol interaksi")]
        [SerializeField] private string interactLabel = "Ambil Quest";
        [Tooltip("Jarak maksimal player agar tombol interaksi muncul (dalam unit Unity)")]
        [SerializeField] private float interactRange = 2.5f;
        [Tooltip("Hanya bisa dipicu sekali")]
        [SerializeField] private bool oneTimeOnly = true;

        [Header("Collider Trigger Settings")]
        [Tooltip("Aktifkan jika ingin dipicu otomatis saat Player masuk ke area Collider (IsTrigger)")]
        [SerializeField] private bool triggerOnEnter = false;
        [SerializeField] private string playerTag = "Player";

        [Header("NPC Talk Quest Integration")]
        [Tooltip("Isi ID NPC jika interaksi ini merupakan quest 'Bicara dengan NPC' (misal: Elder, Blacksmith)")]
        [SerializeField] private string npcId = "";

        [Header("Events")]
        [SerializeField] private UnityEvent onQuestActionTriggered;

        private bool hasBeenUsed = false;

        // ── IInteractable Implementation ───────────────────────────────────────
        public string InteractLabel => interactLabel;
        public float InteractRange => interactRange;

        public void OnInteract()
        {
            if (!interactable) return;
            ExecuteQuestAction();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter) return;
            if (!other.CompareTag(playerTag)) return;

            ExecuteQuestAction();
        }

        /// <summary>
        /// Menjalankan aksi quest sesuai tipe yang dipilih (Start / Complete / Log).
        /// </summary>
        public void ExecuteQuestAction()
        {
            if (oneTimeOnly && hasBeenUsed) return;

            if (questData == null)
            {
                Debug.LogWarning($"[QuestTrigger - {gameObject.name}] QuestData belum di-assign!", this);
                return;
            }

            if (QuestManager.Instance == null)
            {
                Debug.LogError("[QuestTrigger] QuestManager.Instance tidak ditemukan di Scene!", this);
                return;
            }

            bool success = false;
            switch (actionType)
            {
                case QuestActionType.StartQuest:
                    success = QuestManager.Instance.StartQuest(questData);
                    break;
                case QuestActionType.CompleteQuest:
                    success = QuestManager.Instance.CompleteQuest(questData);
                    break;
                case QuestActionType.LogState:
                    LogAssignedQuestState();
                    success = true;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(npcId))
            {
                QuestManager.Instance.RecordNpcTalk(npcId);
            }

            if (success)
            {
                if (oneTimeOnly)
                {
                    hasBeenUsed = true;
                }
                onQuestActionTriggered?.Invoke();
            }
        }

        // ── Public Helper Methods (Bisa dipanggil dari UnityEvent / Script lain) ──

        /// <summary>
        /// Memulai quest secara langsung dari script lain atau UnityEvent.
        /// </summary>
        public void StartAssignedQuest()
        {
            if (questData == null || QuestManager.Instance == null) return;
            QuestManager.Instance.StartQuest(questData);
        }

        /// <summary>
        /// Menyelesaikan quest secara langsung dari script lain atau UnityEvent.
        /// </summary>
        public void CompleteAssignedQuest()
        {
            if (questData == null || QuestManager.Instance == null) return;
            QuestManager.Instance.CompleteQuest(questData);
        }

        /// <summary>
        /// Mengecek dan menampilkan log status quest saat ini ke Unity Console.
        /// </summary>
        public void LogAssignedQuestState()
        {
            if (questData == null || QuestManager.Instance == null) return;
            QuestState state = QuestManager.Instance.GetQuestState(questData);
            Debug.Log($"[QuestTrigger] Status dari quest '{questData.DisplayName}' ({questData.QuestId}) saat ini adalah: {state}");
        }

        /// <summary>
        /// Reset status trigger agar bisa dipicu kembali (jika oneTimeOnly aktif).
        /// </summary>
        public void ResetTrigger()
        {
            hasBeenUsed = false;
        }
    }
}
