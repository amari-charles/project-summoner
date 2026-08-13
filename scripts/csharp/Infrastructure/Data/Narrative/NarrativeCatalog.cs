namespace Fateforged.Data.Narrative;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fateforged.Application.Narrative;

public static class NarrativeCatalog
{
    private const string CatalogPath = "data/narrative/narrative.json";

    public static NarrativeCatalogDefinition All { get; } = Load();

    public static NarrativeCatalogDefinition LoadFromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        var catalog = JsonSerializer.Deserialize<NarrativeCatalogDefinition>(json, options)
            ?? throw new InvalidDataException("Narrative catalog was empty.");
        var errors = Validate(catalog);
        if (errors.Count > 0)
            throw new InvalidDataException(
                $"Narrative catalog is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"
            );
        return catalog;
    }

    public static IReadOnlyList<string> Validate(NarrativeCatalogDefinition catalog)
    {
        var errors = new List<string>();
        foreach (var duplicate in catalog.Cues.GroupBy(c => c.Id).Where(g => g.Count() > 1))
            errors.Add($"Duplicate cue ID '{duplicate.Key}'.");
        foreach (var duplicate in catalog.Dialogue.GroupBy(c => c.Id).Where(g => g.Count() > 1))
            errors.Add($"Duplicate dialogue ID '{duplicate.Key}'.");
        var dialogueIds = catalog.Dialogue.Select(content => content.Id).ToHashSet();
        foreach (var cue in catalog.Cues)
        {
            if (string.IsNullOrWhiteSpace(cue.Id))
                errors.Add("Cue ID is required.");
            if (!dialogueIds.Contains(cue.DialogueId))
                errors.Add($"Cue '{cue.Id}' references missing dialogue '{cue.DialogueId}'.");
            if (cue.Context == NarrativeContext.Battle && cue.PlaybackMode == NarrativePlaybackMode.Blocking
                && cue.Conditions.GetValueOrDefault("multiplayer") == "true")
                errors.Add($"Cue '{cue.Id}' cannot block multiplayer.");
        }
        foreach (var content in catalog.Dialogue)
        {
            if (content.LineKeys.IsDefaultOrEmpty)
                errors.Add($"Dialogue '{content.Id}' requires at least one localized line key.");
            if (!string.IsNullOrWhiteSpace(content.EssentialUiFact))
            {
                var referencingCues = catalog.Cues.Where(cue => cue.DialogueId == content.Id);
                if (!referencingCues.Any() || referencingCues.Any(cue =>
                    cue.Conditions.GetValueOrDefault("ui_fact") != content.EssentialUiFact
                ))
                    errors.Add(
                        $"Essential dialogue '{content.Id}' requires matching UI fact '{content.EssentialUiFact}'."
                    );
            }
            foreach (var choice in content.Choices)
            {
                if (choice.Kind == NarrativeChoiceKind.Consequential && choice.Command == null)
                    errors.Add($"Consequential choice '{content.Id}:{choice.Id}' requires a command.");
                if (choice.Command is { } command && string.IsNullOrWhiteSpace(command.IdempotencyKey))
                    errors.Add($"Choice '{content.Id}:{choice.Id}' requires an idempotency key.");
            }
        }
        return errors;
    }

    private static NarrativeCatalogDefinition Load()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), CatalogPath);
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../", CatalogPath));
        if (!File.Exists(path))
            throw new InvalidDataException($"Narrative catalog was not found at '{path}'.");
        return LoadFromJson(File.ReadAllText(path));
    }
}
