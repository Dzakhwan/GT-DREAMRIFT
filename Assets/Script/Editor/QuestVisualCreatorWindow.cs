using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Dreamrift.QuestSystem;
using Dreamrift.InventorySystem;

namespace GTDreamrift.EditorTools
{
    public class QuestVisualCreatorWindow : EditorWindow
    {
        private List<QuestData> allQuests = new List<QuestData>();
        private QuestData selectedQuest;
        private SerializedObject serializedSelectedQuest;

        // 2D Canvas State
        private Vector2 canvasScrollPos;
        private Vector2 inspectorScrollPos;
        private string searchQuery = "";
        private string saveFolderPath = "Assets/Data/Quests";

        // Node Linking State
        private bool isConnectingNodes = false;
        private QuestData connectSourceQuest = null;

        // Node Card Size
        private const float NodeWidth = 230f;
        private const float NodeHeight = 160f;

        [MenuItem("Tools/Quest System/Visual Quest Creator")]
        public static void ShowWindow()
        {
            QuestVisualCreatorWindow window = GetWindow<QuestVisualCreatorWindow>("Visual Quest Creator");
            window.minSize = new Vector2(900, 550);
            window.Show();
        }

        private void OnEnable()
        {
            ScanAllQuests();
        }

        private void OnFocus()
        {
            ScanAllQuests();
        }

        private void ScanAllQuests()
        {
            allQuests.Clear();
            string[] guids = AssetDatabase.FindAssets("t:QuestData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                QuestData q = AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (q != null)
                {
                    allQuests.Add(q);
                }
            }

            if (selectedQuest != null)
            {
                SelectQuest(selectedQuest);
            }
            else if (allQuests.Count > 0)
            {
                SelectQuest(allQuests[0]);
            }
        }

        private void SelectQuest(QuestData quest)
        {
            selectedQuest = quest;
            if (selectedQuest != null)
            {
                serializedSelectedQuest = new SerializedObject(selectedQuest);
            }
            else
            {
                serializedSelectedQuest = null;
            }
        }

        private void OnGUI()
        {
            DrawTopToolbar();

            float totalWidth = position.width;
            float totalHeight = position.height - 25f; // minus toolbar height

            float canvasWidth = totalWidth * 0.60f;
            float inspectorWidth = totalWidth * 0.40f - 5f;

            EditorGUILayout.BeginHorizontal();

            // Left Side: 2D Interactive Node Chart Graph Canvas
            Rect canvasRect = EditorGUILayout.GetControlRect(false, totalHeight, GUILayout.Width(canvasWidth));
            Draw2DNodeCanvas(canvasRect);

            // Divider Line
            Rect dividerRect = EditorGUILayout.GetControlRect(false, totalHeight, GUILayout.Width(2));
            EditorGUI.DrawRect(dividerRect, new Color(0.15f, 0.15f, 0.15f));

            // Right Side: Inspector & Rewards Manager
            EditorGUILayout.BeginVertical(GUILayout.Width(inspectorWidth));
            DrawInspectorPane();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("➕ Create New Quest", EditorStyles.toolbarButton, GUILayout.Width(140)))
            {
                CreateNewQuestAsset();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🔄 Refresh Graph", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ScanAllQuests();
            }

