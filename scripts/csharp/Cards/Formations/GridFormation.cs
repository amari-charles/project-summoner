using Godot;

namespace ProjectSummoner.Cards.Formations;

/// <summary>
/// Grid formation with staggered rows for army-like spawning.
/// Default formation used by most summon cards.
/// </summary>
public class GridFormation : IFormationStrategy
{
    // =========================================================================
    // CONSTANTS (match GDScript Card.gd)
    // =========================================================================

    /// <summary>Max units for 2-row formation.</summary>
    public const int TwoRowMax = 20;

    /// <summary>Target units per row for 20+ swarms.</summary>
    public const float LargeRowDensity = 3.0f;

    /// <summary>Default spacing between units.</summary>
    public const float DefaultSpacing = 1.8f;

    /// <summary>Default row offset for stagger pattern.</summary>
    public const float DefaultRowOffset = 0.5f;

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    /// <summary>
    /// Spacing between units in the formation.
    /// </summary>
    public float Spacing { get; set; } = DefaultSpacing;

    /// <summary>
    /// Row offset multiplier for brick/stagger pattern.
    /// </summary>
    public float RowOffset { get; set; } = DefaultRowOffset;

    // =========================================================================
    // IMPLEMENTATION
    // =========================================================================

    /// <summary>
    /// Calculate grid offset with staggered rows (brick pattern).
    /// Algorithm ported from Card.gd get_formation_offset().
    /// </summary>
    public Vector3 GetOffset(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        // Calculate grid dimensions - prefer 2 rows for army-like formations
        int rows = totalUnits <= TwoRowMax
            ? 2
            : Mathf.CeilToInt(Mathf.Sqrt(totalUnits / LargeRowDensity));

        int cols = Mathf.CeilToInt((float)totalUnits / rows);

        int row = unitIndex / cols;
        int col = unitIndex % cols;
        int unitsInRow = Mathf.Min(cols, totalUnits - row * cols);

        // Stagger offset for alternating rows (brick pattern)
        float stagger = (row % 2 == 1) ? RowOffset * Spacing : 0.0f;

        // X axis = row depth
        float formationDepth = (rows - 1) * Spacing;
        float xOffset = row * Spacing - formationDepth / 2.0f;

        // Z axis = column spread
        float rowWidth = (unitsInRow - 1) * Spacing;
        float zOffset = col * Spacing - rowWidth / 2.0f + stagger;

        return new Vector3(xOffset, 0, zOffset);
    }
}
