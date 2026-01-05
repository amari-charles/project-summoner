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
    /// Generate formation offset based on formation type.
    /// </summary>
    public static Vector3 GenerateFormationOffset(
        int unitIndex,
        int unitCount,
        string formationType = "grid",
        float spacing = FormationSpacing,
        float rowOffset = FormationRowOffset,
        float groupSpacing = 4.0f,
        int unitsPerGroup = 2)
    {
        if (unitCount <= 1)
        {
            return Vector3.Zero;
        }

        return formationType switch
        {
            "grouped_line" => GenerateGroupedLineOffset(unitIndex, unitCount, spacing, groupSpacing, unitsPerGroup),
            _ => GenerateGridOffset(unitIndex, unitCount, spacing, rowOffset)
        };
    }

    /// <summary>
    /// Generate formation offset for staggered row spawning (default grid).
    /// Returns position offset for unit at given index in a staggered grid formation.
    /// Formation is centered on spawn point with alternating rows offset (brick pattern).
    /// </summary>
    public static Vector3 GenerateGridOffset(int unitIndex, int unitCount, float spacing = FormationSpacing, float rowOffset = FormationRowOffset)
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
        float stagger = row % 2 == 1 ? rowOffset * spacing : 0.0f;

        // X axis = row depth (front row closer to enemy, back row behind)
        float formationDepth = (rows - 1) * spacing;
        float xOffset = row * spacing - formationDepth / 2.0f;

        // Z axis = column spread (units spread out perpendicular to enemy direction)
        float rowWidth = (unitsInRow - 1) * spacing;
        float zOffset = col * spacing - rowWidth / 2.0f + stagger;

        return new Vector3(xOffset, 0, zOffset);
    }

    /// <summary>
    /// Generate grouped line formation offset.
    /// Units are arranged in a horizontal line with larger gaps between groups.
    /// </summary>
    public static Vector3 GenerateGroupedLineOffset(
        int unitIndex,
        int unitCount,
        float unitSpacing = 1.5f,
        float groupSpacing = 4.0f,
        int unitsPerGroup = 2)
    {
        if (unitCount <= 1)
        {
            return Vector3.Zero;
        }

        // Calculate which group this unit belongs to and position within group
        int groupIndex = unitIndex / unitsPerGroup;
        int indexInGroup = unitIndex % unitsPerGroup;

        // Total number of groups
        int totalGroups = Mathf.CeilToInt((float)unitCount / unitsPerGroup);

        // Width of a single group
        float groupWidth = (unitsPerGroup - 1) * unitSpacing;

        // Total width of all groups with gaps between them
        float totalWidth = (totalGroups - 1) * groupSpacing + totalGroups * groupWidth;

        // Starting Z position for this group (centered around origin)
        float groupStartZ = groupIndex * (groupSpacing + groupWidth) - totalWidth / 2.0f;

        // Position within the group
        float inGroupOffset = indexInGroup * unitSpacing;

        // Final Z position
        float zOffset = groupStartZ + inGroupOffset;

        return new Vector3(0, 0, zOffset);
    }
}
