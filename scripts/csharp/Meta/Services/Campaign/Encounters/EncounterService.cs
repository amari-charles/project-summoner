using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Encounters;
using Godot;
using Godot.Collections;

namespace Fateforged.Meta.Campaign.Encounters;

public sealed class EncounterService
{
    private readonly IReadOnlyList<EncounterDefinition> _catalog;
    private readonly System.Collections.Generic.Dictionary<
        EncounterExecutionKind,
        IEncounterExecutionHandler
    > _handlers = [];

    public EncounterService(IReadOnlyList<EncounterDefinition>? catalog = null)
    {
        _catalog = catalog ?? EncounterCatalog.All;
    }

    public void Register(IEncounterExecutionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd(handler.Kind, handler))
            throw new InvalidOperationException(
                $"An encounter handler is already registered for '{handler.Kind}'."
            );
    }

    public Dictionary GetPreparationState(string encounterId)
    {
        var encounter = Find(encounterId);
        if (encounter == null || !_handlers.TryGetValue(encounter.ExecutionKind, out var handler))
            return [];
        var state = handler.GetPreparationState(encounter);
        state["encounter_id"] = encounter.Id;
        state["name_key"] = encounter.NameKey;
        state["execution_kind"] = encounter.ExecutionKind.ToString().ToSnakeCase();
        return state;
    }

    public Dictionary ResolveBattleConfig(string encounterId)
    {
        var encounter = Find(encounterId);
        return encounter != null && _handlers.TryGetValue(encounter.ExecutionKind, out var handler)
            ? handler.ResolveBattleConfig(encounter)
            : [];
    }

    public bool UpdateLoadout(string encounterId, Array<Dictionary> slots)
    {
        var encounter = Find(encounterId);
        return encounter != null
            && _handlers.TryGetValue(encounter.ExecutionKind, out var handler)
            && handler.UpdateLoadout(encounter, slots);
    }

    public Dictionary FillLoadoutFromDeck(string encounterId, string sourceDeckId)
    {
        var encounter = Find(encounterId);
        return encounter != null && _handlers.TryGetValue(encounter.ExecutionKind, out var handler)
            ? handler.FillLoadoutFromDeck(encounter, sourceDeckId)
            : [];
    }

    public Dictionary SaveLoadoutToDeck(string encounterId, string targetDeckId, string newDeckName)
    {
        var encounter = Find(encounterId);
        return encounter != null && _handlers.TryGetValue(encounter.ExecutionKind, out var handler)
            ? handler.SaveLoadoutToDeck(encounter, targetDeckId, newDeckName)
            : [];
    }

    public Dictionary Complete(string encounterId, EncounterOutcome outcome)
    {
        var encounter = Find(encounterId);
        if (encounter == null || !_handlers.TryGetValue(encounter.ExecutionKind, out var handler))
            return [];
        var result = handler.Complete(encounter, outcome);
        if (result.Count == 0)
            return result;
        result["encounter_id"] = encounter.Id;
        result["outcome"] = outcome.ToString().ToSnakeCase();
        return result;
    }

    public Dictionary ConsumeCompletionSummary(string encounterId)
    {
        var encounter = Find(encounterId);
        return encounter != null && _handlers.TryGetValue(encounter.ExecutionKind, out var handler)
            ? handler.ConsumeCompletionSummary()
            : [];
    }

    public Dictionary GetCompletionSummary(string encounterId)
    {
        var encounter = Find(encounterId);
        if (encounter == null || !_handlers.TryGetValue(encounter.ExecutionKind, out var handler))
            return [];
        var summary = handler.GetCompletionSummary();
        if (summary.Count > 0)
            summary["encounter_id"] = encounter.Id;
        return summary;
    }

    private EncounterDefinition? Find(string encounterId) =>
        _catalog.FirstOrDefault(encounter =>
            string.Equals(encounter.Id, encounterId, StringComparison.Ordinal)
        );
}
