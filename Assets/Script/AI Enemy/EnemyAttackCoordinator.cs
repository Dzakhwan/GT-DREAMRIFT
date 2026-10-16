using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mengatur giliran serangan para musuh (Attack Token Coordinator).
/// Memastikan musuh tidak menyerang semua secara bersamaan, melainkan bergantian.
/// </summary>
public class EnemyAttackCoordinator : MonoBehaviour
{
    private static EnemyAttackCoordinator instance;

    public static EnemyAttackCoordinator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<EnemyAttackCoordinator>();
                if (instance == null)
                {
                    GameObject go = new GameObject("EnemyAttackCoordinator");
                    instance = go.AddComponent<EnemyAttackCoordinator>();
                }
            }
            return instance;
        }
    }

    [Header("Attack Coordination Settings")]
    [Tooltip("Jumlah maksimum musuh yang boleh menyerang secara bersamaan.")]
    [SerializeField] private int maxConcurrentAttackers = 1;

    [Tooltip("Jeda minimal (detik) antar serangan musuh yang berbeda agar pemain tidak langsung diserbu beruntun.")]
    [SerializeField] private float minIntervalBetweenAttacks = 0.8f;

    private readonly HashSet<EnemyAIController> currentAttackers = new HashSet<EnemyAIController>();
    private readonly List<EnemyAIController> cleanupList = new List<EnemyAIController>();
    private float lastAttackEndTime = -999f;

    public int MaxConcurrentAttackers
    {
        get => maxConcurrentAttackers;
        set => maxConcurrentAttackers = Mathf.Max(1, value);
    }

    public float MinIntervalBetweenAttacks
    {
        get => minIntervalBetweenAttacks;
        set => minIntervalBetweenAttacks = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Update()
    {
        // Bersihkan referensi musuh yang mungkin hancur atau nonaktif di tengah serangan
        if (currentAttackers.Count > 0)
        {
            cleanupList.Clear();
            foreach (var attacker in currentAttackers)
            {
                if (attacker == null || !attacker.isActiveAndEnabled || attacker.CurrentState == EnemyAIController.EnemyState.Dead)
                {
                    cleanupList.Add(attacker);
                }
            }

            for (int i = 0; i < cleanupList.Count; i++)
            {
                currentAttackers.Remove(cleanupList[i]);
                lastAttackEndTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Meminta izin (token) untuk melancarkan serangan ke Player.
    /// </summary>
    public bool RequestAttackToken(EnemyAIController enemy)
    {
        if (enemy == null) return false;

        // Jika musuh ini sudah memegang token
        if (currentAttackers.Contains(enemy))
            return true;

        // Jika kuota penyerang bersamaan sudah penuh
        if (currentAttackers.Count >= maxConcurrentAttackers)
            return false;

        // Jika jeda cooldown global antar serangan belum tercapai
        if (Time.time - lastAttackEndTime < minIntervalBetweenAttacks)
            return false;

        // Berikan izin / token
        currentAttackers.Add(enemy);
        return true;
    }

    /// <summary>
    /// Mengembalikan token serangan setelah musuh selesai menyerang atau dibatalkan.
    /// </summary>
    public void ReleaseAttackToken(EnemyAIController enemy)
    {
        if (enemy != null && currentAttackers.Remove(enemy))
        {
            lastAttackEndTime = Time.time;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