            GUI.backgroundColor = isConnectingNodes ? new Color(1.0f, 0.7f, 0.2f) : Color.white;
            if (GUILayout.Button(isConnectingNodes ? "🔗 Connecting... (Click Target Node)" : "🔗 Connect Nodes", EditorStyles.toolbarButton, GUILayout.Width(180)))
            {
                isConnectingNodes = !isConnectingNodes;
                if (!isConnectingNodes) connectSourceQuest = null;
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("📐 Auto Layout", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                AutoLayoutNodes();
            }

            GUILayout.Space(10);
            searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(160));

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Output Path:", GUILayout.Width(75));
            saveFolderPath = EditorGUILayout.TextField(saveFolderPath, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        private void Draw2DNodeCanvas(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            // Draw Grid Background
            DrawGrid(rect, 20f, 0.12f, new Color(0.25f, 0.25f, 0.25f));
            DrawGrid(rect, 100f, 0.30f, new Color(0.15f, 0.15f, 0.15f));

            canvasScrollPos = GUI.BeginScrollView(rect, canvasScrollPos, new Rect(0, 0, 2500, 2500), true, true);

            // 1. Draw Bezier Connector Lines (Prerequisite & Next Quest Links)
            DrawAllBezierConnectionLines();

            // 2. Draw Node Cards
            BeginWindows();
            for (int i = 0; i < allQuests.Count; i++)
            {
                QuestData q = allQuests[i];
                if (q == null) continue;

                // Search Filter
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    bool matchName = q.DisplayName.ToLower().Contains(searchQuery.ToLower());
                    bool matchId = q.QuestId.ToLower().Contains(searchQuery.ToLower());
                    if (!matchName && !matchId) continue;
                }

                Rect nodeRect = new Rect(q.NodePosition.x, q.NodePosition.y, NodeWidth, NodeHeight);
                Rect newRect = GUI.Window(i, nodeRect, DrawNodeWindowContent, $"{GetObjectiveBadgeIcon(q.ObjectiveType)} {q.DisplayName}");

                if (newRect.position != q.NodePosition)
                {
                    Undo.RecordObject(q, "Move Quest Node");
                    q.NodePosition = newRect.position;
                    EditorUtility.SetDirty(q);
                }
            }
            EndWindows();

            // Active connecting line feedback
            if (isConnectingNodes && connectSourceQuest != null)
            {
                Vector2 startPos = connectSourceQuest.NodePosition + new Vector2(NodeWidth, NodeHeight * 0.5f);
                Vector2 mousePos = Event.current.mousePosition;
                Handles.DrawBezier(startPos, mousePos, startPos + Vector2.right * 50f, mousePos + Vector2.left * 50f, Color.yellow, null, 3.5f);
                Repaint();
            }

            GUI.EndScrollView();
        }

