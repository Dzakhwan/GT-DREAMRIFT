using System.Collections;
using System.Collections.Generic;
using DialogueEditor;
using Dreamrift.InventorySystem;
using UnityEngine;

public sealed class DialogueGameplayUIController : MonoBehaviour
{
    [Header("Optional Additional Gameplay UI")]
    [Tooltip("Tambahkan root UI lain seperti health bar atau minimap jika perlu ikut disembunyikan.")]
    [SerializeField] private GameObject[] additionalUiRoots;

    private readonly List<GameObject> gameplayUiRoots = new List<GameObject>();
    private readonly List<bool> previousActiveStates = new List<bool>();
    private Coroutine restoreRoutine;

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += HandleConversationStarted;
        ConversationManager.OnConversationEnded += HandleConversationEnded;
    }

    private void Start()
    {
        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive)
        {
            HideGameplayUI();
        }
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= HandleConversationStarted;
        ConversationManager.OnConversationEnded -= HandleConversationEnded;

        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        RestoreGameplayUI();
    }

    private void HandleConversationStarted()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        HideGameplayUI();
    }

    private void HandleConversationEnded()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
        }

        restoreRoutine = StartCoroutine(RestoreAfterDialogueCloses());
    }

    private IEnumerator RestoreAfterDialogueCloses()
    {
        // OnConversationEnded dipanggil saat animasi fade-out baru dimulai.
        // Tunggu sampai ConversationManager benar-benar mematikan UI dialog.
        while (ConversationManager.Instance != null &&
               ConversationManager.Instance.IsConversationActive)
        {
            yield return null;
        }

        RestoreGameplayUI();
        restoreRoutine = null;
    }

    private void HideGameplayUI()
    {
        if (gameplayUiRoots.Count > 0)
        {
            return;
        }

        HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();

        AddComponentsToSet(
            FindObjectsByType<UIVirtualJoystick>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None),
            uniqueRoots);

        AddComponentsToSet(
            FindObjectsByType<UIVirtualButton>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None),
            uniqueRoots);

        AddComponentsToSet(
            FindObjectsByType<UIVirtualTouchZone>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None),
            uniqueRoots);

        AddComponentsToSet(
            FindObjectsByType<InventoryUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None),
            uniqueRoots);

        if (additionalUiRoots != null)
        {
            for (int i = 0; i < additionalUiRoots.Length; i++)
            {
                if (additionalUiRoots[i] != null)
                {
                    uniqueRoots.Add(additionalUiRoots[i]);
                }
            }
        }

        foreach (GameObject uiRoot in uniqueRoots)
        {
            gameplayUiRoots.Add(uiRoot);
            previousActiveStates.Add(uiRoot.activeSelf);
            uiRoot.SetActive(false);
        }
    }

    private void RestoreGameplayUI()
    {
        for (int i = 0; i < gameplayUiRoots.Count; i++)
        {
            GameObject uiRoot = gameplayUiRoots[i];
            if (uiRoot != null)
            {
                uiRoot.SetActive(previousActiveStates[i]);
            }
        }

        gameplayUiRoots.Clear();
        previousActiveStates.Clear();
    }

    private static void AddComponentsToSet<T>(
        T[] components,
        HashSet<GameObject> targetSet)
        where T : Component
    {
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
            {
                targetSet.Add(components[i].gameObject);
            }
        }
    }
}
