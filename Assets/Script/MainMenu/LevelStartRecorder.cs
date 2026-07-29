using UnityEngine;

// Taruh script ini di GameObject mana saja di SETIAP scene level gameplay
// (misal di GameObject yang sama dengan LoadingManager, atau GameObject baru khusus).
// Tugasnya cuma satu: lapor ke ProgressManager "level ini yang baru saja dimainkan".
public class LevelStartRecorder : MonoBehaviour
{
    [Header("Level Identity")]
    // Isi manual sesuai level ini, misal "CutScene1", "Level1", "Level2", dst.
    // Harus sama persis dengan Level Id yang diisi di MainMenuEnvironmentLoader
    public string levelId = "Level1";

    void Start()
    {
        if (ProgressManager.Instance != null)
        {
            ProgressManager.Instance.SetLastPlayedLevel(levelId);
        }
        else
        {
            Debug.LogWarning("[LevelStartRecorder] ProgressManager tidak ditemukan. " +
                "Pastikan scene Main Menu (yang berisi ProgressManager) pernah dimuat duluan sebelum scene ini.");
        }
    }
}
