namespace Fateforged.Simulation.Data;

/// <summary>
/// Runtime identity for a card in match state.
/// CatalogId remains the simulation lookup key; InstanceId tracks progression identity.
/// </summary>
public sealed class SimCardRuntimeRef
{
    public SimCardCatalogId CatalogId { get; set; } = SimCardCatalogId.Empty;
    public SimCardInstanceId InstanceId { get; set; } = SimCardInstanceId.Empty;
}
