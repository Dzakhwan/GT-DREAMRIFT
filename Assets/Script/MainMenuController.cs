using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    // Slot untuk Drag & Drop Scene tujuan (Tarik file scene 'UIPrototype' ke sini di Inspector)
    public SceneField level1Scene;

    [Header("Panel References")]
    // Slot untuk Drag & Drop Panel Settings (Tarik GameObject 'Setting Panel' ke sini di Inspector)
    public GameObject settingsPanel;

    // Slot untuk Drag & Drop Panel Credits (Tarik GameObject 'Credits Panel' ke sini di Inspector)
    public GameObject creditsPanel;

    // Fungsi ini dipanggil saat tombol NEW GAME diklik
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

    // Fungsi ini dipanggil saat tombol SETTINGS diklik
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("SettingsPanel belum di-assign di Inspector!");
        }
    }

    // Fungsi ini dipanggil saat tombol X di panel Settings diklik
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Fungsi ini dipanggil saat tombol CREDITS diklik (dari dalam panel Settings)
    // Settings otomatis nonaktif, diganti tampil Credits
    public void OpenCredits()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("CreditsPanel belum di-assign di Inspector!");
        }
    }

    // Fungsi ini dipanggil saat tombol close di panel Credits diklik
    // Balik lagi ke panel Settings (bukan langsung nutup semua)
    public void CloseCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // Fungsi ini dipanggil saat tombol EXIT GAME diklik
    public void QuitGame()
    {
        Debug.Log("Keluar dari game...");
        Application.Quit();

#if UNITY_EDITOR
        // Supaya bisa ditest langsung di Unity Editor (Application.Quit tidak jalan di Editor)
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}