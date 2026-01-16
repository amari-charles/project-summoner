using ProjectSummoner.Cards;

namespace ProjectSummoner.Data.Summoners;

/// <summary>
/// Defines a summoner's static configuration.
/// This is the template data - runtime state is stored in SummonerInstanceData.
/// </summary>
public class SummonerDefinition
{
    /// <summary>Unique identifier (e.g., "summoner_fire").</summary>
    public required string Id { get; init; }

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
    public string[] InnateTraitIds { get; init; } = [];
}
