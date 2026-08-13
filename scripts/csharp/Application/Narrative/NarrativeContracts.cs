namespace Fateforged.Application.Narrative;

using System.Collections.Generic;
using System.Collections.Immutable;

public enum NarrativeEventType
{
    PreparationOpened,
    BattleStarted,
    BattlePhaseChanged,
    PlayerCommandRejected,
    BattleEventOccurred,
    BattleResolved,
    ActivityCompleted,
    MetaMomentStarted,
}

public enum NarrativeContext
{
    Preparation,
    Battle,
    Results,
    Campus,
}

public enum NarrativeOccurrencePolicy
{
    Always,
    OncePerAttempt,
    OncePerSummoner,
    OncePerAccount,
}

public enum NarrativePlaybackMode
{
    Blocking,
}

public enum NarrativeChoiceKind
{
    Conversational,
    Consequential,
}

public readonly record struct NarrativeEvent(
    NarrativeEventType Type,
    string SourceId,
    long SourceSequence,
    IReadOnlyDictionary<string, string> Facts
);

public sealed record NarrativeCueDefinition
{
    public required string Id { get; init; }
    public required NarrativeEventType Trigger { get; init; }
    public required NarrativeContext Context { get; init; }
    public required string DialogueId { get; init; }
    public int Priority { get; init; }
    public NarrativeOccurrencePolicy Occurrence { get; init; } =
        NarrativeOccurrencePolicy.Always;
    public NarrativePlaybackMode PlaybackMode { get; init; } = NarrativePlaybackMode.Blocking;
    public ImmutableDictionary<string, string> Conditions { get; init; } =
        ImmutableDictionary<string, string>.Empty;
}

public sealed record DialogueChoiceDefinition
{
    public required string Id { get; init; }
    public required string TextKey { get; init; }
    public NarrativeChoiceKind Kind { get; init; } = NarrativeChoiceKind.Conversational;
    public string NextDialogueId { get; init; } = "";
    public NarrativeCommandRequest? Command { get; init; }
}

public sealed record DialogueContentDefinition
{
    public required string Id { get; init; }
    public string SpeakerKey { get; init; } = "";
    public ImmutableArray<string> LineKeys { get; init; } = [];
    public ImmutableArray<DialogueChoiceDefinition> Choices { get; init; } = [];
    public string EssentialUiFact { get; init; } = "";
}

public sealed record NarrativeCommandRequest
{
    public required string CommandType { get; init; }
    public required string IdempotencyKey { get; init; }
    public ImmutableDictionary<string, string> Arguments { get; init; } =
        ImmutableDictionary<string, string>.Empty;
}

public sealed record DialogueResult
{
    public required string CueId { get; init; }
    public bool Skipped { get; init; }
    public string ChoiceId { get; init; } = "";
    public NarrativeCommandRequest? Command { get; init; }
}

public interface INarrativeOccurrenceStore
{
    bool HasCompleted(string cueId, NarrativeOccurrencePolicy policy, string scopeId);
    void MarkCompleted(string cueId, NarrativeOccurrencePolicy policy, string scopeId);
}

public interface INarrativeCommandHandler
{
    bool TryHandle(NarrativeCommandRequest command);
}

public sealed record NarrativeCatalogDefinition
{
    public ImmutableArray<NarrativeCueDefinition> Cues { get; init; } = [];
    public ImmutableArray<DialogueContentDefinition> Dialogue { get; init; } = [];
}
