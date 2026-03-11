using Fateforged.Simulation.Enums;

namespace Fateforged.Simulation.Data;

/// <summary>
/// One slot entry owned by a target in commit-slot melee combat.
/// </summary>
public sealed class MeleeSlotEntry
{
    public int SlotId { get; set; }
    public SimVector3 SlotOffset { get; set; } = SimVector3.Zero;
    public SlotOccupancyState OccupancyState { get; set; } = SlotOccupancyState.Free;
    public int? ReservedUnitId { get; set; }
    public int? OccupiedUnitId { get; set; }

    // Deterministic tie-break metadata for reserve conflicts.
    public float ReservationDistanceSq { get; set; } = float.MaxValue;
    public int ReservationUnitId { get; set; } = int.MaxValue;
}
