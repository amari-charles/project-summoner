using Godot;

namespace Fateforged.Cards.Formations;

/// <summary>
/// Ring formation that places units in a circle around the spawn point.
/// Useful for area control or surrounding a position.
/// </summary>
public class RingFormation : IFormationStrategy
{
    /// <summary>
    /// Radius of the ring formation.
    /// </summary>
    public float Radius { get; set; } = 5.0f;

    public Vector3 GetOffset(int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        float angle = (2.0f * Mathf.Pi * unitIndex) / totalUnits;
        return new Vector3(
            Mathf.Cos(angle) * Radius,
            0,
            Mathf.Sin(angle) * Radius
        );
    }
}
