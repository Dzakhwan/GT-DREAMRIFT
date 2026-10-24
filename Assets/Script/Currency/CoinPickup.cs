using UnityEngine;

namespace Dreamrift.Currency
{
    /// <summary>
    /// Koin 3D yang dapat dipungut oleh Player di dalam game world.
    /// Berputar dan melayang secara anggun, otomatis menambahkan koin ke CurrencyManager saat disentuh.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CoinPickup : MonoBehaviour
    {
        [Header("Coin Value")]
        [Tooltip("Jumlah koin yang didapatkan saat dipungut")]
        [SerializeField, Min(1)] private int coinValue = 1;

        [Header("Animation")]
        [Tooltip("Kecepatan putaran koin (derajat/detik)")]
        [SerializeField] private float rotationSpeed = 120f;

        [Tooltip("Amplitudo melayang naik-turun (bobbing)")]
        [SerializeField] private float bobHeight = 0.15f;

        [Tooltip("Kecepatan melayang naik-turun")]
        [SerializeField] private float bobSpeed = 3f;

        [Header("Effects")]
        [Tooltip("Prefab partikel kilau emas saat koin dipungut (opsional)")]
        [SerializeField] private GameObject pickupVFXPrefab;

        private Vector3 startPosition;
        private bool isCollected = false;

        private void Start()
        {
            startPosition = transform.position;

            // Pastikan collider adalah trigger
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            // Rotasi berputar pada sumbu Y
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Melayang naik-turun perlahan
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddCoins(coinValue);
            }
            else
            {
                Debug.LogWarning("[CoinPickup] CurrencyManager.Instance tidak ditemukan di scene!");
            }

            if (pickupVFXPrefab != null)
            {
                Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        public void SetCoinValue(int val)
        {
            coinValue = Mathf.Max(1, val);
        }
    }
}
