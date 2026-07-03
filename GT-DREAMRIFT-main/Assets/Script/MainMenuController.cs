using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Wajib untuk Input System baru

public class MainMenuController : MonoBehaviour
{
    public enum eHoverState { idleOff, animatingOn, idleOn, animatingOff }

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Cursor Settings")]
    [SerializeField] private Texture2D m_pointerCursor; 

    private float m_hoverT = 0.0f;
    private eHoverState m_hoverState = eHoverState.idleOff;
    private RectTransform m_activeButtonRect; 

    private bool IsAnimating => (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.animatingOff);
    private Vector3 BigSize => Vector3.one * 1.15f;

    private void Update()
    {
        // 1. Logika Animasi Hover Skala (Lerp Ease Out)
        if (IsAnimating && m_activeButtonRect != null)
        {
            m_hoverT += Time.deltaTime;
            float normalised = Mathf.Clamp01(m_hoverT / 0.15f);
            float ease = 1 - Mathf.Pow(1 - normalised, 4); 
            
            if (m_hoverState == eHoverState.animatingOn)
                m_activeButtonRect.localScale = Vector3.Lerp(Vector3.one, BigSize, ease);
            else
                m_activeButtonRect.localScale = Vector3.Lerp(BigSize, Vector3.one, ease);

            if (normalised >= 1)
            {
                m_hoverState = (m_hoverState == eHoverState.animatingOn) ? eHoverState.idleOn : eHoverState.idleOff;
                if (m_hoverState == eHoverState.idleOff) 
                {
                    m_activeButtonRect.localScale = Vector3.one; // PAKSA kembali ke ukuran semula agar tidak nyangkut
                    m_activeButtonRect = null; 
                }
            }
        }

        // 2. Logika ESC (Versi Input System Package)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
        }
    }

    // --- LOGIKA HOVER YANG DIPANGGIL OLEH EVENT TRIGGER ---
    
    public void OnPointerEnterButton(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData != null && pointerData.pointerEnter != null)
        {
            // Jika kursor pindah ke tombol lain sebelum tombol lama selesai mengecil, kembalikan dulu tombol lama ke ukuran normal
            if (m_activeButtonRect != null) m_activeButtonRect.localScale = Vector3.one;

            m_activeButtonRect = pointerData.pointerEnter.GetComponent<RectTransform>();
        }

        m_hoverState = eHoverState.animatingOn;
        m_hoverT = 0f;

        if (m_pointerCursor != null)
        {
            Cursor.SetCursor(m_pointerCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    public void OnPointerExitButton()
    {
        m_hoverState = eHoverState.animatingOff;
        m_hoverT = 0f;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnDisable()
    {
        if (m_activeButtonRect != null) m_activeButtonRect.localScale = Vector3.one;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // --- LOGIKA ON-CLICK TOMBOL UTAMA ---

    public void PlayGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Game Exiting...");
        #if UNITY_EDITOR
            // DIJAMIN AMAN: Menggunakan EditorApplication yang benar agar keluar dari Play Mode
            UnityEditor.EditorApplication.isPlaying = false; 
        #else
            Application.Quit(); 
        #endif
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }
}