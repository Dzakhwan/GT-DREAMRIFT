using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar billboard for top-down camera.
/// Attach to a Canvas (or child) above the enemy head.
/// </summary>
public class BillboardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Slider used as the health fill. Leave empty to auto-find in children.")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Optional Image fill instead of Slider.")]
    [SerializeField] private Image healthFillImage;

    [Tooltip("EnemyHealth to listen to. Leave empty to auto-find on parent.")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Tooltip("Camera to face. Leave empty to use Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Performance")]
    [Tooltip("How often rotation is updated (seconds). 0 = every frame.")]
    [SerializeField, Min(0f)] private float rotationInterval = 0f;

    private float rotationTimer;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>(true);

        if (healthFillImage == null && healthSlider == null)
            healthFillImage = GetComponentInChildren<Image>(true);
    }

    private void OnEnable()
    {
        if (enemyHealth == null) return;

        enemyHealth.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged -= HandleHealthChanged;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        if (rotationInterval > 0f)
        {
            rotationTimer += Time.deltaTime;
            if (rotationTimer < rotationInterval) return;
            rotationTimer = 0f;
        }

        // Face camera without vertical tilt (top-down friendly billboard)
        Vector3 camForward = targetCamera.transform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.001f)
            camForward = targetCamera.transform.up;

        camForward.Normalize();
        transform.rotation = Quaternion.LookRotation(camForward, Vector3.up);
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (max <= 0f) return;

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthFillImage != null)
            healthFillImage.fillAmount = current / max;
    }
}
