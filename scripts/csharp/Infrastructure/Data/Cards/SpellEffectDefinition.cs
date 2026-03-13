using Fateforged.Simulation.Enums;

namespace Fateforged.Cards;

/// <summary>
/// Authoring-time spell effect payload attached to CardDefinition.
/// Converted into SimSpellEffect at match start.
/// </summary>
public class SpellEffectDefinition
{
    /// <summary>What this effect does.</summary>
    public EffectType EffectType { get; init; }

    /// <summary>Magnitude (damage/heal amount, knockback distance, etc.).</summary>
    public float Value { get; init; }

    /// <summary>Duration for buffs/debuffs.</summary>
    public float Duration { get; init; }

    /// <summary>Damage type for damage-class effects.</summary>
    public DamageType DamageType { get; init; } = DamageType.Magic;

    /// <summary>Radius override (0 = use card SpellRadius).</summary>
    public float RadiusOverride { get; init; }

    /// <summary>Which team the effect targets.</summary>
    public SpellAffinity Affinity { get; init; } = SpellAffinity.Enemies;

    /// <summary>Delay before first apply.</summary>
    public float DelaySeconds { get; init; }

    /// <summary>Additional applications after the first one.</summary>
    public int RepeatCount { get; init; }

    /// <summary>Spacing between repeated applications.</summary>
    public float RepeatIntervalSeconds { get; init; }
}
