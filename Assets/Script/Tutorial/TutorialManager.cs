using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using StarterAssets;
using Dreamrift.QuestSystem;

/// <summary>
/// Master Controller untuk Sistem Tutorial Beruntun (3 Fase) di Scene "LV 1".
/// Mengatur:
/// 1. Munculnya Panel UI Tutorial + Pause otomatis (Time.timeScale = 0) dan Resume (Time.timeScale = 1) saat Close.
/// 2. Integrasi langsung ke Quest Book (QuestManager & QuestBookUI) setiap kali player menutup panel tutorial.
/// 3. Alur 3 Fase Objektif:
///    - Fase 1 (Movement): Berjalan ke Pinpoint/Waypoint.
///    - Fase 2 (Dodge/Dash): Menghindari tembakan Projectile dari Turret Spawner dengan Dash.
///    - Fase 3 (Melee Combat): Menghancurkan Dummy Enemy dengan serangan melee (1x atau 3x combo).
/// 4. Fungsi publik Mobile Input: OnDashButtonPressed() & OnAttackButtonPressed() untuk tombol UI Canvas Android.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public enum TutorialPhase
    {
        None = 0,
        Phase1_Movement = 1,
        Phase2_DodgeDash = 2,
        Phase3_MeleeCombat = 3,
        Completed = 4
    }

    public static TutorialManager Instance { get; private set; }

    [Header("=== STATUS TUTORIAL ===")]
    [SerializeField] private TutorialPhase currentPhase = TutorialPhase.None;
    [SerializeField] private bool startTutorialOnAwake = true;

    [Header("=== PLAYER & MOBILE INPUT REFERENCES ===")]
    [Tooltip("Referensi GameObject PlayerArmature di scene LV 1 (otomatis dicari jika kosong)")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private StarterAssetsInputs playerInputs;
    [SerializeField] private ThirdPersonController thirdPersonController;
    [SerializeField] private PlayerFight playerFight;

    [Header("=== TAHAP 1: MOVEMENT (PINPOINT) ===")]
    [Tooltip("GameObject Pinpoint / Waypoint (Cylinder/Cube dengan TutorialTrigger)")]
    [SerializeField] private GameObject movementPinpointObject;
    [SerializeField] private string phase1Title = "Tutorial 1: Pergerakan (Movement)";
    [SerializeField, TextArea(3, 5)] private string phase1Description =
        "Gunakan Analog Kiri untuk bergerak dan Analog Kanan (mata) untuk mengarahkan pandangan.\n\n" +
        "Objektif: Berjalanlah menuju titik Pinpoint bercahaya di depanmu!";
    [Tooltip("Opsional: ScriptableObject QuestData untuk Tahap 1. Jika kosong, akan dibuat otomatis di Quest Book.")]
    [SerializeField] private QuestData phase1QuestData;

    [Header("=== TAHAP 2: DODGE / DASH (PROJECTILE SPAWNER) ===")]
    [Tooltip("GameObject Turret Spawner yang menembakkan Projectile ke arah Player")]
    [SerializeField] private TutorialProjectileSpawner projectileSpawner;
    [Tooltip("Jumlah aksi Dash menghindar yang harus dilakukan untuk lulus Tahap 2")]
    [SerializeField, Min(1)] private int requiredDashDodges = 1;
    [SerializeField] private string phase2Title = "Tutorial 2: Menghindar (Dash / Dodge)";
    [SerializeField, TextArea(3, 5)] private string phase2Description =
        "Awas! Turret musuh muncul dan mengincarmu!\n\n" +
        "Tekan tombol 'DASH' untuk meluncur cepat dan mendapatkan kekebalan sesaat (I-Frame) guna menghindari tembakan Projectile!";
    [Tooltip("Opsional: ScriptableObject QuestData untuk Tahap 2. Jika kosong, akan dibuat otomatis di Quest Book.")]
    [SerializeField] private QuestData phase2QuestData;

    [Header("=== TAHAP 3: MELEE COMBAT (DUMMY TARGET) ===")]
    [Tooltip("GameObject Dummy Enemy (Capsule/Cube dengan DummyTarget.cs)")]
    [SerializeField] private DummyTarget dummyTarget;
    [SerializeField] private string phase3Title = "Tutorial 3: Serangan Melee (Combat)";
    [SerializeField, TextArea(3, 5)] private string phase3Description =
        "Target Dummy telah muncul!\n\n" +
        "Dekati Dummy dan tekan tombol 'SERANG' berulang kali (hingga 3x Combo) untuk menghancurkannya!";
    [Tooltip("Opsional: ScriptableObject QuestData untuk Tahap 3. Jika kosong, akan dibuat otomatis di Quest Book.")]
    [SerializeField] private QuestData phase3QuestData;

    [Header("=== SELESAI TUTORIAL ===")]
    [SerializeField] private bool showCompletionPanel = true;
    [SerializeField] private string completionTitle = "Tutorial Selesai!";
    [SerializeField, TextArea(2, 4)] private string completionDescription =
        "Selamat! Kamu telah menguasai dasar pergerakan, Dash, dan pertarungan Melee.\nCatatan objektifmu dapat dilihat kapan saja di dalam Quest Book.";
    [SerializeField] private UnityEvent onAllTutorialsCompleted;

    // Runtime tracking untuk Fase 2
    private int currentDashCount = 0;
    private bool hasPressedDashRecently = false;
    private float dashWindowTimer = 0f;

    public TutorialPhase CurrentPhase => currentPhase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResolvePlayerReferences();
        InitializeObjectsState();
    }

    private void Start()
    {
        EnsureRuntimeTutorialQuests();

        if (startTutorialOnAwake)
        {
            // Mulai Tahap 1 sedikit setelah scene siap agar UI TutorialPanel & QuestManager sudah terinisialisasi
            Invoke(nameof(StartPhase1_Movement), 0.15f);
        }
    }

    private void Update()
    {
        // Deteksi otomatis jika Player melakukan Dash (baik via tombol UI maupun keyboard/controller) saat Fase 2
        if (currentPhase == TutorialPhase.Phase2_DodgeDash)
        {
            if (thirdPersonController != null && thirdPersonController.IsDashing && !hasPressedDashRecently)
            {
                RegisterDashAttempt();
            }

            if (hasPressedDashRecently)
            {
                dashWindowTimer -= Time.deltaTime;
                if (dashWindowTimer <= 0f)
                {
                    hasPressedDashRecently = false;
                    // Jika Player berhasil melakukan Dash saat ditembak tanpa terkena hit, hitung progres menghindar
                    NotifySuccessfulDashDodge();
                }
            }
        }
    }

    private void ResolvePlayerReferences()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObject != null)
        {
            if (playerInputs == null)
                playerInputs = playerObject.GetComponent<StarterAssetsInputs>();
            if (thirdPersonController == null)
                thirdPersonController = playerObject.GetComponent<ThirdPersonController>();
            if (playerFight == null)
                playerFight = playerObject.GetComponent<PlayerFight>();
        }
    }

    private void InitializeObjectsState()
    {
        if (movementPinpointObject != null)
            movementPinpointObject.SetActive(false);

        if (projectileSpawner != null)
            projectileSpawner.StopSpawning();

        if (dummyTarget != null)
            dummyTarget.gameObject.SetActive(false);
    }

    // =========================================================================
    // 1. INTEGRASI QUEST BOOK OTOMATIS
    // =========================================================================

    private void EnsureRuntimeTutorialQuests()
    {
        if (phase1QuestData == null)
        {
            phase1QuestData = CreateRuntimeQuest(
                "TUT_01_MOVE",
                "Tutorial: Langkah Pertama",
                "Bergerak menuju Pinpoint yang ditentukan.",
                "Kamu mempelajari cara bergerak di dunia Dreamrift. Ikuti titik Pinpoint untuk melanjutkan latihanmu.",
                QuestObjectiveType.ReachLocation);
        }

        if (phase2QuestData == null)
        {
            phase2QuestData = CreateRuntimeQuest(
                "TUT_02_DASH",
                "Tutorial: Refleks Menghindar",
                "Gunakan Dash untuk menghindari serangan Projectile.",
                "Serangan jarak jauh musuh dapat dihindari dengan menekan tombol Dash pada saat yang tepat.",
                QuestObjectiveType.ReachLocation);
        }

        if (phase3QuestData == null)
        {
            phase3QuestData = CreateRuntimeQuest(
                "TUT_03_COMBAT",
                "Tutorial: Tebasan Senjata",
                "Hancurkan Target Dummy dengan serangan Melee.",
                "Gunakan tombol Serang untuk melancarkan hingga 3x serangan Combo beruntun dan menghancurkan musuh di hadapanmu.",
                QuestObjectiveType.DefeatEnemy);
        }
    }

    private QuestData CreateRuntimeQuest(string id, string title, string desc, string story, QuestObjectiveType type)
    {
        QuestData q = ScriptableObject.CreateInstance<QuestData>();
        q.name = id;

        // Isi field melalui Serialized/Reflection agar kompatibel penuh dengan QuestData milik project
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(QuestData).GetField("questId", flags)?.SetValue(q, id);
        typeof(QuestData).GetField("displayName", flags)?.SetValue(q, title);
        typeof(QuestData).GetField("description", flags)?.SetValue(q, desc);
        typeof(QuestData).GetField("storyText", flags)?.SetValue(q, story);
        typeof(QuestData).GetField("objectiveType", flags)?.SetValue(q, type);
        typeof(QuestData).GetField("targetEnemyId", flags)?.SetValue(q, "Dummy");
        typeof(QuestData).GetField("targetKillCount", flags)?.SetValue(q, 1);

        return q;
    }

    private void RegisterAndStartQuestInBook(QuestData quest)
    {
        if (quest == null || QuestManager.Instance == null) return;

        QuestManager.Instance.RegisterQuest(quest);
        QuestManager.Instance.StartQuest(quest);

        // Pastikan Quest tampil juga di Chapter pertama pada QuestBookUI jika ada
        if (QuestBookUI.Instance != null)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var chaptersField = typeof(QuestBookUI).GetField("chapters", flags);
            if (chaptersField != null)
            {
                var chapters = chaptersField.GetValue(QuestBookUI.Instance) as List<QuestChapterData>;
                if (chapters != null && chapters.Count > 0 && chapters[0] != null)
                {
                    chapters[0].AddQuest(quest);
                }
            }
            QuestBookUI.Instance.RefreshCurrentView();
        }

        Debug.Log($"[TutorialManager] Objektif '{quest.DisplayName}' resmi tercatat di Quest Book!");
    }

    private void CompleteQuestInBook(QuestData quest)
    {
        if (quest == null || QuestManager.Instance == null) return;

        if (QuestManager.Instance.GetQuestState(quest) == QuestState.Active)
        {
            QuestManager.Instance.CompleteQuest(quest);
        }
        if (QuestBookUI.Instance != null)
        {
            QuestBookUI.Instance.RefreshCurrentView();
        }
    }

    // =========================================================================
    // 2. ALUR 3 FASE TUTORIAL BERUNTUN
    // =========================================================================

    /// <summary>
    /// Memulai Tahap 1: Movement menuju Pinpoint.
    /// </summary>
    public void StartPhase1_Movement()
    {
        currentPhase = TutorialPhase.Phase1_Movement;

        if (movementPinpointObject != null)
            movementPinpointObject.SetActive(true);

        ShowTutorialPopup(phase1Title, phase1Description, () =>
        {
            // Saat tombol Close pada panel ditekan: Time.timeScale otomatis = 1 dan objektif masuk ke Quest Book
            RegisterAndStartQuestInBook(phase1QuestData);
        });
    }

    /// <summary>
    /// Dipanggil otomatis oleh TutorialTrigger pada Pinpoint saat Player sampai di lokasi.
    /// </summary>
    public void OnPinpointReached()
    {
        if (currentPhase != TutorialPhase.Phase1_Movement) return;

        Debug.Log("[TutorialManager] Tahap 1 (Movement) Selesai! Lanjut ke Tahap 2 (Dodge/Dash).");
        CompleteQuestInBook(phase1QuestData);

        if (movementPinpointObject != null)
            movementPinpointObject.SetActive(false);

        StartPhase2_DodgeDash();
    }

    /// <summary>
    /// Memulai Tahap 2: Dodge / Dash menghindari Projectile dari Spawner.
    /// </summary>
    public void StartPhase2_DodgeDash()
    {
        currentPhase = TutorialPhase.Phase2_DodgeDash;
        currentDashCount = 0;
        hasPressedDashRecently = false;

        // Tampilkan objek Spawner namun baru menembak setelah panel tutorial ditutup
        if (projectileSpawner != null)
            projectileSpawner.gameObject.SetActive(true);

        ShowTutorialPopup(phase2Title, phase2Description, () =>
        {
            RegisterAndStartQuestInBook(phase2QuestData);
            if (projectileSpawner != null)
            {
                projectileSpawner.StartSpawning();
            }
        });
    }

    private void RegisterDashAttempt()
    {
        if (currentPhase != TutorialPhase.Phase2_DodgeDash) return;
        hasPressedDashRecently = true;
        dashWindowTimer = 0.65f;
    }

    public void OnProjectilePassedWithoutHit()
    {
        if (currentPhase == TutorialPhase.Phase2_DodgeDash && hasPressedDashRecently)
        {
            NotifySuccessfulDashDodge();
        }
    }

    public void NotifySuccessfulDashDodge()
    {
        if (currentPhase != TutorialPhase.Phase2_DodgeDash) return;

        hasPressedDashRecently = false;
        currentDashCount++;
        Debug.Log($"[TutorialManager] Progres Dash Dodge: {currentDashCount}/{requiredDashDodges}");

        if (currentDashCount >= requiredDashDodges)
        {
            Debug.Log("[TutorialManager] Tahap 2 (Dodge/Dash) Selesai! Lanjut ke Tahap 3 (Melee Combat).");
            if (projectileSpawner != null)
                projectileSpawner.StopSpawning();

            // Bersihkan sisa projectile di udara
            foreach (var proj in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                Destroy(proj.gameObject);
            }

            CompleteQuestInBook(phase2QuestData);
            StartPhase3_MeleeCombat();
        }
    }

    /// <summary>
    /// Memulai Tahap 3: Melee Combat menghancurkan Dummy Enemy.
    /// </summary>
    public void StartPhase3_MeleeCombat()
    {
        currentPhase = TutorialPhase.Phase3_MeleeCombat;

        if (dummyTarget != null)
            dummyTarget.gameObject.SetActive(true);

        ShowTutorialPopup(phase3Title, phase3Description, () =>
        {
            RegisterAndStartQuestInBook(phase3QuestData);
        });
    }

    /// <summary>
    /// Dipanggil otomatis oleh DummyTarget.cs saat Dummy hancur dipukul Player.
    /// </summary>
    public void OnDummyDestroyed()
    {
        if (currentPhase != TutorialPhase.Phase3_MeleeCombat) return;

        currentPhase = TutorialPhase.Completed;
        CompleteQuestInBook(phase3QuestData);

        Debug.Log("[TutorialManager] Seluruh rangkaian Tutorial selesai!");

        if (showCompletionPanel)
        {
            ShowTutorialPopup(completionTitle, completionDescription, () =>
            {
                onAllTutorialsCompleted?.Invoke();
            });
        }
        else
        {
            onAllTutorialsCompleted?.Invoke();
        }
    }

    private void ShowTutorialPopup(string title, string description, System.Action onClose)
    {
        if (TutorialPanel.Instance != null)
        {
            TutorialPanel.Instance.Show(title, description, onClose);
        }
        else
        {
            Debug.LogWarning($"[TutorialManager] TutorialPanel belum ada di scene! Langsung menjalankan callback untuk '{title}'.");
            onClose?.Invoke();
        }
    }

    // =========================================================================
    // 3. INTEGRASI TOMBOL MOBILE UI CANVAS (ANDROID)
    // =========================================================================

    /// <summary>
    /// Hubungkan fungsi ini ke Event OnClick() pada Button "Dash" di UI Canvas Android.
    /// </summary>
    public void OnDashButtonPressed()
    {
        ResolvePlayerReferences();

        if (playerInputs != null)
        {
            playerInputs.DashInput(true);
        }

        if (currentPhase == TutorialPhase.Phase2_DodgeDash)
        {
            RegisterDashAttempt();
        }
    }

    /// <summary>
    /// Hubungkan fungsi ini ke Event OnClick() pada Button "Serang" di UI Canvas Android.
    /// </summary>
    public void OnAttackButtonPressed()
    {
        ResolvePlayerReferences();

        if (playerFight != null)
        {
            playerFight.OnAttackButtonPressed();
        }
        else if (playerInputs != null)
        {
            playerInputs.FireInput(true);
        }
    }
}

