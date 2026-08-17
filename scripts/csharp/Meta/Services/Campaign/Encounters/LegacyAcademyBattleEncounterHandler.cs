using System;
using Fateforged.Data.Academy;
using Fateforged.Data.Encounters;
using Fateforged.Meta.Campaign.Handlers;
using Godot.Collections;

namespace Fateforged.Meta.Campaign.Encounters;

/// <summary>
/// Temporary adapter that lets generic encounters execute through existing
/// Academy battle machinery while that machinery is migrated and deleted.
/// </summary>
public sealed class LegacyAcademyBattleEncounterHandler : IEncounterExecutionHandler
{
    private const string MigrationPrefix = "academy:";
    private readonly AcademyProgressHandler _academy;

    public LegacyAcademyBattleEncounterHandler(AcademyProgressHandler academy)
    {
        _academy = academy;
    }

    public EncounterExecutionKind Kind => EncounterExecutionKind.Battle;

    public Dictionary GetPreparationState(EncounterDefinition encounter) =>
        TryResolve(encounter, out var courseId, out var activityId)
            ? _academy.GetActivityLaunchState(courseId, activityId)
            : [];

    public Dictionary ResolveBattleConfig(EncounterDefinition encounter) =>
        TryResolve(encounter, out var courseId, out var activityId)
            ? _academy.ResolveActivityBattleConfig(courseId, activityId)
            : [];

    public bool UpdateLoadout(EncounterDefinition encounter, Array<Dictionary> slots) =>
        TryResolve(encounter, out var courseId, out var activityId)
        && _academy.UpdateActivityLoadout(courseId, activityId, slots);

    public Dictionary FillLoadoutFromDeck(EncounterDefinition encounter, string sourceDeckId) =>
        TryResolve(encounter, out var courseId, out var activityId)
            ? _academy.FillActivityLoadoutFromDeck(courseId, activityId, sourceDeckId)
            : [];

    public Dictionary SaveLoadoutToDeck(
        EncounterDefinition encounter,
        string targetDeckId,
        string newDeckName
    ) =>
        TryResolve(encounter, out var courseId, out var activityId)
            ? _academy.SaveActivityLoadoutToDeck(courseId, activityId, targetDeckId, newDeckName)
            : [];

    public Dictionary Complete(EncounterDefinition encounter, EncounterOutcome outcome)
    {
        if (!TryResolve(encounter, out var courseId, out var activityId))
            return [];
        var academyOutcome = outcome switch
        {
            EncounterOutcome.Victory => AcademyActivityOutcome.Victory,
            EncounterOutcome.Defeat => AcademyActivityOutcome.Defeat,
            EncounterOutcome.Abandoned => AcademyActivityOutcome.Abandoned,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        return _academy.CompleteActivity(courseId, activityId, academyOutcome)
            ? _academy.GetLastCompletionSummary()
            : [];
    }

    public Dictionary ConsumeCompletionSummary() => _academy.ConsumeLastCompletionSummary();

    public Dictionary GetCompletionSummary() => _academy.GetLastCompletionSummary();

    private static bool TryResolve(
        EncounterDefinition encounter,
        out string courseId,
        out string activityId
    )
    {
        courseId = "";
        activityId = "";
        if (
            encounter.Configuration.ValueKind != System.Text.Json.JsonValueKind.Object
            || !encounter.Configuration.TryGetProperty("migration_source", out var sourceElement)
        )
            return false;
        var source = sourceElement.GetString() ?? "";
        if (!source.StartsWith(MigrationPrefix, StringComparison.Ordinal))
            return false;
        var parts = source[MigrationPrefix.Length..].Split(':');
        if (parts.Length != 2)
            return false;
        courseId = parts[0];
        activityId = parts[1];
        return !string.IsNullOrWhiteSpace(courseId) && !string.IsNullOrWhiteSpace(activityId);
    }
}
