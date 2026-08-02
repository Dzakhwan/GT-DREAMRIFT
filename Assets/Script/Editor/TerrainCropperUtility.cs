using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GTDreamrift.EditorTools
{
    public enum GameObjectHandlingMode
    {
        DisableOutside,
        DestroyOutside,
        IgnoreOutside
    }

    public static class TerrainCropperUtility
    {
        /// <summary>
        /// Crop selected region from sourceTerrain based on normalized bounds (0..1).
        /// </summary>
        public static Terrain Crop(
            Terrain sourceTerrain,
            Rect normalizedBounds,
            bool includeGameObjects,
            GameObjectHandlingMode objectHandlingMode,
            string saveFolderPath)
        {
            if (sourceTerrain == null || sourceTerrain.terrainData == null)
            {
                Debug.LogError("[TerrainCropper] Source Terrain or TerrainData is null!");
                return null;
            }

            // Clamp bounds to [0..1]
            normalizedBounds.x = Mathf.Clamp01(normalizedBounds.x);
            normalizedBounds.y = Mathf.Clamp01(normalizedBounds.y);
            normalizedBounds.width = Mathf.Clamp(normalizedBounds.width, 0.01f, 1f - normalizedBounds.x);
            normalizedBounds.height = Mathf.Clamp(normalizedBounds.height, 0.01f, 1f - normalizedBounds.y);

            TerrainData srcData = sourceTerrain.terrainData;

            // Ensure destination folder exists
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
                AssetDatabase.Refresh();
            }

            // Create new TerrainData asset
            TerrainData newTerrainData = new TerrainData();

            // Calculate new world size
            Vector3 newSize = new Vector3(
                srcData.size.x * normalizedBounds.width,
                srcData.size.y,
                srcData.size.z * normalizedBounds.height
            );

            // Set resolutions (keeping closest standard resolution or src resolution)
            int targetHeightmapRes = GetValidHeightmapResolution(srcData.heightmapResolution);
            newTerrainData.heightmapResolution = targetHeightmapRes;
            newTerrainData.size = newSize;

            // 1. Resample Heightmap
            float[,] srcHeights = srcData.GetHeights(0, 0, srcData.heightmapResolution, srcData.heightmapResolution);
            float[,] newHeights = ResampleHeightmap(srcHeights, srcData.heightmapResolution, targetHeightmapRes, normalizedBounds);
            newTerrainData.SetHeights(0, 0, newHeights);

            // 2. Resample Alphamaps / Textures
            if (srcData.terrainLayers != null && srcData.terrainLayers.Length > 0)
            {
                newTerrainData.terrainLayers = srcData.terrainLayers;
                int srcAlphaRes = srcData.alphamapResolution;
                int targetAlphaRes = Mathf.NextPowerOfTwo(srcAlphaRes);
                newTerrainData.alphamapResolution = targetAlphaRes;

                float[,,] srcAlphas = srcData.GetAlphamaps(0, 0, srcAlphaRes, srcAlphaRes);
                float[,,] newAlphas = ResampleAlphamap(srcAlphas, srcAlphaRes, targetAlphaRes, srcData.terrainLayers.Length, normalizedBounds);
                newTerrainData.SetAlphamaps(0, 0, newAlphas);
            }

            // 3. Detail Layers (Grass, Detail Prototypes)
            if (srcData.detailPrototypes != null && srcData.detailPrototypes.Length > 0)
            {
                newTerrainData.detailPrototypes = srcData.detailPrototypes;
                int srcDetailRes = srcData.detailResolution;
                newTerrainData.SetDetailResolution(srcDetailRes, srcData.detailResolutionPerPatch);

                for (int layer = 0; layer < srcData.detailPrototypes.Length; layer++)
                {
                    int[,] srcDetail = srcData.GetDetailLayer(0, 0, srcDetailRes, srcDetailRes, layer);
                    int[,] newDetail = ResampleDetailLayer(srcDetail, srcDetailRes, srcDetailRes, normalizedBounds);
                    newTerrainData.SetDetailLayer(0, 0, layer, newDetail);
                }
            }

            // 4. Tree Instances
            if (srcData.treePrototypes != null && srcData.treePrototypes.Length > 0)
            {
                newTerrainData.treePrototypes = srcData.treePrototypes;
                List<TreeInstance> newTrees = new List<TreeInstance>();
                TreeInstance[] srcTrees = srcData.treeInstances;

                foreach (var tree in srcTrees)
                {
                    if (tree.position.x >= normalizedBounds.x && tree.position.x <= normalizedBounds.xMax &&
                        tree.position.z >= normalizedBounds.y && tree.position.z <= normalizedBounds.yMax)
                    {
                        TreeInstance newTree = tree;
                        newTree.position = new Vector3(
                            (tree.position.x - normalizedBounds.x) / normalizedBounds.width,
                            tree.position.y,
                            (tree.position.z - normalizedBounds.y) / normalizedBounds.height
                        );
                        newTrees.Add(newTree);
                    }
                }
                newTerrainData.SetTreeInstances(newTrees.ToArray(), true);
            }

            // Save new TerrainData asset
            string assetName = $"Terrain_Crop_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
            string assetPath = Path.Combine(saveFolderPath, assetName).Replace("\\", "/");
            AssetDatabase.CreateAsset(newTerrainData, assetPath);
            AssetDatabase.SaveAssets();

            // Spawn new Terrain GameObject in Scene
            GameObject newTerrainGO = Terrain.CreateTerrainGameObject(newTerrainData);
            Undo.RegisterCreatedObjectUndo(newTerrainGO, "Crop Terrain");

            Vector3 newTerrainPos = new Vector3(
                sourceTerrain.transform.position.x + sourceTerrainDataWorldOffset(srcData.size.x, normalizedBounds.x),
                sourceTerrain.transform.position.y,
                sourceTerrain.transform.position.z + sourceTerrainDataWorldOffset(srcData.size.z, normalizedBounds.y)
            );
            newTerrainGO.transform.position = newTerrainPos;
            newTerrainGO.name = $"Cropped_{sourceTerrain.name}";

            // 5. Handle Scene GameObjects (Props, Rocks, Buildings) - Excluding UI/Canvas
            if (includeGameObjects)
            {
                ProcessGameObjects(sourceTerrain, newTerrainGO, normalizedBounds, objectHandlingMode);
            }

            Selection.activeGameObject = newTerrainGO;
            Debug.Log($"[TerrainCropper] Successfully created cropped terrain at: {assetPath}");
            return newTerrainGO.GetComponent<Terrain>();
        }

        /// <summary>
        /// Split sourceTerrain into a matrix grid (gridX x gridZ).
        /// </summary>
        public static List<Terrain> SplitGrid(
            Terrain sourceTerrain,
            int gridX,
            int gridZ,
            bool includeGameObjects,
            GameObjectHandlingMode objectHandlingMode,
            string saveFolderPath)
        {
            if (sourceTerrain == null || sourceTerrain.terrainData == null)
            {
                Debug.LogError("[TerrainCropper] Source Terrain is null!");
                return null;
            }

            gridX = Mathf.Max(1, gridX);
            gridZ = Mathf.Max(1, gridZ);

            List<Terrain> resultTerrains = new List<Terrain>();
            float tileWidthNorm = 1.0f / gridX;
            float tileHeightNorm = 1.0f / gridZ;

            GameObject gridParent = new GameObject($"SplitGrid_{sourceTerrain.name}");
            Undo.RegisterCreatedObjectUndo(gridParent, "Split Terrain Grid");

            for (int z = 0; z < gridZ; z++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Rect tileBounds = new Rect(
                        x * tileWidthNorm,
                        z * tileHeightNorm,
                        tileWidthNorm,
                        tileHeightNorm
                    );

                    Terrain tileTerrain = Crop(
                        sourceTerrain,
                        tileBounds,
                        includeGameObjects,
                        objectHandlingMode,
                        saveFolderPath
                    );

                    if (tileTerrain != null)
                    {
                        tileTerrain.name = $"{sourceTerrain.name}_Tile_{x}_{z}";
                        tileTerrain.transform.SetParent(gridParent.transform);
                        resultTerrains.Add(tileTerrain);
                    }
                }
            }

            // Connect neighbors for seamless lod & height rendering
            SetTerrainNeighbors(resultTerrains, gridX, gridZ);

            Debug.Log($"[TerrainCropper] Split terrain into {gridX}x{gridZ} ({resultTerrains.Count} tiles).");
            return resultTerrains;
        }

        private static float sourceTerrainDataWorldOffset(float totalWorldSize, float normalizedOffset)
        {
            return totalWorldSize * normalizedOffset;
        }

        private static int GetValidHeightmapResolution(int currentRes)
        {
            int power = Mathf.RoundToInt(Mathf.Log(currentRes - 1, 2));
            power = Mathf.Clamp(power, 5, 11);
            return (1 << power) + 1;
        }

        private static float[,] ResampleHeightmap(float[,] srcHeights, int srcRes, int targetRes, Rect bounds)
        {
            float[,] result = new float[targetRes, targetRes];
            for (int y = 0; y < targetRes; y++)
            {
                float normZ = Mathf.Lerp(bounds.y, bounds.yMax, (float)y / (targetRes - 1));
                float srcYF = normZ * (srcRes - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(srcYF), 0, srcRes - 1);
                int y1 = Mathf.Clamp(y0 + 1, 0, srcRes - 1);
                float ty = srcYF - y0;

                for (int x = 0; x < targetRes; x++)
                {
                    float normX = Mathf.Lerp(bounds.x, bounds.xMax, (float)x / (targetRes - 1));
                    float srcXF = normX * (srcRes - 1);
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(srcXF), 0, srcRes - 1);
                    int x1 = Mathf.Clamp(x0 + 1, 0, srcRes - 1);
                    float tx = srcXF - x0;

                    float h00 = srcHeights[y0, x0];
                    float h10 = srcHeights[y0, x1];
                    float h01 = srcHeights[y1, x0];
                    float h11 = srcHeights[y1, x1];

                    float h0 = Mathf.Lerp(h00, h10, tx);
                    float h1 = Mathf.Lerp(h01, h11, tx);
                    result[y, x] = Mathf.Lerp(h0, h1, ty);
                }
            }
            return result;
        }

        private static float[,,] ResampleAlphamap(float[,,] srcAlphas, int srcRes, int targetRes, int layers, Rect bounds)
        {
            float[,,] result = new float[targetRes, targetRes, layers];
            for (int y = 0; y < targetRes; y++)
            {
                float normZ = Mathf.Lerp(bounds.y, bounds.yMax, (float)y / (targetRes - 1));
                float srcYF = normZ * (srcRes - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(srcYF), 0, srcRes - 1);
                int y1 = Mathf.Clamp(y0 + 1, 0, srcRes - 1);
                float ty = srcYF - y0;

                for (int x = 0; x < targetRes; x++)
                {
                    float normX = Mathf.Lerp(bounds.x, bounds.xMax, (float)x / (targetRes - 1));
                    float srcXF = normX * (srcRes - 1);
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(srcXF), 0, srcRes - 1);
                    int x1 = Mathf.Clamp(x0 + 1, 0, srcRes - 1);
                    float tx = srcXF - x0;

                    for (int l = 0; l < layers; l++)
                    {
                        float a00 = srcAlphas[y0, x0, l];
                        float a10 = srcAlphas[y0, x1, l];
                        float a01 = srcAlphas[y1, x0, l];
                        float a11 = srcAlphas[y1, x1, l];

                        float a0 = Mathf.Lerp(a00, a10, tx);
                        float a1 = Mathf.Lerp(a01, a11, tx);
                        result[y, x, l] = Mathf.Lerp(a0, a1, ty);
                    }
                }
            }
            return result;
        }

        private static int[,] ResampleDetailLayer(int[,] srcDetail, int srcRes, int targetRes, Rect bounds)
        {
            int[,] result = new int[targetRes, targetRes];
            for (int y = 0; y < targetRes; y++)
            {
                float normZ = Mathf.Lerp(bounds.y, bounds.yMax, (float)y / (targetRes - 1));
                int srcY = Mathf.Clamp(Mathf.RoundToInt(normZ * (srcRes - 1)), 0, srcRes - 1);

                for (int x = 0; x < targetRes; x++)
                {
                    float normX = Mathf.Lerp(bounds.x, bounds.xMax, (float)x / (targetRes - 1));
                    int srcX = Mathf.Clamp(Mathf.RoundToInt(normX * (srcRes - 1)), 0, srcRes - 1);

                    result[y, x] = srcDetail[srcY, srcX];
                }
            }
            return result;
        }

        private static bool IsCanvasOrUI(Transform t)
        {
            if (t == null) return false;
            if (t.GetComponent<Canvas>() != null) return true;
            if (t.GetComponent<RectTransform>() != null) return true;
            if (t.GetComponentInParent<Canvas>(true) != null) return true;
            if (t.GetComponentInParent<CanvasGroup>(true) != null) return true;

            Transform curr = t.parent;
            while (curr != null)
            {
                if (curr.GetComponent<Canvas>() != null || curr.name.ToLower().Contains("canvas"))
                    return true;
                curr = curr.parent;
            }
            return false;
        }

        private static void ProcessGameObjects(
            Terrain sourceTerrain,
            GameObject newTerrainGO,
            Rect normalizedBounds,
            GameObjectHandlingMode handlingMode)
        {
            Vector3 srcPos = sourceTerrain.transform.position;
            Vector3 srcSize = sourceTerrain.terrainData.size;

            float cropXMin = srcPos.x + srcSize.x * normalizedBounds.x;
            float cropXMax = srcPos.x + srcSize.x * normalizedBounds.xMax;
            float cropZMin = srcPos.z + srcSize.z * normalizedBounds.y;
            float cropZMax = srcPos.z + srcSize.z * normalizedBounds.yMax;

            // Full bounds of source terrain to identify candidate objects
            float srcXMin = srcPos.x;
            float srcXMax = srcPos.x + srcSize.x;
            float srcZMin = srcPos.z;
            float srcZMax = srcPos.z + srcSize.z;

            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            List<GameObject> candidates = new List<GameObject>();
            foreach (var root in rootObjects)
            {
                if (root == sourceTerrain.gameObject || root == newTerrainGO)
                    continue;

                // Protect root object if it is a Canvas or UI hierarchy
                if (IsCanvasOrUI(root.transform))
                    continue;

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t.gameObject == root && transforms.Length > 1) continue;

                    // Protect any child object that lives under a Canvas parent or is a UI element
                    if (IsCanvasOrUI(t))
                        continue;

                    Vector3 p = t.position;
                    if (p.x >= srcXMin && p.x <= srcXMax && p.z >= srcZMin && p.z <= srcZMax)
                    {
                        candidates.Add(t.gameObject);
                    }
                }
            }

            foreach (var obj in candidates)
            {
                if (obj == null) continue;
                Vector3 p = obj.transform.position;

                bool isInsideCropBox = (p.x >= cropXMin && p.x <= cropXMax && p.z >= cropZMin && p.z <= cropZMax);

                if (isInsideCropBox)
                {
                    // Keep original hierarchy position! Do not reparent under cropped terrain.
                }
                else
                {
                    if (handlingMode == GameObjectHandlingMode.DisableOutside)
                    {
                        Undo.RecordObject(obj, "Disable Outside Object");
                        obj.SetActive(false);
                    }
                    else if (handlingMode == GameObjectHandlingMode.DestroyOutside)
                    {
                        Undo.DestroyObjectImmediate(obj);
                    }
                }
            }
        }

        private static void SetTerrainNeighbors(List<Terrain> terrains, int gridX, int gridZ)
        {
            for (int z = 0; z < gridZ; z++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    int index = z * gridX + x;
                    if (index >= terrains.Count) continue;

                    Terrain current = terrains[index];
                    Terrain left = (x > 0) ? terrains[z * gridX + (x - 1)] : null;
                    Terrain right = (x < gridX - 1) ? terrains[z * gridX + (x + 1)] : null;
                    Terrain top = (z < gridZ - 1) ? terrains[(z + 1) * gridX + x] : null;
                    Terrain bottom = (z > 0) ? terrains[(z - 1) * gridX + x] : null;

                    current.SetNeighbors(left, top, right, bottom);
                }
            }
        }
    }
}
