using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Area trigger untuk memicu tahapan tutorial atau menyelesaikan objektif Waypoint/Pinpoint.
/// Pasang pada GameObject Pinpoint (misal: Cylinder/Cube dengan Collider IsTrigger = true).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerRole
    {
        ShowPopupOnly = 0,
        ReachPinpointObjective = 1
    }

    [Header("Trigger Role")]
    [Tooltip("ReachPinpointObjective = menyelesaikan Tahap 1 (Movement) saat Player menyentuh Pinpoint ini.\nShowPopupOnly = menampilkan popup tutorial custom.")]
    [SerializeField] private TriggerRole triggerRole = TriggerRole.ReachPinpointObjective;

    [Header("Tutorial Content (Untuk ShowPopupOnly)")]
    [SerializeField] private string title = "Tutorial";
    [SerializeField, TextArea(3, 8)] private string description = "";

    [Header("Behavior")]
    [SerializeField] private bool oneTimeOnly = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideAfterTriggered = true;

    [Header("Visual Pinpoint Animation (Opsional)")]
    [SerializeField] private bool rotatePinpoint = true;
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTutorialTriggered;
    [SerializeField] private UnityEvent onPanelClosed;

    private bool hasBeenTriggered = false;
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (rotatePinpoint)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (oneTimeOnly && hasBeenTriggered) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;

        TriggerTutorial();
    }

    public void TriggerTutorial()
    {
        if (oneTimeOnly && hasBeenTriggered) return;
        hasBeenTriggered = true;

        onTutorialTriggered?.Invoke();

        if (triggerRole == TriggerRole.ReachPinpointObjective)
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnPinpointReached();
            }
        }
        else
        {
            if (TutorialPanel.Instance != null)
            {
                TutorialPanel.Instance.Show(title, description, () =>
                {
                    onPanelClosed?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning("[TutorialTrigger] TutorialPanel.Instance tidak ditemukan di scene!", this);
                onPanelClosed?.Invoke();
            }
        }

        if (hideAfterTriggered)
        {
            gameObject.SetActive(false);
        }
    }

    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (triggerCollider is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (triggerCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (triggerCollider is CapsuleCollider capsule)
        {
            Gizmos.DrawWireSphere(capsule.center, capsule.radius);
        }

        Gizmos.matrix = oldMatrix;
    }
}
