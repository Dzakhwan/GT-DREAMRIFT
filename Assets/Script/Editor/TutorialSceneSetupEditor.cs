#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor Tool untuk men-setup seluruh hierarchy Tutorial System (UI Panel + 3D Primitives Placeholder)
/// secara otomatis di scene aktif (misal: "LV 1") hanya dengan 1 klik melalui menu:
/// Tools -> Dreamrift -> Setup Tutorial System in Current Scene
/// </summary>
public static class TutorialSceneSetupEditor
{
    [MenuItem("Tools/Dreamrift/Setup Tutorial System in Current Scene")]
    public static void SetupTutorialInScene()
    {
        // 1. Cari PlayerArmature
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var tpc = Object.FindFirstObjectByType<StarterAssets.ThirdPersonController>();
            if (tpc != null) player = tpc.gameObject;
        }

        Vector3 basePos = player != null ? player.transform.position : Vector3.zero;
        Vector3 forward = player != null ? player.transform.forward : Vector3.forward;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        // 2. Pastikan ada TutorialPanel di dalam UI_Canvas (atau buat Canvas baru jika belum ada)
        TutorialPanel tutorialPanel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        if (tutorialPanel == null)
        {
            Canvas targetCanvas = null;
            GameObject uiCanvasObj = GameObject.Find("UI_Canvas");
            if (uiCanvasObj != null)
                targetCanvas = uiCanvasObj.GetComponent<Canvas>();
            if (targetCanvas == null)
                targetCanvas = Object.FindFirstObjectByType<Canvas>();

            if (targetCanvas == null)
            {
                GameObject newCanvasObj = new GameObject("UI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                targetCanvas = newCanvasObj.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            tutorialPanel = CreateTutorialPanelUI(targetCanvas.transform);
        }

        // 3. Buat Root GameObject "Tutorial_System"
        GameObject existingSystem = GameObject.Find("Tutorial_System");
        if (existingSystem != null)
        {
            Selection.activeGameObject = existingSystem;
            Debug.Log("[TutorialSceneSetupEditor] 'Tutorial_System' sudah ada di scene dan telah dipilih.");
            return;
        }

        GameObject systemRoot = new GameObject("Tutorial_System");
        Undo.RegisterCreatedObjectUndo(systemRoot, "Create Tutorial System");
        TutorialManager manager = systemRoot.AddComponent<TutorialManager>();

        // 4. Buat Placeholder Tahap 1: Pinpoint Waypoint (Cylinder Primitive)
        GameObject pinpoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pinpoint.name = "Phase1_Pinpoint_Placeholder";
        pinpoint.transform.SetParent(systemRoot.transform);
        pinpoint.transform.position = basePos + forward * 6f + Vector3.up * 1f;
        pinpoint.transform.localScale = new Vector3(1.2f, 1.0f, 1.2f);
        Collider pinCol = pinpoint.GetComponent<Collider>();
        pinCol.isTrigger = true;
        pinpoint.AddComponent<TutorialTrigger>();

        // 5. Buat Placeholder Tahap 2: Turret Spawner (Cube Primitive)
        GameObject spawnerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spawnerObj.name = "Phase2_TurretSpawner_Placeholder";
        spawnerObj.transform.SetParent(systemRoot.transform);
        spawnerObj.transform.position = basePos + forward * 12f + Vector3.up * 1.0f;
        spawnerObj.transform.localScale = new Vector3(1.0f, 1.5f, 1.0f);
        spawnerObj.transform.rotation = Quaternion.LookRotation(-forward);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(spawnerObj.transform, false);
        firePoint.transform.localPosition = new Vector3(0f, 0.2f, 0.7f);

        TutorialProjectileSpawner spawner = spawnerObj.AddComponent<TutorialProjectileSpawner>();
        SerializedObject spawnerSO = new SerializedObject(spawner);
        spawnerSO.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        if (player != null)
            spawnerSO.FindProperty("playerTarget").objectReferenceValue = player.transform;
        spawnerSO.ApplyModifiedProperties();

        // 6. Buat Placeholder Tahap 3: Dummy Target (Capsule Primitive)
        GameObject dummyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummyObj.name = "Phase3_DummyEnemy_Placeholder";
        dummyObj.transform.SetParent(systemRoot.transform);
        dummyObj.transform.position = basePos + forward * 8f + Vector3.up * 1.0f;
        dummyObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) dummyObj.layer = enemyLayer;
        if (InternalEditorUtility_HasTag("Enemy")) dummyObj.tag = "Enemy";
        DummyTarget dummyTarget = dummyObj.AddComponent<DummyTarget>();

        // 7. Hubungkan semua referensi ke TutorialManager
        SerializedObject managerSO = new SerializedObject(manager);
        if (player != null)
            managerSO.FindProperty("playerObject").objectReferenceValue = player;
        managerSO.FindProperty("movementPinpointObject").objectReferenceValue = pinpoint;
        managerSO.FindProperty("projectileSpawner").objectReferenceValue = spawner;
        managerSO.FindProperty("dummyTarget").objectReferenceValue = dummyTarget;
        managerSO.ApplyModifiedProperties();

        Selection.activeGameObject = systemRoot;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(systemRoot.scene);
        Debug.Log("[TutorialSceneSetupEditor] Berhasil membuat 'Tutorial_System' lengkap dengan UI Panel & 3D Placeholders!");
    }

