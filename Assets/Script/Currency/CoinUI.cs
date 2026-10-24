using UnityEngine;
using TMPro;

namespace Dreamrift.Currency
{
    /// <summary>
    /// Menampilkan jumlah koin pemain pada Canvas HUD.
    /// Otomatis subscribe ke CurrencyManager.OnCoinsChanged.
    /// </summary>
    public class CoinUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Komponen TextMeshProUGUI untuk angka koin")]
        [SerializeField] private TextMeshProUGUI coinText;

        [Tooltip("Transform icon koin untuk efek punch/pop saat koin bertambah")]
        [SerializeField] private RectTransform coinIconTransform;

        [Header("Formatting")]
        [SerializeField] private string prefix = "";
        [SerializeField] private string suffix = "";

        private int displayedCoins = -1;
        private Vector3 originalIconScale = Vector3.one;

        private void Awake()
        {
            if (coinIconTransform != null)
                originalIconScale = coinIconTransform.localScale;
        }

        private void OnEnable()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                UpdateDisplay(CurrencyManager.Instance.CurrentCoins, false);
            }
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                UpdateDisplay(CurrencyManager.Instance.CurrentCoins, false);
            }
        }

        private void OnDisable()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
            }
        }

        private void HandleCoinsChanged(int newAmount)
        {
            bool increased = newAmount > displayedCoins && displayedCoins >= 0;
            UpdateDisplay(newAmount, increased);
        }

        private void UpdateDisplay(int amount, bool playPopEffect)
        {
            displayedCoins = amount;
            if (coinText != null)
            {
                coinText.text = $"{prefix}{amount:N0}{suffix}";
            }

            if (playPopEffect && coinIconTransform != null && gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(PunchScaleCoroutine());
            }
        }

        private System.Collections.IEnumerator PunchScaleCoroutine()
        {
            float elapsed = 0f;
            float duration = 0.18f;
            Vector3 peakScale = originalIconScale * 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Naik lalu kembali normal
                float s = t < 0.5f ? (t / 0.5f) : (1f - (t - 0.5f) / 0.5f);
                coinIconTransform.localScale = Vector3.Lerp(originalIconScale, peakScale, s);
                yield return null;
            }

            coinIconTransform.localScale = originalIconScale;
        }
    }
}
