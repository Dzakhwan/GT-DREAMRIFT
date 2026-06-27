using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Tambahkan ini agar bisa baca keyboard baru

public class UIConversationButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum eHoverState { idleOff, animatingOn, idleOn, animatingOff }
    public enum eButtonType { Play, Exit, Settings, Credits }

    [Header("Button Settings")]
    [SerializeField] private eButtonType m_buttonType;
    [SerializeField] private string m_gameSceneName = "Main";

    [Header("Panel References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("UI Elements")]
    [SerializeField] private TMPro.TextMeshProUGUI TextMesh = null;
    [SerializeField] private Image OptionBackgroundImage = null;
    
    private RectTransform m_rect;
    private float m_hoverT = 0.0f;
    private eHoverState m_hoverState = eHoverState.idleOff;

    private bool IsAnimating => (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.animatingOff);
    private Vector3 BigSize => Vector3.one * 1.15f;

    private void Awake() => m_rect = GetComponent<RectTransform>();

    private void Update()
    {
        // 1. Logika Animasi Hover
        if (IsAnimating)
        {
            m_hoverT += Time.deltaTime;
            float normalised = Mathf.Clamp01(m_hoverT / 0.15f);
            float ease = 1 - Mathf.Pow(1 - normalised, 4);
            
            if (m_hoverState == eHoverState.animatingOn)
                m_rect.localScale = Vector3.Lerp(Vector3.one, BigSize, ease);
            else
                m_rect.localScale = Vector3.Lerp(BigSize, Vector3.one, ease);

            if (normalised >= 1)
                m_hoverState = (m_hoverState == eHoverState.animatingOn) ? eHoverState.idleOn : eHoverState.idleOff;
        }

        // 2. Logika ESC (Versi Input System Package)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf) settingsPanel.SetActive(false);
            if (creditsPanel != null && creditsPanel.activeSelf) creditsPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_hoverState = eHoverState.animatingOn;
        m_hoverT = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_hoverState = eHoverState.animatingOff;
        m_hoverT = 0f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        switch (m_buttonType)
        {
            case eButtonType.Play:
                SceneManager.LoadScene(m_gameSceneName);
                break;
            case eButtonType.Settings:
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
            case eButtonType.Credits:
                if (creditsPanel != null) creditsPanel.SetActive(true);
                break;
            case eButtonType.Exit:
                Debug.Log("Game Exiting...");
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                break;
        }
    }
}