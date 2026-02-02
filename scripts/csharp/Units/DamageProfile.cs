using ProjectSummoner.Cards;

namespace ProjectSummoner.Units;

/// <summary>
/// Defines the damage composition for a unit's attacks.
/// Allows units to deal mixed physical and elemental damage.
/// </summary>
/// <remarks>
/// Examples:
/// - Fire Wisp: PhysicalRatio = 0, Element = Fire, ElementalRatio = 1.0 (100% fire)
/// - Knight: PhysicalRatio = 1.0, ElementalRatio = 0 (100% physical)
/// - Fire Knight: PhysicalRatio = 0.5, Element = Fire, ElementalRatio = 0.5 (50/50 split)
///
/// NOTE: This is a stub - not yet integrated into combat. Armor/MagicResist formulas
/// will use this profile to determine damage reduction (physical vs elemental).
/// </remarks>
public record DamageProfile
{
    /// <summary>
    /// Ratio of damage that is physical (reduced by Armor).
    /// Range: 0.0 to 1.0
    /// </summary>
    public float PhysicalRatio { get; init; } = 1.0f;

    /// <summary>
    /// Optional element type for elemental damage.
    /// Null means no elemental component (pure physical).
    /// </summary>
    public Element? Element { get; init; }

    /// <summary>
    /// Ratio of damage that is elemental (reduced by MagicResist).
    /// Range: 0.0 to 1.0
    /// </summary>
    public float ElementalRatio { get; init; } = 0.0f;

    /// <summary>
    /// Default damage profile - 100% physical damage.
    /// </summary>
    public static DamageProfile Physical => new();

    /// <summary>
    /// Creates a pure elemental damage profile.
    /// </summary>
    public static DamageProfile PureElemental(Element element) => new()
    {
        PhysicalRatio = 0f,
        Element = element,
        ElementalRatio = 1.0f
    };

    /// <summary>
    /// Creates a mixed damage profile.
    /// </summary>
    public static DamageProfile Mixed(float physicalRatio, Element element) => new()
    {
        PhysicalRatio = physicalRatio,
        Element = element,
        ElementalRatio = 1.0f - physicalRatio
    };

    /// <summary>
    /// Validates that ratios sum to 1.0 (within tolerance).
    /// </summary>
    public bool IsValid()
    {
        const float tolerance = 0.001f;
        var total = PhysicalRatio + ElementalRatio;
        return System.Math.Abs(total - 1.0f) < tolerance;
    }

    public override string ToString()
    {
        if (ElementalRatio <= 0f)
            return "Physical";
        if (PhysicalRatio <= 0f)
            return $"{Element}";
        return $"{PhysicalRatio:P0} Physical / {ElementalRatio:P0} {Element}";
    }
}
