using UnityEngine;

namespace Dreamrift.Checkpoint
{
    /// <summary>
    /// Objek Checkpoint (Kuil / Tower) yang dapat diinteraksi oleh pemain.
    /// Mengimplementasikan IInteractable sehingga otomatis memunculkan tombol interaksi UI melayang
    /// dari PlayerInteraction saat pemain mendekat.
    /// 
    /// Saat diinteraksi:
    /// 1. Menyimpan checkpoint ke PlayerHealth
    /// 2. Memulihkan HP pemain sampai penuh (Full Heal)
    /// 3. Menyalakan efek visual aura & beacon kuil (warna berubah cerah suci)
    /// </summary>
    public class CheckpointShrine : MonoBehaviour, IInteractable
    {
        [Header("Checkpoint Settings")]
        [Tooltip("Offset titik spawn saat pemain respawn dari kuil ini (agar tidak menabrak pilar)")]
        [SerializeField] private Vector3 respawnOffset = new Vector3(0, 0, 1.5f);

        [Tooltip("Jarak maksimal interaksi pemain dengan kuil")]
        [SerializeField] private float interactRange = 3.5f;
        public float InteractRange => interactRange;

        [Header("Visual & Feedback")]
        [Tooltip("Lampu/Cahaya suci yang ada di kuil")]
        [SerializeField] private Light beaconLight;
        [SerializeField] private Color inactiveColor = new Color(0.85f, 0.45f, 0.2f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.92f, 1.0f);
        [SerializeField] private float activeLightIntensity = 5.5f;
        [SerializeField] private float activeLightRange = 12f;
        [SerializeField] private float inactiveLightRange = 7f;
        [SerializeField] private float lightPulseAmount = 0.9f;
        [SerializeField] private float lightPulseSpeed = 2.4f;

        [Tooltip("Partikel aura suci berkelanjutan yang menyala saat aktif (Beacon Pillar, Motes, Ground Circle)")]
        [SerializeField] private ParticleSystem activeAuraVFX;

        [Tooltip("Partikel ledakan satu kali (Shockwave & Holy Burst) saat kuil diaktifkan")]
        [SerializeField] private ParticleSystem activationBurstVFX;

        [Header("Crystal Core Animation")]
        [Tooltip("Transform kristal inti kuil yang melayang & berotasi")]
        [SerializeField] private Transform crystalTransform;
        [SerializeField] private Renderer crystalRenderer;
        [SerializeField] private Color inactiveCrystalColor = new Color(0.32f, 0.38f, 0.44f, 1f);
        [SerializeField] [ColorUsage(false, true)] private Color inactiveEmissionColor = new Color(0.32f, 0.16f, 0.06f, 1f);
        [SerializeField] private Color activeCrystalColor = new Color(0.25f, 0.94f, 1.0f, 1f);
        [SerializeField] [ColorUsage(false, true)] private Color activeEmissionColor = new Color(0.45f, 2.9f, 4.0f, 1f);
        [SerializeField] private float inactiveRotationSpeed = 16f;
        [SerializeField] private float activeRotationSpeed = 68f;
        [SerializeField] private float floatAmplitude = 0.14f;
        [SerializeField] private float floatFrequency = 1.85f;

        [Header("Audio")]
        [Tooltip("Audio clip saat kuil diaktifkan / dipakai istirahat")]
        [SerializeField] private AudioClip activateSFX;
        [SerializeField] private AudioSource audioSource;

        [Header("State")]
        [SerializeField] private bool isActivated = false;

        public bool IsActivated => isActivated;

        public string InteractLabel => isActivated ? "Istirahat & Pulihkan" : "Aktifkan Kuil";

        private Vector3 crystalBaseLocalPos;
        private bool crystalBaseCached;
        private MaterialPropertyBlock crystalPropBlock;
        private float activationFlashTimer;
        private const float ActivationFlashDuration = 0.65f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            ResolveCrystalReferences();
        }

        private void Start()
        {
            ResolveCrystalReferences();
            UpdateVisuals();
        }

        private void Update()
        {
            float flashNormalized = 0f;
            if (activationFlashTimer > 0f)
            {
                activationFlashTimer = Mathf.Max(0f, activationFlashTimer - Time.deltaTime);
                flashNormalized = activationFlashTimer / ActivationFlashDuration;
            }

            AnimateCrystal(flashNormalized);
            AnimateBeaconLight(flashNormalized);
        }

        public void OnInteract()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;

            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Hitung posisi respawn aman di depan kuil
                Vector3 spawnPos = transform.position + transform.TransformDirection(respawnOffset);
                playerHealth.SetCheckpoint(spawnPos, transform.rotation);

