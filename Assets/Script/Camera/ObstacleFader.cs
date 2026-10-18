using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Menangani efek fade transparansi pada objek rintangan (Batu / Pohon)
/// ketika menghalangi pandangan kamera ke karakter pemain.
/// Mendukung shader URP standar dan FlatKit StylizedSurface.
/// </summary>
public class ObstacleFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Tingkat transparansi target saat terhalang (0.0 = tak terlihat, 1.0 = padat).")]
    [Range(0.05f, 0.95f)]
    public float targetAlpha = 0.35f;

    [Tooltip("Kecepatan transisi transparansi.")]
    public float fadeSpeed = 6.0f;

    private struct MaterialData
    {
        public Material material;
        public Color originalBaseColor;
        public Color originalColorDim;
        public float originalSurface;
        public int originalSrcBlend;
        public int originalDstBlend;
        public int originalZWrite;
        public int originalRenderQueue;
        public bool hasColorDim;
    }

    private readonly List<MaterialData> materialDataList = new List<MaterialData>();
    private float currentAlpha = 1.0f;
    private bool isObscuring = false;
    private bool isTransparentMode = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ColorDimId = Shader.PropertyToID("_ColorDim");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    private void Awake()
    {
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        materialDataList.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            // Menggunakan materials (instance) agar tidak merusak asset file di disk
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null) continue;

                MaterialData data = new MaterialData
                {
                    material = mat,
                    originalBaseColor = mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white,
                    originalColorDim = mat.HasProperty(ColorDimId) ? mat.GetColor(ColorDimId) : Color.gray,
                    hasColorDim = mat.HasProperty(ColorDimId),
                    originalSurface = mat.HasProperty(SurfaceId) ? mat.GetFloat(SurfaceId) : 0f,
                    originalSrcBlend = mat.HasProperty(SrcBlendId) ? mat.GetInt(SrcBlendId) : (int)BlendMode.One,
                    originalDstBlend = mat.HasProperty(DstBlendId) ? mat.GetInt(DstBlendId) : (int)BlendMode.Zero,
                    originalZWrite = mat.HasProperty(ZWriteId) ? mat.GetInt(ZWriteId) : 1,
                    originalRenderQueue = mat.renderQueue
                };

                materialDataList.Add(data);
            }
        }
    }

    /// <summary>
    /// Mengatur apakah objek ini sedang menghalangi kamera atau tidak.
    /// </summary>
    public void SetObscuring(bool obscuring)
    {
        isObscuring = obscuring;
    }

    private void Update()
    {
        float target = isObscuring ? targetAlpha : 1.0f;

        if (Mathf.Abs(currentAlpha - target) > 0.005f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, target, fadeSpeed * Time.deltaTime);
            ApplyAlpha(currentAlpha);
        }
        else if (!isObscuring && isTransparentMode)
        {
            // Sudah kembali ke 1.0 sepenuhnya, kembalikan ke Opaque render mode
            currentAlpha = 1.0f;
            ApplyAlpha(currentAlpha);
            RestoreOpaqueMode();
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (alpha < 0.99f && !isTransparentMode)
        {
            SetTransparentMode();
        }

        for (int i = 0; i < materialDataList.Count; i++)
        {
            MaterialData data = materialDataList[i];
            if (data.material == null) continue;

            // Update _BaseColor & _Color
            Color c = data.originalBaseColor;
            c.a = data.originalBaseColor.a * alpha;
            if (data.material.HasProperty(BaseColorId))
                data.material.SetColor(BaseColorId, c);
            if (data.material.HasProperty(ColorId))
                data.material.SetColor(ColorId, c);

            // Update _ColorDim (FlatKit shadow color)
            if (data.hasColorDim)
            {
                Color dim = data.originalColorDim;
                dim.a = data.originalColorDim.a * alpha;
                data.material.SetColor(ColorDimId, dim);
            }
        }
    }

    private void SetTransparentMode()
    {
        isTransparentMode = true;

        for (int i = 0; i < materialDataList.Count; i++)
        {
            Material mat = materialDataList[i].material;
            if (mat == null) continue;

            if (mat.HasProperty(SurfaceId))
                mat.SetFloat(SurfaceId, 1.0f); // Transparent surface

            if (mat.HasProperty(SrcBlendId))
                mat.SetInt(SrcBlendId, (int)BlendMode.SrcAlpha);

            if (mat.HasProperty(DstBlendId))
                mat.SetInt(DstBlendId, (int)BlendMode.OneMinusSrcAlpha);

            if (mat.HasProperty(ZWriteId))
                mat.SetInt(ZWriteId, 0); // No ZWrite for transparent pass

            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }

    private void RestoreOpaqueMode()
    {
        isTransparentMode = false;

        for (int i = 0; i < materialDataList.Count; i++)
        {
            MaterialData data = materialDataList[i];
            Material mat = data.material;
            if (mat == null) continue;

            if (mat.HasProperty(SurfaceId))
                mat.SetFloat(SurfaceId, data.originalSurface);

            if (mat.HasProperty(SrcBlendId))
                mat.SetInt(SrcBlendId, data.originalSrcBlend);

            if (mat.HasProperty(DstBlendId))
                mat.SetInt(DstBlendId, data.originalDstBlend);

            if (mat.HasProperty(ZWriteId))
                mat.SetInt(ZWriteId, data.originalZWrite);

            mat.renderQueue = data.originalRenderQueue > 0 ? data.originalRenderQueue : (int)RenderQueue.Geometry;
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHABLEND_ON");

            // Kembalikan warna asli persis
            if (mat.HasProperty(BaseColorId))
                mat.SetColor(BaseColorId, data.originalBaseColor);
            if (mat.HasProperty(ColorId))
                mat.SetColor(ColorId, data.originalBaseColor);
            if (data.hasColorDim)
                mat.SetColor(ColorDimId, data.originalColorDim);
        }
    }

    private void OnDestroy()
    {
        // Bersihkan material instances
        for (int i = 0; i < materialDataList.Count; i++)
        {
            if (materialDataList[i].material != null)
            {
                Destroy(materialDataList[i].material);
            }
        }
    }
}
