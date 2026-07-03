using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [Tooltip("Masukkan sprite/gambar urut dari awal sampai akhir")]
    public Sprite[] cutsceneImages;
    
    [Tooltip("Waktu tiap gambar tampil (dalam detik)")]
    public float timePerImage = 3f;
    
    [Tooltip("Nama scene selanjutnya yang akan diload setelah cutscene selesai")]
    public string nextSceneName = "scene game 1";

    [Header("UI Reference")]
    [Tooltip("Masukkan komponen UI Image yang akan menampilkan gambar")]
    public Image displayImage;

    private int currentIndex = 0;

    void Start()
    {
        // Pastikan array gambar tidak kosong dan displayImage sudah di-assign
        if (cutsceneImages.Length > 0 && displayImage != null)
        {
            // Tampilkan gambar pertama
            displayImage.sprite = cutsceneImages[0];
            // Mulai proses pergantian gambar
            StartCoroutine(PlayCutscene());
        }
        else
        {
            Debug.LogWarning("CutsceneManager: Array gambar masih kosong atau UI Image belum dimasukkan!");
        }
    }

    IEnumerator PlayCutscene()
    {
        // Tunggu selama waktu yang ditentukan
        yield return new WaitForSeconds(timePerImage);

        // Lanjut ke index gambar berikutnya
        currentIndex++;

        // Jika masih ada gambar yang tersisa
        if (currentIndex < cutsceneImages.Length)
        {
            // Ganti sprite ke gambar berikutnya
            displayImage.sprite = cutsceneImages[currentIndex];
            // Ulangi proses menunggu
            StartCoroutine(PlayCutscene());
        }
        else
        {
            // Jika gambar sudah habis, load scene selanjutnya
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Load scene berdasarkan nama
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("CutsceneManager: Nama Scene selanjutnya belum diisi!");
        }
    }
}
