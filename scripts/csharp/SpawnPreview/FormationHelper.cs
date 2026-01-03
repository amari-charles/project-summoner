using Godot;

namespace ProjectSummoner.SpawnPreview;

/// <summary>
/// Static helper for generating unit formation positions.
/// Ported from Card.generate_formation_offset() in GDScript.
/// </summary>
public static class FormationHelper
{
    // =========================================================================
    // CONSTANTS
    // =========================================================================

    /// <summary>Distance between units in formation (world units).</summary>
    private const float FormationSpacing = 1.8f;

    /// <summary>Fraction of spacing to offset alternating rows (brick pattern).</summary>
    private const float FormationRowOffset = 0.5f;

    /// <summary>Max units for 2-row formation; larger swarms use more rows.</summary>
    private const int FormationTwoRowMax = 20;

    /// <summary>Target units per row for large swarms (20+).</summary>
    private const float FormationLargeRowDensity = 3.0f;

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Generate formation offset for staggered row spawning.
    /// Returns position offset for unit at given index in a staggered grid formation.
    /// Formation is centered on spawn point with alternating rows offset (brick pattern).
    /// </summary>
    public static Vector3 GenerateFormationOffset(int unitIndex, int unitCount)
    {
        if (unitCount <= 1)
        {
            return Vector3.Zero;
        }

        // Calculate grid dimensions - prefer 2 rows for army-like formations
        // Only use more rows if we have a very large swarm
        int rows = unitCount <= FormationTwoRowMax
            ? 2
            : Mathf.CeilToInt(Mathf.Sqrt(unitCount / FormationLargeRowDensity));
        int cols = Mathf.CeilToInt((float)unitCount / rows);

        // Get row and column for this unit
        int row = unitIndex / cols;
        int col = unitIndex % cols;

        // Calculate how many units are in this row (last row may be partial)
        int unitsInRow = Mathf.Min(cols, unitCount - row * cols);

        // Calculate position with stagger offset for alternating rows
        float stagger = row % 2 == 1 ? FormationRowOffset * FormationSpacing : 0.0f;

        // X axis = row depth (front row closer to enemy, back row behind)
        float formationDepth = (rows - 1) * FormationSpacing;
        float xOffset = row * FormationSpacing - formationDepth / 2.0f;

        // Z axis = column spread (units spread out perpendicular to enemy direction)
        float rowWidth = (unitsInRow - 1) * FormationSpacing;
        float zOffset = col * FormationSpacing - rowWidth / 2.0f + stagger;

        return new Vector3(xOffset, 0, zOffset);
    }
}
