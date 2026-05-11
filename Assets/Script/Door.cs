using UnityEngine;

public class Door : MonoBehaviour
{
    // Slot untuk Drag & Drop Scene selanjutnya
    public SceneField nextLevelScene; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Panggil manager buat pindah ke level berikutnya
            LoadingManager.Instance.LoadLevel(nextLevelScene);
        }
    }
}