    private static bool InternalEditorUtility_HasTag(string tag)
    {
        foreach (string t in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (t == tag) return true;
        }
        return false;
    }

    private static TutorialPanel CreateTutorialPanelUI(Transform canvasTransform)
    {
        GameObject panelHolder = new GameObject("TutorialPanel_Controller", typeof(RectTransform));
        panelHolder.transform.SetParent(canvasTransform, false);
        RectTransform holderRect = panelHolder.GetComponent<RectTransform>();
        holderRect.anchorMin = Vector2.zero;
        holderRect.anchorMax = Vector2.one;
        holderRect.offsetMin = Vector2.zero;
        holderRect.offsetMax = Vector2.zero;

        GameObject panelRoot = new GameObject("PanelRoot", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(panelHolder.transform, false);
        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(680f, 380f);
        Image bg = panelRoot.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.14f, 0.94f);

        // Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelRoot.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.12f, 0.74f);
        titleRect.anchorMax = new Vector2(0.88f, 0.90f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Judul Tutorial";
        titleTmp.fontSize = 28f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.98f, 0.90f, 0.65f, 1f);

        // Description Text
        GameObject descObj = new GameObject("DescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(panelRoot.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.14f, 0.28f);
        descRect.anchorMax = new Vector2(0.86f, 0.72f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        TextMeshProUGUI descTmp = descObj.GetComponent<TextMeshProUGUI>();
        descTmp.text = "Deskripsi instruksi tutorial di sini.";
        descTmp.fontSize = 20f;
        descTmp.alignment = TextAlignmentOptions.Center;
        descTmp.color = Color.white;

        // Close Button
        GameObject btnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(panelRoot.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.10f);
        btnRect.anchorMax = new Vector2(0.65f, 0.23f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.22f, 0.58f, 0.32f, 0.95f);
        Button closeBtn = btnObj.GetComponent<Button>();

        // Load custom sprite if available
        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/UI TUTORIAL/tutorial panel.png");
        if (panelSprite != null)
        {
            bg.sprite = panelSprite;
            bg.color = Color.white;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = true;
            rootRect.sizeDelta = new Vector2(920f, 540f);
        }

        GameObject btnTextObj = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI btnTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "Mengerti (Close)";
        btnTmp.fontSize = 22f;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;

        TutorialPanel tp = panelHolder.AddComponent<TutorialPanel>();
        SerializedObject tpSO = new SerializedObject(tp);
        tpSO.FindProperty("titleText").objectReferenceValue = titleTmp;
        tpSO.FindProperty("descriptionText").objectReferenceValue = descTmp;
        tpSO.FindProperty("closeButton").objectReferenceValue = closeBtn;
        tpSO.FindProperty("panelRoot").objectReferenceValue = panelRoot;
        tpSO.ApplyModifiedProperties();

        panelRoot.SetActive(false);
        return tp;
    }
}
#endif

