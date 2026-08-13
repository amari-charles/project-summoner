namespace Fateforged.Tests.Application;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Application.Narrative;
using Fateforged.Data.Narrative;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class NarrativeDirectorTest
{
    [TestCase]
    public void MatchingQueue_UsesPrioritySequenceAndStableId_AndCoalescesDuplicates()
    {
        var director = NewDirector(
            Cue("z_low", priority: 1),
            Cue("b_high", priority: 10),
            Cue("a_high", priority: 10)
        );
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        AssertThat(director.PendingCueCount).IsEqual(3);

        var played = new List<string>();
        director.RegisterPresenter(
            (int)NarrativeContext.Preparation,
            Callable.From<Godot.Collections.Dictionary>(cue => played.Add(cue["cue_id"].AsString()))
        );
        AssertThat(played[0]).IsEqual("a_high");
        director.CompleteCue("a_high");
        AssertThat(played[1]).IsEqual("b_high");
        director.CompleteCue("b_high");
        AssertThat(played[2]).IsEqual("z_low");
        director.Free();
    }

    [TestCase]
    public void OccurrencePolicies_ApplyAlwaysAttemptSummonerAndAccountScopes()
    {
        var occurrences = new MemoryNarrativeOccurrenceStore();
        var director = NewDirectorWithStore(
            occurrences,
            Cue("always", occurrence: NarrativeOccurrencePolicy.Always),
            Cue("attempt", occurrence: NarrativeOccurrencePolicy.OncePerAttempt),
            Cue("summoner", occurrence: NarrativeOccurrencePolicy.OncePerSummoner),
            Cue("account", occurrence: NarrativeOccurrencePolicy.OncePerAccount)
        );
        director.SetScopes("attempt-a", "summoner-a", "account-a");
        var played = new List<string>();
        director.RegisterPresenter(
            (int)NarrativeContext.Preparation,
            Callable.From<Godot.Collections.Dictionary>(cue => played.Add(cue["cue_id"].AsString()))
        );
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        CompleteAll(director);
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        CompleteAll(director);
        AssertThat(played.FindAll(id => id == "always")).HasSize(2);
        AssertThat(played.FindAll(id => id == "attempt")).HasSize(1);
        AssertThat(played.FindAll(id => id == "summoner")).HasSize(1);
        AssertThat(played.FindAll(id => id == "account")).HasSize(1);
        director.ResetAttempt();
        director.SetScopes("attempt-b", "summoner-a", "account-a");
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        CompleteAll(director);
        AssertThat(played.FindAll(id => id == "attempt")).HasSize(2);
        director.Free();
    }

    [TestCase]
    public void ChoiceBoundary_RequiresAuthoredChoice_AndEmitsConsequentialCommandOnce()
    {
        var command = new NarrativeCommandRequest
        {
            CommandType = "record_doctrine",
            IdempotencyKey = "doctrine:prepared",
        };
        var content = Content("choice");
        content = content with
        {
            Choices =
            [
                new DialogueChoiceDefinition
                {
                    Id = "prepared",
                    TextKey = "choice.prepared",
                    Kind = NarrativeChoiceKind.Consequential,
                    Command = command,
                },
            ],
        };
        var director = new NarrativeDirector();
        var handler = new TestCommandHandler();
        director.ConfigureForTesting(
            [Cue("choice_cue", dialogueId: "choice")],
            [content],
            commands: handler
        );
        var commands = 0;
        director.DialogueCommandRequested += _ => commands++;
        director.RegisterPresenter(
            (int)NarrativeContext.Preparation,
            Callable.From<Godot.Collections.Dictionary>(_ => { })
        );
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        AssertThat(director.CompleteCue("choice_cue", new Godot.Collections.Dictionary())).IsFalse();
        AssertThat(
                director.CompleteCue(
                    "choice_cue",
                    new Godot.Collections.Dictionary { ["choice_id"] = "prepared" }
                )
            )
            .IsTrue();
        AssertThat(commands).IsEqual(1);
        AssertThat(handler.Calls).IsEqual(1);
        director.Free();
    }

    [TestCase]
    public void CancelAndStaleCue_DoNotMarkOccurrenceComplete()
    {
        var isValid = true;
        var director = new NarrativeDirector();
        director.ConfigureForTesting(
            [Cue("cue", occurrence: NarrativeOccurrencePolicy.OncePerSummoner)],
            [Content("dialogue")],
            new MemoryNarrativeOccurrenceStore(),
            _ => isValid
        );
        director.RegisterPresenter(
            (int)NarrativeContext.Preparation,
            Callable.From<Godot.Collections.Dictionary>(_ => { })
        );
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        director.CancelActiveCue();
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        AssertThat(director.ActiveCueId).IsEqual("cue");
        director.CancelActiveCue();
        isValid = false;
        director.PublishEvent((int)NarrativeEventType.PreparationOpened, "activity");
        AssertThat(director.PendingCueCount).IsEqual(0);
        director.Free();
    }

    [TestCase]
    public void CatalogValidation_RejectsMissingContentInvalidChoiceAndBlockingMultiplayer()
    {
        var invalid = new NarrativeCatalogDefinition
        {
            Cues =
            [
                Cue("missing", dialogueId: "absent"),
                Cue("multiplayer") with
                {
                    Context = NarrativeContext.Battle,
                    Conditions = ImmutableDictionary<string, string>.Empty.Add("multiplayer", "true"),
                },
            ],
            Dialogue =
            [
                Content("dialogue") with
                {
                    EssentialUiFact = "deck_rule",
                    Choices =
                    [
                        new DialogueChoiceDefinition
                        {
                            Id = "bad",
                            TextKey = "choice.bad",
                            Kind = NarrativeChoiceKind.Consequential,
                        },
                    ],
                },
            ],
        };
        var errors = NarrativeCatalog.Validate(invalid);
        AssertThat(errors).HasSize(4);
    }

    [TestCase]
    public void AuthoredCatalog_IsStrictAndContainsMigratedContent()
    {
        AssertThat(NarrativeCatalog.All.Cues).IsNotEmpty();
        AssertThat(NarrativeCatalog.Validate(NarrativeCatalog.All)).IsEmpty();
        AssertThat(NarrativeCatalog.All.Dialogue.Select(content => content.Id))
            .Contains("first_trial_lesson");
    }

    private static NarrativeDirector NewDirector(params NarrativeCueDefinition[] cues) =>
        NewDirectorWithStore(new MemoryNarrativeOccurrenceStore(), cues);

    private static NarrativeDirector NewDirectorWithStore(
        INarrativeOccurrenceStore store,
        params NarrativeCueDefinition[] cues
    )
    {
        var director = new NarrativeDirector();
        director.ConfigureForTesting(cues, [Content("dialogue")], store);
        return director;
    }

    private static NarrativeCueDefinition Cue(
        string id,
        int priority = 0,
        NarrativeOccurrencePolicy occurrence = NarrativeOccurrencePolicy.Always,
        string dialogueId = "dialogue"
    ) =>
        new()
        {
            Id = id,
            Trigger = NarrativeEventType.PreparationOpened,
            Context = NarrativeContext.Preparation,
            DialogueId = dialogueId,
            Priority = priority,
            Occurrence = occurrence,
        };

    private static DialogueContentDefinition Content(string id) =>
        new() { Id = id, LineKeys = ["dialogue.test.line"] };

    private static void CompleteAll(NarrativeDirector director)
    {
        while (!string.IsNullOrEmpty(director.ActiveCueId))
            director.CompleteCue(director.ActiveCueId);
    }

    private sealed class TestCommandHandler : INarrativeCommandHandler
    {
        public int Calls { get; private set; }
        public bool TryHandle(NarrativeCommandRequest command)
        {
            Calls++;
            return command.CommandType == "record_doctrine";
        }
    }
}
