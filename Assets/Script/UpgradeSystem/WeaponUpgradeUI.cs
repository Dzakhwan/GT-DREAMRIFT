using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dreamrift.Currency;

/// <summary>
/// Panel UI upgrade senjata (di Main Menu).
/// Menampilkan saldo koin, level upgrade, damage senjata, tier VFX,
/// biaya upgrade berikutnya, dan tombol UPGRADE.
///
/// Setup (Drag & Drop di Inspector):
/// - Taruh script ini di GameObject 'Upgrade Panel'
/// - Isi semua referensi TMP text & tombol
/// - Panel dibuka/ditutup lewat MainMenuController.OpenUpgradePanel()/CloseUpgradePanel()
/// </summary>
public class WeaponUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text saldo koin pemain")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Tooltip("Text level upgrade (contoh: 'Level: 2/5')")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Tooltip("Text damage senjata saat ini")]
    [SerializeField] private TextMeshProUGUI damageText;

    [Tooltip("Text nama tier VFX saat ini")]
    [SerializeField] private TextMeshProUGUI tierText;

    [Tooltip("Image swatch kecil untuk preview warna tier VFX (opsional)")]
    [SerializeField] private Image tierSwatchImage;

    [Tooltip("Text biaya upgrade berikutnya")]
    [SerializeField] private TextMeshProUGUI costText;

    [Tooltip("Tombol UPGRADE")]
    [SerializeField] private Button upgradeButton;

    [Tooltip("Text di dalam tombol UPGRADE (biar bisa diganti 'UPGRADE' / 'MAX')")]
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

    [Tooltip("Image tombol UPGRADE (untuk di-redupkan saat tidak bisa beli)")]
    [SerializeField] private Image upgradeButtonImage;

    [Tooltip("Text pesan status (misal: 'Koin tidak cukup!' / 'Upgrade berhasil!')")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Visual")]
    [Tooltip("Warna tombol saat bisa di-upgrade")]
    [SerializeField] private Color affordableColor = new Color(0.85f, 0.72f, 0.25f);

    [Tooltip("Warna tombol saat koin tidak cukup")]
    [SerializeField] private Color noMoneyColor = new Color(0.45f, 0.45f, 0.45f);

    private void OnEnable()
    {
        // Subscribe event koin & event upgrade
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
        }
        WeaponUpgradeData.OnUpgradeChanged += HandleUpgradeChanged;

        ClearStatus();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }
        WeaponUpgradeData.OnUpgradeChanged -= HandleUpgradeChanged;

        // Hentikan efek status
        CancelInvoke(nameof(ClearStatus));
    }

    // ===================== DISPLAY =====================
    private void RefreshDisplay()
    {
        int level = WeaponUpgradeData.CurrentLevel;

        // Saldo koin
        if (coinText != null)
        {
            int coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentCoins : 0;
            coinText.text = $"{coins:N0}";
        }

        // Level
        if (levelText != null)
        {
            levelText.text = $"Level: {level}/{WeaponUpgradeData.MaxLevel}";
        }

        // Damage (total sekarang, + bonus yang akan didapat bila upgrade)
        if (damageText != null)
        {
            int current = WeaponUpgradeData.CurrentBaseDamage;
            if (!WeaponUpgradeData.IsMaxLevel)
            {
                int next = current + WeaponUpgradeData.DamagePerLevel;
                damageText.text = $"Damage: {current}  →  {next}";
            }
            else
            {
                damageText.text = $"Damage: {current}";
            }
        }

        // Tier VFX + swatch warna
        if (tierText != null)
        {
            tierText.text = $"VFX: {WeaponUpgradeData.GetVfxTierName()}";
        }
        if (tierSwatchImage != null)
        {
            tierSwatchImage.color = WeaponUpgradeData.GetVfxColor();
        }

        // Biaya upgrade & kondisi tombol
        if (WeaponUpgradeData.IsMaxLevel)
        {
            if (costText != null) costText.text = "MAX";
            if (upgradeButtonText != null) upgradeButtonText.text = "MAX";
            if (upgradeButton != null) upgradeButton.interactable = false;
            if (upgradeButtonImage != null) upgradeButtonImage.color = noMoneyColor;
        }
        else
        {
            int? cost = WeaponUpgradeData.GetUpgradeCost();
            if (costText != null) costText.text = $"Biaya: {(cost ?? 0):N0} Koin";
            if (upgradeButtonText != null) upgradeButtonText.text = "UPGRADE";

            bool affordable = WeaponUpgradeData.CanUpgrade();
            if (upgradeButton != null) upgradeButton.interactable = affordable;
            if (upgradeButtonImage != null) upgradeButtonImage.color = affordable ? affordableColor : noMoneyColor;
        }
    }

    // ===================== HANDLER =====================
    private void HandleCoinsChanged(int _)
    {
        RefreshDisplay();
    }

    private void HandleUpgradeChanged(int newLevel)
    {
        RefreshDisplay();
        if (statusText != null)
        {
            statusText.text = $"Upgrade berhasil! Level {newLevel} ({WeaponUpgradeData.GetVfxTierName()})";
            statusText.color = new Color(0.5f, 1f, 0.5f);
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), 2.5f);
        }
    }

    /// <summary>Menampilkan pesan error koin tidak cukup (dipanggil tombol).</summary>
    public void OnUpgradeButtonClicked()
    {
        if (WeaponUpgradeData.IsMaxLevel) return;

        int? cost = WeaponUpgradeData.GetUpgradeCost();
        if (cost != null && CurrencyManager.Instance != null &&
            CurrencyManager.Instance.CurrentCoins < cost.Value)
        {
            if (statusText != null)
            {
                statusText.text = $"Koin tidak cukup! Butuh {cost.Value:N0} koin.";
                statusText.color = new Color(1f, 0.55f, 0.5f);
                CancelInvoke(nameof(ClearStatus));
                Invoke(nameof(ClearStatus), 2.5f);
            }
            return;
        }

        WeaponUpgradeData.TryUpgrade();
    }

    // ===================== UTIL =====================
    private void ClearStatus()
    {
        if (statusText != null) statusText.text = "";
    }
}
