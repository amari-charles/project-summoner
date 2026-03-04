using System;
using System.Collections.Generic;
using Godot;

namespace Fateforged.Infrastructure.Billing;

/// <summary>
/// Registry of all real-money purchasable products.
/// Method names mirror legacy GDScript API for compatibility.
/// </summary>
[GlobalClass]
public partial class BillingCatalogService : Node
{
    private sealed class GemPackData
    {
        public required int Gems { get; init; }
        public required float PriceUsd { get; init; }
        public int BonusPercent { get; init; }
    }

    private sealed class BundleData
    {
        public required string DisplayName { get; init; }
        public required string Description { get; init; }
        public required int Gems { get; init; }
        public required Godot.Collections.Dictionary Rewards { get; init; }
        public required float PriceUsd { get; init; }
    }

    public static BillingCatalogService? Instance { get; private set; }

    private static readonly Dictionary<string, GemPackData> GEM_PACKS = new()
    {
        ["gems_100"] = new() { Gems = 100, PriceUsd = 0.99f, BonusPercent = 0 },
        ["gems_500"] = new() { Gems = 550, PriceUsd = 4.99f, BonusPercent = 10 },
        ["gems_1200"] = new() { Gems = 1320, PriceUsd = 9.99f, BonusPercent = 10 },
        ["gems_2500"] = new() { Gems = 2750, PriceUsd = 19.99f, BonusPercent = 10 },
        ["gems_6500"] = new() { Gems = 7150, PriceUsd = 49.99f, BonusPercent = 10 },
    };

    private static readonly Dictionary<string, BundleData> BUNDLES = new()
    {
        ["starter_pack"] = new()
        {
            DisplayName = "Starter Pack",
            Description = "200 Gems + Lightning Adept Summoner - Great value for new players!",
            Gems = 200,
            Rewards = new Godot.Collections.Dictionary { ["summoners"] = new Godot.Collections.Array<string> { "lightning_adept" } },
            PriceUsd = 4.99f,
        },
        ["summoner_pack_verdant"] = new()
        {
            DisplayName = "Verdant Sage Pack",
            Description = "100 Gems + Verdant Sage Summoner",
            Gems = 100,
            Rewards = new Godot.Collections.Dictionary { ["summoners"] = new Godot.Collections.Array<string> { "verdant_sage" } },
            PriceUsd = 3.99f,
        },
        ["cosmetic_bundle_fire"] = new()
        {
            DisplayName = "Fire Cosmetics Bundle",
            Description = "Fire-themed card back and UI theme",
            Gems = 0,
            Rewards = new Godot.Collections.Dictionary { ["cosmetics"] = new Godot.Collections.Array<string> { "card_back_fire", "ui_theme_fire" } },
            PriceUsd = 2.99f,
        },
    };

    private static readonly Dictionary<string, Dictionary<string, string>> PLATFORM_PRODUCT_IDS = new(StringComparer.OrdinalIgnoreCase)
    {
        ["steam"] = new()
        {
            ["gems_100"] = "gems_100",
            ["gems_500"] = "gems_500",
            ["gems_1200"] = "gems_1200",
            ["gems_2500"] = "gems_2500",
            ["gems_6500"] = "gems_6500",
            ["starter_pack"] = "starter_pack",
            ["summoner_pack_verdant"] = "summoner_pack_verdant",
            ["cosmetic_bundle_fire"] = "cosmetic_bundle_fire",
        },
        ["ios"] = new()
        {
            ["gems_100"] = "com.projectsummoner.gems_100",
            ["gems_500"] = "com.projectsummoner.gems_500",
            ["gems_1200"] = "com.projectsummoner.gems_1200",
            ["gems_2500"] = "com.projectsummoner.gems_2500",
            ["gems_6500"] = "com.projectsummoner.gems_6500",
            ["starter_pack"] = "com.projectsummoner.starter_pack",
            ["summoner_pack_verdant"] = "com.projectsummoner.summoner_pack_verdant",
            ["cosmetic_bundle_fire"] = "com.projectsummoner.cosmetic_bundle_fire",
        },
        ["android"] = new()
        {
            ["gems_100"] = "gems_100",
            ["gems_500"] = "gems_500",
            ["gems_1200"] = "gems_1200",
            ["gems_2500"] = "gems_2500",
            ["gems_6500"] = "gems_6500",
            ["starter_pack"] = "starter_pack",
            ["summoner_pack_verdant"] = "summoner_pack_verdant",
            ["cosmetic_bundle_fire"] = "cosmetic_bundle_fire",
        },
    };

    private readonly Dictionary<string, BillingProduct> _products_cache = new();

    public override void _Ready()
    {
        Instance = this;
        _build_products_cache();
    }

    public BillingProduct? get_product(string product_id)
    {
        return _products_cache.GetValueOrDefault(product_id);
    }

    public Godot.Collections.Array<BillingProduct> get_gem_packs()
    {
        var packs = new Godot.Collections.Array<BillingProduct>();
        foreach (var packId in GEM_PACKS.Keys)
        {
            var product = get_product(packId);
            if (product != null)
                packs.Add(product);
        }

        return packs;
    }

    public Godot.Collections.Array<BillingProduct> get_bundles()
    {
        var bundles = new Godot.Collections.Array<BillingProduct>();
        foreach (var bundleId in BUNDLES.Keys)
        {
            var product = get_product(bundleId);
            if (product != null)
                bundles.Add(product);
        }

        return bundles;
    }

    public Godot.Collections.Array<BillingProduct> get_all_products()
    {
        var all = new Godot.Collections.Array<BillingProduct>();
        foreach (var product in _products_cache.Values)
            all.Add(product);
        return all;
    }

    public bool has_product(string product_id)
    {
        return _products_cache.ContainsKey(product_id);
    }

    public string get_platform_product_id(string product_id, string platform)
    {
        if (!PLATFORM_PRODUCT_IDS.TryGetValue(platform, out var platformMap))
            return product_id;

        return platformMap.GetValueOrDefault(product_id, "");
    }

    /// <summary>
    /// Resolve a platform/store product ID back to the internal product ID.
    /// Returns the input unchanged when no mapping is found.
    /// </summary>
    public string get_internal_product_id(string platform_product_id, string platform)
    {
        if (has_product(platform_product_id))
            return platform_product_id;

        if (!PLATFORM_PRODUCT_IDS.TryGetValue(platform, out var platformMap))
            return platform_product_id;

        foreach (var (internalId, storeId) in platformMap)
        {
            if (storeId == platform_product_id)
                return internalId;
        }

        return platform_product_id;
    }

    public bool is_gem_pack(string product_id)
    {
        return GEM_PACKS.ContainsKey(product_id);
    }

    public bool is_bundle(string product_id)
    {
        return BUNDLES.ContainsKey(product_id);
    }

    private void _build_products_cache()
    {
        _products_cache.Clear();

        foreach (var pair in GEM_PACKS)
        {
            var product = BillingProduct.create_gem_pack(
                pair.Key,
                pair.Value.Gems,
                pair.Value.PriceUsd,
                pair.Value.BonusPercent);
            _products_cache[pair.Key] = product;
        }

        foreach (var pair in BUNDLES)
        {
            var data = pair.Value;
            var product = BillingProduct.create_bundle(
                pair.Key,
                data.DisplayName,
                data.Description,
                data.PriceUsd,
                data.Rewards,
                data.Gems);
            _products_cache[pair.Key] = product;
        }

        GD.Print($"[BillingCatalog] Loaded {_products_cache.Count} products ({GEM_PACKS.Count} gem packs, {BUNDLES.Count} bundles)");
    }
}
