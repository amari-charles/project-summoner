using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Quests;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Infrastructure.Persistence;
using Godot;
using Godot.Collections;

namespace Fateforged.Meta.Campaign.Quests;

public sealed class QuestProgressHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly Func<SummonerId> _getActiveSummoner;
    private readonly QuestRuleRegistry _rules;
    private readonly IReadOnlyList<QuestDefinition> _catalog;

    public QuestProgressHandler(
        IProfileRepository profileRepo,
        Func<SummonerId> getActiveSummoner,
        QuestRuleRegistry rules,
        IReadOnlyList<QuestDefinition>? catalog = null
    )
    {
        _profileRepo = profileRepo;
        _getActiveSummoner = getActiveSummoner;
        _rules = rules;
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
            || definition.AcceptanceRequirements.Any(rule => !_rules.CanApply(rule))
        )
            return false;
        if (definition.AcceptanceEffects.Any(rule => !_rules.CanApply(rule)))
            return false;
        foreach (var effect in definition.AcceptanceEffects)
        {
            if (!_rules.Apply(effect))
                return false;
        }

        // Rule handlers may commit profile state. Reload before applying quest state
        // so their changes cannot be overwritten by a stale object graph.
        progress = GetProgress();
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

    public Dictionary RecordWorldInteraction(string targetId) =>
        AdvanceMatchingStep(
            step =>
                step.Kind == QuestStepKind.InteractWithWorldTarget
                && string.Equals(step.TargetId, targetId, StringComparison.Ordinal),
            "world_interaction"
        );

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

    private bool IsAvailable(QuestDefinition definition, CampaignProgress progress)
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
            var step = definition.Steps[stepIndex];
            if (!matches(step))
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

            foreach (var effect in definition.CompletionEffects)
            {
                if (!_rules.CanApply(effect) || !_rules.Apply(effect))
                    return [];
            }
            progress = GetProgress();
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
            };
        }

        return [];
    }

    private Dictionary ToEntry(QuestDefinition definition, CampaignProgress progress, string state)
    {
        var stepIndex = progress.Quests.CurrentStepByQuestId.GetValueOrDefault(definition.Id, 0);
        QuestStepDefinition? step =
            state == "active" && stepIndex >= 0 && stepIndex < definition.Steps.Length
                ? definition.Steps[stepIndex]
                : null;
        var previews = new Array<Dictionary>();
        foreach (var rule in definition.AcceptanceEffects)
        {
            var preview = _rules.GetPreview(rule);
            if (preview.Count > 0)
                previews.Add(preview);
        }

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
            ["acceptance_previews"] = previews,
        };
    }

    private static Dictionary ToStep(QuestStepDefinition step) =>
        new()
        {
            ["id"] = step.Id,
            ["kind"] = step.Kind.ToString().ToSnakeCase(),
            ["objective_key"] = step.ObjectiveKey,
            ["target_id"] = step.TargetId,
            ["encounter_id"] = step.EncounterId,
        };

    private static Array<string> ToArray(IEnumerable<string> values)
    {
        var result = new Array<string>();
        foreach (var value in values)
            result.Add(value);
        return result;
    }

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
        _catalog.FirstOrDefault(quest =>
            string.Equals(quest.Id, questId, StringComparison.Ordinal)
        );

    private CampaignProgress GetProgress() =>
        _profileRepo.GetCampaignProgress(_getActiveSummoner());

    private void Save(CampaignProgress progress) =>
        _profileRepo.UpdateCampaignProgress(_getActiveSummoner(), progress);

    private static void AddOnce(List<string> values, string value)
    {
        if (!values.Contains(value))
            values.Add(value);
    }
}
