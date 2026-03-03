using Godot;

namespace ProjectSummoner.Cards.Formations;

/// <summary>
/// Line formation that places units in a horizontal line.
/// Useful for defensive walls or front-line positioning.
/// </summary>
public class LineFormation : IFormationStrategy
{
    /// <summary>
    /// Spacing between units in the line.
    /// </summary>
    public float Spacing { get; set; } = 1.5f;

    public Vector3 GetOffset(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        float width = (totalUnits - 1) * Spacing;
        return new Vector3(0, 0, unitIndex * Spacing - width / 2.0f);
    }
}
