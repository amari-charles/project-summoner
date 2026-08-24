using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Encounters;
using Fateforged.Data.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Quests;
using Fateforged.Meta.Summoner;
using Godot;
using Godot.Collections;

namespace Fateforged.Meta.Encounters;

[GlobalClass]
public partial class EncounterService : Node
{
    public static EncounterService? Instance { get; private set; }

    private IReadOnlyList<EncounterDefinition> _catalog = EncounterCatalog.All;
    private readonly System.Collections.Generic.Dictionary<
        EncounterExecutionKind,
        IEncounterExecutionHandler
    > _handlers = [];
    private IProfileRepository? _profileRepo;
    private Func<SummonerId>? _getActiveSummoner;

    public override void _Ready()
    {
        Instance = this;
        Initialize(ProfileRepository.Instance);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitForTesting(
        IProfileRepository repository,
        Func<SummonerId>? activeSummoner = null,
        IReadOnlyList<EncounterDefinition>? catalog = null
    )
    {
        _getActiveSummoner = activeSummoner;
        _catalog = catalog ?? EncounterCatalog.All;
        Initialize(repository);
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

    public Dictionary Complete(string encounterId, int outcome = (int)EncounterOutcome.Victory)
    {
        if (!Enum.IsDefined(typeof(EncounterOutcome), outcome))
            return [];
        var encounter = Find(encounterId);
        if (encounter == null || !_handlers.TryGetValue(encounter.ExecutionKind, out var handler))
            return [];
        var typedOutcome = (EncounterOutcome)outcome;
        var result = handler.Complete(encounter, typedOutcome);
        if (result.Count == 0)
            return result;
        result["encounter_id"] = encounter.Id;
        result["outcome"] = typedOutcome.ToString().ToSnakeCase();
        QuestService.Instance?.RecordEncounterCompleted(
            encounterId,
            typedOutcome.ToString().ToSnakeCase()
        );
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

    private void Initialize(IProfileRepository? repository)
    {
        _profileRepo = repository;
        if (_profileRepo == null)
        {
            GD.PushError("EncounterService: ProfileRepository is unavailable");
            return;
        }
        _getActiveSummoner ??= ResolveActiveSummoner;
        _handlers.Clear();
        _handlers[EncounterExecutionKind.Battle] = new BattleEncounterHandler(
            _profileRepo,
            _getActiveSummoner
        );
    }

    private SummonerId ResolveActiveSummoner()
    {
        return SummonerId.FromString(
            SummonerSelectionService.Instance?.GetActiveSummonerId() ?? ""
        );
    }

    private EncounterDefinition? Find(string encounterId) =>
        _catalog.FirstOrDefault(encounter =>
            string.Equals(encounter.Id, encounterId, StringComparison.Ordinal)
        );
}
