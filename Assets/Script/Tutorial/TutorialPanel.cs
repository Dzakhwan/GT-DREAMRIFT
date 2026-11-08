using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using StarterAssets;

/// <summary>
/// Panel UI untuk menampilkan instruksi tutorial.
/// Otomatis mem-pause game (Time.timeScale = 0) saat terbuka agar Player aman dari serangan musuh,
/// dan mengembalikan Time.timeScale = 1 saat tombol Close ditekan.
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;

    [Header("Pause & Behavior Settings")]
    [Tooltip("Otomatis pause game (Time.timeScale = 0) saat panel tutorial terbuka")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    [Tooltip("Panel otomatis disembunyikan setelah closeButton ditekan")]
    [SerializeField] private bool hideOnClose = true;

    private Action onCloseCallback;
    private StarterAssetsInputs cachedInputs;
    private bool wasCursorLocked;

    public static TutorialPanel Instance { get; private set; }
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TutorialPanel] Multiple instance terdeteksi, gunakan yang pertama.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonPressed);
            closeButton.onClick.AddListener(OnCloseButtonPressed);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Tampilkan panel tutorial dengan judul dan deskripsi tertentu, serta pause game.
    /// </summary>
    public void Show(string title, string description, Action onClose = null)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        onCloseCallback = onClose;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }

        // Hentikan input pergerakan sementara dan pastikan kursor/touch bebas menekan tombol UI
        if (cachedInputs == null)
            cachedInputs = FindFirstObjectByType<StarterAssetsInputs>();

        if (cachedInputs != null)
        {
            wasCursorLocked = cachedInputs.cursorLocked;
            cachedInputs.move = Vector2.zero;
            cachedInputs.look = Vector2.zero;
            cachedInputs.cursorInputForLook = false;
            cachedInputs.SetCursorState(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Sembunyikan panel tutorial tanpa memicu callback.
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }

        RestoreInputState();
        onCloseCallback = null;
    }

    public void OnCloseButtonPressed()
    {
        if (hideOnClose && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        // Kembalikan waktu permainan ke normal (Time.timeScale = 1)
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }

        RestoreInputState();

        Action callback = onCloseCallback;
        onCloseCallback = null;
        callback?.Invoke();
    }

    private void RestoreInputState()
    {
        if (cachedInputs != null)
        {
            cachedInputs.cursorInputForLook = true;
            cachedInputs.SetCursorState(wasCursorLocked);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}
