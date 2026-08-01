using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Tempel script ini langsung di GameObject yang punya komponen Button
// Efek: warna berubah + scale membesar sedikit saat mouse hover
[RequireComponent(typeof(Button))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target Visual (Opsional)")]
    // Kosongkan biar otomatis ambil komponen Graphic (Image/Text) yang ada di GameObject ini
    public Graphic targetGraphic;

    [Header("Color Settings")]
    public Color hoverColor = new Color(1f, 0.85f, 0.4f); // Warna kuning lembut, sesuaikan sendiri
    private Color normalColor;

    [Header("Scale Settings")]
    public float hoverScale = 1.1f;
    private Vector3 normalScale;

    [Header("Transition")]
    public float transitionSpeed = 8f;

    private bool isHovering = false;

    void Awake()
    {
        if (targetGraphic == null)
        {
            // Coba ambil dari child dulu (misal Text di dalam Button), kalau tidak ada baru dari diri sendiri
            targetGraphic = GetComponentInChildren<Graphic>();
        }

        if (targetGraphic != null)
        {
            normalColor = targetGraphic.color;
        }

        normalScale = transform.localScale;
    }

    void Update()
    {
        // Pakai unscaledDeltaTime supaya animasi tetap jalan walau game di-pause (Time.timeScale = 0)
        Vector3 targetScale = isHovering ? normalScale * hoverScale : normalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);

        if (targetGraphic != null)
        {
            Color targetColor = isHovering ? hoverColor : normalColor;
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor, Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}