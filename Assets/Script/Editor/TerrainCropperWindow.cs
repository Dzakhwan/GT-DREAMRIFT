using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GTDreamrift.EditorTools
{
    public class TerrainCropperWindow : EditorWindow
    {
        public enum CropperMode
        {
            RectCrop,
            GridSplit
        }

        [SerializeField] private Terrain targetTerrain;
        [SerializeField] private CropperMode mode = CropperMode.RectCrop;
        [SerializeField] private Rect normalizedCropRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
        [SerializeField] private int gridX = 2;
        [SerializeField] private int gridZ = 2;
        [SerializeField] private bool includeGameObjects = true;
        [SerializeField] private GameObjectHandlingMode objectHandlingMode = GameObjectHandlingMode.DisableOutside;
        [SerializeField] private string saveFolderPath = "Assets/TerrainCrops";

        private Vector2 scrollPos;

        [MenuItem("Tools/Terrain/Interactive Cropper")]
        public static void ShowWindow()
        {
            TerrainCropperWindow window = GetWindow<TerrainCropperWindow>("Terrain Cropper");
            window.minSize = new Vector2(350, 520);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            AutoPickTerrain();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSelectionChange()
        {
            AutoPickTerrain();
            Repaint();
        }

        private void AutoPickTerrain()
        {
            if (Selection.activeGameObject != null)
            {
                Terrain t = Selection.activeGameObject.GetComponent<Terrain>();
                if (t != null)
                {
                    targetTerrain = t;
                }
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(10);
            GUILayout.Label("Terrain & GameObject Cropper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tool ini memungkinkan Anda memotong (crop) atau membagi (split) Terrain beserta GameObject dekorasi di atasnya secara interaktif.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Target Terrain Selector
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("1. Target Selection", EditorStyles.boldLabel);
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);

            if (targetTerrain == null)
            {
                EditorGUILayout.HelpBox("Pilih Terrain di Hierarchy atau assign Terrain di field atas.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }

            Vector3 tSize = targetTerrain.terrainData.size;
            EditorGUILayout.LabelField("Terrain Size", $"{tSize.x}m x {tSize.y}m (Height) x {tSize.z}m");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Mode Selector
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("2. Cropping Mode", EditorStyles.boldLabel);
            mode = (CropperMode)EditorGUILayout.EnumPopup("Operation Mode", mode);

            EditorGUILayout.Space(5);

            if (mode == CropperMode.RectCrop)
            {
                GUILayout.Label("Normalized Bounds (0.0 - 1.0)", EditorStyles.boldLabel);
                normalizedCropRect.x = EditorGUILayout.Slider("Min X", normalizedCropRect.x, 0.0f, 0.95f);
                normalizedCropRect.y = EditorGUILayout.Slider("Min Z", normalizedCropRect.y, 0.0f, 0.95f);
                normalizedCropRect.width = EditorGUILayout.Slider("Width", normalizedCropRect.width, 0.05f, 1.0f - normalizedCropRect.x);
                normalizedCropRect.height = EditorGUILayout.Slider("Length (Z)", normalizedCropRect.height, 0.05f, 1.0f - normalizedCropRect.y);

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset to Full Area"))
                {
                    normalizedCropRect = new Rect(0f, 0f, 1f, 1f);
                }
                if (GUILayout.Button("Center 50% Box"))
                {
                    normalizedCropRect = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
                }
                EditorGUILayout.EndHorizontal();

                // World size info
                float cropW = tSize.x * normalizedCropRect.width;
                float cropH = tSize.z * normalizedCropRect.height;
                EditorGUILayout.HelpBox($"Output Area World Dimensions: {cropW:F1}m x {cropH:F1}m", MessageType.None);
            }
            else if (mode == CropperMode.GridSplit)
            {
                GUILayout.Label("Grid Partition Matrix", EditorStyles.boldLabel);
                gridX = EditorGUILayout.IntSlider("Columns (X Grid)", gridX, 1, 10);
                gridZ = EditorGUILayout.IntSlider("Rows (Z Grid)", gridZ, 1, 10);

                float tileW = tSize.x / gridX;
                float tileH = tSize.z / gridZ;
                EditorGUILayout.HelpBox($"Akan membagi Terrain menjadi {gridX * gridZ} tiles.\nSetiap tile berukuran: {tileW:F1}m x {tileH:F1}m", MessageType.None);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // GameObject & Save Options
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("3. Options & Output Path", EditorStyles.boldLabel);
            includeGameObjects = EditorGUILayout.Toggle("Include GameObjects", includeGameObjects);

            if (includeGameObjects)
            {
                objectHandlingMode = (GameObjectHandlingMode)EditorGUILayout.EnumPopup("Outside Objects Action", objectHandlingMode);
                EditorGUILayout.HelpBox("Catatan: Elemen UI / Canvas terlindungi secara otomatis dan tidak akan di-disable/dihapus.", MessageType.None);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            saveFolderPath = EditorGUILayout.TextField("Save Folder", saveFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Folder for TerrainData", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        saveFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        saveFolderPath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // Execution Button
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (mode == CropperMode.RectCrop)
            {
                if (GUILayout.Button("Crop Selected Region", GUILayout.Height(35)))
                {
                    ExecuteCrop();
                }
            }
            else
            {
                if (GUILayout.Button("Split into Grid Tiles", GUILayout.Height(35)))
                {
                    ExecuteSplit();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private void ExecuteCrop()
        {
            if (targetTerrain == null) return;
            TerrainCropperUtility.Crop(
                targetTerrain,
                normalizedCropRect,
                includeGameObjects,
                objectHandlingMode,
                saveFolderPath
            );
        }

        private void ExecuteSplit()
        {
            if (targetTerrain == null) return;
            TerrainCropperUtility.SplitGrid(
                targetTerrain,
                gridX,
                gridZ,
                includeGameObjects,
                objectHandlingMode,
                saveFolderPath
            );
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (targetTerrain == null || targetTerrain.terrainData == null) return;

            Vector3 tPos = targetTerrain.transform.position;
            Vector3 tSize = targetTerrain.terrainData.size;

            if (mode == CropperMode.RectCrop)
            {
                DrawRectCropHandles(tPos, tSize);
            }
            else if (mode == CropperMode.GridSplit)
            {
                DrawGridSplitVisualization(tPos, tSize);
            }
        }

        private void DrawRectCropHandles(Vector3 tPos, Vector3 tSize)
        {
            float minX = tPos.x + tSize.x * normalizedCropRect.x;
            float maxX = tPos.x + tSize.x * normalizedCropRect.xMax;
            float minZ = tPos.z + tSize.z * normalizedCropRect.y;
            float maxZ = tPos.z + tSize.z * normalizedCropRect.yMax;
            float yPos = tPos.y + 0.5f;

            Vector3 cornerSW = new Vector3(minX, yPos, minZ);
            Vector3 cornerSE = new Vector3(maxX, yPos, minZ);
            Vector3 cornerNE = new Vector3(maxX, yPos, maxZ);
            Vector3 cornerNW = new Vector3(minX, yPos, maxZ);

            // Draw bounding box wireframe
            Handles.color = Color.red;
            Handles.DrawPolyLine(cornerSW, cornerSE, cornerNE, cornerNW, cornerSW);

            // Draw semi-transparent fill
            Vector3[] verts = new Vector3[] { cornerSW, cornerSE, cornerNE, cornerNW };
            Handles.DrawSolidRectangleWithOutline(verts, new Color(1f, 0f, 0f, 0.15f), Color.yellow);

            // Handle controls for corners
            float handleSize = HandleUtility.GetHandleSize(cornerSW) * 0.15f;
            Handles.color = Color.yellow;

            // Interactive Move Corner SW (MinX, MinZ)
            EditorGUI.BeginChangeCheck();
            Vector3 newSW = Handles.FreeMoveHandle(cornerSW, handleSize, Vector3.zero, Handles.RectangleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                float newNormX = Mathf.Clamp((newSW.x - tPos.x) / tSize.x, 0f, normalizedCropRect.xMax - 0.05f);
                float newNormZ = Mathf.Clamp((newSW.z - tPos.z) / tSize.z, 0f, normalizedCropRect.yMax - 0.05f);
                normalizedCropRect.width = normalizedCropRect.xMax - newNormX;
                normalizedCropRect.height = normalizedCropRect.yMax - newNormZ;
                normalizedCropRect.x = newNormX;
                normalizedCropRect.y = newNormZ;
                Repaint();
            }

            // Interactive Move Corner NE (MaxX, MaxZ)
            EditorGUI.BeginChangeCheck();
            Vector3 newNE = Handles.FreeMoveHandle(cornerNE, handleSize, Vector3.zero, Handles.RectangleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                float newMaxX = Mathf.Clamp((newNE.x - tPos.x) / tSize.x, normalizedCropRect.x + 0.05f, 1.0f);
                float newMaxZ = Mathf.Clamp((newNE.z - tPos.z) / tSize.z, normalizedCropRect.y + 0.05f, 1.0f);
                normalizedCropRect.width = newMaxX - normalizedCropRect.x;
                normalizedCropRect.height = newMaxZ - normalizedCropRect.y;
                Repaint();
            }
        }

        private void DrawGridSplitVisualization(Vector3 tPos, Vector3 tSize)
        {
            float yPos = tPos.y + 0.5f;

            Handles.color = Color.cyan;

            // Draw X lines
            float tileWidth = tSize.x / gridX;
            for (int i = 0; i <= gridX; i++)
            {
                float x = tPos.x + i * tileWidth;
                Handles.DrawLine(new Vector3(x, yPos, tPos.z), new Vector3(x, yPos, tPos.z + tSize.z));
            }

            // Draw Z lines
            float tileHeight = tSize.z / gridZ;
            for (int j = 0; j <= gridZ; j++)
            {
                float z = tPos.z + j * tileHeight;
                Handles.DrawLine(new Vector3(tPos.x, yPos, z), new Vector3(tPos.x + tSize.x, yPos, z));
            }
        }
    }
}
