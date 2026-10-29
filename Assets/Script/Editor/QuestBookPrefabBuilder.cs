using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Dreamrift.QuestSystem;

namespace Dreamrift.QuestSystem.Editor
{
    public static class QuestBookPrefabBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/UI";
        private const string EntryPrefabPath = "Assets/Prefabs/UI/QuestBookEntry.prefab";
        private const string BookPanelPrefabPath = "Assets/Prefabs/UI/QuestBookPanel.prefab";

        [MenuItem("Tools/Quest System/Generate Quest Book Prefabs")]
        public static void GenerateAll()
        {
            EnsureDirectory(PrefabDir);
            EnsureSampleChapters();

            TMP_FontAsset irishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/IrishGrover-Regular SDF.asset");
            TMP_FontAsset bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (bodyFont == null) bodyFont = irishFont;

            // 1. Build Entry Prefab
            GameObject entryPrefab = BuildEntryPrefab(irishFont, bodyFont);

            // 2. Build Book Panel Prefab
            BuildBookPanelPrefab(entryPrefab.GetComponent<QuestBookEntryUI>(), irishFont, bodyFont);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#4ADE80><b>[QuestBook]</b> Prefabs successfully generated in Assets/Prefabs/UI/!</color>");
        }

        [MenuItem("Tools/Quest System/Setup Quest Book in Current Scene")]
        public static void SetupInActiveScene()
        {
            GenerateAll();

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[QuestBook] Canvas not found in active scene!");
                return;
            }

            // Check if QuestBookPanel already in scene, destroy to re-instantiate latest prefab
            QuestBookUI existing = canvas.GetComponentInChildren<QuestBookUI>(true);
            if (existing != null)
            {
                Debug.Log("[QuestBook] Replacing existing QuestBookUI instance: " + existing.gameObject.name);
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            // Instantiate QuestBookPanel prefab
            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BookPanelPrefabPath);
            if (panelPrefab == null)
            {
                Debug.LogError("[QuestBook] Book Panel Prefab not found at " + BookPanelPrefabPath);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvas.transform);
            instance.name = "QuestBook_System";
            instance.SetActive(true);

