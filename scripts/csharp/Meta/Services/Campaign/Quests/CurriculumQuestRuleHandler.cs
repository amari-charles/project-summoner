using System;
using System.Text.Json;
using Fateforged.Data.Quests;
using Godot.Collections;

namespace Fateforged.Meta.Campaign.Quests;

/// <summary>
/// Academy-specific adapter registered at the generic quest-rule boundary.
/// QuestProgressHandler does not reference this type or its rule kinds.
/// </summary>
public sealed class CurriculumQuestRuleHandler : IQuestRuleHandler
{
    public const string CommitKind = "commit_curriculum_capacity";
    public const string CreditKind = "record_academic_credit";

    private readonly string _kind;
    private readonly Func<string, bool> _applyCourseEffect;
    private readonly Func<string, bool> _canApplyCourseEffect;

    public CurriculumQuestRuleHandler(
        string kind,
        Func<string, bool> canApplyCourseEffect,
        Func<string, bool> applyCourseEffect
    )
    {
        if (kind is not CommitKind and not CreditKind)
            throw new ArgumentOutOfRangeException(nameof(kind));
        _kind = kind;
        _canApplyCourseEffect = canApplyCourseEffect;
        _applyCourseEffect = applyCourseEffect;
    }

    public string Kind => _kind;

    public bool CanApply(QuestRuleDefinition rule) =>
        TryGetCourseId(rule.Parameters, out var courseId) && _canApplyCourseEffect(courseId);

    public bool Apply(QuestRuleDefinition rule) =>
        TryGetCourseId(rule.Parameters, out var courseId) && _applyCourseEffect(courseId);

    public Dictionary GetPreview(QuestRuleDefinition rule)
    {
        if (!TryGetCourseId(rule.Parameters, out var courseId))
            return [];

        var amount = 0;
        if (
            rule.Parameters.TryGetProperty("amount", out var amountElement)
            && amountElement.ValueKind == JsonValueKind.Number
        )
            amount = amountElement.GetInt32();

        return new Dictionary
        {
            ["kind"] = Kind,
            ["course_id"] = courseId,
            ["amount"] = amount,
            ["is_permanent"] = true,
        };
    }

    private static bool TryGetCourseId(JsonElement parameters, out string courseId)
    {
        courseId = "";
        if (
            parameters.ValueKind != JsonValueKind.Object
            || !parameters.TryGetProperty("course_id", out var courseElement)
            || courseElement.ValueKind != JsonValueKind.String
        )
            return false;
        courseId = courseElement.GetString() ?? "";
        return !string.IsNullOrWhiteSpace(courseId);
    }
}
