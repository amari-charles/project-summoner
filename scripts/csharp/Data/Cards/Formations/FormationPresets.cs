namespace Fateforged.Cards.Formations;

/// <summary>
/// Predefined formation instances for cards to reference.
/// Cards reference these directly instead of specifying formation parameters inline.
/// </summary>
public static class FormationPresets
{
    // =========================================================================
    // GRID FORMATIONS
    // =========================================================================

    /// <summary>
    /// Default grid formation for most summon cards.
    /// 2-row staggered pattern with standard spacing.
    /// </summary>
    public static readonly GridFormation StandardGrid = new()
    {
        Spacing = GridFormation.DefaultSpacing,
        RowOffset = GridFormation.DefaultRowOffset
    };

    /// <summary>
    /// Tighter grid formation for swarm cards (e.g., Fire Swarm).
    /// Closer spacing for compact unit groups.
    /// </summary>
    public static readonly GridFormation TightSwarmGrid = new()
    {
        Spacing = 1.5f,
        RowOffset = 0.5f
    };

    /// <summary>
    /// Fire Ant Swarm formation - 20 ants in 4 rows of 5.
    /// Very tight spacing for overwhelming ant colony effect.
    /// </summary>
    public static readonly GridFormation FireAntSwarm = new()
    {
        Columns = 5,
        Spacing = 1.0f,
        RowOffset = 1.2f
    };

    // =========================================================================
    // GROUPED LINE FORMATIONS
    // =========================================================================

    /// <summary>
    /// Cloud Swarm formation - 3 groups of 2 units in a horizontal line.
    /// Used by Cloud Swarm card (6 Puffs).
    /// </summary>
    public static readonly GroupedLineFormation CloudSwarm = new()
    {
        UnitSpacing = 1.5f,
        GroupSpacing = 8.0f,
        UnitsPerGroup = 2
    };

    // =========================================================================
    // LINE FORMATIONS
    // =========================================================================

    /// <summary>
    /// Standard horizontal line formation.
    /// Units spread out in a single row.
    /// </summary>
    public static readonly LineFormation StandardLine = new()
    {
        Spacing = 1.5f
    };

    // =========================================================================
    // RING FORMATIONS
    // =========================================================================

    /// <summary>
    /// Standard ring formation.
    /// Units arranged in a circle around spawn point.
    /// </summary>
    public static readonly RingFormation StandardRing = new()
    {
        Radius = 5.0f
    };
}
