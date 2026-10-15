using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamrift.QuestSystem
{
    public class QuestLogEntryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image statusBadgeImage;
        [SerializeField] private Button selectButton;

        [Header("Badge Colors")]
        [SerializeField] private Color activeColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color completeColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color notStartedColor = new Color(0.7f, 0.7f, 0.7f);

        private QuestData targetQuest;
        private Action<QuestData> onSelectedCallback;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClicked);
            }
        }

        public void Setup(QuestData quest, QuestState state, Action<QuestData> onSelected)
        {
            targetQuest = quest;
            onSelectedCallback = onSelected;

            if (quest == null) return;

            if (titleText != null)
            {
                titleText.text = quest.DisplayName;
            }

            if (statusText != null)
            {
                statusText.text = state.ToString();
            }

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
                    default:
                        statusBadgeImage.color = notStartedColor;
                        break;
                }
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
