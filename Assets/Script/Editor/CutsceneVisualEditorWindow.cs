using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace GTDreamrift.EditorTools
{
    public class CutsceneVisualEditorWindow : EditorWindow
    {
        private List<CutsceneData> allCutscenes = new List<CutsceneData>();
        private CutsceneData selectedCutscene;
        private SerializedObject serializedSelectedCutscene;

        // Navigation & Filter
        private Vector2 navScrollPos;
        private Vector2 inspectorScrollPos;
        private Vector2 storyboardScrollPos;
        private string searchQuery = "";

        // Previewer State
        private int previewFrameIndex = 0;
        private bool isPreviewPlaying = false;
        private double lastStepTime;

        // Trigger Preset State
        private CutsceneTriggerType presetTriggerType = CutsceneTriggerType.Interact;
        private float presetDelay = 0f;
        private bool presetOneTimeOnly = true;

        [MenuItem("Tools/Cutscene System/Visual Cutscene Editor")]
        public static void ShowWindow()
        {
            CutsceneVisualEditorWindow window = GetWindow<CutsceneVisualEditorWindow>("Visual Cutscene Editor");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }

        public static void OpenWindowWithAsset(CutsceneData asset)
        {
            CutsceneVisualEditorWindow window = GetWindow<CutsceneVisualEditorWindow>("Visual Cutscene Editor");
            window.minSize = new Vector2(1000, 600);
            window.Show();
            window.SelectCutscene(asset);
        }

        private void OnEnable()
        {
            ScanAllCutscenes();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopAudioPreview();
        }

        private void OnFocus()
        {
            ScanAllCutscenes();
        }

        private void ScanAllCutscenes()
        {
            allCutscenes.Clear();
            string[] guids = AssetDatabase.FindAssets("t:CutsceneData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CutsceneData data = AssetDatabase.LoadAssetAtPath<CutsceneData>(path);
                if (data != null)
                {
                    allCutscenes.Add(data);
                }
            }

            if (selectedCutscene != null)
            {
                SelectCutscene(selectedCutscene);
            }
            else if (allCutscenes.Count > 0)
            {
                SelectCutscene(allCutscenes[0]);
            }
        }

        private void SelectCutscene(CutsceneData data)
        {
            selectedCutscene = data;
            if (selectedCutscene != null)
            {
                serializedSelectedCutscene = new SerializedObject(selectedCutscene);
                presetTriggerType = selectedCutscene.defaultTriggerType;
            }
            else
            {
                serializedSelectedCutscene = null;
            }
            previewFrameIndex = 0;
            isPreviewPlaying = false;
        }

        private void OnEditorUpdate()
        {
            if (isPreviewPlaying && selectedCutscene != null && selectedCutscene.cutsceneType == CutsceneType.ImageSequence)
            {
                int frameCount = selectedCutscene.frames != null ? selectedCutscene.frames.Length : 0;
                if (frameCount > 0)
                {
                    float currentDuration = selectedCutscene.GetFrameDuration(previewFrameIndex);
                    if (EditorApplication.timeSinceStartup - lastStepTime >= currentDuration)
                    {
                        lastStepTime = EditorApplication.timeSinceStartup;
                        previewFrameIndex = (previewFrameIndex + 1) % frameCount;

                        // Play SFX for new preview frame
                        AudioClip sfx = selectedCutscene.GetFrameSFX(previewFrameIndex);
                        if (sfx != null)
                        {
                            PlayAudioPreview(sfx, false);
                        }

                        Repaint();
                    }
                }
            }
        }

        private void OnGUI()
        {
            DrawTopToolbar();

            float totalWidth = position.width;
            float totalHeight = position.height - 30f;

            float navWidth = 240f;
            float inspectorWidth = 320f;
            float centerWidth = totalWidth - navWidth - inspectorWidth - 10f;

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar: Asset Navigator
            DrawAssetNavigator(navWidth, totalHeight);

            // Center: Live Previewer & Storyboard
            DrawCenterWorkspace(centerWidth, totalHeight);

            // Right Sidebar: Settings & Trigger Setup
            DrawRightInspector(inspectorWidth, totalHeight);

            EditorGUILayout.EndHorizontal();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 1. TOP TOOLBAR
        // ══════════════════════════════════════════════════════════════════════

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(30));

            GUILayout.Label("🎬 Visual Cutscene Editor", EditorStyles.boldLabel, GUILayout.Width(180));

            if (GUILayout.Button("➕ New Cutscene Data", EditorStyles.toolbarButton, GUILayout.Width(150)))
            {
                CreateNewCutsceneData();
            }

            if (GUILayout.Button("🔄 Refresh Assets", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ScanAllCutscenes();
            }

            GUILayout.FlexibleSpace();

            if (selectedCutscene != null)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
                if (GUILayout.Button("⚡ Spawn Trigger In Active Scene", EditorStyles.toolbarButton, GUILayout.Width(220)))
                {
                    SpawnTriggerInScene();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 2. LEFT SIDEBAR: ASSET NAVIGATOR
        // ══════════════════════════════════════════════════════════════════════

        private void DrawAssetNavigator(float width, float height)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width), GUILayout.Height(height));

            EditorGUILayout.LabelField("Cutscene Assets", EditorStyles.boldLabel);

            // Search Bar
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField);
            EditorGUILayout.Space(5);

            navScrollPos = EditorGUILayout.BeginScrollView(navScrollPos);

            foreach (CutsceneData data in allCutscenes)
            {
                if (data == null) continue;
                if (!string.IsNullOrEmpty(searchQuery) && !data.name.ToLower().Contains(searchQuery.ToLower()) && !data.cutsceneTitle.ToLower().Contains(searchQuery.ToLower()))
                {
                    continue;
                }

                bool isSelected = (selectedCutscene == data);
                GUI.backgroundColor = isSelected ? new Color(0.2f, 0.6f, 0.9f) : Color.white;

                string label = string.IsNullOrEmpty(data.cutsceneTitle) ? data.name : $"{data.name}\n({data.cutsceneTitle})";
                if (GUILayout.Button(label, GUILayout.Height(38)))
                {
                    SelectCutscene(data);
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 3. CENTER WORKSPACE: LIVE PREVIEW & STORYBOARD
        // ══════════════════════════════════════════════════════════════════════

        private void DrawCenterWorkspace(float width, float height)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(height));

            if (selectedCutscene == null)
            {
                EditorGUILayout.HelpBox("Select a CutsceneData asset from the left sidebar or create a new one.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            float previewHeight = height * 0.55f;
            float storyboardHeight = height - previewHeight - 15f;

            // Live Preview Canvas
            DrawLivePreviewCanvas(width, previewHeight);

            EditorGUILayout.Space(5);

            // Storyboard Strip
            DrawStoryboardStrip(width, storyboardHeight);

            EditorGUILayout.EndVertical();
        }

        private void DrawLivePreviewCanvas(float width, float height)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(height));

            EditorGUILayout.LabelField("Live Canvas Preview", EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(width - 20, height - 70);
            EditorGUI.DrawRect(previewRect, Color.black);

            if (selectedCutscene.cutsceneType == CutsceneType.ImageSequence)
            {
                if (selectedCutscene.frames != null && selectedCutscene.frames.Length > 0)
                {
                    previewFrameIndex = Mathf.Clamp(previewFrameIndex, 0, selectedCutscene.frames.Length - 1);
                    Sprite currentSprite = selectedCutscene.frames[previewFrameIndex];
                    if (currentSprite != null && currentSprite.texture != null)
                    {
                        Rect textureRect = currentSprite.rect;
                        Rect texCoords = new Rect(
                            textureRect.x / currentSprite.texture.width,
                            textureRect.y / currentSprite.texture.height,
                            textureRect.width / currentSprite.texture.width,
                            textureRect.height / currentSprite.texture.height
                        );
                        GUI.DrawTextureWithTexCoords(previewRect, currentSprite.texture, texCoords, true);
                    }

                    // Overlay Frame Info
                    float dur = selectedCutscene.GetFrameDuration(previewFrameIndex);
                    AudioClip sfx = selectedCutscene.GetFrameSFX(previewFrameIndex);
                    string info = $"Frame {previewFrameIndex + 1}/{selectedCutscene.frames.Length} | Duration: {dur:F1}s | SFX: {(sfx != null ? sfx.name : "None")}";
                    GUI.Label(new Rect(previewRect.x + 10, previewRect.y + 10, 400, 25), info, EditorStyles.boldLabel);
                }
                else
                {
                    GUI.Label(previewRect, "No frames added to this CutsceneData", EditorStyles.centeredGreyMiniLabel);
                }
            }
            else
            {
                string videoInfo = selectedCutscene.videoClip != null ? $"Video Clip: {selectedCutscene.videoClip.name} ({selectedCutscene.videoClip.length:F1}s)" : "No VideoClip assigned";
                GUI.Label(previewRect, videoInfo, EditorStyles.whiteLargeLabel);
            }

            // Playback Controls Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(isPreviewPlaying ? "⏸ Pause" : "▶ Play Preview", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                isPreviewPlaying = !isPreviewPlaying;
                lastStepTime = EditorApplication.timeSinceStartup;

                if (isPreviewPlaying && selectedCutscene.bgmClip != null)
                {
                    PlayAudioPreview(selectedCutscene.bgmClip, true);
                }
                else if (!isPreviewPlaying)
                {
                    StopAudioPreview();
                }
            }

            if (GUILayout.Button("⏹ Stop", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                isPreviewPlaying = false;
                previewFrameIndex = 0;
                StopAudioPreview();
            }

            if (selectedCutscene.cutsceneType == CutsceneType.ImageSequence && selectedCutscene.frames != null && selectedCutscene.frames.Length > 0)
            {
                if (GUILayout.Button("⏮ Prev", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    previewFrameIndex = Mathf.Max(0, previewFrameIndex - 1);
                }

                previewFrameIndex = EditorGUILayout.IntSlider(previewFrameIndex, 0, selectedCutscene.frames.Length - 1, GUILayout.Width(180));

                if (GUILayout.Button("⏭ Next", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    previewFrameIndex = Mathf.Min(selectedCutscene.frames.Length - 1, previewFrameIndex + 1);
                }

                if (GUILayout.Button("🔊 Test SFX", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    AudioClip sfx = selectedCutscene.GetFrameSFX(previewFrameIndex);
                    if (sfx != null) PlayAudioPreview(sfx, false);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawStoryboardStrip(float width, float height)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(height));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Storyboard Sequence", EditorStyles.boldLabel);
            if (GUILayout.Button("➕ Add Frame Slot", GUILayout.Width(130)))
            {
                AddFrameSlot();
            }
            EditorGUILayout.EndHorizontal();

            storyboardScrollPos = EditorGUILayout.BeginScrollView(storyboardScrollPos, true, false, GUILayout.Height(height - 35));
            EditorGUILayout.BeginHorizontal();

            if (selectedCutscene != null && selectedCutscene.frames != null)
            {
                EnsureFrameArraysMatch();

                for (int i = 0; i < selectedCutscene.frames.Length; i++)
                {
                    bool isSelected = (i == previewFrameIndex);
                    Color bg = isSelected ? new Color(0.2f, 0.7f, 1f, 0.4f) : new Color(0f, 0f, 0f, 0.2f);

                    EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(130), GUILayout.Height(height - 60));
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 0), bg);

                    EditorGUILayout.LabelField($"# {i + 1}", EditorStyles.boldLabel);

                    // Sprite Field
                    selectedCutscene.frames[i] = (Sprite)EditorGUILayout.ObjectField(selectedCutscene.frames[i], typeof(Sprite), false, GUILayout.Width(115), GUILayout.Height(75));

                    // Frame Duration Field
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Dur (s):", GUILayout.Width(45));
                    float dur = selectedCutscene.frameDurations[i];
                    selectedCutscene.frameDurations[i] = EditorGUILayout.FloatField(dur, GUILayout.Width(65));
                    EditorGUILayout.EndHorizontal();

                    // SFX Clip Field
                    selectedCutscene.frameSfx[i] = (AudioClip)EditorGUILayout.ObjectField(selectedCutscene.frameSfx[i], typeof(AudioClip), false, GUILayout.Width(115));

                    // Action buttons
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(55)))
                    {
                        previewFrameIndex = i;
                    }
                    if (GUILayout.Button("❌", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        RemoveFrameAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 4. RIGHT SIDEBAR: SETTINGS & TRIGGER SETUP
        // ══════════════════════════════════════════════════════════════════════

        private void DrawRightInspector(float width, float height)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(width), GUILayout.Height(height));

            if (serializedSelectedCutscene == null || selectedCutscene == null)
            {
                EditorGUILayout.HelpBox("No Cutscene Selected", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            serializedSelectedCutscene.Update();

            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);

            EditorGUILayout.LabelField("Cutscene Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            SerializedProperty titleProp = serializedSelectedCutscene.FindProperty("cutsceneTitle");
            SerializedProperty typeProp = serializedSelectedCutscene.FindProperty("cutsceneType");
            SerializedProperty videoProp = serializedSelectedCutscene.FindProperty("videoClip");
            SerializedProperty timePerFrameProp = serializedSelectedCutscene.FindProperty("timePerFrame");
            SerializedProperty manualProp = serializedSelectedCutscene.FindProperty("manualAdvance");

            SerializedProperty bgmProp = serializedSelectedCutscene.FindProperty("bgmClip");
            SerializedProperty bgmVolProp = serializedSelectedCutscene.FindProperty("bgmVolume");
            SerializedProperty sfxVolProp = serializedSelectedCutscene.FindProperty("sfxVolume");

            SerializedProperty allowSkipProp = serializedSelectedCutscene.FindProperty("allowSkip");
            SerializedProperty loadSceneProp = serializedSelectedCutscene.FindProperty("loadSceneAfter");
            SerializedProperty pauseProp = serializedSelectedCutscene.FindProperty("pauseGameDuringCutscene");
            SerializedProperty defaultTriggerProp = serializedSelectedCutscene.FindProperty("defaultTriggerType");

            EditorGUILayout.PropertyField(titleProp);
            EditorGUILayout.PropertyField(typeProp);

            if ((CutsceneType)typeProp.enumValueIndex == CutsceneType.Video)
            {
                EditorGUILayout.PropertyField(videoProp);
            }
            else
            {
                EditorGUILayout.PropertyField(timePerFrameProp);
                EditorGUILayout.PropertyField(manualProp);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Audio Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bgmProp);
            EditorGUILayout.PropertyField(bgmVolProp);
            EditorGUILayout.PropertyField(sfxVolProp);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Skip & Post-Cutscene Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(allowSkipProp);
            EditorGUILayout.PropertyField(loadSceneProp);

            if (string.IsNullOrEmpty(loadSceneProp.stringValue))
            {
                EditorGUILayout.HelpBox("ℹ️ Load Scene After KOSONG -> Cutscene akan selesai & TETAP di scene saat ini.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"ℹ️ Akan berpindah ke Scene '{loadSceneProp.stringValue}' setelah cutscene selesai.", MessageType.None);
            }

            EditorGUILayout.PropertyField(pauseProp);
            EditorGUILayout.PropertyField(defaultTriggerProp);

            serializedSelectedCutscene.ApplyModifiedProperties();

            EditorGUILayout.Space(15);
            EditorGUILayout.Separator();

            // Trigger Spawn Panel
            EditorGUILayout.LabelField("⚡ Scene Trigger Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Quickly generate a CutsceneTriggerHandler object configured in your active Scene.", MessageType.None);

            presetTriggerType = (CutsceneTriggerType)EditorGUILayout.EnumPopup("Trigger Type", presetTriggerType);
            presetDelay = EditorGUILayout.FloatField("Delay (sec)", presetDelay);
            presetOneTimeOnly = EditorGUILayout.Toggle("One Time Only", presetOneTimeOnly);

            EditorGUILayout.Space(5);
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.9f);
            if (GUILayout.Button("⚡ Spawn Trigger GameObject In Scene", GUILayout.Height(32)))
            {
                SpawnTriggerInScene();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS & ASSET CREATION
        // ══════════════════════════════════════════════════════════════════════

        private void CreateNewCutsceneData()
        {
            string folderPath = "Assets/Data/Cutscenes";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/NewCutsceneData.asset");
            CutsceneData asset = ScriptableObject.CreateInstance<CutsceneData>();
            asset.cutsceneTitle = "New Cutscene";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ScanAllCutscenes();
            SelectCutscene(asset);
        }

        private void SpawnTriggerInScene()
        {
            if (selectedCutscene == null) return;

            GameObject go = new GameObject($"CutsceneTrigger_{selectedCutscene.name}");
            CutsceneTriggerHandler trigger = go.AddComponent<CutsceneTriggerHandler>();

            SerializedObject so = new SerializedObject(trigger);
            so.FindProperty("cutsceneData").objectReferenceValue = selectedCutscene;
            so.FindProperty("triggerType").enumValueIndex = (int)presetTriggerType;
            so.FindProperty("delayBeforeStart").floatValue = presetDelay;
            so.FindProperty("oneTimeOnly").boolValue = presetOneTimeOnly;
            so.ApplyModifiedProperties();

            if (presetTriggerType == CutsceneTriggerType.ZoneEnter || presetTriggerType == CutsceneTriggerType.Interact)
            {
                BoxCollider col = go.AddComponent<BoxCollider>();
                col.isTrigger = (presetTriggerType == CutsceneTriggerType.ZoneEnter);
                col.size = new Vector3(2f, 2f, 2f);
            }

            Undo.RegisterCreatedObjectUndo(go, "Spawn Cutscene Trigger");
            Selection.activeGameObject = go;
            Debug.Log($"[Visual Cutscene Editor] Spawned CutsceneTriggerHandler for '{selectedCutscene.name}' with trigger mode '{presetTriggerType}' in Scene!");
        }

        private void EnsureFrameArraysMatch()
        {
            if (selectedCutscene == null || selectedCutscene.frames == null) return;
            int len = selectedCutscene.frames.Length;

            if (selectedCutscene.frameDurations == null || selectedCutscene.frameDurations.Length != len)
            {
                Array.Resize(ref selectedCutscene.frameDurations, len);
            }

            if (selectedCutscene.frameSfx == null || selectedCutscene.frameSfx.Length != len)
            {
                Array.Resize(ref selectedCutscene.frameSfx, len);
            }
        }

        private void AddFrameSlot()
        {
            if (selectedCutscene == null) return;
            List<Sprite> frames = new List<Sprite>(selectedCutscene.frames ?? new Sprite[0]);
            frames.Add(null);
            selectedCutscene.frames = frames.ToArray();
            EnsureFrameArraysMatch();
            EditorUtility.SetDirty(selectedCutscene);
        }

        private void RemoveFrameAtIndex(int index)
        {
            if (selectedCutscene == null || selectedCutscene.frames == null || index < 0 || index >= selectedCutscene.frames.Length) return;

            List<Sprite> frames = new List<Sprite>(selectedCutscene.frames);
            List<float> dur = new List<float>(selectedCutscene.frameDurations);
            List<AudioClip> sfx = new List<AudioClip>(selectedCutscene.frameSfx);

            frames.RemoveAt(index);
            if (index < dur.Count) dur.RemoveAt(index);
            if (index < sfx.Count) sfx.RemoveAt(index);

            selectedCutscene.frames = frames.ToArray();
            selectedCutscene.frameDurations = dur.ToArray();
            selectedCutscene.frameSfx = sfx.ToArray();

            EditorUtility.SetDirty(selectedCutscene);
        }

        private static void PlayAudioPreview(AudioClip clip, bool loop)
        {
            if (clip == null) return;
            try
            {
                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                MethodInfo method = audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                method?.Invoke(null, new object[] { clip, 0, loop });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Audio preview error: {ex.Message}");
            }
        }

        private static void StopAudioPreview()
        {
            try
            {
                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                MethodInfo method = audioUtilClass.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, null);
            }
            catch { }
        }
    }
}
