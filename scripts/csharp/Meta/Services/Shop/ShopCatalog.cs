using System.Collections.Generic;

namespace Fateforged.Meta.Shop;

/// <summary>
/// A single card entry inside a card pack offering.
/// </summary>
public sealed class PackCardEntry
{
    public required string CatalogId { get; init; }
    public int Count { get; init; } = 1;
}

/// <summary>
/// Typed definition for a shop offering.
/// Localization keys are stored (not resolved strings) so the caller
/// resolves them at display time via Loc.t().
/// </summary>
public sealed class OfferingDefinition
{
    public required string OfferingId { get; init; }
    public OfferingType OfferingType { get; init; }
    public required string NameKey { get; init; }
    public required string DescriptionKey { get; init; }
    public int BasePrice { get; init; }
    public CurrencyType CurrencyType { get; init; } = CurrencyType.Gold;
    public PurchaseLimitType PurchaseLimitType { get; init; } = PurchaseLimitType.None;
    public int PurchaseLimit { get; init; }

    // Card-specific
    public string? CardCatalogId { get; init; }
    public int CardCount { get; init; } = 1;

    // Card pack-specific
    public List<PackCardEntry>? PackCards { get; init; }

    // Summoner-specific
    public string? SummonerId { get; init; }

    // Cosmetic-specific
    public string? CosmeticType { get; init; }
    public string? CosmeticId { get; init; }

    // Emote-specific
    public string? EmoteId { get; init; }

    // Real-money billing
    public string? ProductId { get; init; }
}

/// <summary>
/// Typed definition for a shop (collection of offerings).
/// </summary>
public sealed class ShopDefinition
{
    public required string Id { get; init; }
    public ShopType ShopType { get; init; }
    public required string NameKey { get; init; }
    public required List<OfferingDefinition> Offerings { get; init; }
}

/// <summary>
/// Static catalog of all shop definitions.
/// Typed catalog of all shop definitions.
/// </summary>
public static class ShopCatalog
{
    // Card catalog ID constants (mirrors GDScript CardIDs)
    private const string FireWisp = "fire_wisp";
    private const string Pebbloom = "pebbloom";
    private const string Puff = "puff";
    private const string ManaBolt = "mana_bolt";

    public static Dictionary<string, ShopDefinition> BuildCatalog()
    {
        return new Dictionary<string, ShopDefinition>
        {
            ["general"] = BuildGeneralShop(),
            ["caravan_tutorial"] = BuildCaravanTutorialShop(),
            ["premium_store"] = BuildPremiumStore(),
        };
    }

    private static ShopDefinition BuildGeneralShop() =>
        new()
        {
            Id = "general",
            ShopType = ShopType.General,
            NameKey = "shop.general.name",
            Offerings =
            [
                new OfferingDefinition
                {
                    OfferingId = "general_fire_wisp",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.fire_wisp.name",
                    DescriptionKey = "shop.offering.fire_wisp.description",
                    CardCatalogId = FireWisp,
                    CardCount = 1,
                    BasePrice = 30,
                },
                new OfferingDefinition
                {
                    OfferingId = "general_pebbloom",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.pebbloom.name",
                    DescriptionKey = "shop.offering.pebbloom.description",
                    CardCatalogId = Pebbloom,
                    CardCount = 1,
                    BasePrice = 20,
                },
                new OfferingDefinition
                {
                    OfferingId = "general_puff",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.puff.name",
                    DescriptionKey = "shop.offering.puff.description",
                    CardCatalogId = Puff,
                    CardCount = 1,
                    BasePrice = 35,
                },
                new OfferingDefinition
                {
                    OfferingId = "general_basic_spell_pack",
                    OfferingType = OfferingType.CardPack,
                    NameKey = "shop.offering.basic_spell_pack.name",
                    DescriptionKey = "shop.offering.basic_spell_pack.description",
                    PackCards = [new PackCardEntry { CatalogId = ManaBolt, Count = 2 }],
                    BasePrice = 50,
                },
                new OfferingDefinition
                {
                    OfferingId = "general_summon_pack",
                    OfferingType = OfferingType.CardPack,
                    NameKey = "shop.offering.summon_pack.name",
                    DescriptionKey = "shop.offering.summon_pack.description",
                    PackCards =
                    [
                        new PackCardEntry { CatalogId = Pebbloom, Count = 2 },
                        new PackCardEntry { CatalogId = FireWisp, Count = 1 },
                    ],
                    BasePrice = 70,
                },
            ],
        };

