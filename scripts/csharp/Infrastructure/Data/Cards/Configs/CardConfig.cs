using Godot;

namespace Fateforged.Cards.Configs;

/// <summary>
/// Base configuration for all card types.
/// Contains identity and gameplay properties shared by all cards.
/// Note: Not marked [GlobalClass] to avoid conflict with GDScript CardConfig class.
/// </summary>
public partial class CardConfig : Resource
{
    // =========================================================================
    // IDENTITY
    // =========================================================================

    /// <summary>
    /// Unique identifier in the card catalog.
    /// </summary>
    [Export]
    public string CatalogId { get; set; } = "";

    /// <summary>
    /// Display name of the card.
    /// </summary>
    [Export]
    public string CardName { get; set; } = "Unknown Card";

    /// <summary>
    /// Type of card (Summon or Spell).
    /// </summary>
    [Export]
    public CardType CardType { get; set; } = CardType.Summon;

    /// <summary>
    /// Card description for UI display.
    /// </summary>
    [Export]
    public string Description { get; set; } = "";

    /// <summary>
    /// Card rarity (common, rare, epic, legendary).
    /// </summary>
    [Export]
    public string Rarity { get; set; } = "common";

    // =========================================================================
    // GAMEPLAY
    // =========================================================================

    /// <summary>
    /// Mana cost to play this card.
    /// </summary>
    [Export]
    public int ManaCost { get; set; } = 1;

    /// <summary>
    /// Cooldown after playing (seconds before another card can be played).
    /// </summary>
    [Export]
    public float Cooldown { get; set; } = 2.0f;

    // =========================================================================
    // VISUAL
    // =========================================================================

    /// <summary>
    /// Path to card icon texture for UI display.
    /// </summary>
    [Export]
    public string CardIconPath { get; set; } = "";

    /// <summary>
    /// Card icon for UI display (loaded from CardIconPath).
    /// </summary>
    [Export]
    public Texture2D? CardIcon { get; set; }

    // =========================================================================
    // METADATA
    // =========================================================================

    /// <summary>
    /// Tags for filtering and categorization (e.g., ["melee", "fire", "spirit"]).
    /// </summary>
    [Export]
    public Godot.Collections.Array<string> Tags { get; set; } = new();

    /// <summary>
    /// Unlock condition for progression system (e.g., "default", "campaign_1", "dev_only").
    /// </summary>
    [Export]
    public string UnlockCondition { get; set; } = "default";

    /// <summary>
    /// Elemental affinity ID (e.g., "fire", "earth", "water", "air").
    /// Used by modifier system for elemental interactions.
    /// </summary>
    [Export]
    public string ElementalAffinity { get; set; } = "";

    // =========================================================================
    // CONVERSION
    // =========================================================================

    /// <summary>
    /// Create a CardConfig from a GDScript dictionary.
    /// </summary>
    public static CardConfig FromDictionary(Godot.Collections.Dictionary dict)
    {
        var config = new CardConfig();
        PopulateFromDictionary(config, dict);
        return config;
    }

    /// <summary>
    /// Populate base config fields from dictionary (used by subclasses).
    /// </summary>
    protected static void PopulateFromDictionary(
        CardConfig config,
        Godot.Collections.Dictionary dict
    )
    {
        if (dict.TryGetValue("catalog_id", out var catalogId))
            config.CatalogId = catalogId.AsString();

        if (dict.TryGetValue("card_name", out var cardName))
            config.CardName = cardName.AsString();

        if (dict.TryGetValue("card_type", out var cardType))
            config.CardType = cardType.AsInt32() == 1 ? CardType.Spell : CardType.Summon;

        if (dict.TryGetValue("description", out var desc))
            config.Description = desc.AsString();

        if (dict.TryGetValue("rarity", out var rarity))
            config.Rarity = rarity.AsString();

        if (dict.TryGetValue("mana_cost", out var manaCost))
            config.ManaCost = manaCost.AsInt32();

        if (dict.TryGetValue("cooldown", out var cooldown))
            config.Cooldown = cooldown.AsSingle();

        if (dict.TryGetValue("card_icon_path", out var iconPath))
            config.CardIconPath = iconPath.AsString();

        if (dict.TryGetValue("tags", out var tags) && tags.VariantType == Variant.Type.Array)
        {
            config.Tags = new Godot.Collections.Array<string>();
            foreach (var tag in tags.AsGodotArray())
            {
                if (tag.VariantType == Variant.Type.String)
                    config.Tags.Add(tag.AsString());
            }
        }

        if (dict.TryGetValue("unlock_condition", out var unlockCond))
            config.UnlockCondition = unlockCond.AsString();

        // Extract elemental affinity from categories
        if (
            dict.TryGetValue("categories", out var categories)
            && categories.VariantType == Variant.Type.Dictionary
        )
        {
            var catDict = categories.AsGodotDictionary();
            if (catDict.TryGetValue("elemental_affinity", out var affinity))
            {
                // Handle both Resource objects (ElementTypes) and string IDs
                if (
                    affinity.VariantType == Variant.Type.Object
                    && affinity.Obj is Resource resource
                )
                {
                    // ElementTypes has an "id" property
                    config.ElementalAffinity = (string)resource.Get("id");
                }
                else if (affinity.VariantType == Variant.Type.String)
                {
                    config.ElementalAffinity = affinity.AsString();
                }
            }
        }
    }

    /// <summary>
    /// Convert to GDScript-compatible dictionary.
    /// </summary>
    public virtual Godot.Collections.Dictionary ToDictionary()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["catalog_id"] = CatalogId,
            ["card_name"] = CardName,
            ["card_type"] = (int)CardType,
            ["description"] = Description,
            ["rarity"] = Rarity,
            ["mana_cost"] = ManaCost,
            ["cooldown"] = Cooldown,
            ["card_icon_path"] = CardIconPath,
            ["tags"] = Tags,
            ["unlock_condition"] = UnlockCondition,
        };

        if (!string.IsNullOrEmpty(ElementalAffinity))
        {
            dict["categories"] = new Godot.Collections.Dictionary
            {
                ["elemental_affinity"] = ElementalAffinity,
            };
        }

        return dict;
    }
}
