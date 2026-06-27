using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor;

public class ConversationStarter : MonoBehaviour
{
    [Header("Conversation")]
    [SerializeField] private NPCConversation myConversation;

    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 3f;

    [Header("UI")]
    [SerializeField] private GameObject desktopUI;   // UI Press F
    [SerializeField] private GameObject androidUI;  // Tombol Interact Android

    private Transform player;
    private bool playerInRange;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Sembunyikan UI saat awal
        if (desktopUI != null)
            desktopUI.SetActive(false);

        if (androidUI != null)
            androidUI.SetActive(false);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Jika player dekat NPC
        if (distance <= interactionRadius)
        {
            playerInRange = true;

#if UNITY_ANDROID
            // Android
            if (androidUI != null)
                androidUI.SetActive(true);
#else
            // Desktop
            if (desktopUI != null)
                desktopUI.SetActive(true);

            // Tekan G di desktop
            if (Input.GetKeyDown(KeyCode.G))
            {
                StartDialogue();
            }
#endif
        }
        else
        {
            playerInRange = false;

            if (desktopUI != null)
                desktopUI.SetActive(false);

            if (androidUI != null)
                androidUI.SetActive(false);
        }
    }

    // Dipanggil tombol Android
    public void StartDialogue()
    {
        if (playerInRange)
        {
            ConversationManager.Instance.StartConversation(myConversation);
        }
    }

    // Visual radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}