    private static ShopDefinition BuildCaravanTutorialShop() =>
        new()
        {
            Id = "caravan_tutorial",
            ShopType = ShopType.Caravan,
            NameKey = "shop.caravan.merriweather.name",
            Offerings =
            [
                new OfferingDefinition
                {
                    OfferingId = "caravan_fire_wisp",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.fire_wisp.name",
                    DescriptionKey = "shop.offering.fire_wisp.description",
                    CardCatalogId = FireWisp,
                    CardCount = 1,
                    BasePrice = 25,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "caravan_pebbloom",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.pebbloom.name",
                    DescriptionKey = "shop.offering.pebbloom.description",
                    CardCatalogId = Pebbloom,
                    CardCount = 1,
                    BasePrice = 20,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "caravan_puff",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.puff.name",
                    DescriptionKey = "shop.offering.puff.description",
                    CardCatalogId = Puff,
                    CardCount = 1,
                    BasePrice = 30,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "caravan_mana_bolt",
                    OfferingType = OfferingType.Card,
                    NameKey = "shop.offering.mana_bolt.name",
                    DescriptionKey = "shop.offering.mana_bolt.description",
                    CardCatalogId = ManaBolt,
                    CardCount = 1,
                    BasePrice = 35,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
            ],
        };

    private static ShopDefinition BuildPremiumStore() =>
        new()
        {
            Id = "premium_store",
            ShopType = ShopType.Premium,
            NameKey = "shop.premium.name",
            Offerings =
            [
                // Summoners
                new OfferingDefinition
                {
                    OfferingId = "summoner_lightning_adept",
                    OfferingType = OfferingType.Summoner,
                    NameKey = "shop.offering.lightning_adept.name",
                    DescriptionKey = "shop.offering.lightning_adept.description",
                    SummonerId = "summoner_lightning_adept",
                    BasePrice = 750,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "summoner_verdant_sage",
                    OfferingType = OfferingType.Summoner,
                    NameKey = "shop.offering.verdant_sage.name",
                    DescriptionKey = "shop.offering.verdant_sage.description",
                    SummonerId = "summoner_verdant_sage",
                    BasePrice = 750,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "summoner_void_walker",
                    OfferingType = OfferingType.Summoner,
                    NameKey = "shop.offering.void_walker.name",
                    DescriptionKey = "shop.offering.void_walker.description",
                    SummonerId = "summoner_void_walker",
                    BasePrice = 750,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                // Cosmetics
                new OfferingDefinition
                {
                    OfferingId = "cosmetic_card_back_placeholder_1",
                    OfferingType = OfferingType.Cosmetic,
                    NameKey = "shop.offering.placeholder_card_back_1.name",
                    DescriptionKey = "shop.offering.placeholder_card_back_1.description",
                    CosmeticType = "card_back",
                    CosmeticId = "card_back_placeholder_1",
                    BasePrice = 200,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "cosmetic_ui_theme_placeholder_1",
                    OfferingType = OfferingType.Cosmetic,
                    NameKey = "shop.offering.placeholder_ui_theme_1.name",
                    DescriptionKey = "shop.offering.placeholder_ui_theme_1.description",
                    CosmeticType = "ui_theme",
                    CosmeticId = "ui_theme_placeholder_1",
                    BasePrice = 500,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                // Emotes
                new OfferingDefinition
                {
                    OfferingId = "emote_laugh",
                    OfferingType = OfferingType.Emote,
                    NameKey = "shop.offering.emote_laugh.name",
                    DescriptionKey = "shop.offering.emote_laugh.description",
                    EmoteId = "emote_laugh",
                    BasePrice = 150,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "emote_shocked",
                    OfferingType = OfferingType.Emote,
                    NameKey = "shop.offering.emote_shocked.name",
                    DescriptionKey = "shop.offering.emote_shocked.description",
                    EmoteId = "emote_shocked",
                    BasePrice = 150,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "emote_thinking",
                    OfferingType = OfferingType.Emote,
                    NameKey = "shop.offering.emote_thinking.name",
                    DescriptionKey = "shop.offering.emote_thinking.description",
                    EmoteId = "emote_thinking",
                    BasePrice = 200,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "emote_taunt",
                    OfferingType = OfferingType.Emote,
                    NameKey = "shop.offering.emote_taunt.name",
                    DescriptionKey = "shop.offering.emote_taunt.description",
                    EmoteId = "emote_taunt",
                    BasePrice = 250,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "emote_confident",
                    OfferingType = OfferingType.Emote,
                    NameKey = "shop.offering.emote_confident.name",
                    DescriptionKey = "shop.offering.emote_confident.description",
                    EmoteId = "emote_confident",
                    BasePrice = 300,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
                new OfferingDefinition
                {
                    OfferingId = "emote_victory",
                    OfferingType = OfferingType.Emote,
                    NameKey = "shop.offering.emote_victory.name",
                    DescriptionKey = "shop.offering.emote_victory.description",
                    EmoteId = "emote_victory",
                    BasePrice = 350,
                    CurrencyType = CurrencyType.Gems,
                    PurchaseLimitType = PurchaseLimitType.Account,
                    PurchaseLimit = 1,
                },
            ],
        };
}
