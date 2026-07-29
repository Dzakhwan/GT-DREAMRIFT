using UnityEngine;

/// <summary>
/// Pasang script ini ke Canvas World Space yang ada di atas kepala Enemy.
/// Script ini akan membuat Canvas selalu menghadap kamera utama (Billboard effect).
/// </summary>
public class FaceCamera : MonoBehaviour
{
    [Tooltip("Biarkan kosong, otomatis pakai Main Camera")]
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    [Tooltip("Seberapa sering rotasi diperbarui (detik). 0 = setiap frame, 0.05 = 20x/detik")]
    [SerializeField] private float updateInterval = 0.05f;
    private float timer;

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // Perbarui rotasi sesuai interval, bukan setiap frame → lebih hemat
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        transform.rotation = Quaternion.LookRotation(
            transform.position - targetCamera.transform.position
        );
    }
}
