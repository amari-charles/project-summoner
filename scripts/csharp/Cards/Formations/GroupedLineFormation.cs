using Godot;

namespace ProjectSummoner.Cards.Formations;

/// <summary>
/// Line formation with grouped units (e.g., 6 units in 3 groups of 2).
/// Units are positioned in a horizontal line with larger gaps between groups.
/// </summary>
public class GroupedLineFormation : IFormationStrategy
{
    // =========================================================================
    // DEFAULTS
    // =========================================================================

    public const float DefaultUnitSpacing = 1.5f;
    public const float DefaultGroupSpacing = 4.0f;
    public const int DefaultUnitsPerGroup = 2;

    // =========================================================================
    // PROPERTIES
    // =========================================================================

    /// <summary>
    /// Spacing between units within a group.
    /// </summary>
    public float UnitSpacing { get; set; } = DefaultUnitSpacing;

    /// <summary>
    /// Spacing between groups (larger gap).
    /// </summary>
    public float GroupSpacing { get; set; } = DefaultGroupSpacing;

    /// <summary>
    /// Number of units per group.
    /// </summary>
    public int UnitsPerGroup { get; set; } = DefaultUnitsPerGroup;

    public Vector3 GetOffset(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        // Calculate which group this unit belongs to and position within group
        int groupIndex = unitIndex / UnitsPerGroup;
        int indexInGroup = unitIndex % UnitsPerGroup;

        // Calculate total number of groups
        int totalGroups = (totalUnits + UnitsPerGroup - 1) / UnitsPerGroup;

        // Width of a single group
        float groupWidth = (UnitsPerGroup - 1) * UnitSpacing;

        // Total width of all groups with gaps between them
        float totalWidth = (totalGroups - 1) * GroupSpacing + totalGroups * groupWidth;

        // Starting Z position for this group (centered around origin)
        float groupStartZ = groupIndex * (GroupSpacing + groupWidth) - totalWidth / 2.0f;

        // Position within the group
        float inGroupOffset = indexInGroup * UnitSpacing;

        // Final Z position
        float zOffset = groupStartZ + inGroupOffset;

        return new Vector3(0, 0, zOffset);
    }
}
