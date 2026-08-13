namespace Fateforged.Application.Narrative;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Data.Narrative;
using Fateforged.Infrastructure.Persistence;
using Godot;

[GlobalClass]
public partial class NarrativeDirector : Node
{
    [Signal]
    public delegate void NarrativeEventAcceptedEventHandler(int eventType, string sourceId, long sourceSequence);
    [Signal]
    public delegate void CueReadyEventHandler(Godot.Collections.Dictionary cue);
    [Signal]
    public delegate void CueCancelledEventHandler(string cueId);
    [Signal]
    public delegate void CueCompletedEventHandler(string cueId);
    [Signal]
    public delegate void DialogueCommandRequestedEventHandler(Godot.Collections.Dictionary command);
    [Signal]
    public delegate void BlockingStateChangedEventHandler(int context, bool blocked);

    private sealed record QueuedCue(NarrativeCueDefinition Cue, NarrativeEvent SourceEvent);

    private readonly Dictionary<NarrativeContext, Callable> _presenters = [];
    private readonly List<QueuedCue> _queue = [];
    private readonly HashSet<string> _queuedIds = [];
    private readonly HashSet<string> _deliveredCommands = [];
    private ImmutableArray<NarrativeCueDefinition> _cues = [];
    private ImmutableDictionary<string, DialogueContentDefinition> _dialogue =
        ImmutableDictionary<string, DialogueContentDefinition>.Empty;
    private INarrativeOccurrenceStore _occurrences = new MemoryNarrativeOccurrenceStore();
    private INarrativeCommandHandler _commands = new RejectingNarrativeCommandHandler();
    private Func<NarrativeEvent, bool>? _revalidator;
    private QueuedCue? _active;
    private long _nextSourceSequence;
    private string _attemptScope = "attempt";
    private string _summonerScope = "";
    private string _accountScope = "account";

    public int PendingCueCount => _queue.Count + (_active == null ? 0 : 1);
    public string ActiveCueId => _active?.Cue.Id ?? "";

    public bool IsCueActiveOrQueued(string cueId) =>
        _active?.Cue.Id == cueId || _queuedIds.Contains(cueId);

    public int GetPendingCueCount() => PendingCueCount;

    public override void _Ready()
    {
        ConfigureCatalog(NarrativeCatalog.All);
        if (ProfileRepository.Instance is { } profiles)
        {
            _occurrences = new ProfileNarrativeOccurrenceStore(profiles);
            _summonerScope = profiles.GetProfileMetadata()?.Meta.SelectedSummoner ?? "";
            _accountScope = profiles.GetCurrentProfileId().Value;
        }
    }

    public void ConfigureForTesting(
        IEnumerable<NarrativeCueDefinition> cues,
        IEnumerable<DialogueContentDefinition> dialogue,
        INarrativeOccurrenceStore? occurrences = null,
        Func<NarrativeEvent, bool>? revalidator = null,
        INarrativeCommandHandler? commands = null
    )
    {
        _cues = cues.ToImmutableArray();
        _dialogue = dialogue.ToImmutableDictionary(item => item.Id, StringComparer.Ordinal);
        _occurrences = occurrences ?? new MemoryNarrativeOccurrenceStore();
        _revalidator = revalidator;
        _commands = commands ?? new RejectingNarrativeCommandHandler();
        ClearRuntimeState();
    }

    public void SetScopes(string attemptId, string summonerId, string accountId)
    {
        _attemptScope = string.IsNullOrWhiteSpace(attemptId) ? "attempt" : attemptId;
        _summonerScope = summonerId ?? "";
        _accountScope = string.IsNullOrWhiteSpace(accountId) ? "account" : accountId;
    }

    public string BeginAttempt(string attemptId = "")
    {
        var resolvedAttemptId = string.IsNullOrWhiteSpace(attemptId)
            ? Guid.NewGuid().ToString("N")
            : attemptId;
        if (string.Equals(_attemptScope, resolvedAttemptId, StringComparison.Ordinal))
            return resolvedAttemptId;
        if (_occurrences is MemoryNarrativeOccurrenceStore memory)
            memory.ResetAttempt();
        if (_occurrences is ProfileNarrativeOccurrenceStore profile)
            profile.ResetAttempt();
        _attemptScope = resolvedAttemptId;
        return resolvedAttemptId;
    }

