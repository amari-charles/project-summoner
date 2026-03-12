using System.Collections.Generic;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Enums;
using Fateforged.Infrastructure.Billing;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Economy;
using Fateforged.Meta.Rewards;
using Fateforged.Meta.Summoner;
using Godot;
using GdArray = Godot.Collections.Array;
using GdDict = Godot.Collections.Dictionary;

namespace Fateforged.Meta.Shop;

/// <summary>
/// Shop Service - Manages shop offerings and purchases.
///
/// Handles General shop, Caravan (campaign event) shops, and Premium Store.
/// Data layer: ProfileRepo owns all persistent state.
/// Service layer: ShopService orchestrates purchases and validates.
/// All dependencies accessed directly via static Instance patterns.
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

    // Direct service references
    private IProfileRepository? _profileRepo;
    private EconomyService? _economy;
    private RewardService? _rewardService;
    private SummonerSelectionService? _summonerSelection;
    private PlatformBillingService? _platformBilling;
    private BillingCatalogService? _billingCatalog;
    private Node? _loc;

    // Typed catalog
    private Dictionary<string, ShopDefinition> _catalog = new();

    // Pending real-money purchases (productId -> context)
    private readonly Dictionary<string, PendingBillingPurchase> _pendingBillingPurchases = new();

    private sealed class PendingBillingPurchase
    {
        public required string OfferingId { get; init; }
        public required string ShopId { get; init; }
        public required string PurchaseKey { get; init; }
        public required OfferingDefinition Offering { get; init; }
    }

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        GD.Print("ShopService: Initializing...");

        _profileRepo = ProfileRepository.Instance;
        _economy = EconomyService.Instance;
        _rewardService = RewardService.Instance;
        _summonerSelection = SummonerSelectionService.Instance;
        _platformBilling = PlatformBillingService.Instance;
        _billingCatalog = BillingCatalogService.Instance;
        _loc = GetNodeOrNull("/root/Loc");

        _catalog = ShopCatalog.BuildCatalog();

        // Connect to PlatformBilling signals (deferred one frame like the old GDScript)
        CallDeferred(MethodName.ConnectBillingSignals);

        GD.Print($"ShopService: Ready ({_catalog.Count} shops loaded)");
    }

    private void ConnectBillingSignals()
    {
        ResolveBillingDependencies();

        if (_platformBilling != null)
        {
            var onCompleted = Callable.From<string, string>(OnBillingPurchaseCompleted);
            var onFailed = Callable.From<string, string>(OnBillingPurchaseFailed);
            var onCancelled = Callable.From<string>(OnBillingPurchaseCancelled);

            if (!_platformBilling.IsConnected("purchase_completed", onCompleted))
                _platformBilling.Connect("purchase_completed", onCompleted);
            if (!_platformBilling.IsConnected("purchase_failed", onFailed))
                _platformBilling.Connect("purchase_failed", onFailed);
            if (!_platformBilling.IsConnected("purchase_cancelled", onCancelled))
                _platformBilling.Connect("purchase_cancelled", onCancelled);
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================================
    // SHOP QUERIES (GDScript-callable API)
    // =========================================================================

    /// <summary>
    /// Get all offerings for a shop as an array of dictionaries.
    /// For caravan shops, filters out already-purchased offerings.
    /// </summary>
    public GdArray GetShopOfferings(string shopId)
    {
        var result = new GdArray();

        if (!_catalog.TryGetValue(shopId, out var shop))
            return result;

        // For caravan shops, get purchased IDs to filter
        HashSet<string>? purchasedIds = null;
        if (shop.ShopType == ShopType.Caravan)
        {
            purchasedIds = [];
            var summonerId = new SummonerId(_summonerSelection?.GetActiveSummonerId() ?? "");
            var purchased = _profileRepo?.GetCaravanPurchases(summonerId);
            if (purchased != null)
            {
                foreach (var id in purchased)
                    purchasedIds.Add(id);
            }
        }

        foreach (var offering in shop.Offerings)
        {
            // Skip purchased caravan offerings
            if (purchasedIds != null && purchasedIds.Contains(offering.OfferingId))
                continue;

            result.Add(OfferingToDict(offering));
        }

        return result;
    }

    /// <summary>
    /// Check if an offering is already owned (for one-time purchase types).
    /// </summary>
    public bool IsOfferingOwned(string offeringId, string shopId)
    {
        var offering = FindOfferingDef(offeringId, shopId);
        if (offering == null)
            return false;

        return !string.IsNullOrEmpty(CheckAlreadyOwned(offering));
    }

    /// <summary>
    /// Check if a purchase can proceed, returning affordability and reason.
    /// </summary>
    public GdDict CanPurchaseOffering(string offeringId, string shopId)
    {
        var offering = FindOfferingDef(offeringId, shopId);
        if (offering == null)
            return new GdDict
            {
                ["can_purchase"] = false,
                ["reason"] = "Offering not found",
                ["price"] = 0,
            };

        // Check already owned
        var ownedReason = CheckAlreadyOwned(offering);
        if (!string.IsNullOrEmpty(ownedReason))
            return new GdDict
            {
                ["can_purchase"] = false,
                ["reason"] = ownedReason,
                ["price"] = offering.BasePrice,
            };

        // Check purchase limit
        var refreshEpoch = GetRefreshEpoch(shopId);
        var purchaseKey = BuildPurchaseKey(shopId, offeringId, refreshEpoch);
        var purchaseCount = _profileRepo?.GetPurchaseCount(purchaseKey) ?? 0;

        if (offering.PurchaseLimitType != PurchaseLimitType.None && offering.PurchaseLimit > 0)
        {
            if (purchaseCount >= offering.PurchaseLimit)
                return new GdDict
                {
                    ["can_purchase"] = false,
                    ["reason"] = "Purchase limit reached",
                    ["price"] = offering.BasePrice,
                };
        }

        // Check currency
        var isCaravan = shopId.StartsWith("caravan");
        var price = offering.BasePrice;

        switch (offering.CurrencyType)
        {
            case CurrencyType.Gold:
                var gold = isCaravan
                    ? (_economy?.GetCampaignGold() ?? 0)
                    : (_economy?.GetGold() ?? 0);
                if (gold < price)
                    return new GdDict
                    {
                        ["can_purchase"] = false,
                        ["reason"] = $"Not enough gold (need {price}, have {gold})",
                        ["price"] = price,
                    };
                break;
            case CurrencyType.Gems:
                var gems = _economy?.GetGems() ?? 0;
                if (gems < price)
                    return new GdDict
                    {
                        ["can_purchase"] = false,
                        ["reason"] = $"Not enough gems (need {price}, have {gems})",
                        ["price"] = price,
                    };
                break;
            case CurrencyType.RealMoney:
                break; // Always valid to initiate
        }

        return new GdDict
        {
            ["can_purchase"] = true,
            ["reason"] = "",
            ["price"] = price,
        };
    }

    // =========================================================================
    // PURCHASE FLOW (GDScript-callable API)
    // =========================================================================

    /// <summary>
    /// Purchase an offering. Returns true if purchase initiated/completed.
    /// Emits PurchaseCompleted or PurchaseFailed.
    /// </summary>
    public bool PurchaseOffering(string offeringId, string shopId)
    {
        var offering = FindOfferingDef(offeringId, shopId);
        if (offering == null)
        {
            EmitPurchaseFailed(offeringId, "Offering not found");
            return false;
        }

        // Validate ownership
        var ownedReason = CheckAlreadyOwned(offering);
        if (!string.IsNullOrEmpty(ownedReason))
        {
            EmitPurchaseFailed(offeringId, ownedReason);
            return false;
        }

        // Get refresh state and purchase count
        var refreshEpoch = GetRefreshEpoch(shopId);
        var purchaseKey = BuildPurchaseKey(shopId, offeringId, refreshEpoch);
        var purchaseCount = _profileRepo?.GetPurchaseCount(purchaseKey) ?? 0;

        // Determine currency amounts
        var isCaravan = shopId.StartsWith("caravan");
        var playerGold = isCaravan
            ? (_economy?.GetCampaignGold() ?? 0)
            : (_economy?.GetGold() ?? 0);
        var playerGems = _economy?.GetGems() ?? 0;

        // Validate purchase limits and currency
        var failureReason = ValidatePurchase(offering, playerGold, playerGems, purchaseCount);
        if (!string.IsNullOrEmpty(failureReason))
        {
            EmitPurchaseFailed(offeringId, failureReason);
            return false;
        }

        var price = offering.BasePrice;

        switch (offering.CurrencyType)
        {
            case CurrencyType.Gold:
                if (isCaravan)
                    return CompleteCaravanPurchase(
                        offering,
                        offeringId,
                        shopId,
                        purchaseKey,
                        price
                    );
                else
                    return CompleteCurrencyPurchase(
                        offering,
                        offeringId,
                        shopId,
                        purchaseKey,
                        price,
                        CurrencyType.Gold
                    );

            case CurrencyType.Gems:
                return CompleteCurrencyPurchase(
                    offering,
                    offeringId,
                    shopId,
                    purchaseKey,
                    price,
                    CurrencyType.Gems
                );

            case CurrencyType.RealMoney:
                ResolveBillingDependencies();
                var productId = offering.ProductId ?? offeringId;
                _pendingBillingPurchases[productId] = new PendingBillingPurchase
                {
                    OfferingId = offeringId,
                    ShopId = shopId,
                    PurchaseKey = purchaseKey,
                    Offering = offering,
                };
                _platformBilling?.purchase(productId);
                GD.Print($"ShopService: Initiated real-money purchase for '{offeringId}'");
                return true; // Async — result via billing signals

            default:
                EmitPurchaseFailed(offeringId, $"Unknown currency type: {offering.CurrencyType}");
                return false;
        }
    }

    // =========================================================================
    // PURCHASE IMPLEMENTATIONS
    // =========================================================================

    private bool CompleteCaravanPurchase(
        OfferingDefinition offering,
        string offeringId,
        string shopId,
        string purchaseKey,
        int price
    )
    {
        // Spend campaign gold
        if (_economy?.SpendCampaignGold(price) != true)
        {
            EmitPurchaseFailed(offeringId, $"Cannot afford {price} campaign gold");
            return false;
        }

        // Grant rewards
        var rewards = BuildRewardDict(offering, shopId);
        if (_rewardService?.GrantRewards(rewards) != true)
        {
            // Rollback
            _economy?.AddCampaignGold(price);
            EmitPurchaseFailed(offeringId, "Failed to grant rewards");
            return false;
        }

        // Record purchase
        _profileRepo?.IncrementPurchaseCount(purchaseKey);
        _profileRepo?.AddCaravanPurchase(
            offeringId,
            new SummonerId(_summonerSelection?.GetActiveSummonerId() ?? "")
        );

        EmitSignal(SignalName.PurchaseCompleted, offeringId, shopId);
        GD.Print(
            $"ShopService: Completed caravan purchase '{offeringId}' for {price} campaign gold"
        );
        return true;
    }

    private bool CompleteCurrencyPurchase(
        OfferingDefinition offering,
        string offeringId,
        string shopId,
        string purchaseKey,
        int price,
        CurrencyType currency
    )
    {
        // Map shop CurrencyType to profile ResourceType
        var resourceType = currency switch
        {
            CurrencyType.Gold => ResourceType.Gold,
            CurrencyType.Gems => ResourceType.Gems,
            _ => ResourceType.Gold,
        };

        // Deduct currency
        _profileRepo?.UpdateResources(
            new Dictionary<ResourceType, int> { [resourceType] = -price }
        );

        // Grant rewards
        var rewards = BuildRewardDict(offering, shopId);
        if (_rewardService?.GrantRewards(rewards) != true)
        {
            // Rollback
            _profileRepo?.UpdateResources(
                new Dictionary<ResourceType, int> { [resourceType] = price }
            );
            EmitPurchaseFailed(offeringId, "Failed to grant rewards");
            return false;
        }

        // Track purchase
        _profileRepo?.IncrementPurchaseCount(purchaseKey);

        EmitSignal(SignalName.PurchaseCompleted, offeringId, shopId);
        GD.Print($"ShopService: Purchased '{offeringId}' for {price} {currency.ToStringValue()}");
        return true;
    }

    // =========================================================================
    // REWARD BUILDING
    // =========================================================================

    private GdDict BuildRewardDict(OfferingDefinition offering, string shopId)
    {
        var rewards = new GdDict();
        var isCaravan =
            _catalog.TryGetValue(shopId, out var shop) && shop.ShopType == ShopType.Caravan;
        var activeSummonerId = isCaravan ? (_summonerSelection?.GetActiveSummonerId() ?? "") : null;

        switch (offering.OfferingType)
        {
            case OfferingType.Card:
                var cardDict = new GdDict
                {
                    ["catalog_id"] = offering.CardCatalogId ?? "",
                    ["count"] = offering.CardCount,
                    ["rarity"] = "common",
                };
                AddBindingToCardDict(cardDict, isCaravan, activeSummonerId);
                rewards["cards"] = new GdArray { cardDict };
                break;

            case OfferingType.CardPack:
                if (offering.PackCards != null)
                {
                    var cardsArray = new GdArray();
                    foreach (var entry in offering.PackCards)
                    {
                        var packCardDict = new GdDict
                        {
                            ["catalog_id"] = entry.CatalogId,
                            ["count"] = entry.Count,
                            ["rarity"] = "common",
                        };
                        AddBindingToCardDict(packCardDict, isCaravan, activeSummonerId);
                        cardsArray.Add(packCardDict);
                    }
                    rewards["cards"] = cardsArray;
                }
                break;

            case OfferingType.Summoner:
                rewards["summoner"] = offering.SummonerId ?? "";
                break;

            case OfferingType.Cosmetic:
                rewards["cosmetic"] = offering.CosmeticId ?? "";
                break;

            case OfferingType.Emote:
                rewards["emote"] = offering.EmoteId ?? "";
                break;
        }

        return rewards;
    }

    private static void AddBindingToCardDict(
        GdDict cardDict,
        bool isCaravanShop,
        string? summonerId
    )
    {
        if (isCaravanShop && !string.IsNullOrEmpty(summonerId))
        {
            cardDict["binding"] = (int)ContentBinding.SummonerBound;
            cardDict["bound_to"] = summonerId;
        }
        else
        {
            cardDict["binding"] = (int)ContentBinding.AccountWide;
        }
    }

    // =========================================================================
    // VALIDATION HELPERS
    // =========================================================================

    private string CheckAlreadyOwned(OfferingDefinition offering)
    {
        switch (offering.OfferingType)
        {
            case OfferingType.Summoner:
                if (
                    !string.IsNullOrEmpty(offering.SummonerId)
                    && _summonerSelection?.IsSummonerUnlocked(offering.SummonerId) == true
                )
                    return "Already owned";
                break;

            case OfferingType.Cosmetic:
                if (
                    !string.IsNullOrEmpty(offering.CosmeticId)
                    && _profileRepo?.IsCosmeticOwned(new CosmeticId(offering.CosmeticId)) == true
                )
                    return "Already owned";
                break;

            case OfferingType.Emote:
                if (
                    !string.IsNullOrEmpty(offering.EmoteId)
                    && _profileRepo?.IsEmoteOwned(new EmoteId(offering.EmoteId)) == true
                )
                    return "Already owned";
                break;
        }

        return "";
    }

    private static string ValidatePurchase(
        OfferingDefinition offering,
        int playerGold,
        int playerGems,
        int purchaseCount
    )
    {
        // Check purchase limit
        if (offering.PurchaseLimitType != PurchaseLimitType.None && offering.PurchaseLimit > 0)
        {
            if (purchaseCount >= offering.PurchaseLimit)
                return "Purchase limit reached";
        }

        var price = offering.BasePrice;
        switch (offering.CurrencyType)
        {
            case CurrencyType.Gold:
                if (playerGold < price)
                    return $"Not enough gold (need {price}, have {playerGold})";
                break;
            case CurrencyType.Gems:
                if (playerGems < price)
                    return $"Not enough gems (need {price}, have {playerGems})";
                break;
            case CurrencyType.RealMoney:
                break;
        }

        return "";
    }

    private OfferingDefinition? FindOfferingDef(string offeringId, string shopId)
    {
        if (!_catalog.TryGetValue(shopId, out var shop))
            return null;

        foreach (var offering in shop.Offerings)
        {
            if (offering.OfferingId == offeringId)
                return offering;
        }

        return null;
    }

    private string BuildPurchaseKey(string shopId, string offeringId, int refreshEpoch)
    {
        return $"{shopId}::{offeringId}::{refreshEpoch}";
    }

    private int GetRefreshEpoch(string shopId)
    {
        var state = _profileRepo?.GetShopRefreshState(new ShopId(shopId));
        return state?.RefreshEpoch ?? 0;
    }

    // =========================================================================
    // PLATFORM BILLING HANDLERS
    // =========================================================================

    private void OnBillingPurchaseCompleted(string productId, string transactionId)
    {
        ResolveBillingDependencies();
        GD.Print(
            $"ShopService: Billing purchase completed - product: {productId}, txn: {transactionId}"
        );

        if (_pendingBillingPurchases.Remove(productId, out var pending))
        {
            // Shop offering purchase
            var rewards = BuildRewardDict(pending.Offering, pending.ShopId);
            if (_rewardService?.GrantRewards(rewards) == true)
            {
                _profileRepo?.IncrementPurchaseCount(pending.PurchaseKey);
                EmitSignal(SignalName.PurchaseCompleted, pending.OfferingId, pending.ShopId);
                GD.Print($"ShopService: Real-money purchase completed for '{pending.OfferingId}'");
            }
            else
            {
                GD.PushError(
                    $"ShopService: CRITICAL - Payment completed but failed to grant rewards for '{pending.OfferingId}'"
                );
                EmitSignal(
                    SignalName.PurchaseFailed,
                    pending.OfferingId,
                    "Failed to grant rewards after payment"
                );
            }
        }
        else
        {
            // Direct billing product (gem pack, not a shop offering)
            if (_billingCatalog != null)
            {
                var product = _billingCatalog.get_product(productId);
                if (product != null)
                {
                    var gemsAmount = product.gems_amount;
                    if (gemsAmount > 0)
                    {
                        _economy?.AddGems(gemsAmount);
                        GD.Print($"ShopService: Granted {gemsAmount} gems from billing purchase");
                    }

                    var rewardsDict = product.rewards;
                    if (rewardsDict.Count > 0)
                    {
                        _rewardService?.GrantRewards(rewardsDict);
                        GD.Print("ShopService: Granted direct rewards from billing purchase");
                    }
                }
                else
                {
                    GD.PushWarning($"ShopService: Unknown billing product: {productId}");
                }
            }
        }
    }

    private void OnBillingPurchaseFailed(string productId, string error)
    {
        GD.Print($"ShopService: Billing purchase failed - product: {productId}, error: {error}");

        if (_pendingBillingPurchases.Remove(productId, out var pending))
        {
            EmitPurchaseFailed(pending.OfferingId, $"Payment failed: {error}");
        }
    }

    private void OnBillingPurchaseCancelled(string productId)
    {
        GD.Print($"ShopService: Billing purchase cancelled - product: {productId}");

        if (_pendingBillingPurchases.Remove(productId, out var pending))
        {
            EmitPurchaseFailed(pending.OfferingId, "Purchase cancelled");
        }
    }

    private void ResolveBillingDependencies()
    {
        _platformBilling ??= PlatformBillingService.Instance;
        _billingCatalog ??= BillingCatalogService.Instance;
    }

    // =========================================================================
    // DICTIONARY CONVERSION
    // =========================================================================

    private GdDict OfferingToDict(OfferingDefinition def)
    {
        var dict = new GdDict
        {
            ["offering_id"] = def.OfferingId,
            ["offering_type"] = (int)def.OfferingType,
            ["offering_type_name"] = def.OfferingType switch
            {
                OfferingType.Card => "card",
                OfferingType.CardPack => "card_pack",
                OfferingType.Currency => "currency",
                OfferingType.Special => "special",
                OfferingType.Summoner => "summoner",
                OfferingType.Cosmetic => "cosmetic",
                OfferingType.Emote => "emote",
                _ => "card",
            },
            ["display_name"] = ResolveLoc(def.NameKey),
            ["description"] = ResolveLoc(def.DescriptionKey),
            ["base_price"] = def.BasePrice,
            ["currency_type"] = def.CurrencyType.ToStringValue(),
            ["purchase_limit_type"] = def.PurchaseLimitType.ToStringValue(),
            ["purchase_limit"] = def.PurchaseLimit,
        };

        // Type-specific fields
        if (def.CardCatalogId != null)
            dict["card_catalog_id"] = def.CardCatalogId;

        dict["card_count"] = def.CardCount;

        if (def.PackCards != null)
        {
            var packArray = new GdArray();
            foreach (var entry in def.PackCards)
                packArray.Add(
                    new GdDict { ["catalog_id"] = entry.CatalogId, ["count"] = entry.Count }
                );
            dict["pack_cards"] = packArray;
        }

        if (def.SummonerId != null)
            dict["summoner_id"] = def.SummonerId;

        if (def.CosmeticType != null)
            dict["cosmetic_type"] = def.CosmeticType;

        if (def.CosmeticId != null)
            dict["cosmetic_id"] = def.CosmeticId;

        if (def.EmoteId != null)
            dict["emote_id"] = def.EmoteId;

        return dict;
    }

    private string ResolveLoc(string key)
    {
        if (_loc != null)
            return _loc.Call("t", key).AsString();
        return key; // Fallback to key if Loc not available
    }

    private void EmitPurchaseFailed(string offeringId, string reason)
    {
        GD.PushWarning($"ShopService: Purchase failed for '{offeringId}': {reason}");
        EmitSignal(SignalName.PurchaseFailed, offeringId, reason);
    }
}