                // Pulihkan darah sampai penuh
                playerHealth.Heal(playerHealth.MaxHealth);
            }

            // Aktifkan state & visuals beserta burst aktivasi
            SetActivatedStateImmediate(true, triggerBurst: true);

            // Putar audio jika ada
            if (audioSource != null && activateSFX != null)
            {
                audioSource.PlayOneShot(activateSFX);
            }

            Debug.Log($"[CheckpointShrine] {gameObject.name} diaktifkan! Checkpoint disimpan dan HP dipulihkan.");
        }

        public void SetActivatedStateImmediate(bool active, bool triggerBurst = false)
        {
            ResolveCrystalReferences();
            isActivated = active;

            if (active && triggerBurst)
            {
                activationFlashTimer = ActivationFlashDuration;
                if (activationBurstVFX != null)
                {
                    activationBurstVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    activationBurstVFX.Play(true);
                }
            }

            UpdateVisuals();
            float flashNormalized = activationFlashTimer > 0f ? (activationFlashTimer / ActivationFlashDuration) : 0f;
            AnimateCrystal(flashNormalized);
            AnimateBeaconLight(flashNormalized);
        }

        private void ResolveCrystalReferences()
        {
            if (crystalTransform == null)
            {
                Transform found = transform.Find("Visuals/Crystal_Core");
                if (found == null)
                    found = transform.Find("Crystal_Core");
                crystalTransform = found;
            }

            if (crystalTransform != null && !crystalBaseCached)
            {
                crystalBaseLocalPos = crystalTransform.localPosition;
                crystalBaseCached = true;
            }

            if (crystalRenderer == null && crystalTransform != null)
            {
                crystalRenderer = crystalTransform.GetComponent<Renderer>();
            }

            if (crystalPropBlock == null)
            {
                crystalPropBlock = new MaterialPropertyBlock();
            }
        }

        private void UpdateVisuals()
        {
            if (beaconLight != null)
            {
                beaconLight.color = isActivated ? activeColor : inactiveColor;
                beaconLight.range = isActivated ? activeLightRange : inactiveLightRange;
                beaconLight.intensity = isActivated ? activeLightIntensity : (activeLightIntensity * 0.25f);
            }

            if (activeAuraVFX != null)
            {
                if (isActivated)
                {
                    if (!activeAuraVFX.isPlaying)
                        activeAuraVFX.Play(true);
                }
                else
                {
                    if (activeAuraVFX.isPlaying)
                        activeAuraVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            ApplyCrystalMaterialState(0f);
        }

        private void AnimateCrystal(float flashNormalized)
        {
            if (crystalTransform != null && crystalBaseCached)
            {
                float rotSpeed = isActivated ? activeRotationSpeed : inactiveRotationSpeed;
                crystalTransform.Rotate(Vector3.up, rotSpeed * Time.deltaTime, Space.Self);

                float amp = isActivated ? floatAmplitude : (floatAmplitude * 0.2f);
                float bobOffset = Mathf.Sin(Time.time * floatFrequency) * amp;
                crystalTransform.localPosition = crystalBaseLocalPos + Vector3.up * bobOffset;
            }

            ApplyCrystalMaterialState(flashNormalized);
        }

        private void AnimateBeaconLight(float flashNormalized)
        {
            if (beaconLight == null) return;

            if (isActivated)
            {
                float pulse = Mathf.Sin(Time.time * lightPulseSpeed) * lightPulseAmount;
                float flashBoost = flashNormalized * (activeLightIntensity * 1.6f);
                beaconLight.color = Color.Lerp(activeColor, Color.white, flashNormalized * 0.45f);
                beaconLight.range = activeLightRange + flashNormalized * 4f;
                beaconLight.intensity = Mathf.Max(0.5f, activeLightIntensity + pulse + flashBoost);
            }
            else
            {
                beaconLight.color = inactiveColor;
                beaconLight.range = inactiveLightRange;
                beaconLight.intensity = activeLightIntensity * 0.25f;
            }
        }

        private void ApplyCrystalMaterialState(float flashNormalized)
        {
            if (crystalRenderer == null) return;
            if (crystalPropBlock == null)
                crystalPropBlock = new MaterialPropertyBlock();

            crystalRenderer.GetPropertyBlock(crystalPropBlock);

            Color baseCol = isActivated ? activeCrystalColor : inactiveCrystalColor;
            Color emisCol;
            if (isActivated)
            {
                float pulse = 1f + 0.22f * Mathf.Sin(Time.time * (lightPulseSpeed * 1.15f));
                float flashMultiplier = 1f + flashNormalized * 1.5f;
                emisCol = activeEmissionColor * (pulse * flashMultiplier);
            }
            else
            {
                emisCol = inactiveEmissionColor;
            }

            crystalPropBlock.SetColor(BaseColorId, baseCol);
            crystalPropBlock.SetColor(ColorId, baseCol);
            crystalPropBlock.SetColor(EmissionColorId, emisCol);
            crystalRenderer.SetPropertyBlock(crystalPropBlock);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 spawnPos = transform.position + transform.TransformDirection(respawnOffset);
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
        }
    }
}
