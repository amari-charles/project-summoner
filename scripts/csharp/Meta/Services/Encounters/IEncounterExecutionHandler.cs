using Fateforged.Data.Encounters;
using Godot.Collections;

namespace Fateforged.Meta.Encounters;

public interface IEncounterExecutionHandler
{
    EncounterExecutionKind Kind { get; }

    Dictionary GetPreparationState(EncounterDefinition encounter);

    Dictionary ResolveBattleConfig(EncounterDefinition encounter);

    bool UpdateLoadout(EncounterDefinition encounter, Array<Dictionary> slots);

    Dictionary FillLoadoutFromDeck(EncounterDefinition encounter, string sourceDeckId);

    Dictionary SaveLoadoutToDeck(
        EncounterDefinition encounter,
        string targetDeckId,
        string newDeckName
    );

    Dictionary Complete(EncounterDefinition encounter, EncounterOutcome outcome);

    Dictionary GetCompletionSummary();

    Dictionary ConsumeCompletionSummary();
}
