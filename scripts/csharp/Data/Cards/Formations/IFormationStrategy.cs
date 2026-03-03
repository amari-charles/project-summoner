using Godot;

namespace ProjectSummoner.Cards.Formations;

/// <summary>
/// Strategy interface for calculating unit spawn positions in a formation.
/// Implementations define different formation patterns (grid, ring, line, etc.).
/// </summary>
public interface IFormationStrategy
{
    /// <summary>
    /// Calculate the offset from the base spawn position for a specific unit in the formation.
    /// </summary>
    /// <param name="unitIndex">Zero-based index of the unit being positioned.</param>
    /// <param name="totalUnits">Total number of units in the formation.</param>
    /// <returns>Offset vector from the base spawn position (Y is typically 0).</returns>
    Vector3 GetOffset(int unitIndex, int totalUnits);
}
