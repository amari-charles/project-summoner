using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fateforged.Data.Encounters;
using Fateforged.Data.Rewards;

namespace Fateforged.Data.Quests;

public static class QuestCatalog
{
    private const string CatalogPath = "data/quests/quests.json";

    public static IReadOnlyList<QuestDefinition> All { get; } = Load();

    public static QuestDefinition? Find(string id) =>
        All.FirstOrDefault(quest => string.Equals(quest.Id, id, StringComparison.Ordinal));

    private static IReadOnlyList<QuestDefinition> Load()
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
            throw new InvalidDataException($"Quest catalog was not found at '{path}'.");

        var file =
            JsonSerializer.Deserialize<QuestFile>(File.ReadAllText(path), RewardJson.Options)
            ?? throw new InvalidDataException("Quest catalog was empty.");
        var errors = Validate(file.Quests);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Quest catalog is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"
            );
        }

        return file.Quests;
    }

    private static string ResolveCatalogPath()
    {
        var workingPath = Path.Combine(Directory.GetCurrentDirectory(), CatalogPath);
        return File.Exists(workingPath)
            ? workingPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../", CatalogPath));
    }

    private static List<string> Validate(ImmutableArray<QuestDefinition> quests)
    {
        var errors = new List<string>();
        if (quests.IsDefaultOrEmpty)
            errors.Add("At least one quest is required.");

        foreach (
            var duplicate in quests.GroupBy(quest => quest.Id).Where(group => group.Count() > 1)
        )
            errors.Add($"Duplicate quest ID '{duplicate.Key}'.");

        foreach (var quest in quests)
        {
            if (string.IsNullOrWhiteSpace(quest.Id))
                errors.Add("Quest ID is required.");
            if (string.IsNullOrWhiteSpace(quest.TitleKey))
                errors.Add($"Quest '{quest.Id}' requires a title key.");
            if (string.IsNullOrWhiteSpace(quest.Source.Id))
                errors.Add($"Quest '{quest.Id}' requires a source ID.");
            if (quest.Steps.IsDefaultOrEmpty)
                errors.Add($"Quest '{quest.Id}' requires at least one step.");

            foreach (var response in quest.Dialogue.Responses)
            {
                if (
                    string.IsNullOrWhiteSpace(response.Id)
                    || string.IsNullOrWhiteSpace(response.TextKey)
                    || response.Action is not ("accept_quest" or "decline_quest")
                )
                    errors.Add($"Quest '{quest.Id}' contains an invalid dialogue response.");
            }

            foreach (
                var duplicate in quest
                    .Steps.GroupBy(step => step.Id)
                    .Where(group => group.Count() > 1)
            )
                errors.Add($"Quest '{quest.Id}' repeats step ID '{duplicate.Key}'.");

            foreach (var step in quest.Steps)
            {
                if (
                    string.IsNullOrWhiteSpace(step.Id)
                    || string.IsNullOrWhiteSpace(step.ObjectiveKey)
                )
                    errors.Add($"Quest '{quest.Id}' contains an incomplete step.");
                if (
                    step.Kind is QuestStepKind.TalkToNpc or QuestStepKind.InteractWithWorldTarget
                    && string.IsNullOrWhiteSpace(step.TargetId)
                )
                    errors.Add($"Quest '{quest.Id}' step '{step.Id}' requires a target ID.");
                if (step.Kind == QuestStepKind.CompleteEncounter)
                {
                    if (string.IsNullOrWhiteSpace(step.EncounterId))
                        errors.Add(
                            $"Quest '{quest.Id}' step '{step.Id}' requires an encounter ID."
                        );
                    else if (EncounterCatalog.Find(step.EncounterId) == null)
                        errors.Add(
                            $"Quest '{quest.Id}' step '{step.Id}' references unknown encounter '{step.EncounterId}'."
                        );
                }
            }

            foreach (
                var rule in quest
                    .AcceptanceRequirements.Concat(quest.AcceptanceEffects)
                    .Concat(quest.CompletionEffects)
            )
            {
                if (string.IsNullOrWhiteSpace(rule.Kind))
                    errors.Add($"Quest '{quest.Id}' contains a rule without a kind.");
            }
        }

        return errors;
    }

    private sealed record QuestFile
    {
        public ImmutableArray<QuestDefinition> Quests { get; init; } = [];
    }
}
