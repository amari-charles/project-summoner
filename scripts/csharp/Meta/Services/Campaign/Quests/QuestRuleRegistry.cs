using System;
using System.Collections.Generic;
using Fateforged.Data.Quests;
using GdDict = Godot.Collections.Dictionary;

namespace Fateforged.Meta.Campaign.Quests;

public sealed class QuestRuleRegistry
{
    private readonly Dictionary<string, IQuestRuleHandler> _handlers = new(StringComparer.Ordinal);

    public void Register(IQuestRuleHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (string.IsNullOrWhiteSpace(handler.Kind))
            throw new ArgumentException("Quest rule handler kind is required.", nameof(handler));
        if (!_handlers.TryAdd(handler.Kind, handler))
            throw new InvalidOperationException(
                $"A quest rule handler is already registered for '{handler.Kind}'."
            );
    }

    public bool CanApply(QuestRuleDefinition rule) =>
        _handlers.TryGetValue(rule.Kind, out var handler) && handler.CanApply(rule);

    public bool Apply(QuestRuleDefinition rule) =>
        _handlers.TryGetValue(rule.Kind, out var handler) && handler.Apply(rule);

    public GdDict GetPreview(QuestRuleDefinition rule) =>
        _handlers.TryGetValue(rule.Kind, out var handler) ? handler.GetPreview(rule) : [];
}
