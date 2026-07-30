using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Panel UI untuk menampilkan instruksi tutorial.
/// Pasang script ini ke GameObject Canvas > TutorialPanel.
///
/// Cara setup:
///   1. Buat Canvas di scene (jika belum ada)
///   2. Di Canvas, buat GameObject Panel > beri nama "TutorialPanel"
///   3. Pasang TutorialPanel.cs ke GameObject tersebut
///   4. Assign masing-masing komponen di Inspector:
///      - TitleText       → TextMeshPro untuk judul
///      - DescriptionText → TextMeshPro untuk deskripsi
///      - CloseButton     → Button "OK/Lanjut"
///      - PanelRoot       → GameObject root panel (untuk show/hide)
///   5. Set PanelRoot inactive di awal scene agar tersembunyi saat start
///
/// TutorialTrigger akan otomatis mencari instance panel ini
/// saat player masuk area trigger.
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;

    [Header("Settings")]
    [Tooltip("Panel otomatis hilang jika true setelah closeButton ditekan")]
    [SerializeField] private bool hideOnClose = true;

    // Callback yang dipanggil saat panel ditutup
    private System.Action onCloseCallback;

    public static TutorialPanel Instance { get; private set; }

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
            closeButton.onClick.AddListener(OnCloseButtonPressed);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Tampilkan panel tutorial dengan judul dan deskripsi tertentu.
    /// </summary>
    /// <param name="title">Judul tutorial</param>
    /// <param name="description">Isi instruksi</param>
    /// <param name="onClose">Callback saat panel ditutup (opsional)</param>
    public void Show(string title, string description, System.Action onClose = null)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        onCloseCallback = onClose;

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>
    /// Sembunyikan panel tutorial.
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        onCloseCallback = null;
    }

    private void OnCloseButtonPressed()
    {
        if (hideOnClose)
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }
}
