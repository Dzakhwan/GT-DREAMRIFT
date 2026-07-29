using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// Load scene environment secara ADDITIVE di belakang Canvas Main Menu (tanpa duplikasi objek).
// Environment di-PAUSE (Time.timeScale = 0) begitu selesai dimuat, supaya animasi/cerita
// tidak langsung main sendiri -- baru lanjut jalan begitu ActivateLoadedEnvironment() dipanggil
// (misal saat tombol New Game diklik).
public class MainMenuEnvironmentLoader : MonoBehaviour
{
    [Serializable]
    public class EnvironmentEntry
    {
        // Harus sama persis dengan levelId yang dicatat LevelStartRecorder
        public string levelId;

        // Nama scene yang berisi environment ini (harus terdaftar di Build Settings)
        public string environmentSceneName;
    }

    [Header("Environment per Level")]
    public EnvironmentEntry[] environmentEntries;

    private string currentLoadedScene;
    private List<Camera> disabledCameras = new List<Camera>();
    private List<AudioListener> disabledListeners = new List<AudioListener>();
    private List<GameObject> disabledEventSystems = new List<GameObject>();
    private List<Canvas> pushedBackCanvases = new List<Canvas>();

    void Start()
    {
        LoadEnvironmentForCurrentProgress();
    }

    public void LoadEnvironmentForCurrentProgress()
    {
        if (ProgressManager.Instance == null)
        {
            Debug.LogWarning("[MainMenuEnvironmentLoader] ProgressManager belum ada di scene!");
            return;
        }

        string lastLevel = ProgressManager.Instance.GetLastPlayedLevel();
        string sceneToLoad = null;

        foreach (EnvironmentEntry entry in environmentEntries)
        {
            if (entry.levelId == lastLevel)
            {
                sceneToLoad = entry.environmentSceneName;
                break;
            }
        }

        if (sceneToLoad == null && environmentEntries.Length > 0)
        {
            sceneToLoad = environmentEntries[0].environmentSceneName;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            StartCoroutine(LoadEnvironmentRoutine(sceneToLoad));
        }
    }

    private IEnumerator LoadEnvironmentRoutine(string sceneName)
    {
        if (!string.IsNullOrEmpty(currentLoadedScene) && currentLoadedScene != sceneName)
        {
            yield return SceneManager.UnloadSceneAsync(currentLoadedScene);
            currentLoadedScene = null;
        }

        if (currentLoadedScene != sceneName)
        {
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return loadOp;
            currentLoadedScene = sceneName;

            PrepareLoadedSceneAsBackground(sceneName);

            // Freeze semua Animator/script yang bergerak berdasarkan Time.deltaTime
            Time.timeScale = 0f;
        }
    }

    // Menyiapkan scene yang baru di-load supaya jadi background yang "aman":
    // - Kamera/AudioListener/EventSystem tambahan dimatikan (dicatat, supaya bisa dihidupkan lagi nanti)
    // - Canvas yang ada di scene itu didorong ke belakang Canvas Main Menu (supaya tidak menutupi tombol)
    private void PrepareLoadedSceneAsBackground(string sceneName)
    {
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!loadedScene.IsValid()) return;

        disabledCameras.Clear();
        disabledListeners.Clear();
        disabledEventSystems.Clear();
        pushedBackCanvases.Clear();

        GameObject[] rootObjects = loadedScene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam.gameObject.activeSelf)
                {
                    disabledCameras.Add(cam);
                    cam.gameObject.SetActive(false);
                }
            }

            foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
            {
                if (listener.enabled)
                {
                    disabledListeners.Add(listener);
                    listener.enabled = false;
                }
            }

            foreach (var es in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
            {
                if (es.gameObject.activeSelf)
                {
                    disabledEventSystems.Add(es.gameObject);
                    es.gameObject.SetActive(false);
                }
            }

            // Dorong semua Canvas di scene ini jauh ke belakang, supaya tidak menutupi Canvas Main Menu
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = -100;
                pushedBackCanvases.Add(canvas);

                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = false;
                }
            }
        }
    }

    // Dipanggil saat tombol New Game diklik: melanjutkan (bukan mengulang dari awal)
    // environment/cutscene yang sudah dimuat, dan mengambil alih tampilan sepenuhnya
    // dari Main Menu (kamera, audio listener, event system scene ini diaktifkan lagi).
    public void ActivateLoadedEnvironment()
    {
        foreach (Camera cam in disabledCameras)
        {
            if (cam != null) cam.gameObject.SetActive(true);
        }

        foreach (AudioListener listener in disabledListeners)
        {
            if (listener != null) listener.enabled = true;
        }

        foreach (GameObject esObj in disabledEventSystems)
        {
            if (esObj != null) esObj.SetActive(true);
        }

        foreach (Canvas canvas in pushedBackCanvases)
        {
            if (canvas != null)
            {
                canvas.overrideSorting = false;
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = true;
            }
        }

        // Lanjutkan waktu supaya animasi/cerita jalan lagi dari titik terakhir (bukan reset ke awal)
        Time.timeScale = 1f;
    }

    // Dipakai kalau environment yang di-load BEDA dari scene tujuan sebenarnya
    // (misal background cuma preview, bukan scene yang sama persis)
    public void UnloadCurrentEnvironment()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(currentLoadedScene))
        {
            SceneManager.UnloadSceneAsync(currentLoadedScene);
            currentLoadedScene = null;
        }
    }
}