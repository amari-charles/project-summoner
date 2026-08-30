using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;

namespace Fateforged.Data.Encounters;

public static class EncounterCatalog
{
    private const string CatalogPath = "data/encounters/encounters.json";

    public static IReadOnlyList<EncounterDefinition> All { get; } = Load();

    public static EncounterDefinition? Find(string id) =>
        All.FirstOrDefault(encounter => string.Equals(encounter.Id, id, StringComparison.Ordinal));

    private static IReadOnlyList<EncounterDefinition> Load()
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
            throw new InvalidDataException($"Encounter catalog was not found at '{path}'.");

        var file =
            JsonSerializer.Deserialize<EncounterFile>(File.ReadAllText(path), RewardJson.Options)
            ?? throw new InvalidDataException("Encounter catalog was empty.");
        var errors = Validate(file.Encounters);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Encounter catalog is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"
            );
        }

        return file.Encounters;
    }

    private static string ResolveCatalogPath()
    {
        var workingPath = Path.Combine(Directory.GetCurrentDirectory(), CatalogPath);
        return File.Exists(workingPath)
            ? workingPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../", CatalogPath));
    }

    private static List<string> Validate(ImmutableArray<EncounterDefinition> encounters)
    {
        var errors = new List<string>();
        if (encounters.IsDefaultOrEmpty)
            errors.Add("At least one encounter is required.");

        foreach (
            var duplicate in encounters
                .GroupBy(encounter => encounter.Id)
                .Where(group => group.Count() > 1)
        )
            errors.Add($"Duplicate encounter ID '{duplicate.Key}'.");

        foreach (var encounter in encounters)
        {
            if (string.IsNullOrWhiteSpace(encounter.Id))
                errors.Add("Encounter ID is required.");
            if (string.IsNullOrWhiteSpace(encounter.NameKey))
                errors.Add($"Encounter '{encounter.Id}' requires a name key.");
            if (encounter.BattleConfig == null)
                errors.Add($"Encounter '{encounter.Id}' requires battle configuration.");
            if (
                !string.IsNullOrWhiteSpace(encounter.ProgressionBattleId)
                && EventCatalog.GetEvent<BattleEventDefinition>(
                    EventId.FromString(encounter.ProgressionBattleId)
                ) == null
            )
                errors.Add(
                    $"Encounter '{encounter.Id}' references unknown progression battle '{encounter.ProgressionBattleId}'."
                );
            if (
                encounter.Loadout.Mode == EncounterDeckMode.Fixed
                && encounter.Loadout.SuppliedCards.Count == 0
            )
                errors.Add($"Fixed encounter '{encounter.Id}' requires supplied cards.");
        }

        return errors;
    }

    private sealed record EncounterFile
    {
        public ImmutableArray<EncounterDefinition> Encounters { get; init; } = [];
    }
}
