using UnityEditor;
using UnityEngine;

namespace GTDreamrift.EditorTools
{
    [CustomEditor(typeof(CutsceneData))]
    public class CutsceneDataCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CutsceneData data = (CutsceneData)target;

            // Header Banner & Button to Open Visual Editor
            EditorGUILayout.Space(5);
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                fixedHeight = 35
            };

            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
            if (GUILayout.Button("🎬 Open in Visual Cutscene Editor", buttonStyle))
            {
                CutsceneVisualEditorWindow.OpenWindowWithAsset(data);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // Summary Box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Cutscene Quick Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Title", string.IsNullOrEmpty(data.cutsceneTitle) ? "(Untitled)" : data.cutsceneTitle);
            EditorGUILayout.LabelField("Media Type", data.cutsceneType.ToString());

            if (data.cutsceneType == CutsceneType.ImageSequence)
            {
                int frameCount = data.frames != null ? data.frames.Length : 0;
                EditorGUILayout.LabelField("Frame Count", $"{frameCount} frames");
                
                float totalTime = 0f;
                for (int i = 0; i < frameCount; i++)
                {
                    totalTime += data.GetFrameDuration(i);
                }
                EditorGUILayout.LabelField("Total Duration", $"~{totalTime:F1}s");
            }
            else
            {
                EditorGUILayout.LabelField("Video Clip", data.videoClip != null ? data.videoClip.name : "(Not Assigned)");
            }

            EditorGUILayout.LabelField("BGM", data.bgmClip != null ? data.bgmClip.name : "None");
            EditorGUILayout.LabelField("Default Trigger", data.defaultTriggerType.ToString());
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Standard Inspector
            DrawDefaultInspector();
        }
    }
}
