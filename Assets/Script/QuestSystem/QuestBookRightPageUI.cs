using TMPro;
using UnityEngine;

namespace Dreamrift.QuestSystem
{
    /// <summary>
    /// Komponen Halaman Kanan Quest Book (Halaman Cerita & Lore Naratif).
    /// Menampilkan kisah mendalam misi atau prolog bab bergaya jurnal petualang kuno.
    /// </summary>
    public class QuestBookRightPageUI : MonoBehaviour
    {
        [Header("Story Header")]
        [SerializeField] private TextMeshProUGUI storyTitleText;
        [SerializeField] private TextMeshProUGUI chapterSubheadingText;

        [Header("Narrative Body (Scrollable)")]
        [SerializeField] private TextMeshProUGUI storyNarrativeText;
        [SerializeField] private TextMeshProUGUI adventurerNoteText;

        [Header("Decorations & Stamps")]
        [SerializeField] private GameObject completionStamp;

        [Header("Sealed State UI")]
        [SerializeField] private GameObject normalStoryContainer;
        [SerializeField] private GameObject sealedStoryContainer;
        [SerializeField] private TextMeshProUGUI sealedStoryTitleText;
        [SerializeField] private TextMeshProUGUI sealedStoryNoticeText;
        [SerializeField] private GameObject sealedStampGraphic; // Visual tanda tersegel

        public void DisplayStory(QuestChapterData chapter, int chapterIndex, QuestData selectedQuest, QuestState questState)
        {
            bool isSealed = chapter != null && chapter.IsSealed(QuestManager.Instance);

            if (isSealed)
            {
                if (normalStoryContainer != null) normalStoryContainer.SetActive(false);
                if (sealedStoryContainer != null) sealedStoryContainer.SetActive(true);

                if (sealedStoryTitleText != null) sealedStoryTitleText.text = $"Chapter {chapterIndex + 1}";
                if (sealedStoryNoticeText != null)
                {
                    sealedStoryNoticeText.text = "Lembaran perkamen ini terselubung oleh segel magis kuno.\n\nKisah petualangan, rahasia dunia, dan catatan kronik di halaman ini baru akan terungkap setelah kamu menuntaskan seluruh petualangan di bab sebelumnya...";
                }

                if (sealedStampGraphic != null) sealedStampGraphic.SetActive(true);
                if (completionStamp != null) completionStamp.SetActive(false);
                return;
            }

            if (normalStoryContainer != null) normalStoryContainer.SetActive(true);
            if (sealedStoryContainer != null) sealedStoryContainer.SetActive(false);
            if (sealedStampGraphic != null) sealedStampGraphic.SetActive(false);

            if (selectedQuest != null)
            {
                // Judul Kisah Misi
                if (storyTitleText != null)
                {
                    storyTitleText.text = selectedQuest.DisplayName;
                }

                // Sub-heading Chapter
                if (chapterSubheadingText != null)
                {
                    chapterSubheadingText.text = chapter != null ? chapter.ChapterTitle : "Kisah Misi";
                }

                // Teks Cerita / Narasi & Catatan Petualang Terpadu
                if (storyNarrativeText != null)
                {
                    if (!string.IsNullOrWhiteSpace(selectedQuest.Description) && selectedQuest.Description != selectedQuest.StoryText)
                    {
                        storyNarrativeText.text = $"{selectedQuest.StoryText}\n\n<size=88%><color=#4A2E1B><i>\"Catatan: {selectedQuest.Description}\"</i></color></size>";
                    }
                    else
                    {
                        storyNarrativeText.text = selectedQuest.StoryText;
                    }
                }

                if (adventurerNoteText != null)
                {
                    adventurerNoteText.gameObject.SetActive(false);
                }

                // Stempel Selesai
                if (completionStamp != null)
                {
                    completionStamp.SetActive(questState == QuestState.Complete);
                }
            }
            else
            {
                // Tampilkan Prologue Chapter umum jika belum ada misi terpilih
                if (storyTitleText != null)
                {
                    storyTitleText.text = chapter != null ? chapter.ChapterTitle : "Jurnal Petualang";
                }

                if (chapterSubheadingText != null)
                {
                    chapterSubheadingText.text = "Prolog Bab";
                }

                if (storyNarrativeText != null)
                {
                    storyNarrativeText.text = chapter != null && !string.IsNullOrWhiteSpace(chapter.ChapterPrologue)
                        ? chapter.ChapterPrologue
                        : "Buka lembaran bab dan pilih salah satu misi di halaman kiri untuk membaca kisah selengkapnya...";
                }

                if (adventurerNoteText != null)
                {
                    adventurerNoteText.gameObject.SetActive(false);
                }

                if (completionStamp != null)
                {
                    completionStamp.SetActive(false);
                }
            }
        }
    }
}
