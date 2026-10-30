using System;
using UnityEngine;
using Dreamrift.Currency;

/// <summary>
/// Data & logika upgrade senjata (staff melee PlayerFight).
/// Static class: bisa diakses dari mana saja (Main Menu UI, PlayerFight, dsb)
/// tanpa perlu MonoBehaviour di scene.
/// Level upgrade tersimpan persisten via PlayerPrefs.
///
/// Cara kerja:
/// - Tiap level menambah damage senjata (DamagePerLevel)
/// - Tiap level mengubah tier VFX (warna lebih kental + scale lebih besar)
/// - Upgrade hanya bisa jika koin cukup (dicek lewat CurrencyManager)
/// </summary>
public static class WeaponUpgradeData
{
    // ===================== CONFIG (mudah diubah di sini) =====================
    public const int MaxLevel = 5;                 // level 0 (dasar) s/d level 5 (max)
    public const int DamagePerLevel = 8;           // tambahan damage per level upgrade
    public const string UpgradeLevelPrefKey = "Weapon_UpgradeLevel"; // key PlayerPrefs

    // Harga upgrade per level (index 0 = upgrade ke level 1, dst)
    private static readonly int[] UpgradeCosts = { 50, 100, 200, 400, 800 };

    // Tier warna VFX per level (index 0 = dasar). Makin tinggi level, makin kental.
    private static readonly Color[] VfxTierColors =
    {
        new Color(0.50f, 0.72f, 1.00f), // Lv0: biru muda (dasar)
        new Color(0.35f, 0.85f, 1.00f), // Lv1: cyan
        new Color(0.62f, 0.45f, 1.00f), // Lv2: ungu
        new Color(1.00f, 0.55f, 0.20f), // Lv3: oranye
        new Color(1.00f, 0.40f, 0.35f), // Lv4: merah bata
        new Color(1.00f, 0.82f, 0.29f), // Lv5: emas
    };

    // Scale bonus VFX per level (index 0 = dasar, 1.0x). Makin tinggi level makin besar.
    private static readonly float[] VfxTierScales = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f };

    // Nama tier untuk ditampilkan di UI
    private static readonly string[] VfxTierNames =
    {
        "Dasar", "Cyan", "Ungu", "Oranye", "Merah", "Emas",
    };

    // ===================== EVENT =====================
    /// <summary>Dipanggil setiap kali level upgrade berubah (untuk refresh UI).</summary>
    public static event Action<int> OnUpgradeChanged;

    // ===================== PROPERTI =====================
    /// <summary>Level upgrade saat ini (0 = dasar, MaxLevel = maksimum).</summary>
    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(UpgradeLevelPrefKey, 0);
        private set
        {
            value = Mathf.Clamp(value, 0, MaxLevel);
            PlayerPrefs.SetInt(UpgradeLevelPrefKey, value);
            PlayerPrefs.Save();
            OnUpgradeChanged?.Invoke(value);
        }
    }

    /// <summary>Bonus damage senjata dari level upgrade saat ini.</summary>
    public static int BonusDamage => CurrentLevel * DamagePerLevel;

    /// <summary>Total damage dasar senjata saat ini (tanpa multiplier combo).</summary>
    public static int CurrentBaseDamage => BaseWeaponDamage + BonusDamage;

    /// <summary>Damage dasar senjata sebelum upgrade (sinkron dengan PlayerFight.baseDamage).</summary>
    public const int BaseWeaponDamage = 20;

    /// <summary>Level sudah maksimum?</summary>
    public static bool IsMaxLevel => CurrentLevel >= MaxLevel;

    // ===================== HARGA & KEMAMPUAN =====================
    /// <summary>
    /// Harga upgrade ke level berikutnya.
    /// Mengembalikan null jika sudah di level maksimum.
    /// </summary>
    public static int? GetUpgradeCost()
    {
        if (IsMaxLevel) return null;

        int nextLevel = CurrentLevel + 1;
        return UpgradeCosts[nextLevel - 1];
    }

    /// <summary>Apakah koin pemain cukup untuk upgrade berikutnya?</summary>
    public static bool CanUpgrade()
    {
        if (IsMaxLevel) return false;

        int? cost = GetUpgradeCost();
        if (cost == null) return false;

        // CurrencyManager mungkin belum ada di scene tertentu (misal scene prototype)
        if (CurrencyManager.Instance == null) return false;
        return CurrencyManager.Instance.CanAfford(cost.Value);
    }

    /// <summary>
    /// Mencoba upgrade: potong koin lalu naikkan level.
    /// Mengembalikan true jika berhasil.
    /// </summary>
    public static bool TryUpgrade()
    {
        if (IsMaxLevel)
        {
            Debug.Log("[WeaponUpgrade] Senjata sudah level maksimum.");
            return false;
        }

        int? cost = GetUpgradeCost();
        if (cost == null) return false;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("[WeaponUpgrade] CurrencyManager tidak ada di scene ini — upgrade gagal.");
            return false;
        }

        if (!CurrencyManager.Instance.SpendCoins(cost.Value))
        {
            Debug.Log($"[WeaponUpgrade] Koin tidak cukup. Butuh {cost.Value}.");
            return false;
        }

        int newLevel = CurrentLevel + 1;
        CurrentLevel = newLevel;
        Debug.Log($"[WeaponUpgrade] Upgrade ke level {newLevel}! Damage +{DamagePerLevel}, VFX tier '{GetVfxTierName()}'.");

        return true;
    }

    /// <summary>Reset level upgrade ke dasar (debug/testing).</summary>
    public static void ResetUpgrade()
    {
        CurrentLevel = 0;
    }

    // ===================== VFX TIER =====================
    /// <summary>Warna VFX untuk level upgrade tertentu (default: level saat ini).</summary>
    public static Color GetVfxColor(int level = -1)
    {
        if (level < 0) level = CurrentLevel;
        return VfxTierColors[Mathf.Clamp(level, 0, VfxTierColors.Length - 1)];
    }

    /// <summary>Scale VFX untuk level upgrade tertentu (default: level saat ini).</summary>
    public static float GetVfxScale(int level = -1)
    {
        if (level < 0) level = CurrentLevel;
        return VfxTierScales[Mathf.Clamp(level, 0, VfxTierScales.Length - 1)];
    }

    /// <summary>Nama tier VFX untuk level tertentu (default: level saat ini).</summary>
    public static string GetVfxTierName(int level = -1)
    {
        if (level < 0) level = CurrentLevel;
        return VfxTierNames[Mathf.Clamp(level, 0, VfxTierNames.Length - 1)];
    }
}
