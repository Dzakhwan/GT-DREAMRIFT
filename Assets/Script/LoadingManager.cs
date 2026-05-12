using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI References")]
    public GameObject loadingCanvas; 
    public Slider progressBar;       

    private void Awake()
    {
        // Bikin kebal biar gak hancur pas pindah scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadLevel(SceneField targetScene)
    {
        StartCoroutine(LoadAsynchronously(targetScene));
    }

    private IEnumerator LoadAsynchronously(string sceneName)
    {
        loadingCanvas.SetActive(true);
        progressBar.value = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        operation.allowSceneActivation = false;

        while (progressBar.value < 1f || operation.progress < 0.9f)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Animasi pergerakan slider
            progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, Time.deltaTime * 2f);
            
            yield return null;
        }

        
        yield return new WaitForSeconds(0.5f);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        loadingCanvas.SetActive(false);
    }
}