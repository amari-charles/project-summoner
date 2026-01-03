using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Targeting.Scorers;

/// <summary>
/// Scores targets by distance. Closer targets get higher scores.
/// </summary>
[GlobalClass]
public partial class DistanceScorer : BaseTargetScorer
{
    [Export]
    public float MaxDistance { get; set; } = 20f;

    [Export]
    public float Weight { get; set; } = 1f;

    public override float CalculateScore(Unit3D unit, Node3D target)
    {
        float distance = unit.GlobalPosition.DistanceTo(target.GlobalPosition);
        return (MaxDistance - distance) * Weight;
    }
}
