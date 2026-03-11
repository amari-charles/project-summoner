using System.Collections.Generic;

namespace Fateforged.Simulation.Data;

/// <summary>
/// Slot state container for one target (unit or summoner target id).
/// </summary>
public sealed class TargetSlotState
{
    public int TargetId { get; set; }

    // World-stable layout axis used to orient slot offsets.
    public SimVector3 LayoutAxis { get; set; } = new SimVector3(1f, 0f, 0f);

    public SimVector3 LastAnchorPosition { get; set; } = SimVector3.Zero;
    public float LastAxisRefreshTime { get; set; }

    public List<MeleeSlotEntry> Slots { get; } = new();
}
