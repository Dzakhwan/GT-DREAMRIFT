#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class StaffGripSetup
{
    private const string StaffFbxPath = "Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture.fbx";
    private const string StaffMatPath = "Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Materials/M_CrystalCrescentStaff.mat";
    private const string PrefabPath = "Assets/Prefabs/PlayerArmature.prefab";
    private const string SetupDoneKey = "StaffGripSetup_Attached_v1";

    static StaffGripSetup()
    {
        EditorApplication.delayCall += ExecuteSetupOnce;
    }

    [MenuItem("Tools/Combat/Attach Crescent Staff to Player")]
    public static void ForceExecute()
    {
        EditorPrefs.DeleteKey(SetupDoneKey);
        ExecuteSetup();
    }

    private static void ExecuteSetupOnce()
    {
        if (EditorPrefs.GetBool(SetupDoneKey, false)) return;
        ExecuteSetup();
    }

    public static void ExecuteSetup()
    {
        AssetDatabase.Refresh();

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StaffFbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[StaffGripSetup] FBX tidak ditemukan di: {StaffFbxPath}");
            return;
        }

        Material staffMat = AssetDatabase.LoadAssetAtPath<Material>(StaffMatPath);
        if (staffMat == null)
        {
            Debug.LogWarning($"[StaffGripSetup] Material belum ada di {StaffMatPath}, membuat baru...");
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            staffMat = new Material(litShader);
            staffMat.name = "M_CrystalCrescentStaff";
            
            // Texture bindings
            Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture.png");
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_normal.png");
            Texture2D metallicTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ASSET/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_fbx/Meshy_AI_Crystal_Crescent_Staf_1018145354_texture_metallic_roughness.png");

            if (baseTex != null) staffMat.SetTexture("_BaseMap", baseTex);
            if (normalTex != null) staffMat.SetTexture("_BumpMap", normalTex);
            if (metallicTex != null) staffMat.SetTexture("_MetallicGlossMap", metallicTex);
            staffMat.SetFloat("_Smoothness", 0.7f);
            staffMat.SetFloat("_Metallic", 0.5f);

            AssetDatabase.CreateAsset(staffMat, StaffMatPath);
            AssetDatabase.SaveAssets();
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[StaffGripSetup] Gagal memuat prefab di: {PrefabPath}");
            return;
        }

        Animator anim = prefabRoot.GetComponent<Animator>();
        Transform rightHand = anim != null ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (rightHand == null)
        {
            rightHand = FindDeepChild(prefabRoot.transform, "RightHand");
        }
        if (rightHand == null)
        {
            rightHand = FindDeepChild(prefabRoot.transform, "Right_Hand");
        }

        if (rightHand == null)
        {
            Debug.LogError("[StaffGripSetup] Tidak menemukan tulang tangan kanan di PlayerArmature!");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // Hapus socket staff lama jika ada di seluruh hierarki
        var oldSockets = new System.Collections.Generic.List<GameObject>();
        foreach (var t in prefabRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == "Staff_Socket")
            {
                oldSockets.Add(t.gameObject);
            }
        }
        foreach (var s in oldSockets)
        {
            if (s != null) GameObject.DestroyImmediate(s);
        }

        // Buat Weapon Socket langsung di tulang tangan kanan teranimasi
        GameObject socketObj = new GameObject("Staff_Socket");
        socketObj.transform.SetParent(rightHand, false);

        // Instansiasi model FBX tongkat
        GameObject staffInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, socketObj.transform);
        if (staffInstance == null)
        {
            staffInstance = GameObject.Instantiate(fbxAsset, socketObj.transform);
        }
        staffInstance.name = "Meshy_Crystal_Crescent_Staff";

        // Terapkan material ke semua renderer pada tongkat
        Renderer[] renderers = staffInstance.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.sharedMaterial = staffMat;
        }

        // Atur transform socket agar pas dalam genggaman telapak tangan kanan (menghadap ke depan dan atas)
        socketObj.transform.localPosition = new Vector3(0.0f, 0.075f, 0.005f);
        socketObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        socketObj.transform.localScale = Vector3.one * 0.7f;
        staffInstance.transform.localPosition = new Vector3(0f, 0.30f, 0f);

        // Buat Staff Tip untuk attackOrigin
        GameObject tipObj = new GameObject("StaffTip_AttackOrigin");
        tipObj.transform.SetParent(socketObj.transform, false);
        tipObj.transform.localPosition = new Vector3(0f, 1.25f, 0f); // Tepat di ujung kepala sabit kristal
        Transform staffTip = tipObj.transform;

        // Hubungkan ke PlayerFight attackOrigin dan update tuning grip
        PlayerFight pf = prefabRoot.GetComponent<PlayerFight>();
        if (pf != null)
        {
            SerializedObject so = new SerializedObject(pf);
            SerializedProperty originProp = so.FindProperty("attackOrigin");
            if (originProp != null)
            {
                originProp.objectReferenceValue = staffTip;
            }

            SerializedProperty posProp = so.FindProperty("staffGripPosition");
            if (posProp != null) posProp.vector3Value = new Vector3(0.0f, 0.075f, 0.005f);

            SerializedProperty rotProp = so.FindProperty("staffGripRotation");
            if (rotProp != null) rotProp.vector3Value = new Vector3(0f, 0f, 90f);

            SerializedProperty scaleProp = so.FindProperty("staffGripScale");
            if (scaleProp != null) scaleProp.vector3Value = Vector3.one * 0.7f;

            SerializedProperty offsetProp = so.FindProperty("staffModelOffset");
            if (offsetProp != null) offsetProp.vector3Value = new Vector3(0f, 0.30f, 0f);

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[StaffGripSetup] PlayerFight dihubungkan ke StaffTip_AttackOrigin dan grip tuning diperbarui.");
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        EditorPrefs.SetBool(SetupDoneKey, true);
        Debug.Log("[StaffGripSetup] BERHASIL! Tongkat Crystal Crescent Staff telah dipasangkan ke tangan kanan PlayerArmature!");
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
