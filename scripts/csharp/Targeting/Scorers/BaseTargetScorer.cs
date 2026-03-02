using Godot;
using System.Collections.Generic;
namespace ProjectSummoner.Targeting.Scorers;

/// <summary>
/// Abstract base class for target scoring.
/// Scorers determine target priority - higher scores = more preferred targets.
/// </summary>
[GlobalClass]
public abstract partial class BaseTargetScorer : Resource
{
    /// <summary>
    /// Calculate a score for a potential target. Higher = more preferred.
    /// </summary>
    public abstract float CalculateScore(Node3D unit, Node3D target);

    /// <summary>
    /// Get the best target from a list using this scorer.
    /// Default implementation picks the highest-scoring target.
    /// </summary>
    public virtual Node3D? GetBestTarget(Node3D unit, IEnumerable<Node3D> candidates)
    {
        Node3D? best = null;
        float bestScore = float.MinValue;

        foreach (var target in candidates)
        {
            float score = CalculateScore(unit, target);
            if (score > bestScore)
            {
                bestScore = score;
                best = target;
            }
        }

        return best;
    }
}
