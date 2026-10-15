using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Data (Opsional)")]
    [Tooltip("Data cutscene. Jika di-assign, data gambar & durasi akan diambil dari sini.")]
    public CutsceneData cutsceneData;

    [Header("Fallback Cutscene Settings")]
    [Tooltip("Masukkan sprite/gambar urut dari awal sampai akhir (hanya digunakan jika cutsceneData kosong)")]
    public Sprite[] cutsceneImages;
    
    [Tooltip("Waktu tiap gambar tampil dalam detik (hanya digunakan jika cutsceneData kosong)")]
    public float timePerImage = 3f;
    
    [Tooltip("Nama scene selanjutnya yang akan diload setelah cutscene selesai")]
    public string nextSceneName = "scene game 1";

    [Header("UI Reference")]
    [Tooltip("Masukkan komponen UI Image yang akan menampilkan gambar")]
    public Image displayImage;

    [Header("Audio Reference")]
    [Tooltip("AudioSource untuk memutar BGM / SFX (opsional)")]
    public AudioSource audioSource;

    private int currentIndex = 0;

    void Start()
    {
        Sprite[] activeFrames = GetActiveFrames();

        if (activeFrames != null && activeFrames.Length > 0 && displayImage != null)
        {
            displayImage.sprite = activeFrames[0];
            PlayAudioForFrame(0);
            StartCoroutine(PlayCutscene());
        }
        else
        {
            Debug.LogWarning("CutsceneManager: Array gambar masih kosong atau UI Image belum dimasukkan!");
        }
    }

    IEnumerator PlayCutscene()
    {
        float duration = GetDurationForCurrentFrame();
        yield return new WaitForSeconds(duration);

        currentIndex++;
        Sprite[] activeFrames = GetActiveFrames();

        if (activeFrames != null && currentIndex < activeFrames.Length)
        {
            displayImage.sprite = activeFrames[currentIndex];
            PlayAudioForFrame(currentIndex);
            StartCoroutine(PlayCutscene());
        }
        else
        {
            LoadNextScene();
        }
    }

    private Sprite[] GetActiveFrames()
    {
        if (cutsceneData != null && cutsceneData.frames != null && cutsceneData.frames.Length > 0)
        {
            return cutsceneData.frames;
        }
        return cutsceneImages;
    }

    private float GetDurationForCurrentFrame()
    {
        if (cutsceneData != null)
        {
            return cutsceneData.GetFrameDuration(currentIndex);
        }
        return timePerImage > 0f ? timePerImage : 3f;
    }

    private void PlayAudioForFrame(int index)
    {
        if (cutsceneData == null || audioSource == null) return;

        // BGM di frame awal
        if (index == 0 && cutsceneData.bgmClip != null)
        {
            audioSource.clip = cutsceneData.bgmClip;
            audioSource.volume = cutsceneData.bgmVolume;
            audioSource.loop = true;
            audioSource.Play();
        }

        // SFX per frame
        AudioClip sfx = cutsceneData.GetFrameSFX(index);
        if (sfx != null)
        {
            audioSource.PlayOneShot(sfx, cutsceneData.sfxVolume);
        }
    }

    void LoadNextScene()
    {
        string targetScene = (cutsceneData != null && !string.IsNullOrEmpty(cutsceneData.loadSceneAfter)) 
            ? cutsceneData.loadSceneAfter 
            : nextSceneName;

        if (!string.IsNullOrEmpty(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogWarning("CutsceneManager: Nama Scene selanjutnya belum diisi!");
        }
    }
}
