using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    // Slot untuk Drag & Drop Scene tujuan (Tarik file scene 'UIPrototype' ke sini di Inspector)
    public SceneField level1Scene; 

    // Fungsi ini dipanggil saat tombol START GAME diklik
    public void PlayGame()
    {
        // Memastikan LoadingManager ada sebelum memanggil fungsi pindah scene
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadLevel(level1Scene);
        }
        else
        {
            Debug.LogError("LoadingManager tidak ditemukan di scene!");
        }
    }
}