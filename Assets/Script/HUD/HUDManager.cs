using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("Health Settings")]
    public Image healthFill;
    public GameObject healthBarUI;
    public float maxHealth = 100f;
    public float currentHealth;
    public float decayRate = 5f;

    [Header("Pause Panel")]
    public GameObject pausePanel;
    public Image pausePanelBackground;
    private bool isPaused = false;
    private bool pausedByHealthEmpty = false;

    [Header("Pause Panel Contents")]
    public GameObject titleText;
    public GameObject resumeButton;
    public GameObject settingsButton;
    public GameObject exitButton;

    [Header("Settings Panel")]
    public GameObject settingPanel;

    [Header("Scenes Without HUD")]
    public List<string> scenesWithoutHUD = new List<string> { "Main Menu", "CutScene1" };

    private bool inMainMenu = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentHealth = maxHealth;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inMainMenu = scenesWithoutHUD.Contains(scene.name);
        healthBarUI.SetActive(!inMainMenu);
    }

    void Update()
    {
        if (!isPaused && !inMainMenu && currentHealth > 0)
        {
            currentHealth -= decayRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthUI();

            if (currentHealth <= 0)
            {
                pausedByHealthEmpty = true;
                ShowPause();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !pausedByHealthEmpty)
        {
            TogglePause();
        }
    }

    void UpdateHealthUI()
    {
        healthFill.fillAmount = currentHealth / maxHealth;
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else ShowPause();
    }

    void ShowPause()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;

        if (pausedByHealthEmpty)
        {
            currentHealth = maxHealth;
            UpdateHealthUI();
            pausedByHealthEmpty = false;
        }
    }

    public void ExitToMainMenu()
    {
        isPaused = false;
        pausedByHealthEmpty = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;

        SceneManager.LoadScene("Main Menu");
    }

    public void OpenSettings()
    {
        settingPanel.SetActive(true);

        titleText.SetActive(false);
        resumeButton.SetActive(false);
        settingsButton.SetActive(false);
        exitButton.SetActive(false);

        pausePanelBackground.enabled = false;
    }

    public void CloseSettings()
    {
        settingPanel.SetActive(false);

        titleText.SetActive(true);
        resumeButton.SetActive(true);
        settingsButton.SetActive(true);
        exitButton.SetActive(true);

        pausePanelBackground.enabled = true;
    }
}