using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Scorers;

/// <summary>
/// Bonus score for targets below a flying unit.
/// Useful for flying units that prefer to attack ground targets directly beneath them.
/// </summary>
[GlobalClass]
public partial class BelowTargetScorer : BaseTargetScorer
{
    [Export]
    public float Radius { get; set; } = 6f;

    [Export]
    public float Weight { get; set; } = 5f;

    public override float CalculateScore(Unit3D unit, Node3D target)
    {
        // Only applies to flying units
        if (unit.MovementLayer != (int)MovementLayer.Air)
            return 0f;

        Vector3 delta = unit.GlobalPosition - target.GlobalPosition;
        float xzDist = new Vector2(delta.X, delta.Z).Length();
        float yDiff = delta.Y;

        // Target must be below us and within radius
        if (xzDist <= Radius && yDiff > 0)
        {
            return Weight * (1f - xzDist / Radius);
        }

        return 0f;
    }
}
