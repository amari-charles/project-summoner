namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Constants for modifier condition keys.
/// Use these instead of hardcoded strings in Conditions dictionaries.
/// </summary>
public static class ConditionKeys
{
    /// <summary>
    /// Elemental affinity of the unit being modified.
    /// Value: element name string (e.g., "fire", "water").
    /// </summary>
    public const string ElementalAffinity = "elemental_affinity";

    /// <summary>
    /// Creature type of the unit being modified.
    /// Value: creature type string (e.g., "beast", "elemental").
    /// </summary>
    public const string CreatureType = "creature_type";

    /// <summary>
    /// Unit role tag for filtering.
    /// Value: role string (e.g., "tank", "swarm").
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// Card rarity for conditional effects.
    /// Value: rarity string (e.g., "common", "rare", "epic").
    /// </summary>
    public const string Rarity = "rarity";

    /// <summary>
    /// Whether the unit is ranged.
    /// Value: boolean.
    /// </summary>
    public const string IsRanged = "is_ranged";

    /// <summary>
    /// Specific card ID for card-specific effects.
    /// Value: card ID string.
    /// </summary>
    public const string CardId = "card_id";

    /// <summary>
    /// Specific unit ID for unit-specific effects.
    /// Value: unit ID string.
    /// </summary>
    public const string UnitId = "unit_id";
}
