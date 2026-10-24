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
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 1.0f);
        [SerializeField] private float activeLightIntensity = 3.5f;

        [Tooltip("Partikel aura suci yang menyala saat aktif (opsional)")]
        [SerializeField] private ParticleSystem activeAuraVFX;

        [Tooltip("Audio clip saat kuil diaktifkan / dipakai istirahat")]
        [SerializeField] private AudioClip activateSFX;
        [SerializeField] private AudioSource audioSource;

        [Header("State")]
        [SerializeField] private bool isActivated = false;

        public bool IsActivated => isActivated;

        public string InteractLabel => isActivated ? "Istirahat & Pulihkan" : "Aktifkan Kuil";

        private void Start()
        {
            UpdateVisuals();
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

            // Aktifkan state & visuals
            isActivated = true;
            UpdateVisuals();

            // Putar audio jika ada
            if (audioSource != null && activateSFX != null)
            {
                audioSource.PlayOneShot(activateSFX);
            }

            Debug.Log($"[CheckpointShrine] {gameObject.name} diaktifkan! Checkpoint disimpan dan HP dipulihkan.");
        }

        private void UpdateVisuals()
        {
            if (beaconLight != null)
            {
                beaconLight.color = isActivated ? activeColor : inactiveColor;
                beaconLight.intensity = isActivated ? activeLightIntensity : (activeLightIntensity * 0.4f);
            }

            if (activeAuraVFX != null)
            {
                if (isActivated && !activeAuraVFX.isPlaying)
                    activeAuraVFX.Play();
                else if (!isActivated && activeAuraVFX.isPlaying)
                    activeAuraVFX.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 spawnPos = transform.position + transform.TransformDirection(respawnOffset);
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
        }
    }
}
