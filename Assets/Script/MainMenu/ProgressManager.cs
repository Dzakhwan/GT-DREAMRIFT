using UnityEngine;

// Taruh script ini di GameObject kosong bernama "ProgressManager" di scene Main Menu.
// Sama seperti GameSettingsManager, GameObject ini persistent (DontDestroyOnLoad),
// jadi datanya tetap ada walau pindah-pindah scene.
public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    private const string LAST_LEVEL_KEY = "LastPlayedLevel";

    // Default kalau belum pernah ada progress tersimpan sama sekali (pemain baru)
    // Pemain baru akan melihat CutScene1 dulu sebagai background Main Menu
    private const string DEFAULT_LEVEL_ID = "CutScene1";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Dipanggil oleh LevelStartRecorder setiap kali sebuah level scene dimulai
    public void SetLastPlayedLevel(string levelId)
    {
        PlayerPrefs.SetString(LAST_LEVEL_KEY, levelId);
        Debug.Log("[ProgressManager] Level terakhir tersimpan: " + levelId);
    }

    // Dipanggil oleh MainMenuBackground untuk tau background mana yang harus ditampilkan
    public string GetLastPlayedLevel()
    {
        return PlayerPrefs.GetString(LAST_LEVEL_KEY, DEFAULT_LEVEL_ID);
    }
}
