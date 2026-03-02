using Godot;
using System.Linq;
namespace ProjectSummoner.Targeting.Scorers;

/// <summary>
/// Combines multiple scorers additively.
/// Final score is the sum of all sub-scorer scores.
/// </summary>
[GlobalClass]
public partial class CompositeScorer : BaseTargetScorer
{
    [Export]
    public Godot.Collections.Array<BaseTargetScorer> Scorers { get; set; } = new();

    public override float CalculateScore(Node3D unit, Node3D target)
    {
        float total = 0f;
        foreach (var scorer in Scorers)
        {
            if (scorer != null)
            {
                total += scorer.CalculateScore(unit, target);
            }
        }
        return total;
    }
}
