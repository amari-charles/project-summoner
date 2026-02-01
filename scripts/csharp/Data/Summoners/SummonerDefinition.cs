using ProjectSummoner.Cards;
using ProjectSummoner.Data.Traits;

namespace ProjectSummoner.Data.Summoners;

/// <summary>
/// Defines a summoner's static configuration.
/// This is the template data - runtime state is stored in SummonerInstanceData.
/// </summary>
public class SummonerDefinition
{
    /// <summary>Unique identifier (e.g., "summoner_cole").</summary>
    public required SummonerId Id { get; init; }

    /// <summary>Localization key for display name.</summary>
    public required string NameKey { get; init; }

    /// <summary>Localization key for description.</summary>
    public required string DescriptionKey { get; init; }

    /// <summary>Elemental affinity of this summoner.</summary>
    public Element ElementalAffinity { get; init; } = Element.Neutral;

    /// <summary>Base health points.</summary>
    public float BaseHealth { get; init; } = 1000f;

    /// <summary>Maximum mana capacity.</summary>
    public float MaxMana { get; init; } = 100f;

    /// <summary>Path to summoner portrait icon.</summary>
    public string IconPath { get; init; } = "";

    /// <summary>Card frame style (common, rare, epic, legendary).</summary>
    public string CardFrameStyle { get; init; } = "legendary";

    /// <summary>How this summoner is unlocked.</summary>
    public SummonerUnlockCondition UnlockCondition { get; init; } = SummonerUnlockCondition.DevOnly;

    /// <summary>Trait IDs that this summoner has innately.</summary>
    public TraitId[] InnateTraitIds { get; init; } = [];

    /// <summary>Card catalog ID granted when this summoner is first selected.</summary>
    public CardId StarterCardId { get; init; } = CardIds.FireWisp;

    // =========================================================================
    // TRAIT ELIGIBILITY TAGS
    // =========================================================================

    /// <summary>
    /// Tags for trait eligibility matching (NOT modifier tags for amplification).
    /// These determine which traits can be acquired by this summoner at level-up.
    /// Traits with matching tags (via OR logic) become available.
    /// Use TraitTags constants for type safety.
    /// Example: [TraitTags.Summoner, TraitTags.Global, TraitTags.Fire, TraitTags.Cole]
    ///
    /// Note: This is different from StatModifier.Tags which are used for
    /// amplification targeting. See docs/features/modifier-system.md for details.
    /// </summary>
    public string[] TraitEligibilityTags { get; init; } = [Traits.TraitTags.Summoner, Traits.TraitTags.Global];
}
