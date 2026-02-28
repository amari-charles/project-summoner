using ProjectSummoner.Vfx;

namespace ProjectSummoner.Abilities;

/// <summary>
/// Base interface for ability configurations.
/// Each implementation defines the parameters for a specific ability type.
/// Used in UnitDefinition.Abilities to specify abilities at spawn time.
/// </summary>
public interface IAbilityConfig
{
    /// <summary>
    /// Creates an instance of the ability with configured parameters.
    /// Called during unit spawning to attach abilities to units.
    /// </summary>
    BaseAbility CreateAbility();
}

/// <summary>
/// Configuration for SlowOnHitAbility.
/// Applies a speed debuff to targets when the unit attacks.
/// </summary>
public record SlowOnHitConfig(
    float SlowPercent = 0.3f,
    float Duration = 2.0f,
    VfxId VfxId = default
) : IAbilityConfig
{
    public BaseAbility CreateAbility()
    {
        return new SlowOnHitAbility
        {
            SlowPercent = SlowPercent,
            SlowDuration = Duration,
            SlowAppliedVfx = VfxId
        };
    }
}

