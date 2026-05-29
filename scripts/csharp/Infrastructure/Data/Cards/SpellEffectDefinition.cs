using Fateforged.Simulation.Enums;
using Fateforged.Simulation.Effects;
using Fateforged.Units;

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

    /// <summary>
    /// Typed lifetime for buff/debuff effects.
    /// Duration remains as bridge field during migration.
    /// </summary>
    public EffectLifetime Lifetime { get; init; } = EffectLifetime.Timed(0f);

    /// <summary>Damage type for damage-class effects.</summary>
    public DamageType DamageType { get; init; } = DamageType.Magic;

    /// <summary>Radius override (0 = use card SpellRadius).</summary>
    public float RadiusOverride { get; init; }

    /// <summary>Area shape used by this effect when resolving AoE recipients.</summary>
    public SpellAreaShape AreaShape { get; init; } = SpellAreaShape.Circle;

    /// <summary>Which team the effect targets.</summary>
    public SpellAffinity Affinity { get; init; } = SpellAffinity.Enemies;

    /// <summary>Optional element requirement for valid targets.</summary>
    public Element? RequiredTargetElement { get; init; }

    /// <summary>Delay before first apply.</summary>
    public float DelaySeconds { get; init; }

    /// <summary>Additional applications after the first one.</summary>
    public int RepeatCount { get; init; }

    /// <summary>Spacing between repeated applications.</summary>
    public float RepeatIntervalSeconds { get; init; }

    /// <summary>Status payload identity for status apply/consume effects.</summary>
    public StatusEffectKind StatusKind { get; init; } = StatusEffectKind.None;

    /// <summary>Status payload tick interval.</summary>
    public float StatusTickInterval { get; init; } = 1f;

    /// <summary>Status payload potency per stack.</summary>
    public float StatusPotencyPerStack { get; init; }

    /// <summary>Status payload max stacks.</summary>
    public int StatusMaxStacks { get; init; } = 1;

    /// <summary>Optional payload fired when a buff created by this effect is removed.</summary>
    public BuffRemovalEffectConfig? RemovalEffect { get; init; }

    /// <summary>Tags required/blocked before this effect can affect a target.</summary>
    public EffectTagRequirements TagRequirements { get; init; } = new();

    /// <summary>Tags granted while a buff created by this effect is active.</summary>
    public string[] GrantedTags { get; init; } = [];

    /// <summary>Policy used if a matching active buff already exists.</summary>
    public EffectStackPolicy StackPolicy { get; init; } = EffectStackPolicy.Independent;

    /// <summary>Optional stack key used by non-independent stack policies.</summary>
    public string StackKey { get; init; } = "";

    /// <summary>Optional cue identity emitted for this effect's lifecycle.</summary>
    public string CueId { get; init; } = "";
}
