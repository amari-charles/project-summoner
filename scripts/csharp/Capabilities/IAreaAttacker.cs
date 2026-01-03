using Godot;

namespace ProjectSummoner.Capabilities;

/// <summary>
/// Interface for units that deal area-of-effect damage on attacks.
/// </summary>
public interface IAreaAttacker
{
    /// <summary>Radius of the AOE damage.</summary>
    float AoeRadius { get; }

    /// <summary>Damage multiplier for AOE (often 1.0 for full damage).</summary>
    float AoeDamageMultiplier { get; }

    /// <summary>
    /// Apply area damage centered at a position.
    /// </summary>
    /// <param name="center">Center point of the AOE.</param>
    /// <param name="baseDamage">Base damage to apply.</param>
    void ApplyAreaDamage(Vector3 center, float baseDamage);
}
