using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fateforged.Data.Rewards;

namespace Fateforged.Data.Academy;

/// <summary>Strict JSON-backed catalog of the five professor quest stewards.</summary>
public static class AcademyProfessorCatalog
{
    private const string CatalogPath = "data/academy/professors.json";

    public static IReadOnlyList<AcademyProfessorDefinition> All { get; } = Load();

    public static AcademyProfessorDefinition? Find(ProfessorId id) =>
        All.FirstOrDefault(professor => professor.Id == id);

    private static IReadOnlyList<AcademyProfessorDefinition> Load()
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
            throw new InvalidDataException($"Academy professor catalog was not found at '{path}'.");

        var file =
            JsonSerializer.Deserialize<AcademyProfessorFile>(
                File.ReadAllText(path),
                RewardJson.Options
            ) ?? throw new InvalidDataException("Academy professor catalog was empty.");

        var errors = Validate(file.Professors);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Academy professor catalog is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"
            );
        }

        return file.Professors;
    }

    private static string ResolveCatalogPath()
    {
        var workingPath = Path.Combine(Directory.GetCurrentDirectory(), CatalogPath);
        if (File.Exists(workingPath))
            return workingPath;

        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../", CatalogPath)
        );
    }

    private static List<string> Validate(ImmutableArray<AcademyProfessorDefinition> professors)
    {
        var errors = new List<string>();
        if (professors.IsDefaultOrEmpty)
            errors.Add("At least one professor is required.");

        foreach (
            var duplicate in professors
                .GroupBy(professor => professor.Id)
                .Where(group => group.Count() > 1)
        )
            errors.Add($"Duplicate professor ID '{duplicate.Key}'.");

        foreach (var professor in professors)
        {
            if (!professor.Id.HasValue)
                errors.Add("Professor ID is required.");
            if (string.IsNullOrWhiteSpace(professor.NameKey))
                errors.Add($"Professor '{professor.Id}' requires a name key.");
            if (string.IsNullOrWhiteSpace(professor.LandmarkKey))
                errors.Add($"Professor '{professor.Id}' requires a landmark key.");
            foreach (
                var duplicate in professor
                    .CourseIds.GroupBy(id => id)
                    .Where(group => group.Count() > 1)
            )
                errors.Add($"Professor '{professor.Id}' repeats course '{duplicate.Key}'.");
        }

        return errors;
    }

    private sealed record AcademyProfessorFile
    {
        public ImmutableArray<AcademyProfessorDefinition> Professors { get; init; } = [];
    }
}
