using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace DialogueEditor
{
    public class UIConversationButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public enum eHoverState { idleOff, animatingOn, idleOn, animatingOff }
        public enum eButtonType { Option, Speech, End, MainMenuPlay, MainMenuExit, MainMenuSettings, MainMenuCredits }

        [Header("Menu Settings")]
        [SerializeField] private eButtonType m_buttonType;
        [SerializeField] private string m_gameSceneName = "GameScene";

        [Header("UI Elements")]
        [SerializeField] private TMPro.TextMeshProUGUI TextMesh = null;
        [SerializeField] private Image OptionBackgroundImage = null;
        
        private RectTransform m_rect;
        private float m_hoverT = 0.0f;
        private eHoverState m_hoverState = eHoverState.idleOff;
        private ConversationNode m_node;

        private bool IsAnimating => (m_hoverState == eHoverState.animatingOn || m_hoverState == eHoverState.animatingOff);
        private Vector3 BigSize => Vector3.one * 1.15f;

        private void Awake() => m_rect = GetComponent<RectTransform>();

        private void Update()
        {
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
        }

        // --- Logika Input ---
        public void OnPointerEnter(PointerEventData eventData) { SetHovering(true); }
        public void OnPointerExit(PointerEventData eventData) { SetHovering(false); }
        public void OnPointerClick(PointerEventData eventData) { DoClickBehaviour(); }

        private void DoClickBehaviour()
        {
            // Fungsi untuk Main Menu
            if (m_buttonType == eButtonType.MainMenuPlay) SceneManager.LoadScene(m_gameSceneName);
            else if (m_buttonType == eButtonType.MainMenuExit) Application.Quit();
            else if (m_buttonType == eButtonType.MainMenuSettings) Debug.Log("Settings Clicked");
            else if (m_buttonType == eButtonType.MainMenuCredits) Debug.Log("Credits Clicked");

            // Fungsi untuk Dialog
            if (ConversationManager.Instance != null)
            {
                if (m_buttonType == eButtonType.Speech) ConversationManager.Instance.SpeechSelected(m_node as SpeechNode);
                else if (m_buttonType == eButtonType.Option) ConversationManager.Instance.OptionSelected(m_node as OptionNode);
                else if (m_buttonType == eButtonType.End) ConversationManager.Instance.EndButtonSelected();
            }
        }

        // --- Fungsi "Wajib" Agar ConversationManager Tidak Error ---
        public void SetHovering(bool selected)
        {
            if (selected) m_hoverState = eHoverState.animatingOn;
            else m_hoverState = eHoverState.animatingOff;
            m_hoverT = 0f;
        }

        public void SetAlpha(float a)
        {
            if (OptionBackgroundImage != null) { Color c = OptionBackgroundImage.color; c.a = a; OptionBackgroundImage.color = c; }
            if (TextMesh != null) { Color c = TextMesh.color; c.a = a; TextMesh.color = c; }
        }

        public void SetImage(Sprite sprite, bool sliced)
        {
            if (OptionBackgroundImage == null) return;
            OptionBackgroundImage.sprite = sprite;
            OptionBackgroundImage.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        public void OnButtonPressed() => DoClickBehaviour();
        public void OnClick() => DoClickBehaviour();

        public void SetupButton(eButtonType type, ConversationNode node, TMPro.TMP_FontAsset continueFont = null, TMPro.TMP_FontAsset endFont = null)
        {
            m_buttonType = type;
            m_node = node;
            if (TextMesh != null && node != null) { TextMesh.text = node.Text; TextMesh.font = node.TMPFont; }
        }
    }
}