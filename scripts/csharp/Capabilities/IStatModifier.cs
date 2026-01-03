using Godot.Collections;

namespace ProjectSummoner.Capabilities;

/// <summary>
/// Interface for stat modifiers that can be applied to units.
/// Modifiers affect stats through additive and multiplicative bonuses.
/// </summary>
public interface IStatModifier
{
    /// <summary>
    /// Additive stat bonuses (applied first).
    /// Key: stat name (e.g., "max_hp", "attack_damage")
    /// Value: amount to add
    /// </summary>
    Dictionary<string, float> StatAdds { get; }

    /// <summary>
    /// Multiplicative stat bonuses (applied after adds).
    /// Key: stat name
    /// Value: multiplier (1.0 = no change, 1.5 = +50%)
    /// </summary>
    Dictionary<string, float> StatMults { get; }

    /// <summary>
    /// Boolean flags for special behaviors.
    /// Key: flag name (e.g., "immune_to_slow", "deals_fire_damage")
    /// Value: flag state
    /// </summary>
    Dictionary<string, bool> Flags { get; }
}
