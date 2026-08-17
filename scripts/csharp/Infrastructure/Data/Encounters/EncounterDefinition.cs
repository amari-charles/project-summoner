using System.Collections.Immutable;
using System.Text.Json;
using Fateforged.Data.Rewards;

namespace Fateforged.Data.Encounters;

public enum EncounterExecutionKind
{
    Battle,
}

public enum EncounterOutcome
{
    Victory,
    Defeat,
    Abandoned,
}

public sealed class EncounterDefinition
{
    public string Id { get; init; } = "";

    public string NameKey { get; init; } = "";

    public EncounterExecutionKind ExecutionKind { get; init; } = EncounterExecutionKind.Battle;

    public JsonElement Configuration { get; init; }

    public ImmutableArray<RewardOfferDefinition> RewardOffers { get; init; } = [];
}
