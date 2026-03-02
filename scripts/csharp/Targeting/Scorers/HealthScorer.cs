using Godot;
using ProjectSummoner.Capabilities;
namespace ProjectSummoner.Targeting.Scorers;

/// <summary>
/// Scores targets by health. Lower HP targets get higher scores (focus fire).
/// </summary>
[GlobalClass]
public partial class HealthScorer : BaseTargetScorer
{
    [Export]
    public float Weight { get; set; } = 10f;

    public override float CalculateScore(Node3D unit, Node3D target)
    {
        if (target is IDamageable damageable)
        {
            float hpPercent = damageable.CurrentHp / damageable.MaxHp;
            return (1f - hpPercent) * Weight;  // Lower HP = higher score
        }

        return 0f;
    }
}