    public bool PublishEvent(int eventType, string sourceId, Godot.Collections.Dictionary? facts = null)
    {
        if (!Enum.IsDefined(typeof(NarrativeEventType), eventType) || string.IsNullOrWhiteSpace(sourceId))
            return false;
        RefreshProfileScopes();
        var typedFacts = new Dictionary<string, string>(StringComparer.Ordinal);
        if (facts != null)
            foreach (var key in facts.Keys)
                typedFacts[key.AsString()] = facts[key].AsString();
        var sourceEvent = new NarrativeEvent(
            (NarrativeEventType)eventType,
            sourceId,
            _nextSourceSequence++,
            typedFacts
        );
        EmitSignal(SignalName.NarrativeEventAccepted, eventType, sourceId, sourceEvent.SourceSequence);
        foreach (var cue in _cues.Where(cue => Matches(cue, sourceEvent)))
        {
            if (_queuedIds.Contains(cue.Id) || _active?.Cue.Id == cue.Id)
                continue;
            if (_occurrences.HasCompleted(cue.Id, cue.Occurrence, ScopeFor(cue.Occurrence, sourceEvent)))
                continue;
            _queue.Add(new QueuedCue(cue, sourceEvent));
            _queuedIds.Add(cue.Id);
        }
        _queue.Sort(Compare);
        TryPresentNext();
        return true;
    }

    public void RegisterPresenter(int context, Callable presenter)
    {
        if (!Enum.IsDefined(typeof(NarrativeContext), context))
            return;
        _presenters[(NarrativeContext)context] = presenter;
        TryPresentNext();
    }

    public void UnregisterPresenter(int context)
    {
        if (Enum.IsDefined(typeof(NarrativeContext), context))
            _presenters.Remove((NarrativeContext)context);
    }

    public bool CompleteCue(string cueId, Godot.Collections.Dictionary? result = null)
    {
        if (_active == null || _active.Cue.Id != cueId)
            return false;
        var content = _dialogue[_active.Cue.DialogueId];
        var choiceId = result?.GetValueOrDefault("choice_id", "").AsString() ?? "";
        var choice = string.IsNullOrEmpty(choiceId)
            ? null
            : content.Choices.FirstOrDefault(candidate => candidate.Id == choiceId);
        if (content.Choices.Length > 0 && choice == null)
            return false;
        if (choice?.Command is { } command && !_deliveredCommands.Contains(command.IdempotencyKey))
        {
            if (!_commands.TryHandle(command))
                return false;
            _deliveredCommands.Add(command.IdempotencyKey);
            EmitSignal(SignalName.DialogueCommandRequested, ToCommandDictionary(command));
        }
        if (choice is { NextDialogueId.Length: > 0 })
        {
            var nextCue = _active.Cue with { DialogueId = choice.NextDialogueId };
            _active = new QueuedCue(nextCue, _active.SourceEvent);
            var view = ToCueDictionary(nextCue, _dialogue[nextCue.DialogueId]);
            EmitSignal(SignalName.CueReady, view);
            _presenters[nextCue.Context].Call(view);
            return true;
        }
        var completedCueId = _active.Cue.Id;
        var completedContext = _active.Cue.Context;
        _occurrences.MarkCompleted(
            _active.Cue.Id,
            _active.Cue.Occurrence,
            ScopeFor(_active.Cue.Occurrence, _active.SourceEvent)
        );
        _active = null;
        EmitSignal(SignalName.BlockingStateChanged, (int)completedContext, false);
        EmitSignal(SignalName.CueCompleted, completedCueId);
        TryPresentNext();
        return true;
    }

    public void CancelActiveCue()
    {
        if (_active == null)
            return;
        var cueId = _active.Cue.Id;
        var context = _active.Cue.Context;
        _active = null;
        EmitSignal(SignalName.BlockingStateChanged, (int)context, false);
        EmitSignal(SignalName.CueCancelled, cueId);
        TryPresentNext();
    }

    public void ResetAttempt()
    {
        _nextSourceSequence = 0;
        if (_occurrences is MemoryNarrativeOccurrenceStore memory)
            memory.ResetAttempt();
        if (_occurrences is ProfileNarrativeOccurrenceStore profile)
            profile.ResetAttempt();
        ClearRuntimeState();
    }

    private void ConfigureCatalog(NarrativeCatalogDefinition catalog)
    {
        _cues = catalog.Cues;
        _dialogue = catalog.Dialogue.ToImmutableDictionary(item => item.Id, StringComparer.Ordinal);
    }

    private void RefreshProfileScopes()
    {
        if (
            _occurrences is not ProfileNarrativeOccurrenceStore
            || ProfileRepository.Instance is not { } profiles
        )
            return;
        _summonerScope = profiles.GetProfileMetadata()?.Meta.SelectedSummoner ?? "";
        _accountScope = profiles.GetCurrentProfileId().Value;
    }

