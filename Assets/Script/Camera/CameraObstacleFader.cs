using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mendeteksi rintangan (Batu / Pohon) yang menghalangi garis pandang kamera ke pemain,
/// lalu memicu efek semi-transparan secara otomatis melalui ObstacleFader.
/// Pasang script ini pada Main Camera atau PlayerFollowCamera.
/// </summary>
public class CameraObstacleFader : MonoBehaviour
{
    [Header("Target Player")]
    [Tooltip("Transform pemain. Jika kosong, script otomatis mencari GameObject ber-tag 'Player'.")]
    [SerializeField] private Transform playerTarget;

    [Tooltip("Offset titik pandang ke tubuh pemain (misal 1.0m untuk dada/torso).")]
    [SerializeField] private Vector3 playerOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Detection Settings")]
    [Tooltip("Radius SphereCast untuk mendeteksi rintangan antara kamera dan pemain.")]
    [SerializeField] private float sphereCastRadius = 0.4f;

    [Tooltip("Layer mask untuk rintangan yang bisa menjadi transparan (Default, Environment, dll).")]
    [SerializeField] private LayerMask obstacleLayerMask = ~0;

    [Header("Fade Settings")]
    [Tooltip("Tingkat transparansi target saat terhalang (0.35 = 35% terlihat).")]
    [Range(0.05f, 0.9f)]
    [SerializeField] private float targetAlpha = 0.35f;

    [Tooltip("Kecepatan transisi fade.")]
    [SerializeField] private float fadeSpeed = 6.0f;

    private readonly HashSet<ObstacleFader> currentFaders = new HashSet<ObstacleFader>();
    private readonly HashSet<ObstacleFader> previousFaders = new HashSet<ObstacleFader>();

    private void Start()
    {
        FindPlayerTarget();
    }

    private void FindPlayerTarget()
    {
        if (playerTarget != null) return;

        // 1. Cari via ThirdPersonController (komponen utama pemain)
        var controller = FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        if (controller != null)
        {
            playerTarget = controller.transform;
            return;
        }

        // 2. Cari via nama GameObject
        GameObject playerObj = GameObject.Find("PlayerArmature");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            return;
        }

        // 3. Cari via tag CinemachineTarget
        playerObj = GameObject.FindGameObjectWithTag("CinemachineTarget");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            return;
        }

        // 4. Cari via tag Player
        try
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                return;
            }
        }
        catch { }

        // 5. Cari via CharacterController
        var cc = FindFirstObjectByType<CharacterController>();
        if (cc != null)
        {
            playerTarget = cc.transform;
            return;
        }
    }

    /// <summary>
    /// Menghitung posisi pusat target pandang pemain dengan memperhitungkan CameraRoot atau offset.
    /// </summary>
    public Vector3 GetTargetPoint()
    {
        if (playerTarget == null) return transform.position;

        // Jika target adalah PlayerCameraRoot atau ber-tag CinemachineTarget, posisinya sudah berada di kepala/dada
        if (playerTarget.name.Contains("CameraRoot") || playerTarget.CompareTag("CinemachineTarget"))
        {
            return playerTarget.position;
        }

        return playerTarget.position + playerOffset;
    }

    /// <summary>
    /// Mencari GameObject root dari rintangan (seperti instance pohon atau batu)
    /// agar semua renderer child (seperti Leaves dan Trunk) memudar bersamaan.
    /// </summary>
    private GameObject FindObstacleTarget(Collider col)
    {
        if (col == null) return null;

        // Cek jika sudah memiliki fader terpasang
        ObstacleFader existingFader = col.GetComponent<ObstacleFader>();
        if (existingFader != null) return existingFader.gameObject;

        existingFader = col.GetComponentInParent<ObstacleFader>();
        if (existingFader != null) return existingFader.gameObject;

        Transform t = col.transform;

        // Jika collider berada di child mesh (misal Pine01-1-Leaves di bawah Pine01)
        if (t.parent != null)
        {
            string parentName = t.parent.name.ToLower();
            bool isParentAContainer = parentName.Contains("trees") || 
                                      parentName.Contains("rocks") || 
                                      parentName.Contains("scenery") || 
                                      parentName.Contains("environment");

            if (isParentAContainer)
            {
                // t adalah instance objek langsung di bawah container (misal Rock01)
                return t.gameObject;
            }
            else if (t.parent.parent != null)
            {
                string grandParentName = t.parent.parent.name.ToLower();
                bool isGrandParentAContainer = grandParentName.Contains("trees") || 
                                              grandParentName.Contains("rocks") || 
                                              grandParentName.Contains("scenery") || 
                                              grandParentName.Contains("environment");
                if (isGrandParentAContainer)
                {
                    // t.parent adalah instance objek (misal Pine01 (14)), sedangkan t adalah Leaves atau Trunk
                    return t.parent.gameObject;
                }
            }
        }

        // Fallback: gunakan t jika memiliki Renderer sendiri atau di child-nya
        if (t.GetComponentInChildren<Renderer>() != null)
        {
            return t.gameObject;
        }

        return col.gameObject;
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null) return;
        }

        Vector3 cameraPos = transform.position;
        Vector3 targetPos = GetTargetPoint();
        Vector3 direction = targetPos - cameraPos;
        float distance = direction.magnitude;

        if (distance <= 0.01f) return;

        // Siapkan set untuk frame ini
        previousFaders.Clear();
        foreach (var fader in currentFaders)
        {
            previousFaders.Add(fader);
        }
        currentFaders.Clear();

        // Tembakkan SphereCast dari kamera menuju pemain
        RaycastHit[] hits = Physics.SphereCastAll(
            cameraPos,
            sphereCastRadius,
            direction.normalized,
            distance,
            obstacleLayerMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider col = hit.collider;
            if (col == null) continue;

            // Jangan fade pemain sendiri atau terrain tanah
            if (col.CompareTag("Player") || col.transform.root == playerTarget.root)
                continue;
            if (col is TerrainCollider)
                continue;

            // Cari target objek yang tepat (instance pohon / batu)
            GameObject targetObj = FindObstacleTarget(col);
            if (targetObj == null) continue;

            ObstacleFader fader = targetObj.GetComponent<ObstacleFader>();
            if (fader == null)
            {
                // Pasang ObstacleFader secara dinamis jika memiliki Renderer
                Renderer rend = targetObj.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    fader = targetObj.AddComponent<ObstacleFader>();
                    fader.targetAlpha = targetAlpha;
                    fader.fadeSpeed = fadeSpeed;
                }
            }

            if (fader != null)
            {
                fader.targetAlpha = targetAlpha;
                fader.fadeSpeed = fadeSpeed;
                fader.SetObscuring(true);
                currentFaders.Add(fader);
            }
        }

        // Untuk rintangan yang frame lalu terhalang tapi sekarang sudah tidak terhalang
        foreach (ObstacleFader oldFader in previousFaders)
        {
            if (oldFader != null && !currentFaders.Contains(oldFader))
            {
                oldFader.SetObscuring(false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTarget != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 camPos = transform.position;
            Vector3 targetPos = GetTargetPoint();
            Gizmos.DrawLine(camPos, targetPos);
            Gizmos.DrawWireSphere(camPos, sphereCastRadius);
            Gizmos.DrawWireSphere(targetPos, sphereCastRadius);
        }
    }
}
