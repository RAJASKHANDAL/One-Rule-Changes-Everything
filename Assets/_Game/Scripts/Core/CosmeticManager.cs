using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent coin wallet (PlayerPrefs). Run coins are added as they are collected; purchases spend from it exactly once.</summary>
public static class Wallet
{
    const string Key = "OneRule.Wallet";

    public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));
    public static event Action Changed;

    public static void Add(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(Key, Coins + amount);
        Changed?.Invoke();
    }

    public static bool TrySpend(int amount)
    {
        if (amount < 0 || Coins < amount) return false;
        PlayerPrefs.SetInt(Key, Coins - amount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static void Save() => PlayerPrefs.Save();
}

public enum CosmeticCategory { Scarf, Hood, Trail, CoinSparks }

[Serializable]
public class CosmeticItem
{
    public string Id;
    public string Name;
    public CosmeticCategory Category;
    public int Cost;
    public Color Color;
    public CosmeticItem(string id, string name, CosmeticCategory cat, int cost, Color color) { Id = id; Name = name; Category = cat; Cost = cost; Color = color; }
}

/// <summary>
/// Purely cosmetic unlocks (never affects movement, hitboxes, health or difficulty). Unlocked ids and the equipped item per
/// category are persisted in PlayerPrefs and validated on every read.
/// </summary>
public static class CosmeticManager
{
    const string UnlockedKey = "OneRule.Cosmetics.Unlocked";
    const string EquippedPrefix = "OneRule.Cosmetics.Equipped.";

    public static event Action Changed;

    static readonly CosmeticItem[] items =
    {
        new CosmeticItem("scarf_orange", "ORANGE SCARF", CosmeticCategory.Scarf, 0, new Color(1f, 0.52f, 0.12f)),
        new CosmeticItem("scarf_pink", "PINK SCARF", CosmeticCategory.Scarf, 30, new Color(1f, 0.3f, 0.68f)),
        new CosmeticItem("scarf_cyan", "CYAN SCARF", CosmeticCategory.Scarf, 30, new Color(0.25f, 0.88f, 1f)),
        new CosmeticItem("hood_indigo", "INDIGO HOOD", CosmeticCategory.Hood, 0, new Color(0.2f, 0.18f, 0.42f)),
        new CosmeticItem("hood_teal", "TEAL HOOD", CosmeticCategory.Hood, 40, new Color(0.1f, 0.5f, 0.55f)),
        new CosmeticItem("hood_crimson", "CRIMSON HOOD", CosmeticCategory.Hood, 40, new Color(0.55f, 0.12f, 0.25f)),
        new CosmeticItem("trail_none", "NO TRAIL", CosmeticCategory.Trail, 0, Color.clear),
        new CosmeticItem("trail_cyan", "CYAN TRAIL", CosmeticCategory.Trail, 60, new Color(0.25f, 0.88f, 1f)),
        new CosmeticItem("trail_pink", "PINK TRAIL", CosmeticCategory.Trail, 60, new Color(1f, 0.3f, 0.68f)),
        new CosmeticItem("sparks_gold", "GOLD SPARKS", CosmeticCategory.CoinSparks, 0, new Color(1f, 0.85f, 0.3f)),
        new CosmeticItem("sparks_pink", "PINK SPARKS", CosmeticCategory.CoinSparks, 40, new Color(1f, 0.4f, 0.8f)),
        new CosmeticItem("sparks_cyan", "CYAN SPARKS", CosmeticCategory.CoinSparks, 40, new Color(0.4f, 0.95f, 1f)),
    };

    public static IReadOnlyList<CosmeticItem> All => items;

    public static CosmeticItem Find(string id)
    {
        foreach (var it in items) if (it.Id == id) return it;
        return null;
    }

    public static CosmeticItem Default(CosmeticCategory cat)
    {
        foreach (var it in items) if (it.Category == cat && it.Cost == 0) return it;
        return null;
    }

    public static bool IsUnlocked(string id)
    {
        var item = Find(id);
        if (item == null) return false;
        if (item.Cost == 0) return true;
        foreach (var part in PlayerPrefs.GetString(UnlockedKey, "").Split(','))
            if (part == id) return true;
        return false;
    }

    /// <summary>Equipped item for a category; falls back to the free default if the stored value is unknown or locked.</summary>
    public static CosmeticItem Equipped(CosmeticCategory cat)
    {
        var stored = Find(PlayerPrefs.GetString(EquippedPrefix + cat, ""));
        if (stored != null && stored.Category == cat && IsUnlocked(stored.Id)) return stored;
        return Default(cat);
    }

    public static bool IsEquipped(string id)
    {
        var item = Find(id);
        return item != null && Equipped(item.Category).Id == id;
    }

    /// <summary>Buys an item. Returns true only if it was locked, affordable, and the cost was deducted (exactly once).</summary>
    public static bool TryBuy(string id)
    {
        var item = Find(id);
        if (item == null || IsUnlocked(id)) return false;
        if (!Wallet.TrySpend(item.Cost)) return false;
        string existing = PlayerPrefs.GetString(UnlockedKey, "");
        PlayerPrefs.SetString(UnlockedKey, existing.Length == 0 ? id : existing + "," + id);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static bool Equip(string id)
    {
        var item = Find(id);
        if (item == null || !IsUnlocked(id)) return false;
        PlayerPrefs.SetString(EquippedPrefix + item.Category, id);
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }
}
