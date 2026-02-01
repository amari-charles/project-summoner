using Godot;

namespace ProjectSummoner.Units;

/// <summary>
/// A ranged unit that coordinates targeting with its "mama" duck.
/// Ducklings target whatever their mama is currently targeting.
/// If mama has no target or is dead, falls back to normal targeting.
/// </summary>
[GlobalClass]
public partial class DucklingUnit3D : RangedUnit3D
{
    /// <summary>
    /// Reference to the mama duck this duckling follows.
    /// Set at spawn time by the spawning system.
    /// </summary>
    [Export]
    public Unit3D? MamaDuck { get; set; }

    /// <summary>
    /// Override target acquisition to use mama's target when available.
    /// </summary>
    protected override Node3D? AcquireTarget()
    {
        // If mama exists and has a valid target, use that
        if (MamaDuck != null && IsInstanceValid(MamaDuck) && MamaDuck.IsAlive)
        {
            var mamaTarget = MamaDuck.CurrentTarget;
            if (mamaTarget != null && IsInstanceValid(mamaTarget))
            {
                return mamaTarget;
            }
        }

        // Fallback to normal targeting if mama has no target or is dead
        return base.AcquireTarget();
    }
}
