using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Dreamrift.QuestSystem
{
    /// <summary>
    /// Komponen UI HUD untuk menampilkan isi misi (Judul & Deskripsi) yang sedang aktif di layar
    /// menggunakan TextMeshProUGUI. Otomatis membaca event dari QuestManager.
    /// </summary>
    public sealed class QuestTrackerUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Panel penampung UI Quest (opsional, bisa otomatis disembunyikan saat tidak ada misi)")]
        [SerializeField] private GameObject trackerPanel;

        [Tooltip("Komponen TextMeshProUGUI untuk judul misi (Quest DisplayName)")]
        [SerializeField] private TextMeshProUGUI questTitleText;

        [Tooltip("Komponen TextMeshProUGUI untuk deskripsi/tujuan misi (Quest Description)")]
        [SerializeField] private TextMeshProUGUI questDescriptionText;

        [Header("Display Settings")]
        [Tooltip("Jika true, panel akan disembunyikan saat tidak ada misi aktif.")]
        [SerializeField] private bool hideWhenNoActiveQuest = true;

        [Tooltip("Teks judul default saat tidak ada misi aktif (jika hideWhenNoActiveQuest = false)")]
        [SerializeField] private string noQuestTitle = "Misi";

        [Tooltip("Teks deskripsi default saat tidak ada misi aktif")]
        [SerializeField] private string noQuestDescription = "Tidak ada misi aktif.";

        [Header("Events")]
        [Tooltip("Event yang dipanggil saat ada misi baru yang muncul di UI (misal mainkan suara/animasi)")]
        [SerializeField] private UnityEvent onQuestTracked;

        [Tooltip("Event yang dipanggil saat misi selesai di UI")]
        [SerializeField] private UnityEvent onQuestCompletedUI;

        private void Start()
        {
            // Subscribe ke event QuestManager
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStarted += OnQuestStarted;
                QuestManager.Instance.QuestCompleted += OnQuestCompleted;

                // Jika sudah ada quest aktif sebelumnya (misal scene load), langsung tampilkan
                if (QuestManager.Instance.CurrentActiveQuest != null)
                {
                    DisplayQuest(QuestManager.Instance.CurrentActiveQuest);
                }
                else
                {
                    ClearQuestDisplay();
                }
            }
            else
            {
                Debug.LogWarning("[QuestTrackerUI] QuestManager.Instance tidak ditemukan pada Start().", this);
                ClearQuestDisplay();
            }
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.QuestStarted -= OnQuestStarted;
                QuestManager.Instance.QuestCompleted -= OnQuestCompleted;
            }
        }

        private void OnQuestStarted(QuestData quest)
        {
            if (quest != null)
            {
                DisplayQuest(quest);
                onQuestTracked?.Invoke();
            }
        }

        private void OnQuestCompleted(QuestData quest)
        {
            onQuestCompletedUI?.Invoke();

            // Jika quest yang selesai adalah quest aktif terakhir, hapus tampilan
            if (QuestManager.Instance == null || QuestManager.Instance.CurrentActiveQuest == null)
            {
                ClearQuestDisplay();
            }
            else
            {
                DisplayQuest(QuestManager.Instance.CurrentActiveQuest);
            }
        }

        /// <summary>
        /// Menampilkan isi quest (Judul & Deskripsi) ke komponen TextMeshProUGUI.
        /// </summary>
        public void DisplayQuest(QuestData quest)
        {
            if (quest == null)
            {
                ClearQuestDisplay();
                return;
            }

            if (trackerPanel != null && !trackerPanel.activeSelf)
            {
                trackerPanel.SetActive(true);
            }

            if (questTitleText != null)
            {
                questTitleText.text = quest.DisplayName;
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.text = quest.Description;
            }
        }

        /// <summary>
        /// Mereset tampilan misi ke kondisi kosong atau menyembunyikan panel.
        /// </summary>
        public void ClearQuestDisplay()
        {
            if (hideWhenNoActiveQuest)
            {
                if (trackerPanel != null)
                {
                    trackerPanel.SetActive(false);
                }
            }
            else
            {
                if (trackerPanel != null && !trackerPanel.activeSelf)
                {
                    trackerPanel.SetActive(true);
                }

                if (questTitleText != null)
                {
                    questTitleText.text = noQuestTitle;
                }

                if (questDescriptionText != null)
                {
                    questDescriptionText.text = noQuestDescription;
                }
            }
        }
    }
}
