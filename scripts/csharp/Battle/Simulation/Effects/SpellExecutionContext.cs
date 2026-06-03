using Fateforged.Simulation.Data;

namespace Fateforged.Simulation.Effects;

public sealed class SpellExecutionContext
{
    public SimCardData CardData { get; init; } = new();
    public int Team { get; init; }
    public SimVector3 CastPosition { get; init; }
    public int? TargetUnitId { get; init; }
    public int SourceUnitId { get; init; }
    public SimVector3 SourcePosition { get; init; }
}
