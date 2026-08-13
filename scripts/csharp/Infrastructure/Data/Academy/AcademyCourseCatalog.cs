using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fateforged.Data.Rewards;

namespace Fateforged.Data.Academy;

/// <summary>
/// Strict JSON-backed Academy course catalog. Course-owned reward offers remain
/// inline with the activity or course that earns them.
/// </summary>
public static class AcademyCourseCatalog
{
    private const string CatalogPath = "data/academy/courses.json";

    public static IReadOnlyList<AcademyCourseDefinition> All { get; } = Load();

    public static IReadOnlyList<AcademyCourseDefinition> ForSemester(int year, int semester) =>
        All.Where(course => course.Year == year && course.Semester == semester).ToArray();

    public static AcademyCourseDefinition? Find(CourseId id) =>
        All.FirstOrDefault(course => course.Id == id);

    private static IReadOnlyList<AcademyCourseDefinition> Load()
    {
        var path = ResolveCatalogPath();
        if (!File.Exists(path))
            throw new InvalidDataException($"Academy course catalog was not found at '{path}'.");

        var file =
            JsonSerializer.Deserialize<AcademyCourseFile>(
                File.ReadAllText(path),
                RewardJson.Options
            ) ?? throw new InvalidDataException("Academy course catalog was empty.");

        var errors = Validate(file.Courses);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Academy course catalog is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}"
            );
        }

        return file.Courses;
    }

    private static string ResolveCatalogPath()
    {
        var workingPath = Path.Combine(Directory.GetCurrentDirectory(), CatalogPath);
        if (File.Exists(workingPath))
            return workingPath;

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../", CatalogPath));
    }

    private static List<string> Validate(ImmutableArray<AcademyCourseDefinition> courses)
    {
        var errors = new List<string>();
        if (courses.IsDefaultOrEmpty)
            errors.Add("At least one course is required.");

        foreach (var duplicate in courses.GroupBy(course => course.Id).Where(group => group.Count() > 1))
            errors.Add($"Duplicate course ID '{duplicate.Key}'.");

        foreach (var course in courses)
        {
            if (!course.Id.HasValue)
                errors.Add("Course ID is required.");
            if (course.Activities.Count == 0)
                errors.Add($"Course '{course.Id}' requires at least one activity.");
            foreach (
                var duplicate in course.Activities
                    .GroupBy(activity => activity.Id, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
            )
                errors.Add($"Course '{course.Id}' has duplicate activity ID '{duplicate.Key}'.");

            var activityIds = course
                .Activities.Select(activity => activity.Id)
                .ToHashSet(StringComparer.Ordinal);
            for (var activityIndex = 0; activityIndex < course.Activities.Count; activityIndex++)
            {
                var activity = course.Activities[activityIndex];
                var prerequisites = course.GetActivityPrerequisites(activityIndex);
                foreach (
                    var duplicate in prerequisites
                        .GroupBy(id => id, StringComparer.Ordinal)
                        .Where(group => group.Count() > 1)
                )
                    errors.Add(
                        $"Course '{course.Id}' activity '{activity.Id}' repeats prerequisite '{duplicate.Key}'."
                    );
                foreach (var prerequisite in prerequisites)
                {
                    if (prerequisite == activity.Id)
                        errors.Add(
                            $"Course '{course.Id}' activity '{activity.Id}' cannot require itself."
                        );
                    else if (!activityIds.Contains(prerequisite))
                        errors.Add(
                            $"Course '{course.Id}' activity '{activity.Id}' references unknown prerequisite '{prerequisite}'."
                        );
                }
            }

            if (HasActivityPrerequisiteCycle(course))
                errors.Add($"Course '{course.Id}' activity prerequisites contain a cycle.");
        }

        return errors;
    }

    private static bool HasActivityPrerequisiteCycle(AcademyCourseDefinition course)
    {
        var indexById = course
            .Activities.Select((activity, index) => (activity.Id, index))
            .ToDictionary(entry => entry.Id, entry => entry.index, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);

        bool Visit(string activityId)
        {
            if (visited.Contains(activityId))
                return false;
            if (!visiting.Add(activityId))
                return true;
            if (indexById.TryGetValue(activityId, out var activityIndex))
            {
                foreach (var prerequisite in course.GetActivityPrerequisites(activityIndex))
                {
                    if (indexById.ContainsKey(prerequisite) && Visit(prerequisite))
                        return true;
                }
            }

            visiting.Remove(activityId);
            visited.Add(activityId);
            return false;
        }

        return course.Activities.Any(activity => Visit(activity.Id));
    }

    private sealed record AcademyCourseFile
    {
        public ImmutableArray<AcademyCourseDefinition> Courses { get; init; } = [];
    }
}