    private void TryPresentNext()
    {
        if (_active != null)
            return;
        while (_queue.Count > 0)
        {
            var next = _queue[0];
            if (!_presenters.TryGetValue(next.Cue.Context, out var presenter))
                return;
            _queue.RemoveAt(0);
            _queuedIds.Remove(next.Cue.Id);
            if ((_revalidator != null && !_revalidator(next.SourceEvent)) || !Matches(next.Cue, next.SourceEvent))
                continue;
            _active = next;
            var view = ToCueDictionary(next.Cue, _dialogue[next.Cue.DialogueId]);
            EmitSignal(SignalName.BlockingStateChanged, (int)next.Cue.Context, true);
            EmitSignal(SignalName.CueReady, view);
            presenter.Call(view);
            return;
        }
    }

    private static bool Matches(NarrativeCueDefinition cue, NarrativeEvent sourceEvent)
    {
        if (cue.Trigger != sourceEvent.Type)
            return false;
        foreach (var (key, expected) in cue.Conditions)
        {
            var actual = key == "source_id"
                ? sourceEvent.SourceId
                : sourceEvent.Facts.GetValueOrDefault(key, "");
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private string ScopeFor(NarrativeOccurrencePolicy policy, NarrativeEvent sourceEvent) =>
        policy switch
        {
            NarrativeOccurrencePolicy.OncePerAttempt => sourceEvent.Facts.GetValueOrDefault("attempt_id", _attemptScope),
            NarrativeOccurrencePolicy.OncePerSummoner => sourceEvent.Facts.GetValueOrDefault("summoner_id", _summonerScope),
            NarrativeOccurrencePolicy.OncePerAccount => _accountScope,
            _ => "",
        };

    private static int Compare(QueuedCue left, QueuedCue right)
    {
        var priority = right.Cue.Priority.CompareTo(left.Cue.Priority);
        if (priority != 0)
            return priority;
        var sequence = left.SourceEvent.SourceSequence.CompareTo(right.SourceEvent.SourceSequence);
        return sequence != 0 ? sequence : string.CompareOrdinal(left.Cue.Id, right.Cue.Id);
    }

    private static Godot.Collections.Dictionary ToCueDictionary(
        NarrativeCueDefinition cue,
        DialogueContentDefinition content
    )
    {
        var lines = new Godot.Collections.Array<string>(content.LineKeys);
        var choices = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var choice in content.Choices)
            choices.Add(new Godot.Collections.Dictionary
            {
                ["id"] = choice.Id,
                ["text_key"] = choice.TextKey,
                ["kind"] = choice.Kind.ToString(),
                ["consequential"] = choice.Kind == NarrativeChoiceKind.Consequential,
            });
        return new Godot.Collections.Dictionary
        {
            ["cue_id"] = cue.Id,
            ["context"] = cue.Context.ToString(),
            ["playback_mode"] = cue.PlaybackMode.ToString(),
            ["dialogue_id"] = content.Id,
            ["speaker_key"] = content.SpeakerKey,
            ["line_keys"] = lines,
            ["choices"] = choices,
        };
    }

    private static Godot.Collections.Dictionary ToCommandDictionary(NarrativeCommandRequest command)
    {
        var arguments = new Godot.Collections.Dictionary();
        foreach (var (key, value) in command.Arguments)
            arguments[key] = value;
        return new Godot.Collections.Dictionary
        {
            ["command_type"] = (int)command.CommandType,
            ["idempotency_key"] = command.IdempotencyKey,
            ["arguments"] = arguments,
        };
    }

    private void ClearRuntimeState()
    {
        _queue.Clear();
        _queuedIds.Clear();
        _active = null;
    }
}

public sealed class MemoryNarrativeOccurrenceStore : INarrativeOccurrenceStore
{
    private readonly HashSet<string> _completed = [];
    public bool HasCompleted(string cueId, NarrativeOccurrencePolicy policy, string scopeId) =>
        policy != NarrativeOccurrencePolicy.Always && _completed.Contains(Key(cueId, policy, scopeId));
    public void MarkCompleted(string cueId, NarrativeOccurrencePolicy policy, string scopeId)
    {
        if (policy != NarrativeOccurrencePolicy.Always)
            _completed.Add(Key(cueId, policy, scopeId));
    }
    public void ResetAttempt() => _completed.RemoveWhere(key => key.StartsWith("OncePerAttempt:", StringComparison.Ordinal));
    private static string Key(string cueId, NarrativeOccurrencePolicy policy, string scopeId) => $"{policy}:{scopeId}:{cueId}";
}

public sealed class RejectingNarrativeCommandHandler : INarrativeCommandHandler
{
    public bool TryHandle(NarrativeCommandRequest command) => false;
}
