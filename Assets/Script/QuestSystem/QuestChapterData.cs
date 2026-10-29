using System.Collections.Generic;
using UnityEngine;

namespace Dreamrift.QuestSystem
{
    [CreateAssetMenu(fileName = "New Chapter", menuName = "Dreamrift/Quest/Quest Chapter", order = 1)]
    public class QuestChapterData : ScriptableObject
    {
        [Header("Chapter Identity")]
        [SerializeField] private string chapterId = "Chapter_01";
        [SerializeField] private string chapterTitle = "Chapter 1: Valley Awakening";
        [SerializeField] private int chapterOrder = 1;

        [Header("Chapter Lore & Prologue")]
        [Tooltip("Teks cerita pengantar bab saat belum ada quest spesifik yang dipilih.")]
        [SerializeField, TextArea(6, 16)] private string chapterPrologue = "Dahulu kala, sebelum kabut menutupi lembah ini, para penjaga mimpi menjaga gerbang dimensi tetap seimbang...";

        [Header("Unlock & Seal Settings")]
        [Tooltip("Jika true, chapter ini terkunci/tersegel secara default sampai syarat terpenuhi.")]
        [SerializeField] private bool isSealedByDefault = false;
        [Tooltip("Chapter yang harus selesai terlebih dahulu untuk membuka segel bab ini.")]
        [SerializeField] private QuestChapterData prerequisiteChapter;

        [Header("Quests in this Chapter")]
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        public string ChapterId { get => chapterId; set => chapterId = value; }
        public string ChapterTitle { get => chapterTitle; set => chapterTitle = value; }
        public int ChapterOrder { get => chapterOrder; set => chapterOrder = value; }
        public string ChapterPrologue { get => chapterPrologue; set => chapterPrologue = value; }
        public bool IsSealedByDefault { get => isSealedByDefault; set => isSealedByDefault = value; }
        public QuestChapterData PrerequisiteChapter { get => prerequisiteChapter; set => prerequisiteChapter = value; }
        public IReadOnlyList<QuestData> Quests => quests;

        public bool IsSealed(QuestManager qm)
        {
            if (prerequisiteChapter != null && qm != null)
            {
                foreach (var q in prerequisiteChapter.Quests)
                {
                    if (q != null && qm.GetQuestState(q) != QuestState.Complete)
                    {
                        return true;
                    }
                }
                return false;
            }
            return isSealedByDefault;
        }

        public void AddQuest(QuestData quest)
        {
            if (quest != null && !quests.Contains(quest))
            {
                quests.Add(quest);
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                chapterId = name;
            }
        }
    }
}
