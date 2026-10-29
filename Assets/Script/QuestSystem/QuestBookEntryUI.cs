using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamrift.QuestSystem
{
    /// <summary>
    /// Komponen tombol item misi dalam daftar halaman kiri Quest Book.
    /// Menampilkan judul misi, status badge, progres langsung (misal: 3/5 Slime), dan indikator seleksi.
    /// </summary>
    public class QuestBookEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image statusBadgeImage;
        [SerializeField] private GameObject selectionHighlight;

        [Header("Badge Colors")]
        [SerializeField] private Color activeColor = new Color(0.2f, 0.65f, 1f);      // Biru Aktif
        [SerializeField] private Color completeColor = new Color(0.25f, 0.78f, 0.35f); // Hijau Selesai
        [SerializeField] private Color availableColor = new Color(0.95f, 0.75f, 0.2f); // Emas Tersedia
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f);   // Abu-abu Terkunci

        private QuestData targetQuest;
        private Action<QuestData> onSelectedCallback;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClicked);
            }
        }

        public void Setup(QuestData quest, QuestState state, string progressStatus, bool isSelected, Action<QuestData> onSelected)
        {
            targetQuest = quest;
            onSelectedCallback = onSelected;

            if (quest == null) return;

            // Judul Misi
            if (titleText != null)
            {
                titleText.text = quest.DisplayName;
            }

            // Status Teks
            if (statusText != null)
            {
                switch (state)
                {
                    case QuestState.Active:
                        statusText.text = "Aktif";
                        break;
                    case QuestState.Complete:
                        statusText.text = "Selesai";
                        break;
                    case QuestState.Available:
                        statusText.text = "Tersedia";
                        break;
                    case QuestState.Locked:
                        statusText.text = "Terkunci";
                        break;
                    default:
                        statusText.text = state.ToString();
                        break;
                }
            }

            // Progres Misi Terintegrasi
            if (progressText != null)
            {
                if (state == QuestState.Active && !string.IsNullOrWhiteSpace(progressStatus))
                {
                    progressText.gameObject.SetActive(true);
                    progressText.text = progressStatus;
                }
                else
                {
                    progressText.gameObject.SetActive(false);
                }
            }

            // Badge Warna Status
            if (statusBadgeImage != null)
            {
                switch (state)
                {
                    case QuestState.Active:
                        statusBadgeImage.color = activeColor;
                        break;
                    case QuestState.Complete:
                        statusBadgeImage.color = completeColor;
                        break;
                    case QuestState.Available:
                        statusBadgeImage.color = availableColor;
                        break;
                    default:
                        statusBadgeImage.color = lockedColor;
                        break;
                }
            }

            // State Seleksi
            SetSelected(isSelected);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(isSelected);
            }
        }

        private void OnClicked()
        {
            if (targetQuest != null)
            {
                onSelectedCallback?.Invoke(targetQuest);
            }
        }
    }
}
