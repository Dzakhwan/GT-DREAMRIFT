using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

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

    [Header("Legacy UI (Opsional - abaikan jika menggunakan sistem PlayerInteraction)")]
    [SerializeField] private GameObject desktopUI;   // UI Press F
    [SerializeField] private GameObject androidUI;  // Tombol Interact Android

    public string InteractLabel => string.IsNullOrEmpty(interactLabel) ? "Bicara" : interactLabel;
    public float InteractionRadius => interactionRadius;

    private Transform player;
    private bool playerInRange;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Sembunyikan UI lama saat awal
        if (desktopUI != null)
            desktopUI.SetActive(false);

        if (androidUI != null)
            androidUI.SetActive(false);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactionRadius;

        // Fallback opsional jika masih menggunakan Legacy UI terpisah
        if (desktopUI != null)
            desktopUI.SetActive(playerInRange);

        if (androidUI != null)
            androidUI.SetActive(playerInRange);
    }

    /// <summary>
    /// Implementasi IInteractable.Dipanggil oleh PlayerInteraction saat tombol UI interaksi ditekan.
    /// </summary>
    public void OnInteract()
    {
        StartDialogue();
    }

    // Dipanggil oleh OnInteract() atau tombol Android/Legacy
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

    // Visual radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}