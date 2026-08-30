using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Quests;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Progression;
using Fateforged.Infrastructure.Persistence;
using Godot;
using Godot.Collections;

namespace Fateforged.Meta.Quests;

public sealed class QuestProgressHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly Func<SummonerId> _getActiveSummoner;
    private readonly QuestRewardProcessor _rewards;
    private readonly IReadOnlyList<QuestDefinition> _catalog;

    public QuestProgressHandler(
        IProfileRepository profileRepo,
        Func<SummonerId> getActiveSummoner,
        QuestRewardProcessor rewards,
        IReadOnlyList<QuestDefinition>? catalog = null
    )
    {
        _profileRepo = profileRepo;
        _getActiveSummoner = getActiveSummoner;
        _rewards = rewards;
        _catalog = catalog ?? QuestCatalog.All;
    }

    public bool Accept(string questId)
    {
        var definition = Find(questId);
        var progress = GetProgress();
        if (
            definition == null
            || !IsAvailable(definition, progress)
            || progress.Quests.ActiveQuestIds.Contains(questId)
            || progress.Quests.CompletedQuestIds.Contains(questId)
            || RemainingCapacity(progress) < definition.CurriculumCost
        )
            return false;

        AddOnce(progress.Quests.DiscoveredQuestIds, questId);
        AddOnce(progress.Quests.ActiveQuestIds, questId);
        progress.Quests.CurrentStepByQuestId[questId] = 0;
        progress.Quests.TrackedQuestId = questId;
        Save(progress);
        return true;
    }

    public bool Track(string questId)
    {
        var progress = GetProgress();
        if (!string.IsNullOrEmpty(questId) && !progress.Quests.ActiveQuestIds.Contains(questId))
            return false;
        progress.Quests.TrackedQuestId = questId;
        Save(progress);
        return true;
    }

    public Dictionary RecordWorldInteraction(string targetId)
    {
        var advanced = AdvanceMatchingStep(
            step =>
                step.Kind == QuestStepKind.InteractWithWorldTarget
                && string.Equals(step.TargetId, targetId, StringComparison.Ordinal),
            "world_interaction"
        );
        return advanced.Count > 0 ? advanced : GetLaunchableEncounter(targetId);
    }

    public Dictionary RecordNpcInteraction(string npcId) =>
        AdvanceMatchingStep(
            step =>
                step.Kind == QuestStepKind.TalkToNpc
                && string.Equals(step.TargetId, npcId, StringComparison.Ordinal),
            "npc_interaction"
        );

    public Dictionary RecordEncounterCompleted(string encounterId, string outcome) =>
        AdvanceMatchingStep(
            step =>
                step.Kind == QuestStepKind.CompleteEncounter
                && string.Equals(step.EncounterId, encounterId, StringComparison.Ordinal)
                && string.Equals(step.RequiredOutcome, outcome, StringComparison.OrdinalIgnoreCase),
            "encounter_completed"
        );

    public Dictionary RecordUiSurfaceOpened(string surfaceId) =>
        AdvanceMatchingStep(
            step =>
                step.Kind == QuestStepKind.OpenUiSurface
                && string.Equals(step.TargetId, surfaceId, StringComparison.Ordinal),
            "ui_surface_opened"
        );

    public Dictionary GetJournalState()
    {
        var progress = GetProgress();
        var active = new Array<Dictionary>();
        var opportunities = new Array<Dictionary>();
        var completed = new Array<Dictionary>();
        foreach (var definition in _catalog)
        {
            if (progress.Quests.CompletedQuestIds.Contains(definition.Id))
                completed.Add(ToEntry(definition, progress, "completed"));
            else if (progress.Quests.ActiveQuestIds.Contains(definition.Id))
                active.Add(ToEntry(definition, progress, "active"));
            else if (
                IsAvailable(definition, progress)
                && (
                    definition.Visibility == QuestVisibility.Announced
                    || progress.Quests.DiscoveredQuestIds.Contains(definition.Id)
                )
            )
                opportunities.Add(ToEntry(definition, progress, "opportunity"));
        }

        return new Dictionary
        {
            ["current_year"] = 1,
            ["capacity_total"] = progress.Quests.CurriculumCapacity,
            ["capacity_committed"] = UsedCapacity(progress),
            ["capacity_completed"] = CompletedCapacity(progress),
            ["capacity_remaining"] = RemainingCapacity(progress),
            ["tracked_quest_id"] = progress.Quests.TrackedQuestId,
            ["active"] = active,
            ["opportunities"] = opportunities,
            ["completed"] = completed,
        };
    }

    public Dictionary GetNpcState(string npcId)
    {
        var progress = GetProgress();
        var opportunities = new Array<Dictionary>();
        var active = new Array<Dictionary>();
        foreach (var definition in _catalog.Where(quest => quest.Source.Id == npcId))
        {
            if (progress.Quests.ActiveQuestIds.Contains(definition.Id))
                active.Add(ToEntry(definition, progress, "active"));
            else if (
                IsAvailable(definition, progress)
                && !progress.Quests.CompletedQuestIds.Contains(definition.Id)
                && (
                    definition.Visibility == QuestVisibility.Announced
                    || progress.Quests.DiscoveredQuestIds.Contains(definition.Id)
                )
            )
                opportunities.Add(ToEntry(definition, progress, "opportunity"));
        }

        var hasTurnIn = active.Any(entry =>
            entry.TryGetValue("current_step_kind", out var kind)
            && kind.AsString() == "talk_to_npc"
            && entry["current_target_id"].AsString() == npcId
        );
        return new Dictionary
        {
            ["id"] = npcId,
            ["quest_marker"] =
                hasTurnIn ? "?"
                : opportunities.Count > 0 ? "!"
                : "",
            ["opportunities"] = opportunities,
            ["active"] = active,
        };
    }

    private bool IsAvailable(QuestDefinition definition, SummonerProgress progress)
    {
        if (
            definition.PrerequisiteQuestIds.Any(prerequisite =>
                !progress.Quests.CompletedQuestIds.Contains(prerequisite)
            )
        )
            return false;
        if (string.IsNullOrWhiteSpace(definition.ExclusiveGroupId))
            return true;
        return !_catalog.Any(other =>
            other.Id != definition.Id
            && other.ExclusiveGroupId == definition.ExclusiveGroupId
            && (
                progress.Quests.ActiveQuestIds.Contains(other.Id)
                || progress.Quests.CompletedQuestIds.Contains(other.Id)
            )
        );
    }

    private Dictionary GetLaunchableEncounter(string targetId)
    {
        var progress = GetProgress();
        foreach (var questId in progress.Quests.ActiveQuestIds)
        {
            var definition = Find(questId);
            var stepIndex = progress.Quests.CurrentStepByQuestId.GetValueOrDefault(questId, 0);
            if (definition == null || stepIndex < 0 || stepIndex >= definition.Steps.Length)
                continue;
            var step = definition.Steps[stepIndex];
            if (
                step.Kind != QuestStepKind.CompleteEncounter
                || !string.Equals(step.TargetId, targetId, StringComparison.Ordinal)
            )
                continue;
            return new Dictionary
            {
                ["advanced"] = false,
                ["completed"] = false,
                ["event_kind"] = "world_interaction",
                ["quest_id"] = questId,
                ["current_step"] = ToStep(step),
            };
        }
        return [];
    }

    private Dictionary AdvanceMatchingStep(
        Func<QuestStepDefinition, bool> matches,
        string eventKind
    )
    {
        var progress = GetProgress();
        foreach (var questId in progress.Quests.ActiveQuestIds.ToArray())
        {
            var definition = Find(questId);
            var stepIndex = progress.Quests.CurrentStepByQuestId.GetValueOrDefault(questId, 0);
            if (definition == null || stepIndex < 0 || stepIndex >= definition.Steps.Length)
                continue;
            if (!matches(definition.Steps[stepIndex]))
                continue;

            stepIndex++;
            if (stepIndex < definition.Steps.Length)
            {
                progress.Quests.CurrentStepByQuestId[questId] = stepIndex;
                Save(progress);
                return new Dictionary
                {
                    ["advanced"] = true,
                    ["completed"] = false,
                    ["event_kind"] = eventKind,
                    ["quest_id"] = questId,
                    ["current_step"] = ToStep(definition.Steps[stepIndex]),
                };
            }

            var completionSummary = _rewards.Complete(definition);
            if (definition.RewardOffers.Length > 0 && completionSummary.Count == 0)
                return [];
            progress.Quests.ActiveQuestIds.Remove(questId);
            AddOnce(progress.Quests.CompletedQuestIds, questId);
            progress.Quests.CurrentStepByQuestId.Remove(questId);
            if (progress.Quests.TrackedQuestId == questId)
                progress.Quests.TrackedQuestId = "";
            Save(progress);
            return new Dictionary
            {
                ["advanced"] = true,
                ["completed"] = true,
                ["event_kind"] = eventKind,
                ["quest_id"] = questId,
                ["completion_summary"] = completionSummary,
            };
        }
        return [];
    }

    private Dictionary ToEntry(QuestDefinition definition, SummonerProgress progress, string state)
    {
        var stepIndex = progress.Quests.CurrentStepByQuestId.GetValueOrDefault(definition.Id, 0);
        QuestStepDefinition? step =
            state == "active" && stepIndex >= 0 && stepIndex < definition.Steps.Length
                ? definition.Steps[stepIndex]
                : null;
        return new Dictionary
        {
            ["id"] = definition.Id,
            ["source_kind"] = definition.Source.Kind,
            ["source_id"] = definition.Source.Id,
            ["source_name_key"] = definition.Source.NameKey,
            ["state"] = state,
            ["title_key"] = definition.TitleKey,
            ["description_key"] = definition.DescriptionKey,
            ["location_key"] = definition.Source.LocationKey,
            ["current_objective_key"] = step?.ObjectiveKey ?? "",
            ["current_step_kind"] = step?.Kind.ToString().ToSnakeCase() ?? "",
            ["current_target_id"] = step?.TargetId ?? "",
            ["current_encounter_id"] = step?.EncounterId ?? "",
            ["is_tracked"] = progress.Quests.TrackedQuestId == definition.Id,
            ["offer_dialogue_keys"] = ToArray(definition.Dialogue.OfferLineKeys),
            ["accepted_dialogue_keys"] = ToArray(definition.Dialogue.AcceptedLineKeys),
            ["response_choices"] = ToResponses(definition.Dialogue.Responses),
            ["active_dialogue_keys"] = ToArray(step?.DialogueKeys ?? []),
            ["curriculum_cost"] = definition.CurriculumCost,
            ["reward_previews"] = _rewards.GetPreviews(definition),
        };
    }

    private int UsedCapacity(SummonerProgress progress) =>
        _catalog
            .Where(quest =>
                progress.Quests.ActiveQuestIds.Contains(quest.Id)
                || progress.Quests.CompletedQuestIds.Contains(quest.Id)
            )
            .Sum(quest => quest.CurriculumCost);

    private int CompletedCapacity(SummonerProgress progress) =>
        _catalog
            .Where(quest => progress.Quests.CompletedQuestIds.Contains(quest.Id))
            .Sum(quest => quest.CurriculumCost);

    private int RemainingCapacity(SummonerProgress progress) =>
        Math.Max(0, progress.Quests.CurriculumCapacity - UsedCapacity(progress));

    private static Dictionary ToStep(QuestStepDefinition step) =>
        new()
        {
            ["id"] = step.Id,
            ["kind"] = step.Kind.ToString().ToSnakeCase(),
            ["objective_key"] = step.ObjectiveKey,
            ["target_id"] = step.TargetId,
            ["encounter_id"] = step.EncounterId,
        };

    private static Array<string> ToArray(IEnumerable<string> values) => new(values);

    private static Array<Dictionary> ToResponses(
        IEnumerable<QuestDialogueResponseDefinition> responses
    )
    {
        var result = new Array<Dictionary>();
        foreach (var response in responses)
        {
            result.Add(
                new Dictionary
                {
                    ["id"] = response.Id,
                    ["text_key"] = response.TextKey,
                    ["action"] = response.Action,
                }
            );
        }
        return result;
    }

    private QuestDefinition? Find(string questId) =>
        _catalog.FirstOrDefault(quest => string.Equals(quest.Id, questId, StringComparison.Ordinal));

    private SummonerProgress GetProgress() =>
        _profileRepo.GetSummonerProgress(_getActiveSummoner());

    private void Save(SummonerProgress progress) =>
        _profileRepo.UpdateSummonerProgress(_getActiveSummoner(), progress);

    private static void AddOnce(List<string> values, string value)
    {
        if (!values.Contains(value))
            values.Add(value);
    }
}
