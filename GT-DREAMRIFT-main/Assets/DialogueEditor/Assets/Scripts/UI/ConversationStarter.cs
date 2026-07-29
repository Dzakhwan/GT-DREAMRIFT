using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

/// <summary>
/// Komponen pemicu percakapan untuk NPC yang terintegrasi penuh dengan IInteractable.
/// Pasang script ini ke NPC yang memiliki Collider dengan Layer 'Interactable'.
/// Saat pemain mengarahkan Raycast ke NPC dan menekan tombol Interact universal,
/// OnInteract() akan dipanggil dan memulai obrolan (NPCConversation).
/// </summary>
public class ConversationStarter : MonoBehaviour, IInteractable
{
    [Header("Conversation")]
    [SerializeField] private NPCConversation myConversation;

    [Header("Interaction (IInteractable)")]
    [Tooltip("Teks label tombol saat Player mendeteksi NPC ini (bisa diatur di Inspector)")]
    [SerializeField] private string interactLabel = "Bicara";

    [Tooltip("Jarak maksimal untuk memicu percakapan")]
    [SerializeField] private float interactionRadius = 3f;

    [Header("UI To Hide During Conversation")]
    [SerializeField] private List<GameObject> uiToHideDuringConversation;

    public string InteractLabel => string.IsNullOrEmpty(interactLabel) ? "Bicara" : interactLabel;
    public float InteractionRadius => interactionRadius;

    /// <summary>
    /// Implementasi IInteractable. Dipanggil oleh PlayerInteraction saat tombol UI interaksi ditekan.
    /// </summary>
    public void OnInteract()
    {
        StartDialogue();
    }

    /// <summary>
    /// Memulai obrolan menggunakan DialogueEditor.
    /// </summary>
    public void StartDialogue()
    {
        if (myConversation == null)
        {
            Debug.LogWarning("ConversationStarter: myConversation belum di-assign di Inspector!", this);
            return;
        }

        if (uiToHideDuringConversation != null)
        {
            foreach (var go in uiToHideDuringConversation)
            {
                if (go != null) go.SetActive(false);
            }
        }
        
        ConversationManager.OnConversationEnded += RestoreUI;
        ConversationManager.Instance.StartConversation(myConversation);
    }

    private void RestoreUI()
    {
        ConversationManager.OnConversationEnded -= RestoreUI;
        if (uiToHideDuringConversation != null)
        {
            foreach (var go in uiToHideDuringConversation)
            {
                if (go != null) go.SetActive(true);
            }
        }
    }

    // Visualisasi radius interaksi di Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}