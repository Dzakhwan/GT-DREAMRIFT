using UnityEngine;

/// <summary>
/// Mengatur jarak render (culling distance) per Layer pada Kamera.
/// Memastikan objek di luar radius pandang kamera langsung di-cull dan tidak dirender.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraCullingDistance : MonoBehaviour
{
    [Header("Layer Cull Distances (Meters)")]
    [Tooltip("Jarak maksimal untuk merender musuh (0 = pakai camera far clip plane).")]
    [SerializeField] private float enemyCullDistance = 45f;

    private void Start()
    {
        ApplyLayerCullDistances();
    }

    public void ApplyLayerCullDistances()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        float[] distances = new float[32];
        for (int i = 0; i < 32; i++)
        {
            distances[i] = cam.farClipPlane;
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0 && enemyLayer < 32 && enemyCullDistance > 0f)
        {
            distances[enemyLayer] = enemyCullDistance;
        }

        cam.layerCullDistances = distances;
    }
}
