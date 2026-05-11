using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Slot untuk Drag & Drop Scene
    public SceneField level1Scene; 

    public void PlayGame()
    {
        // Panggil manager buat pindah scene
        LoadingManager.Instance.LoadLevel(level1Scene);
    }
}