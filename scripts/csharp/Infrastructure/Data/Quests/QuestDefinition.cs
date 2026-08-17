using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;

namespace Fateforged.Data.Quests;

public enum QuestVisibility
{
    Announced,
    Hidden,
}

public enum QuestStepKind
{
    TalkToNpc,
    InteractWithWorldTarget,
    CompleteEncounter,
}

public sealed class QuestDefinition
{
    public string Id { get; init; } = "";

    public string TitleKey { get; init; } = "";

    public string DescriptionKey { get; init; } = "";

    public QuestVisibility Visibility { get; init; } = QuestVisibility.Announced;

    public QuestSourceDefinition Source { get; init; } = new();

    public QuestDialogueDefinition Dialogue { get; init; } = new();

    public ImmutableArray<QuestRuleDefinition> AcceptanceRequirements { get; init; } = [];

    public ImmutableArray<QuestRuleDefinition> AcceptanceEffects { get; init; } = [];

    public ImmutableArray<QuestRuleDefinition> CompletionEffects { get; init; } = [];

    public ImmutableArray<QuestStepDefinition> Steps { get; init; } = [];
}

public sealed class QuestDialogueDefinition
{
    public ImmutableArray<string> OfferLineKeys { get; init; } = [];

    public ImmutableArray<string> AcceptedLineKeys { get; init; } = [];
}

public sealed class QuestSourceDefinition
{
    public string Kind { get; init; } = "npc";

    public string Id { get; init; } = "";

    public string NameKey { get; init; } = "";

    public string LocationKey { get; init; } = "";
}

public sealed class QuestRuleDefinition
{
    public string Kind { get; init; } = "";

    public JsonElement Parameters { get; init; }
}

public sealed class QuestStepDefinition
{
    public string Id { get; init; } = "";

    public QuestStepKind Kind { get; init; }

    public string ObjectiveKey { get; init; } = "";

    public string TargetId { get; init; } = "";

    public string EncounterId { get; init; } = "";

    public string RequiredOutcome { get; init; } = "victory";

    public List<string> DialogueKeys { get; init; } = [];
}