            // Add HUD Toggle Button next to Inventory Bag button
            CreateHudButton(canvas, instance.GetComponent<QuestBookUI>());

            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("<color=#38BDF8><b>[QuestBook]</b> Successfully integrated Quest Book into scene " + canvas.gameObject.scene.name + "!</color>");
        }

        private static GameObject BuildEntryPrefab(TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            GameObject go = new GameObject("QuestBookEntry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Outline));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 58f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = 58f;
            le.preferredHeight = 58f;
            le.flexibleWidth = 1f;

            Outline ol = go.GetComponent<Outline>();
            ol.effectColor = new Color(0.60f, 0.45f, 0.30f, 0.40f);
            ol.effectDistance = new Vector2(1f, -1f);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.96f, 0.92f, 0.84f, 0.92f); // Kertas perkamen lembut dan jelas

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 1f);
            cb.highlightedColor = new Color(1f, 0.97f, 0.90f, 1f);
            cb.pressedColor = new Color(0.88f, 0.82f, 0.72f, 1f);
            btn.colors = cb;

            // Highlight border
            GameObject highlight = new GameObject("SelectionHighlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            highlight.transform.SetParent(go.transform, false);
            RectTransform hRt = highlight.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.sizeDelta = Vector2.zero;
            Image hImg = highlight.GetComponent<Image>();
            hImg.color = new Color(0.85f, 0.65f, 0.25f, 0.35f); // Emas border highlight
            highlight.SetActive(false);

            // Status Badge Dot / Strip
            GameObject badge = new GameObject("StatusBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badge.transform.SetParent(go.transform, false);
            RectTransform bRt = badge.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0.5f);
            bRt.anchorMax = new Vector2(0f, 0.5f);
            bRt.anchoredPosition = new Vector2(16f, 0f);
            bRt.sizeDelta = new Vector2(14f, 14f);
            Image bImg = badge.GetComponent<Image>();
            bImg.color = new Color(0.2f, 0.65f, 1f);

            // Quest Title Text
            GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(go.transform, false);
            RectTransform tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.offsetMin = new Vector2(34f, 0f);
            tRt.offsetMax = new Vector2(-75f, -6f);
            TextMeshProUGUI tText = titleGo.GetComponent<TextMeshProUGUI>();
            tText.font = titleFont;
            tText.fontSize = 17f;
            tText.fontStyle = FontStyles.Bold;
            tText.color = new Color(0.20f, 0.12f, 0.08f); // Cokelat tua pekat
            tText.text = "Judul Misi Petualang";
            tText.raycastTarget = false;

            // Progress / Status detail under title
            GameObject progGo = new GameObject("ProgressText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            progGo.transform.SetParent(go.transform, false);
            RectTransform pRt = progGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(1f, 0.5f);
            pRt.offsetMin = new Vector2(34f, 6f);
            pRt.offsetMax = new Vector2(-75f, 0f);
            TextMeshProUGUI pText = progGo.GetComponent<TextMeshProUGUI>();
            pText.font = bodyFont;
            pText.fontSize = 13f;
            pText.color = new Color(0.12f, 0.35f, 0.65f); // Biru petualang
            pText.text = "Objektif: 0 / 1";
            pText.raycastTarget = false;

            // Status Tag Text (Right aligned)
            GameObject statGo = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            statGo.transform.SetParent(go.transform, false);
            RectTransform sRt = statGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(1f, 0f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = new Vector2(-70f, 0f);
            sRt.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI sText = statGo.GetComponent<TextMeshProUGUI>();
            sText.font = bodyFont;
            sText.fontSize = 12f;
            sText.alignment = TextAlignmentOptions.Right;
            sText.color = new Color(0.45f, 0.35f, 0.25f);
            sText.text = "Aktif";
            sText.raycastTarget = false;

            // Wire up QuestBookEntryUI
            QuestBookEntryUI entryUI = go.AddComponent<QuestBookEntryUI>();
            SerializedObject so = new SerializedObject(entryUI);
            so.FindProperty("selectButton").objectReferenceValue = btn;
            so.FindProperty("titleText").objectReferenceValue = tText;
            so.FindProperty("statusText").objectReferenceValue = sText;
            so.FindProperty("progressText").objectReferenceValue = pText;
            so.FindProperty("statusBadgeImage").objectReferenceValue = bImg;
            so.FindProperty("selectionHighlight").objectReferenceValue = highlight;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, EntryPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildBookPanelPrefab(QuestBookEntryUI entryPrefab, TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            GameObject systemRoot = new GameObject("QuestBook_System", typeof(RectTransform));
            RectTransform sysRt = systemRoot.GetComponent<RectTransform>();
            sysRt.anchorMin = Vector2.zero;
            sysRt.anchorMax = Vector2.one;
            sysRt.sizeDelta = Vector2.zero;

            GameObject panelRoot = new GameObject("QuestBook_Panel", typeof(RectTransform));
            panelRoot.transform.SetParent(systemRoot.transform, false);
            RectTransform rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // 1. Dark Backdrop Dimmer
            GameObject dimmer = new GameObject("DimmerBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dimmer.transform.SetParent(panelRoot.transform, false);
            RectTransform dimRt = dimmer.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.sizeDelta = Vector2.zero;
            Image dimImg = dimmer.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.65f);

            // 2. Book Frame Container (16:9 Aspect Ratio)
            GameObject bookObj = new GameObject("BookContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UIFrameAnimation), typeof(AspectRatioFitter));
            bookObj.transform.SetParent(panelRoot.transform, false);
            RectTransform bookRt = bookObj.GetComponent<RectTransform>();
            bookRt.anchorMin = new Vector2(0.5f, 0.5f);
            bookRt.anchorMax = new Vector2(0.5f, 0.5f);
            bookRt.anchoredPosition = Vector2.zero;
            bookRt.sizeDelta = new Vector2(1360f, 765f); // 16:9 proporsi besar dan megah

            AspectRatioFitter arf = bookObj.GetComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 16f / 9f;

            // Load all 19 frame sprites
            Sprite[] frames = new Sprite[19];
            for (int i = 0; i <= 18; i++)
            {
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/UI QUEST BOOK/Timeline 1_{i:D4}.png");
            }

            Image bookImg = bookObj.GetComponent<Image>();
            bookImg.sprite = frames[18]; // Default show open book in editor preview

            UIFrameAnimation anim = bookObj.GetComponent<UIFrameAnimation>();
            SerializedObject animSo = new SerializedObject(anim);
            animSo.FindProperty("targetImage").objectReferenceValue = bookImg;
            animSo.FindProperty("framesPerSecond").floatValue = 24f;
            SerializedProperty framesProp = animSo.FindProperty("frames");
            framesProp.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
            animSo.ApplyModifiedProperties();

            // 3. Pages Content CanvasGroup (fades in when book opens)
            GameObject pagesGroup = new GameObject("PagesGroup", typeof(RectTransform), typeof(CanvasGroup));
            pagesGroup.transform.SetParent(bookObj.transform, false);
            RectTransform pagesRt = pagesGroup.GetComponent<RectTransform>();
            pagesRt.anchorMin = Vector2.zero;
            pagesRt.anchorMax = Vector2.one;
            pagesRt.sizeDelta = Vector2.zero;
            CanvasGroup cg = pagesGroup.GetComponent<CanvasGroup>();

            // --- LEFT PAGE ---
            GameObject leftPageGo = new GameObject("LeftPage_Missions", typeof(RectTransform), typeof(QuestBookLeftPageUI));
            leftPageGo.transform.SetParent(pagesGroup.transform, false);
            RectTransform lpRt = leftPageGo.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.08f, 0.10f);
            lpRt.anchorMax = new Vector2(0.47f, 0.88f);
            lpRt.offsetMin = Vector2.zero;
            lpRt.offsetMax = Vector2.zero;

            // Chapter Header bar
            GameObject chapHeader = new GameObject("ChapterHeader", typeof(RectTransform));
            chapHeader.transform.SetParent(leftPageGo.transform, false);
            RectTransform chRt = chapHeader.GetComponent<RectTransform>();
            chRt.anchorMin = new Vector2(0f, 1f);
            chRt.anchorMax = new Vector2(1f, 1f);
            chRt.anchoredPosition = new Vector2(0f, -24f);
            chRt.sizeDelta = new Vector2(0f, 48f);

            // Prev Button (<)
            GameObject prevBtnGo = new GameObject("PrevButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            prevBtnGo.transform.SetParent(chapHeader.transform, false);
            RectTransform prevRt = prevBtnGo.GetComponent<RectTransform>();
            prevRt.anchorMin = new Vector2(0f, 0.5f);
            prevRt.anchorMax = new Vector2(0f, 0.5f);
            prevRt.anchoredPosition = new Vector2(16f, 0f);
            prevRt.sizeDelta = new Vector2(36f, 36f);
            prevBtnGo.GetComponent<Image>().color = new Color(0.85f, 0.78f, 0.65f, 0.8f);
            GameObject prevTxt = CreateSimpleText(prevBtnGo.transform, "<", titleFont, 22f, FontStyles.Bold, new Color(0.2f, 0.1f, 0.05f));

            // Next Button (>)
            GameObject nextBtnGo = new GameObject("NextButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            nextBtnGo.transform.SetParent(chapHeader.transform, false);
            RectTransform nextRt = nextBtnGo.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(1f, 0.5f);
            nextRt.anchorMax = new Vector2(1f, 0.5f);
            nextRt.anchoredPosition = new Vector2(-16f, 0f);
            nextRt.sizeDelta = new Vector2(36f, 36f);
            nextBtnGo.GetComponent<Image>().color = new Color(0.85f, 0.78f, 0.65f, 0.8f);
            GameObject nextTxt = CreateSimpleText(nextBtnGo.transform, ">", titleFont, 22f, FontStyles.Bold, new Color(0.2f, 0.1f, 0.05f));

            // Chapter Title Text
            GameObject chapTitleGo = new GameObject("ChapterTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            chapTitleGo.transform.SetParent(chapHeader.transform, false);
            RectTransform ctRt = chapTitleGo.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = new Vector2(48f, 0f);
            ctRt.offsetMax = new Vector2(-48f, 0f);
            TextMeshProUGUI ctText = chapTitleGo.GetComponent<TextMeshProUGUI>();
            ctText.font = titleFont;
            ctText.fontSize = 21f;
            ctText.fontStyle = FontStyles.Bold;
            ctText.alignment = TextAlignmentOptions.Center;
            ctText.color = new Color(0.28f, 0.16f, 0.08f);
            ctText.text = "Chapter 1: Lembah Mimpi";

            // Chapter Counter
            GameObject counterGo = new GameObject("ChapterCounterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            counterGo.transform.SetParent(leftPageGo.transform, false);
            RectTransform cntRt = counterGo.GetComponent<RectTransform>();
            cntRt.anchorMin = new Vector2(0f, 1f);
            cntRt.anchorMax = new Vector2(1f, 1f);
            cntRt.anchoredPosition = new Vector2(0f, -54f);
            cntRt.sizeDelta = new Vector2(0f, 20f);
            TextMeshProUGUI cntText = counterGo.GetComponent<TextMeshProUGUI>();
            cntText.font = bodyFont;
            cntText.fontSize = 12f;
            cntText.alignment = TextAlignmentOptions.Center;
            cntText.color = new Color(0.55f, 0.42f, 0.30f);
            cntText.text = "Bab 1 / 1";

            // Divider Line
            GameObject divider = new GameObject("DividerLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(leftPageGo.transform, false);
            RectTransform divRt = divider.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.05f, 1f);
            divRt.anchorMax = new Vector2(0.95f, 1f);
            divRt.anchoredPosition = new Vector2(0f, -70f);
            divRt.sizeDelta = new Vector2(0f, 2f);
            divider.GetComponent<Image>().color = new Color(0.6f, 0.45f, 0.3f, 0.45f);

            // Normal Content Container (Holds QuestScrollView & TrackQuestButton)
            GameObject normalContent = new GameObject("NormalContentContainer", typeof(RectTransform));
            normalContent.transform.SetParent(leftPageGo.transform, false);
            RectTransform ncRt = normalContent.GetComponent<RectTransform>();
            ncRt.anchorMin = Vector2.zero;
            ncRt.anchorMax = Vector2.one;
            ncRt.offsetMin = Vector2.zero;
            ncRt.offsetMax = Vector2.zero;

            // Scroll View for Quests
            GameObject scrollGo = new GameObject("QuestScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(normalContent.transform, false);
            RectTransform sRt = scrollGo.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(0f, 55f); // Leave room for Track Button at bottom
            sRt.offsetMax = new Vector2(0f, -78f);

            ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            sr.viewport = vpRt;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.sizeDelta = new Vector2(0f, 0f);
            sr.content = cRt;

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Track Button at Bottom
            GameObject trackBtnGo = new GameObject("TrackQuestButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            trackBtnGo.transform.SetParent(normalContent.transform, false);
            RectTransform trRt = trackBtnGo.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0.5f, 0f);
            trRt.anchorMax = new Vector2(0.5f, 0f);
            trRt.anchoredPosition = new Vector2(0f, 24f);
            trRt.sizeDelta = new Vector2(280f, 42f);
            Image trImg = trackBtnGo.GetComponent<Image>();
            trImg.color = new Color(0.25f, 0.16f, 0.10f); // Cokelat gelap vintage
            GameObject trTextGo = CreateSimpleText(trackBtnGo.transform, "Lacak Misi Ini", titleFont, 16f, FontStyles.Bold, new Color(0.95f, 0.90f, 0.80f));

            // Sealed State Container for Left Page
            GameObject sealedLeftGo = new GameObject("SealedContainer", typeof(RectTransform));
            sealedLeftGo.transform.SetParent(leftPageGo.transform, false);
            RectTransform slRt = sealedLeftGo.GetComponent<RectTransform>();
            slRt.anchorMin = Vector2.zero;
            slRt.anchorMax = Vector2.one;
            slRt.offsetMin = new Vector2(15f, 20f);
            slRt.offsetMax = new Vector2(-15f, -75f);

            // Seal Icon Graphic
            GameObject sealIconGo = new GameObject("SealIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            sealIconGo.transform.SetParent(sealedLeftGo.transform, false);
            RectTransform siRt = sealIconGo.GetComponent<RectTransform>();
            siRt.anchorMin = new Vector2(0.5f, 0.5f);
            siRt.anchorMax = new Vector2(0.5f, 0.5f);
            siRt.anchoredPosition = new Vector2(0f, 40f);
            siRt.sizeDelta = new Vector2(80f, 80f);
            Image siImg = sealIconGo.GetComponent<Image>();
            siImg.color = new Color(0.68f, 0.16f, 0.14f, 0.92f); // Merah stempel lilin pekat
            Outline siOl = sealIconGo.GetComponent<Outline>();
            siOl.effectColor = new Color(0.35f, 0.08f, 0.05f, 0.8f);
            siOl.effectDistance = new Vector2(2f, -2f);
            CreateSimpleText(sealIconGo.transform, "X", titleFont, 44f, FontStyles.Bold, new Color(0.96f, 0.90f, 0.76f));

            // Seal Title
            GameObject sealTitleGo = new GameObject("SealTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sealTitleGo.transform.SetParent(sealedLeftGo.transform, false);
            RectTransform stlRt = sealTitleGo.GetComponent<RectTransform>();
            stlRt.anchorMin = new Vector2(0.5f, 0.5f);
            stlRt.anchorMax = new Vector2(0.5f, 0.5f);
            stlRt.anchoredPosition = new Vector2(0f, -20f);
            stlRt.sizeDelta = new Vector2(300f, 34f);
            TextMeshProUGUI stlText = sealTitleGo.GetComponent<TextMeshProUGUI>();
            stlText.font = titleFont;
            stlText.fontSize = 20f;
            stlText.fontStyle = FontStyles.Bold;
            stlText.alignment = TextAlignmentOptions.Center;
            stlText.color = new Color(0.35f, 0.12f, 0.08f);
            stlText.text = "BAB TERSEGEL";

            // Sealed Notice Text
            GameObject sealNoticeGo = new GameObject("SealedNoticeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sealNoticeGo.transform.SetParent(sealedLeftGo.transform, false);
            RectTransform snRt = sealNoticeGo.GetComponent<RectTransform>();
            snRt.anchorMin = new Vector2(0.5f, 0.5f);
            snRt.anchorMax = new Vector2(0.5f, 0.5f);
            snRt.anchoredPosition = new Vector2(0f, -85f);
            snRt.sizeDelta = new Vector2(320f, 110f);
            TextMeshProUGUI snText = sealNoticeGo.GetComponent<TextMeshProUGUI>();
            snText.font = bodyFont;
            snText.fontSize = 14f;
            snText.alignment = TextAlignmentOptions.Center;
            snText.color = new Color(0.40f, 0.25f, 0.15f);
            snText.enableWordWrapping = true;
            snText.text = "Bab ini masih terkunci oleh segel misterius.\n\nSelesaikan seluruh petualangan pada bab sebelumnya untuk membuka segel ini.";
            sealedLeftGo.SetActive(false);

            // Hook up QuestBookLeftPageUI
            QuestBookLeftPageUI leftUI = leftPageGo.GetComponent<QuestBookLeftPageUI>();
            SerializedObject leftSo = new SerializedObject(leftUI);
            leftSo.FindProperty("chapterTitleText").objectReferenceValue = ctText;
            leftSo.FindProperty("chapterCounterText").objectReferenceValue = cntText;
            leftSo.FindProperty("prevChapterButton").objectReferenceValue = prevBtnGo.GetComponent<Button>();
            leftSo.FindProperty("nextChapterButton").objectReferenceValue = nextBtnGo.GetComponent<Button>();
            leftSo.FindProperty("questListContainer").objectReferenceValue = cRt;
            leftSo.FindProperty("entryPrefab").objectReferenceValue = entryPrefab;
            leftSo.FindProperty("trackQuestButton").objectReferenceValue = trackBtnGo.GetComponent<Button>();
            leftSo.FindProperty("trackButtonText").objectReferenceValue = trTextGo.GetComponent<TextMeshProUGUI>();
            leftSo.FindProperty("normalContentContainer").objectReferenceValue = normalContent;
            leftSo.FindProperty("sealedContainer").objectReferenceValue = sealedLeftGo;
            leftSo.FindProperty("sealedNoticeText").objectReferenceValue = snText;
            leftSo.ApplyModifiedProperties();

            // --- RIGHT PAGE (STORY & LORE) ---
            GameObject rightPageGo = new GameObject("RightPage_Story", typeof(RectTransform), typeof(QuestBookRightPageUI));
            rightPageGo.transform.SetParent(pagesGroup.transform, false);
            RectTransform rpRt = rightPageGo.GetComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.53f, 0.10f);
            rpRt.anchorMax = new Vector2(0.92f, 0.88f);
            rpRt.offsetMin = Vector2.zero;
            rpRt.offsetMax = Vector2.zero;

            // Normal Story Container
            GameObject normalStoryGo = new GameObject("NormalStoryContainer", typeof(RectTransform));
            normalStoryGo.transform.SetParent(rightPageGo.transform, false);
            RectTransform nsRt = normalStoryGo.GetComponent<RectTransform>();
            nsRt.anchorMin = Vector2.zero;
            nsRt.anchorMax = Vector2.one;
            nsRt.offsetMin = Vector2.zero;
            nsRt.offsetMax = Vector2.zero;

            // Story Title Text
            GameObject storyTitleGo = new GameObject("StoryTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            storyTitleGo.transform.SetParent(normalStoryGo.transform, false);
            RectTransform stRt = storyTitleGo.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 1f);
            stRt.anchorMax = new Vector2(1f, 1f);
            stRt.anchoredPosition = new Vector2(0f, -24f);
            stRt.sizeDelta = new Vector2(0f, 40f);
            TextMeshProUGUI stText = storyTitleGo.GetComponent<TextMeshProUGUI>();
            stText.font = titleFont;
            stText.fontSize = 24f;
            stText.fontStyle = FontStyles.Bold;
            stText.alignment = TextAlignmentOptions.Center;
            stText.color = new Color(0.24f, 0.12f, 0.06f);
            stText.text = "Kisah di Balik Kabut";

            // Story Subheading
            GameObject subGo = new GameObject("ChapterSubheadingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            subGo.transform.SetParent(normalStoryGo.transform, false);
            RectTransform shRt = subGo.GetComponent<RectTransform>();
            shRt.anchorMin = new Vector2(0f, 1f);
            shRt.anchorMax = new Vector2(1f, 1f);
            shRt.anchoredPosition = new Vector2(0f, -54f);
            shRt.sizeDelta = new Vector2(0f, 20f);
            TextMeshProUGUI shText = subGo.GetComponent<TextMeshProUGUI>();
            shText.font = bodyFont;
            shText.fontSize = 13f;
            shText.fontStyle = FontStyles.Italic;
            shText.alignment = TextAlignmentOptions.Center;
            shText.color = new Color(0.55f, 0.42f, 0.30f);
            shText.text = "Chapter 1: Lembah Mimpi";

            // Decorative Divider
            GameObject rDivider = new GameObject("StoryDividerLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rDivider.transform.SetParent(normalStoryGo.transform, false);
            RectTransform rdivRt = rDivider.GetComponent<RectTransform>();
            rdivRt.anchorMin = new Vector2(0.08f, 1f);
            rdivRt.anchorMax = new Vector2(0.92f, 1f);
            rdivRt.anchoredPosition = new Vector2(0f, -70f);
            rdivRt.sizeDelta = new Vector2(0f, 2f);
            rDivider.GetComponent<Image>().color = new Color(0.6f, 0.45f, 0.3f, 0.45f);

            // Scroll View for Story
            GameObject storyScrollGo = new GameObject("StoryScrollView", typeof(RectTransform), typeof(ScrollRect));
            storyScrollGo.transform.SetParent(normalStoryGo.transform, false);
            RectTransform ssRt = storyScrollGo.GetComponent<RectTransform>();
            ssRt.anchorMin = Vector2.zero;
            ssRt.anchorMax = Vector2.one;
            ssRt.offsetMin = new Vector2(0f, 10f);
            ssRt.offsetMax = new Vector2(0f, -78f);

            ScrollRect ssSr = storyScrollGo.GetComponent<ScrollRect>();
            ssSr.horizontal = false;
            ssSr.vertical = true;
            ssSr.movementType = ScrollRect.MovementType.Clamped;

            GameObject sViewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D));
            sViewport.transform.SetParent(storyScrollGo.transform, false);
            RectTransform svpRt = sViewport.GetComponent<RectTransform>();
            svpRt.anchorMin = Vector2.zero;
            svpRt.anchorMax = Vector2.one;
            svpRt.sizeDelta = Vector2.zero;
            ssSr.viewport = svpRt;

            GameObject sContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            sContent.transform.SetParent(sViewport.transform, false);
            RectTransform scRt = sContent.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0f, 1f);
            scRt.anchorMax = new Vector2(1f, 1f);
            scRt.pivot = new Vector2(0.5f, 1f);
            scRt.sizeDelta = new Vector2(0f, 0f);
            ssSr.content = scRt;

            VerticalLayoutGroup svlg = sContent.GetComponent<VerticalLayoutGroup>();
            svlg.padding = new RectOffset(10, 10, 8, 8);
            svlg.spacing = 14f;
            svlg.childControlWidth = true;
            svlg.childControlHeight = false;
            svlg.childForceExpandWidth = true;
            svlg.childForceExpandHeight = false;

            ContentSizeFitter scsf = sContent.GetComponent<ContentSizeFitter>();
            scsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Narrative Story Text
            GameObject narrativeGo = new GameObject("NarrativeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            narrativeGo.transform.SetParent(sContent.transform, false);
            TextMeshProUGUI nText = narrativeGo.GetComponent<TextMeshProUGUI>();
            nText.font = bodyFont;
            nText.fontSize = 16.5f;
            nText.lineSpacing = 22f;
            nText.color = new Color(0.18f, 0.12f, 0.08f); // Tinta gelap perkamen
            nText.text = "Dahulu kala, sebelum kabut tebal turun menyelimuti lembah ini, para penjaga mimpi menjaga gerbang dimensi tetap seimbang...";
            nText.enableWordWrapping = true;

            // Adventurer Note
            GameObject noteGo = new GameObject("AdventurerNoteText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            noteGo.transform.SetParent(sContent.transform, false);
            TextMeshProUGUI noteText = noteGo.GetComponent<TextMeshProUGUI>();
            noteText.font = bodyFont;
            noteText.fontSize = 14f;
            noteText.fontStyle = FontStyles.Italic;
            noteText.color = new Color(0.48f, 0.32f, 0.18f);
            noteText.text = "\"Catatan: Temukan petunjuk tersembunyi di sekitar bebatuan kuno.\"";
            noteText.enableWordWrapping = true;

            // Completion Stamp Badge
            GameObject stampGo = new GameObject("CompletionStamp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            stampGo.transform.SetParent(normalStoryGo.transform, false);
            RectTransform stpRt = stampGo.GetComponent<RectTransform>();
            stpRt.anchorMin = new Vector2(1f, 0f);
            stpRt.anchorMax = new Vector2(1f, 0f);
            stpRt.anchoredPosition = new Vector2(-45f, 45f);
            stpRt.sizeDelta = new Vector2(150f, 46f);
            stpRt.localEulerAngles = new Vector3(0f, 0f, -12f);
            Image stpImg = stampGo.GetComponent<Image>();
            stpImg.color = new Color(0.75f, 0.15f, 0.15f, 0.85f); // Merah stempel lilin
            CreateSimpleText(stampGo.transform, "TERLESELESAIKAN", titleFont, 14f, FontStyles.Bold, Color.white);
            stampGo.SetActive(false);

            // Sealed Story Container (under rightPageGo)
            GameObject sealedStoryGo = new GameObject("SealedStoryContainer", typeof(RectTransform));
            sealedStoryGo.transform.SetParent(rightPageGo.transform, false);
            RectTransform ssRt2 = sealedStoryGo.GetComponent<RectTransform>();
            ssRt2.anchorMin = Vector2.zero;
            ssRt2.anchorMax = Vector2.one;
            ssRt2.offsetMin = Vector2.zero;
            ssRt2.offsetMax = Vector2.zero;

            // Sealed Story Title Text
            GameObject sealedStoryTitleGo = new GameObject("SealedStoryTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sealedStoryTitleGo.transform.SetParent(sealedStoryGo.transform, false);
            RectTransform sstRt = sealedStoryTitleGo.GetComponent<RectTransform>();
            sstRt.anchorMin = new Vector2(0f, 1f);
            sstRt.anchorMax = new Vector2(1f, 1f);
            sstRt.anchoredPosition = new Vector2(0f, -24f);
            sstRt.sizeDelta = new Vector2(0f, 40f);
            TextMeshProUGUI sstText = sealedStoryTitleGo.GetComponent<TextMeshProUGUI>();
            sstText.font = titleFont;
            sstText.fontSize = 24f;
            sstText.fontStyle = FontStyles.Bold;
            sstText.alignment = TextAlignmentOptions.Center;
            sstText.color = new Color(0.24f, 0.12f, 0.06f);
            sstText.text = "Chapter 2";

            // Sealed Story Divider
            GameObject sRDivider = new GameObject("SealedStoryDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sRDivider.transform.SetParent(sealedStoryGo.transform, false);
            RectTransform srdivRt = sRDivider.GetComponent<RectTransform>();
            srdivRt.anchorMin = new Vector2(0.08f, 1f);
            srdivRt.anchorMax = new Vector2(0.92f, 1f);
            srdivRt.anchoredPosition = new Vector2(0f, -70f);
            srdivRt.sizeDelta = new Vector2(0f, 2f);
            sRDivider.GetComponent<Image>().color = new Color(0.6f, 0.45f, 0.3f, 0.45f);

            // Sealed Stamp Graphic (Visual Tanda Tersegel)
            GameObject sealBadgeGo = new GameObject("SealedStampGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            sealBadgeGo.transform.SetParent(sealedStoryGo.transform, false);
            RectTransform sbgRt = sealBadgeGo.GetComponent<RectTransform>();
            sbgRt.anchorMin = new Vector2(0.5f, 0.5f);
            sbgRt.anchorMax = new Vector2(0.5f, 0.5f);
            sbgRt.anchoredPosition = new Vector2(0f, 35f);
            sbgRt.sizeDelta = new Vector2(200f, 58f);
            sbgRt.localEulerAngles = new Vector3(0f, 0f, -8f);
            Image sbgImg = sealBadgeGo.GetComponent<Image>();
            sbgImg.color = new Color(0.70f, 0.16f, 0.14f, 0.92f); // Merah stempel lilin
            Outline sbgOl = sealBadgeGo.GetComponent<Outline>();
            sbgOl.effectColor = new Color(0.35f, 0.05f, 0.05f, 0.85f);
            sbgOl.effectDistance = new Vector2(2f, -2f);
            CreateSimpleText(sealBadgeGo.transform, "TERSEGEL", titleFont, 20f, FontStyles.Bold, new Color(0.98f, 0.94f, 0.85f));

            // Sealed Story Notice Text
            GameObject sNoticeGo = new GameObject("SealedStoryNoticeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sNoticeGo.transform.SetParent(sealedStoryGo.transform, false);
            RectTransform snrRt = sNoticeGo.GetComponent<RectTransform>();
            snrRt.anchorMin = new Vector2(0.5f, 0.5f);
            snrRt.anchorMax = new Vector2(0.5f, 0.5f);
            snrRt.anchoredPosition = new Vector2(0f, -65f);
            snrRt.sizeDelta = new Vector2(340f, 140f);
            TextMeshProUGUI snrText = sNoticeGo.GetComponent<TextMeshProUGUI>();
            snrText.font = bodyFont;
            snrText.fontSize = 15f;
            snrText.fontStyle = FontStyles.Italic;
            snrText.alignment = TextAlignmentOptions.Center;
            snrText.color = new Color(0.42f, 0.28f, 0.18f);
            snrText.enableWordWrapping = true;
            snrText.text = "Lembaran perkamen ini terselubung oleh segel magis kuno.\n\nKisah petualangan, rahasia dunia, dan catatan kronik di halaman ini baru akan terungkap setelah kamu menuntaskan seluruh petualangan di bab sebelumnya...";

            sealedStoryGo.SetActive(false);

            // Hook up QuestBookRightPageUI
            QuestBookRightPageUI rightUI = rightPageGo.GetComponent<QuestBookRightPageUI>();
            SerializedObject rightSo = new SerializedObject(rightUI);
            rightSo.FindProperty("storyTitleText").objectReferenceValue = stText;
            rightSo.FindProperty("chapterSubheadingText").objectReferenceValue = shText;
            rightSo.FindProperty("storyNarrativeText").objectReferenceValue = nText;
            rightSo.FindProperty("adventurerNoteText").objectReferenceValue = noteText;
            rightSo.FindProperty("completionStamp").objectReferenceValue = stampGo;
            rightSo.FindProperty("normalStoryContainer").objectReferenceValue = normalStoryGo;
            rightSo.FindProperty("sealedStoryContainer").objectReferenceValue = sealedStoryGo;
            rightSo.FindProperty("sealedStoryTitleText").objectReferenceValue = sstText;
            rightSo.FindProperty("sealedStoryNoticeText").objectReferenceValue = snrText;
            rightSo.FindProperty("sealedStampGraphic").objectReferenceValue = sealBadgeGo;
            rightSo.ApplyModifiedProperties();

            // 4. Close Button ('X')
            GameObject closeBtnGo = new GameObject("CloseBookButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(bookObj.transform, false);
            RectTransform clRt = closeBtnGo.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(1f, 1f);
            clRt.anchorMax = new Vector2(1f, 1f);
            clRt.anchoredPosition = new Vector2(-48f, -48f);
            clRt.sizeDelta = new Vector2(44f, 44f);
            Image clImg = closeBtnGo.GetComponent<Image>();
            clImg.color = new Color(0.75f, 0.22f, 0.18f); // Merah wax
            CreateSimpleText(closeBtnGo.transform, "X", titleFont, 24f, FontStyles.Bold, Color.white);

            // Master QuestBookUI Setup on systemRoot (which stays active!)
            QuestBookUI bookUI = systemRoot.AddComponent<QuestBookUI>();
            SerializedObject bookSo = new SerializedObject(bookUI);
            bookSo.FindProperty("bookPanelRoot").objectReferenceValue = panelRoot;
            bookSo.FindProperty("pagesCanvasGroup").objectReferenceValue = cg;
            bookSo.FindProperty("frameAnimator").objectReferenceValue = anim;
            bookSo.FindProperty("leftPage").objectReferenceValue = leftUI;
            bookSo.FindProperty("rightPage").objectReferenceValue = rightUI;
            bookSo.FindProperty("closeButton").objectReferenceValue = closeBtnGo.GetComponent<Button>();
            bookSo.FindProperty("toggleShortcutKey").intValue = (int)KeyCode.J;

            // Assign Chapter_01_Valley.asset and Chapter_02_Forest.asset
            QuestChapterData ch1 = AssetDatabase.LoadAssetAtPath<QuestChapterData>("Assets/Data/Quests/Chapter_01_Valley.asset");
            QuestChapterData ch2 = AssetDatabase.LoadAssetAtPath<QuestChapterData>("Assets/Data/Quests/Chapter_02_Forest.asset");
            List<QuestChapterData> chapterList = new List<QuestChapterData>();
            if (ch1 != null) chapterList.Add(ch1);
            if (ch2 != null) chapterList.Add(ch2);

            SerializedProperty chapList = bookSo.FindProperty("chapters");
            chapList.arraySize = chapterList.Count;
            for (int i = 0; i < chapterList.Count; i++)
            {
                chapList.GetArrayElementAtIndex(i).objectReferenceValue = chapterList[i];
            }

            bookSo.ApplyModifiedProperties();

            // Sembunyikan panel buku di awal, tapi systemRoot tetap aktif agar input Key J selalu terdeteksi
            panelRoot.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(systemRoot, BookPanelPrefabPath);
            Object.DestroyImmediate(systemRoot);
        }

        private static void CreateHudButton(Canvas canvas, QuestBookUI bookUI)
        {
            Transform existingBtn = canvas.transform.Find("Open_QuestBook_Button");
            if (existingBtn == null) existingBtn = canvas.transform.Find("Inventory/Open_QuestBook_Button");
            if (existingBtn != null)
            {
                Undo.DestroyObjectImmediate(existingBtn.gameObject);
            }

            GameObject hudBtn = new GameObject("Open_QuestBook_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));

            Transform invBtn = canvas.transform.Find("Inventory/Open_Inventory_Button");
            if (invBtn != null)
            {
                hudBtn.transform.SetParent(invBtn.parent, false);
                RectTransform rt1 = invBtn.GetComponent<RectTransform>();
                RectTransform rt2 = hudBtn.GetComponent<RectTransform>();
                rt2.anchorMin = rt1.anchorMin;
                rt2.anchorMax = rt1.anchorMax;
                rt2.pivot = rt1.pivot;
                rt2.sizeDelta = rt1.sizeDelta;
                rt2.anchoredPosition = new Vector2(rt1.anchoredPosition.x - 115f, rt1.anchoredPosition.y);
            }
            else
            {
                hudBtn.transform.SetParent(canvas.transform, false);
                RectTransform rt = hudBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(490f, -495f);
                rt.sizeDelta = new Vector2(100f, 100f);
            }

            Image img = hudBtn.GetComponent<Image>();
            img.color = new Color(0.28f, 0.18f, 0.12f, 0.95f); // Cokelat kulit buku kuno

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/IrishGrover-Regular SDF.asset");
            CreateSimpleText(hudBtn.transform, "Quest\n[J]", font, 18f, FontStyles.Bold, Color.white);

            Button btn = hudBtn.GetComponent<Button>();
            btn.onClick.AddListener(bookUI.ToggleBook);

            // Connect hudBookButton on QuestBookUI
            SerializedObject so = new SerializedObject(bookUI);
            so.FindProperty("hudBookButton").objectReferenceValue = btn;
            so.ApplyModifiedProperties();
        }

        private static void EnsureSampleChapters()
        {
            EnsureDirectory("Assets/Data/Quests");

            QuestChapterData ch1 = AssetDatabase.LoadAssetAtPath<QuestChapterData>("Assets/Data/Quests/Chapter_01_Valley.asset");
            if (ch1 == null)
            {
                ch1 = ScriptableObject.CreateInstance<QuestChapterData>();
                ch1.ChapterId = "Chapter_01";
                ch1.ChapterTitle = "Chapter 1: Lembah Mimpi";
                ch1.ChapterOrder = 1;
                ch1.ChapterPrologue = "Dahulu kala, kabut tebal menyelimuti lembah misterius ini. Para pengelana yang tersesat mempercayai bahwa siapa saja yang berhasil melintasi lembah ini akan menemukan rahasia dunia yang terlupakan. Bersiaplah, karena perjalananmu baru saja dimulai...";
                ch1.IsSealedByDefault = false;
                AssetDatabase.CreateAsset(ch1, "Assets/Data/Quests/Chapter_01_Valley.asset");
            }

            QuestChapterData ch2 = AssetDatabase.LoadAssetAtPath<QuestChapterData>("Assets/Data/Quests/Chapter_02_Forest.asset");
            if (ch2 == null)
            {
                ch2 = ScriptableObject.CreateInstance<QuestChapterData>();
                ch2.ChapterId = "Chapter_02";
                ch2.ChapterTitle = "Chapter 2: Hutan Terlarang";
                ch2.ChapterOrder = 2;
                ch2.ChapterPrologue = "Jauh di pedalaman hutan berkabut, pohon-pohon purba berbisik dalam bahasa kuno. Tempat suci ini menyimpan pecahan artefak mimpi yang tersegel oleh mantra masa lalu. Belum ada pengelana yang berhasil menembus tabir kegelapannya...";
                ch2.IsSealedByDefault = true;
                ch2.PrerequisiteChapter = ch1;
                AssetDatabase.CreateAsset(ch2, "Assets/Data/Quests/Chapter_02_Forest.asset");
            }
            else
            {
                ch2.IsSealedByDefault = true;
                if (ch2.PrerequisiteChapter == null) ch2.PrerequisiteChapter = ch1;
                EditorUtility.SetDirty(ch2);
            }

            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateSimpleText(Transform parent, string text, TMP_FontAsset font, float size, FontStyles style, Color color)
        {
            GameObject tGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            tGo.transform.SetParent(parent, false);
            RectTransform rt = tGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = tGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = text;
            tmp.raycastTarget = false;
            return tGo;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
