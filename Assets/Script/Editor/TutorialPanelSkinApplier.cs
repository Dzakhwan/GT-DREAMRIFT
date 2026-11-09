#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tool untuk otomatis memasang dan menyesuaikan asset "tutorial panel.png"
/// ke GameObject TutorialPanel di scene LV 1.
/// Bisa dijalankan via menu: Tools -> Dreamrift -> Apply Tutorial Panel Asset Skin
/// </summary>
public static class TutorialPanelSkinApplier
{
    private const string TexturePath = "Assets/UI/UI TUTORIAL/tutorial panel.png";

    [MenuItem("Tools/Dreamrift/Apply Tutorial Panel Asset Skin")]
    public static void ApplySkin()
    {
        // 1. Pastikan Texture ter-import sebagai Sprite (Single)
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer != null)
        {
            bool modified = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                modified = true;
            }
            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                modified = true;
            }
            if (modified)
            {
                importer.SaveAndReimport();
            }
        }

        Sprite panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        if (panelSprite == null)
        {
            Debug.LogError($"[TutorialPanelSkin] Gagal memuat Sprite dari '{TexturePath}'. Pastikan file ada.");
            return;
        }

        // 2. Cari TutorialPanel / PanelRoot di scene
        GameObject panelRoot = null;
        TutorialPanel tutorialPanel = Object.FindFirstObjectByType<TutorialPanel>(FindObjectsInactive.Include);
        if (tutorialPanel != null)
        {
            var so = new SerializedObject(tutorialPanel);
            var rootProp = so.FindProperty("panelRoot");
            if (rootProp != null && rootProp.objectReferenceValue != null)
            {
                panelRoot = rootProp.objectReferenceValue as GameObject;
            }
        }

        if (panelRoot == null)
        {
            panelRoot = GameObject.Find("PanelRoot");
        }

        if (panelRoot == null)
        {
            Debug.LogError("[TutorialPanelSkin] Tidak menemukan GameObject 'PanelRoot' di scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(panelRoot.transform.parent != null ? panelRoot.transform.parent.gameObject : panelRoot, "Apply Tutorial Panel Skin");

        // 3. Setup PanelRoot RectTransform & Image
        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            // Reset anchor ke Center agar panel proporsional di tengah layar
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            // Ukuran proporsional gambar panel tutorial
            rootRect.sizeDelta = new Vector2(920f, 540f);
            rootRect.localScale = Vector3.one;
        }

        Image panelImg = panelRoot.GetComponent<Image>();
        if (panelImg == null)
        {
            panelImg = panelRoot.AddComponent<Image>();
        }
        panelImg.sprite = panelSprite;
        panelImg.color = Color.white;
        panelImg.type = Image.Type.Simple;
        panelImg.preserveAspect = true;

        // 4. Sesuaikan TitleText
        Transform titleTransform = panelRoot.transform.Find("TitleText");
        if (titleTransform != null)
        {
            RectTransform titleRect = titleTransform.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.12f, 0.74f);
            titleRect.anchorMax = new Vector2(0.88f, 0.90f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleTmp = titleTransform.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null)
            {
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.fontSize = 28f;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = new Color(0.98f, 0.90f, 0.65f, 1f); // Gold/kuning estetik
            }
        }

        // 5. Sesuaikan DescriptionText
        Transform descTransform = panelRoot.transform.Find("DescriptionText");
        if (descTransform != null)
        {
            RectTransform descRect = descTransform.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.14f, 0.28f);
            descRect.anchorMax = new Vector2(0.86f, 0.72f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            TextMeshProUGUI descTmp = descTransform.GetComponent<TextMeshProUGUI>();
            if (descTmp != null)
            {
                descTmp.alignment = TextAlignmentOptions.Center;
                descTmp.fontSize = 20f;
                descTmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                descTmp.enableWordWrapping = true;
            }
        }

        // 6. Sesuaikan CloseButton
        Transform btnTransform = panelRoot.transform.Find("CloseButton");
        if (btnTransform != null)
        {
            RectTransform btnRect = btnTransform.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.35f, 0.10f);
            btnRect.anchorMax = new Vector2(0.65f, 0.23f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            Image btnImg = btnTransform.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = new Color(0.22f, 0.58f, 0.32f, 0.95f); // Warna hijau serasi
            }

            Transform btnText = btnTransform.Find("Text (TMP)");
            if (btnText != null)
            {
                TextMeshProUGUI btnTmp = btnText.GetComponent<TextMeshProUGUI>();
                if (btnTmp != null)
                {
                    btnTmp.fontSize = 20f;
                    btnTmp.text = "Mengerti";
                }
            }
        }

        // Pastikan scene ditandai dirty agar tersimpan
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelRoot.scene);
        Debug.Log("[TutorialPanelSkin] Sukses! Sprite 'tutorial panel.png' berhasil dipasang dan posisi teks/tombol disesuaikan rapi di tengah layar.");
    }
}
#endif

