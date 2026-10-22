using UnityEngine;
using System;

namespace Dreamrift.Currency
{
    /// <summary>
    /// Mengelola jumlah koin (currency) pemain.
    /// Tersimpan persisten menggunakan PlayerPrefs dan menyediakan event OnCoinsChanged untuk UI.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private const string COINS_PREF_KEY = "Player_Coins";

        [Header("Starting Balance (New Game)")]
        [SerializeField] private int initialCoins = 0;

        [Header("Audio (Opsional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip coinPickupSFX;

        public int CurrentCoins { get; private set; }

        public event Action<int> OnCoinsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadCoins();
        }

        private void Start()
        {
            OnCoinsChanged?.Invoke(CurrentCoins);
        }

        private void LoadCoins()
        {
            CurrentCoins = PlayerPrefs.GetInt(COINS_PREF_KEY, initialCoins);
        }

        private void SaveCoins()
        {
            PlayerPrefs.SetInt(COINS_PREF_KEY, CurrentCoins);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Tambah jumlah koin pemain dan picu event UI.
        /// </summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            CurrentCoins += amount;
            SaveCoins();
            OnCoinsChanged?.Invoke(CurrentCoins);

            if (audioSource != null && coinPickupSFX != null)
            {
                audioSource.PlayOneShot(coinPickupSFX);
            }
        }

        /// <summary>
        /// Belanja/kurangi koin jika mencukupi. Mengembalikan true jika berhasil.
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;
            if (CurrentCoins < amount) return false;

            CurrentCoins -= amount;
            SaveCoins();
            OnCoinsChanged?.Invoke(CurrentCoins);
            return true;
        }

        public bool CanAfford(int amount)
        {
            return CurrentCoins >= amount;
        }

        /// <summary>
        /// Reset saldo koin (misal saat restart game).
        /// </summary>
        public void ResetCoins()
        {
            CurrentCoins = initialCoins;
            SaveCoins();
            OnCoinsChanged?.Invoke(CurrentCoins);
        }
    }
}
