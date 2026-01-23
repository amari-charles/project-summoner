using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Services.Profile;

namespace ProjectSummoner.Services.Shop;

/// <summary>
/// Shop Service - Manages shop offerings and purchases.
///
/// Handles both Caravan (campaign event) shops and General UI shop.
/// Data layer: ProfileRepo owns all persistent state.
/// Service layer: ShopService orchestrates purchases and validates.
/// </summary>
[GlobalClass]
public partial class ShopService : Node
{
    public static ShopService? Instance { get; private set; }

    [Signal]
    public delegate void PurchaseCompletedEventHandler(string offeringId, string shopId);

    [Signal]
    public delegate void PurchaseFailedEventHandler(string offeringId, string reason);

    [Signal]
    public delegate void ShopRefreshedEventHandler(string shopId);

    private IProfileRepository? _profileRepo;

    // Shop catalog (loaded from GDScript)
    private readonly Dictionary<string, Godot.Collections.Dictionary> _shops = [];

    // Purchase history cache
    private readonly Dictionary<string, int> _purchaseCache = [];

    // Callbacks for GDScript dependencies
    private Func<Godot.Collections.Dictionary>? _getResourcesFunc;
    private Action<Godot.Collections.Dictionary>? _updateResourcesFunc;
    private Func<string, bool>? _isSummonerUnlockedFunc;
    private Func<string, bool>? _isCosmeticOwnedFunc;
    private Func<string, bool>? _isEmoteOwnedFunc;
    private Func<Godot.Collections.Dictionary, bool>? _grantRewardsFunc;
    private Func<string, int>? _getPurchaseCountFunc;
    private Func<string, bool>? _incrementPurchaseCountFunc;
    private Func<string, Godot.Collections.Dictionary>? _getShopRefreshStateFunc;
    private Action<string>? _initiateBillingPurchaseFunc;
    private Func<int>? _addGemsFunc;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(Initialize));
    }

    private void Initialize()
    {
        GD.Print("ShopService: Initializing...");

        _profileRepo = ProfileRepositoryBridge.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("ShopService: ProfileRepositoryBridge.Instance not available");
            return;
        }

        GD.Print("ShopService: Ready");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================================
    // CALLBACK INJECTION
    // =========================================================================

    /// <summary>Set ProfileRepo callbacks.</summary>
    public void SetProfileCallbacks(
        Callable getResources,
        Callable updateResources,
        Callable isSummonerUnlocked,
        Callable isCosmeticOwned,
        Callable isEmoteOwned,
        Callable getPurchaseCount,
        Callable incrementPurchaseCount,
        Callable getShopRefreshState)
    {
        _getResourcesFunc = () => getResources.Call().AsGodotDictionary();
        _updateResourcesFunc = (dict) => updateResources.Call(dict);
        _isSummonerUnlockedFunc = (id) => isSummonerUnlocked.Call(id).AsBool();
        _isCosmeticOwnedFunc = (id) => isCosmeticOwned.Call(id).AsBool();
        _isEmoteOwnedFunc = (id) => isEmoteOwned.Call(id).AsBool();
        _getPurchaseCountFunc = (key) => getPurchaseCount.Call(key).AsInt32();
        _incrementPurchaseCountFunc = (key) => incrementPurchaseCount.Call(key).AsBool();
        _getShopRefreshStateFunc = (shopId) => getShopRefreshState.Call(shopId).AsGodotDictionary();
    }

    /// <summary>Set RewardService callback.</summary>
    public void SetRewardCallbacks(Callable grantRewards)
    {
        _grantRewardsFunc = (rewards) => grantRewards.Call(rewards).AsBool();
    }

    /// <summary>Set PlatformBilling callback.</summary>
    public void SetBillingCallbacks(Callable initiatePurchase, Callable addGems)
    {
        _initiateBillingPurchaseFunc = (productId) => initiatePurchase.Call(productId);
        _addGemsFunc = () => addGems.Call().AsInt32();
    }

    // =========================================================================
    // SHOP CATALOG LOADING
    // =========================================================================

    /// <summary>Load shops from GDScript.</summary>
    public void LoadShopsFromGDScript(Godot.Collections.Dictionary shops)
    {
        _shops.Clear();

        foreach (var key in shops.Keys)
        {
            var shopId = key.AsString();
            if (shops[key].Obj is Godot.Collections.Dictionary shopDict)
            {
                _shops[shopId] = shopDict;
            }
        }

        GD.Print($"ShopService: Loaded {_shops.Count} shops");
    }

    /// <summary>Load purchase cache from GDScript.</summary>
    public void LoadPurchaseCache(Godot.Collections.Dictionary cache)
    {
        _purchaseCache.Clear();

        foreach (var key in cache.Keys)
        {
            var keyStr = key.AsString();
            var count = cache[key].AsInt32();
            _purchaseCache[keyStr] = count;
        }

        GD.Print($"ShopService: Loaded {_purchaseCache.Count} purchase cache entries");
    }

    // =========================================================================
    // SHOP QUERIES
    // =========================================================================

    /// <summary>Get shop definition.</summary>
    public Godot.Collections.Dictionary GetShop(string shopId)
    {
        return _shops.GetValueOrDefault(shopId, []);
    }

    /// <summary>Get all offerings for a shop as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetShopOfferingsDict(string shopId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (!_shops.TryGetValue(shopId, out var shop))
            return result;

        var offeringsVariant = shop.GetValueOrDefault("offerings", new Godot.Collections.Array());
        if (offeringsVariant.Obj is Godot.Collections.Array offerings)
        {
            foreach (var offeringVariant in offerings)
            {
                if (offeringVariant.Obj is Godot.Collections.Dictionary offering)
                {
                    result.Add(offering);
                }
            }
        }

        return result;
    }

    /// <summary>Find an offering by ID in a shop.</summary>
    public Godot.Collections.Dictionary FindOffering(string offeringId, string shopId)
    {
        if (!_shops.TryGetValue(shopId, out var shop))
            return [];

        var offeringsVariant = shop.GetValueOrDefault("offerings", new Godot.Collections.Array());
        if (offeringsVariant.Obj is Godot.Collections.Array offerings)
        {
            foreach (var offeringVariant in offerings)
            {
                if (offeringVariant.Obj is Godot.Collections.Dictionary offering)
                {
                    var id = offering.GetValueOrDefault("offering_id", "").AsString();
                    if (id == offeringId)
                        return offering;
                }
            }
        }

        return [];
    }

    // =========================================================================
    // PURCHASE LOGIC
    // =========================================================================

    /// <summary>Build purchase key with refresh epoch.</summary>
    public string BuildPurchaseKey(string shopId, string offeringId, int refreshEpoch)
    {
        return $"{shopId}::{offeringId}::{refreshEpoch}";
    }

    /// <summary>Check if an offering is already owned (for one-time purchases).</summary>
    public string CheckAlreadyOwned(Godot.Collections.Dictionary offering)
    {
        var offeringType = offering.GetValueOrDefault("offering_type", 0).AsInt32();

        // OfferingType enum values from ShopOffering.gd:
        // CARD = 0, CARD_PACK = 1, CURRENCY = 2, SPECIAL = 3, SUMMONER = 4, COSMETIC = 5, EMOTE = 6
        const int SUMMONER = 4;
        const int COSMETIC = 5;
        const int EMOTE = 6;

        switch (offeringType)
        {
            case SUMMONER:
                var summonerId = offering.GetValueOrDefault("summoner_id", "").AsString();
                if (_isSummonerUnlockedFunc?.Invoke(summonerId) == true)
                    return "Already owned";
                break;

            case COSMETIC:
                var cosmeticId = offering.GetValueOrDefault("cosmetic_id", "").AsString();
                if (_isCosmeticOwnedFunc?.Invoke(cosmeticId) == true)
                    return "Already owned";
                break;

            case EMOTE:
                var emoteId = offering.GetValueOrDefault("emote_id", "").AsString();
                if (_isEmoteOwnedFunc?.Invoke(emoteId) == true)
                    return "Already owned";
                break;
        }

        return ""; // Not owned, can purchase
    }

    /// <summary>Check if offering is owned (public API).</summary>
    public bool IsOfferingOwned(Godot.Collections.Dictionary offering)
    {
        return !string.IsNullOrEmpty(CheckAlreadyOwned(offering));
    }

    /// <summary>Validate purchase and return failure reason (empty if valid).</summary>
    public string ValidatePurchase(
        Godot.Collections.Dictionary offering,
        int playerGold,
        int playerGems,
        int purchaseCount)
    {
        // Check already owned
        var alreadyOwnedReason = CheckAlreadyOwned(offering);
        if (!string.IsNullOrEmpty(alreadyOwnedReason))
            return alreadyOwnedReason;

        var basePrice = offering.GetValueOrDefault("base_price", 0).AsInt32();
        var currencyType = offering.GetValueOrDefault("currency_type", "gold").AsString();
        var purchaseLimitType = offering.GetValueOrDefault("purchase_limit_type", "none").AsString();
        var purchaseLimit = offering.GetValueOrDefault("purchase_limit", 0).AsInt32();

        // Check purchase limit
        if (purchaseLimitType != "none" && purchaseLimit > 0)
        {
            if (purchaseCount >= purchaseLimit)
                return "Purchase limit reached";
        }

        // Check currency
        switch (currencyType)
        {
            case "gold":
                if (playerGold < basePrice)
                    return $"Not enough gold (need {basePrice}, have {playerGold})";
                break;
            case "gems":
                if (playerGems < basePrice)
                    return $"Not enough gems (need {basePrice}, have {playerGems})";
                break;
            case "real_money":
                // Real money purchases are always valid to initiate
                break;
        }

        return ""; // Valid
    }

    /// <summary>Complete a currency purchase (gold or gems).</summary>
    public bool CompleteCurrencyPurchase(
        Godot.Collections.Dictionary offering,
        string offeringId,
        string shopId,
        string purchaseKey,
        int price,
        string currency)
    {
        // Step 1: Deduct currency
        var deduction = new Godot.Collections.Dictionary { [currency] = -price };
        _updateResourcesFunc?.Invoke(deduction);

        // Step 2: Build and grant rewards
        var rewards = BuildRewardDict(offering);
        if (_grantRewardsFunc?.Invoke(rewards) != true)
        {
            // Rollback: Refund currency
            var refund = new Godot.Collections.Dictionary { [currency] = price };
            _updateResourcesFunc?.Invoke(refund);
            EmitSignal(SignalName.PurchaseFailed, offeringId, "Failed to grant rewards");
            return false;
        }

        // Step 3: Track purchase
        if (_incrementPurchaseCountFunc?.Invoke(purchaseKey) == true)
        {
            _purchaseCache[purchaseKey] = _purchaseCache.GetValueOrDefault(purchaseKey, 0) + 1;
        }
        else
        {
            GD.PushWarning("ShopService: Failed to track purchase count");
        }

        EmitSignal(SignalName.PurchaseCompleted, offeringId, shopId);
        GD.Print($"ShopService: Purchased '{offeringId}' for {price} {currency}");
        return true;
    }

    /// <summary>Build reward dictionary for RewardService.</summary>
    public Godot.Collections.Dictionary BuildRewardDict(Godot.Collections.Dictionary offering)
    {
        var rewards = new Godot.Collections.Dictionary();
        var offeringType = offering.GetValueOrDefault("offering_type", 0).AsInt32();

        // OfferingType enum values:
        // CARD = 0, CARD_PACK = 1, CURRENCY = 2, SPECIAL = 3, SUMMONER = 4, COSMETIC = 5, EMOTE = 6
        const int CARD = 0;
        const int CARD_PACK = 1;
        const int SUMMONER = 4;
        const int COSMETIC = 5;
        const int EMOTE = 6;

        switch (offeringType)
        {
            case CARD:
                var cardCatalogId = offering.GetValueOrDefault("card_catalog_id", "").AsString();
                var cardCount = offering.GetValueOrDefault("card_count", 1).AsInt32();
                var cardArray = new Godot.Collections.Array
                {
                    new Godot.Collections.Dictionary
                    {
                        ["catalog_id"] = cardCatalogId,
                        ["count"] = cardCount,
                        ["rarity"] = "common"
                    }
                };
                rewards["cards"] = cardArray;
                break;

            case CARD_PACK:
                var packCardsVariant = offering.GetValueOrDefault("pack_cards", new Godot.Collections.Array());
                if (packCardsVariant.Obj is Godot.Collections.Array packCards)
                {
                    var cardsArray = new Godot.Collections.Array();
                    foreach (var cardVariant in packCards)
                    {
                        if (cardVariant.Obj is Godot.Collections.Dictionary cardData)
                        {
                            cardsArray.Add(new Godot.Collections.Dictionary
                            {
                                ["catalog_id"] = cardData.GetValueOrDefault("catalog_id", "").AsString(),
                                ["count"] = cardData.GetValueOrDefault("count", 1).AsInt32(),
                                ["rarity"] = "common"
                            });
                        }
                    }
                    rewards["cards"] = cardsArray;
                }
                break;

            case SUMMONER:
                rewards["summoner"] = offering.GetValueOrDefault("summoner_id", "").AsString();
                break;

            case COSMETIC:
                rewards["cosmetic"] = offering.GetValueOrDefault("cosmetic_id", "").AsString();
                break;

            case EMOTE:
                rewards["emote"] = offering.GetValueOrDefault("emote_id", "").AsString();
                break;
        }

        return rewards;
    }

    /// <summary>Get purchase count from cache or profile.</summary>
    public int GetPurchaseCount(string purchaseKey)
    {
        if (_purchaseCache.TryGetValue(purchaseKey, out var count))
            return count;

        count = _getPurchaseCountFunc?.Invoke(purchaseKey) ?? 0;
        _purchaseCache[purchaseKey] = count;
        return count;
    }

    /// <summary>Get refresh epoch for a shop.</summary>
    public int GetRefreshEpoch(string shopId)
    {
        var state = _getShopRefreshStateFunc?.Invoke(shopId) ?? [];
        return state.GetValueOrDefault("refresh_epoch", 0).AsInt32();
    }

    /// <summary>Get current player gold.</summary>
    public int GetPlayerGold()
    {
        var resources = _getResourcesFunc?.Invoke() ?? [];
        return resources.GetValueOrDefault("gold", 0).AsInt32();
    }

    /// <summary>Get current player gems.</summary>
    public int GetPlayerGems()
    {
        var resources = _getResourcesFunc?.Invoke() ?? [];
        return resources.GetValueOrDefault("gems", 0).AsInt32();
    }

    /// <summary>Initiate a real-money purchase.</summary>
    public void InitiateBillingPurchase(string productId)
    {
        _initiateBillingPurchaseFunc?.Invoke(productId);
    }

    /// <summary>Handle successful billing purchase.</summary>
    public void OnBillingPurchaseCompleted(
        Godot.Collections.Dictionary offering,
        string offeringId,
        string shopId,
        string purchaseKey)
    {
        var rewards = BuildRewardDict(offering);

        if (_grantRewardsFunc?.Invoke(rewards) == true)
        {
            // Track purchase
            if (_incrementPurchaseCountFunc?.Invoke(purchaseKey) == true)
            {
                _purchaseCache[purchaseKey] = _purchaseCache.GetValueOrDefault(purchaseKey, 0) + 1;
            }

            EmitSignal(SignalName.PurchaseCompleted, offeringId, shopId);
            GD.Print($"ShopService: Real-money purchase completed for '{offeringId}'");
        }
        else
        {
            GD.PushError($"ShopService: CRITICAL - Payment completed but failed to grant rewards for '{offeringId}'");
            EmitSignal(SignalName.PurchaseFailed, offeringId, "Failed to grant rewards after payment");
        }
    }

    /// <summary>Emit purchase failed signal.</summary>
    public void EmitPurchaseFailed(string offeringId, string reason)
    {
        GD.PushWarning($"ShopService: Purchase failed for '{offeringId}': {reason}");
        EmitSignal(SignalName.PurchaseFailed, offeringId, reason);
    }
}