        private void DrawGrid(Rect rect, float gridSpacing, float gridOpacity, Color gridColor)
        {
            int widthDivs = Mathf.CeilToInt(2500 / gridSpacing);
            int heightDivs = Mathf.CeilToInt(2500 / gridSpacing);

            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, 0, 0), new Vector3(gridSpacing * i, 2500, 0));
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(new Vector3(0, gridSpacing * j, 0), new Vector3(2500, gridSpacing * j, 0));
            }

            Handles.color = Color.white;
        }

        private void DrawAllBezierConnectionLines()
        {
            foreach (QuestData q in allQuests)
            {
                if (q == null) continue;

                // Draw lines to Next Quests On Complete (Cyan/Blue curve)
                if (q.NextQuestsOnComplete != null)
                {
                    foreach (QuestData next in q.NextQuestsOnComplete)
                    {
                        if (next == null) continue;

                        Vector3 startPos = q.NodePosition + new Vector2(NodeWidth, NodeHeight * 0.5f);
                        Vector3 endPos = next.NodePosition + new Vector2(0, NodeHeight * 0.5f);

                        Vector3 startTan = startPos + Vector3.right * 60f;
                        Vector3 endTan = endPos + Vector3.left * 60f;

                        Handles.DrawBezier(startPos, endPos, startTan, endTan, new Color(0.2f, 0.7f, 1.0f, 0.9f), null, 3.5f);
                        DrawConnectionArrow(endPos);
                    }
                }

                // Draw lines from Prerequisites (Yellow/Gold curve)
                if (q.Prerequisites != null)
                {
                    foreach (QuestData pre in q.Prerequisites)
                    {
                        if (pre == null) continue;

                        Vector3 startPos = pre.NodePosition + new Vector2(NodeWidth, NodeHeight * 0.5f);
                        Vector3 endPos = q.NodePosition + new Vector2(0, NodeHeight * 0.5f);

                        Vector3 startTan = startPos + Vector3.right * 60f;
                        Vector3 endTan = endPos + Vector3.left * 60f;

                        Handles.DrawBezier(startPos, endPos, startTan, endTan, new Color(1.0f, 0.8f, 0.2f, 0.7f), null, 2.5f);
                    }
                }
            }
        }

        private void DrawConnectionArrow(Vector3 pos)
        {
            Handles.color = new Color(0.2f, 0.7f, 1.0f, 1.0f);
            Handles.DrawSolidDisc(pos, Vector3.forward, 5f);
            Handles.color = Color.white;
        }

        private void DrawNodeWindowContent(int windowID)
        {
            if (windowID < 0 || windowID >= allQuests.Count) return;
            QuestData q = allQuests[windowID];
            if (q == null) return;

            bool isSelected = (selectedQuest == q);

            // Runtime state badge
            QuestState runtimeState = QuestState.NotStarted;
            if (Application.isPlaying && QuestManager.Instance != null)
            {
                runtimeState = QuestManager.Instance.GetQuestState(q);
            }

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {q.QuestId}", EditorStyles.miniLabel);
            if (Application.isPlaying)
            {
                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                if (runtimeState == QuestState.Active) statusStyle.normal.textColor = Color.cyan;
                else if (runtimeState == QuestState.Complete) statusStyle.normal.textColor = Color.green;
                else if (runtimeState == QuestState.Locked) statusStyle.normal.textColor = Color.red;

                GUILayout.Label($"[{runtimeState}]", statusStyle);
            }
            EditorGUILayout.EndHorizontal();

            // Objective Summary
            EditorGUILayout.LabelField(GetObjectiveSummary(q), EditorStyles.wordWrappedMiniLabel);

            // Rewards Summary
            EditorGUILayout.LabelField($"Hadiah: {GetRewardsSummary(q)}", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            // Select button
            if (GUILayout.Button(isSelected ? "Selected" : "Select", EditorStyles.miniButton, GUILayout.Height(20)))
            {
                SelectQuest(q);
            }

            // Port Connect Button
            if (isConnectingNodes)
            {
                if (connectSourceQuest == null)
                {
                    if (GUILayout.Button("Out ➡️", EditorStyles.miniButtonRight, GUILayout.Height(20)))
                    {
                        connectSourceQuest = q;
                    }
                }
                else if (connectSourceQuest != q)
                {
                    if (GUILayout.Button("📥 Link In", EditorStyles.miniButtonRight, GUILayout.Height(20)))
                    {
                        LinkNodes(connectSourceQuest, q);
                        connectSourceQuest = null;
                        isConnectingNodes = false;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void LinkNodes(QuestData source, QuestData target)
        {
            if (source == null || target == null || source == target) return;

            Undo.RecordObject(source, "Link Quest Branch");
            Undo.RecordObject(target, "Link Quest Prerequisite");

            if (!source.NextQuestsOnComplete.Contains(target))
            {
                source.NextQuestsOnComplete.Add(target);
            }

            if (!target.Prerequisites.Contains(source))
            {
                target.Prerequisites.Add(source);
            }

            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            Debug.Log($"[VisualQuestCreator] Linked Branch: '{source.DisplayName}' ➔ '{target.DisplayName}'");
        }

        private void DrawInspectorPane()
        {
            EditorGUILayout.Space(5);

            if (selectedQuest == null || serializedSelectedQuest == null)
            {
                EditorGUILayout.HelpBox("Pilih Quest Node pada 2D Canvas di sebelah kiri untuk mengedit.", MessageType.Info);
                return;
            }

            serializedSelectedQuest.Update();
            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Editing: {selectedQuest.DisplayName}", EditorStyles.boldLabel);
            if (GUILayout.Button("Ping Asset", GUILayout.Width(75)))
            {
                EditorGUIUtility.PingObject(selectedQuest);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 1. Identity & Objectives
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("1. Identity & Objective", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("questId"));
            EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("description"));

            EditorGUILayout.Space(5);
            SerializedProperty objTypeProp = serializedSelectedQuest.FindProperty("objectiveType");
            EditorGUILayout.PropertyField(objTypeProp);

            QuestObjectiveType objType = (QuestObjectiveType)objTypeProp.enumValueIndex;
            switch (objType)
            {
                case QuestObjectiveType.TalkToNPC:
                    EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("targetNpcId"));
                    break;
                case QuestObjectiveType.DefeatEnemy:
                    EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("targetEnemyId"));
                    EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("targetKillCount"));
                    break;
                case QuestObjectiveType.CollectItem:
                    EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("targetItem"));
                    EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("targetItemAmount"));
                    break;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2. Branching Chains & Prerequisites
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("2. Branching Chains & Prerequisites", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("prerequisites"), true);
            EditorGUILayout.PropertyField(serializedSelectedQuest.FindProperty("nextQuestsOnComplete"), true);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 3. Multiple Rewards Manager
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("3. Multiple Rewards Manager", EditorStyles.boldLabel);
            SerializedProperty rewardsProp = serializedSelectedQuest.FindProperty("rewards");
            EditorGUILayout.PropertyField(rewardsProp, true);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Action Buttons & Live Debugger
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
            if (GUILayout.Button("💾 Save Quest Asset Changes", GUILayout.Height(30)))
            {
                serializedSelectedQuest.ApplyModifiedProperties();
                EditorUtility.SetDirty(selectedQuest);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[VisualQuestCreator] Saved: {selectedQuest.DisplayName}");
            }
            GUI.backgroundColor = Color.white;

            if (Application.isPlaying && QuestManager.Instance != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Live Debugger (Play Mode)", EditorStyles.boldLabel);

                QuestState state = QuestManager.Instance.GetQuestState(selectedQuest);
                EditorGUILayout.LabelField("Current Status:", state.ToString(), EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶️ Start"))
                {
                    QuestManager.Instance.StartQuest(selectedQuest);
                }
                if (GUILayout.Button("✅ Complete"))
                {
                    QuestManager.Instance.CompleteQuest(selectedQuest);
                }
                if (GUILayout.Button("🔄 Reset"))
                {
                    QuestManager.Instance.ResetQuest(selectedQuest);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            serializedSelectedQuest.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
        }

        private void AutoLayoutNodes()
        {
            float startX = 60f;
            float startY = 60f;
            float spacingX = 270f;
            float spacingY = 190f;

            for (int i = 0; i < allQuests.Count; i++)
            {
                QuestData q = allQuests[i];
                if (q == null) continue;

                Undo.RecordObject(q, "Auto Layout Nodes");
                int col = i % 4;
                int row = i / 4;

                q.NodePosition = new Vector2(startX + col * spacingX, startY + row * spacingY);
                EditorUtility.SetDirty(q);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[VisualQuestCreator] Arranged nodes in auto-layout grid.");
        }

        private void CreateNewQuestAsset()
        {
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
                AssetDatabase.Refresh();
            }

            QuestData newQuest = ScriptableObject.CreateInstance<QuestData>();
            newQuest.NodePosition = new Vector2(80f + (allQuests.Count % 4) * 260f, 80f + (allQuests.Count / 4) * 180f);

            string fileName = $"New_Quest_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
            string fullPath = Path.Combine(saveFolderPath, fileName).Replace("\\", "/");

            AssetDatabase.CreateAsset(newQuest, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ScanAllQuests();
            SelectQuest(newQuest);

            Debug.Log($"[VisualQuestCreator] Created new Quest asset at: {fullPath}");
        }

        private string GetObjectiveBadgeIcon(QuestObjectiveType type)
        {
            switch (type)
            {
                case QuestObjectiveType.TalkToNPC: return "🗣️";
                case QuestObjectiveType.ReachLocation: return "📍";
                case QuestObjectiveType.DefeatEnemy: return "⚔️";
                case QuestObjectiveType.CollectItem: return "🎒";
                default: return "📜";
            }
        }

        private string GetObjectiveSummary(QuestData quest)
        {
            switch (quest.ObjectiveType)
            {
                case QuestObjectiveType.TalkToNPC:
                    return $"Bicara NPC: \"{quest.TargetNpcId}\"";
                case QuestObjectiveType.ReachLocation:
                    return "Jelajahi lokasi target";
                case QuestObjectiveType.DefeatEnemy:
                    return $"Kalahkan {quest.TargetKillCount}x \"{quest.TargetEnemyId}\"";
                case QuestObjectiveType.CollectItem:
                    string itemName = quest.TargetItem != null ? quest.TargetItem.DisplayName : "Item";
                    return $"Kumpulkan {quest.TargetItemAmount}x \"{itemName}\"";
                default:
                    return "No objective";
            }
        }

        private string GetRewardsSummary(QuestData quest)
        {
            if (quest.Rewards == null || quest.Rewards.Count == 0) return "Tidak ada";

            List<string> list = new List<string>();
            foreach (var r in quest.Rewards)
            {
                if (r.item != null)
                {
                    list.Add($"{r.amount}x {r.item.DisplayName}");
                }
            }
            return list.Count > 0 ? string.Join(", ", list) : "Tidak ada";
        }
    }
}
