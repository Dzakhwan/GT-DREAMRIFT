using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDetector : MonoBehaviour
{
    [SerializeField] private string[] disabledScenes = { "Main Menu", "CutScene1", "Room" };
    [SerializeField] private bool logToConsole = false;

    private PlayerFight playerFight;

    private void Awake()
    {
        playerFight = GetComponent<PlayerFight>();
    }

    private void Start()
    {
        if (playerFight == null) return;

        string currentScene = SceneManager.GetActiveScene().name;

        foreach (string sceneName in disabledScenes)
        {
            if (currentScene == sceneName)
            {
                playerFight.enabled = false;
                if (logToConsole)
                    Debug.Log($"[SceneDetector] PlayerFight disabled di scene '{currentScene}'");
                return;
            }
        }

        playerFight.enabled = true;
        if (logToConsole)
            Debug.Log($"[SceneDetector] PlayerFight enabled di scene '{currentScene}'");
    }
}
