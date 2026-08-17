namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using Fateforged.Data.Encounters;
using Fateforged.Data.Quests;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class GenericQuestEncounterFoundationTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (var i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (GodotObject.IsInstanceValid(node))
                node.Free();
        }
        _createdNodes.Clear();
    }

    [TestCase]
    public void IntroductionQuest_UsesGenericTypedStepsAndEncounterReference()
    {
        var quest = QuestCatalog.Find("introduction_to_magic");

        AssertThat(quest).IsNotNull();
        AssertThat(quest!.Source.Id).IsEqual("general_magic");
        AssertThat(quest.Steps).HasSize(3);
        AssertThat(quest.Steps[0].Kind).IsEqual(QuestStepKind.InteractWithWorldTarget);
        AssertThat(quest.Steps[0].TargetId).IsEqual("practice_grounds");
        AssertThat(quest.Steps[1].Kind).IsEqual(QuestStepKind.CompleteEncounter);
        AssertThat(quest.Steps[1].EncounterId).IsEqual("intro_summoning_practice");
        AssertThat(quest.Steps[2].Kind).IsEqual(QuestStepKind.TalkToNpc);

        var encounter = EncounterCatalog.Find(quest.Steps[1].EncounterId);
        AssertThat(encounter).IsNotNull();
        AssertThat(encounter!.ExecutionKind).IsEqual(EncounterExecutionKind.Battle);
    }

    [TestCase]
    public void QuestProgress_RoundTripsWithoutAcademyState()
    {
        var progress = new CampaignProgress
        {
            Quests = new QuestProgress
            {
                DiscoveredQuestIds = ["introduction_to_magic"],
                ActiveQuestIds = ["introduction_to_magic"],
                CurrentStepByQuestId = new Dictionary<string, int>
                {
                    ["introduction_to_magic"] = 1,
                },
                TrackedQuestId = "introduction_to_magic",
            },
        };

        var restored = DtoConverters.FromCampaignDict(DtoConverters.ToDict(progress));

        AssertThat(restored).IsNotNull();
        AssertThat(restored!.Quests.ActiveQuestIds).Contains("introduction_to_magic");
        AssertThat(restored.Quests.CurrentStepByQuestId["introduction_to_magic"]).IsEqual(1);
        AssertThat(restored.Quests.TrackedQuestId).IsEqual("introduction_to_magic");
        AssertThat(restored.Academy.EnrolledCourses).IsEmpty();
    }

    [TestCase]
    public void IntroductionQuest_AdvancesThroughWorldEncounterAndNpcEvents()
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId("generic_quest_intro_flow"));
        repo.ResetProfile();
        if (!repo.IsSummonerUnlocked(SummonerIds.Cole))
            repo.UnlockSummoner(SummonerIds.Cole);

        var campaign = CreateNode<CampaignService>();
        campaign.InitForTesting(repo);
        campaign.SetActiveSummonerGetter(Callable.From(() => (string)SummonerIds.Cole));

        var initial = campaign.GetGenericQuestJournalState();
        AssertThat(initial["opportunities"].AsGodotArray()).HasSize(1);
        AssertThat(campaign.AcceptQuest("introduction_to_magic")).IsTrue();

        var accepted = campaign.GetGenericQuestJournalState();
        var active = accepted["active"].AsGodotArray()[0].AsGodotDictionary();
        AssertThat(active["current_step_kind"].AsString()).IsEqual("interact_with_world_target");
        AssertThat(active["current_target_id"].AsString()).IsEqual("practice_grounds");
        AssertThat(repo.GetCampaignProgress(SummonerIds.Cole).Academy.RemainingEnrollments)
            .IsEqual(2);

        var worldResult = campaign.RecordQuestWorldInteraction("practice_grounds");
        AssertThat(worldResult["advanced"].AsBool()).IsTrue();
        AssertThat(worldResult["current_step"].AsGodotDictionary()["encounter_id"].AsString())
            .IsEqual("intro_summoning_practice");

        var preparation = campaign.GetEncounterPreparationState("intro_summoning_practice");
        AssertThat(preparation["encounter_id"].AsString()).IsEqual("intro_summoning_practice");
        AssertThat(preparation["execution_kind"].AsString()).IsEqual("battle");
        AssertThat(campaign.ResolveEncounterBattleConfig("intro_summoning_practice")).IsNotEmpty();

        AssertThat(campaign.TrackQuest("")).IsTrue();
        AssertThat(campaign.TrackQuest("introduction_to_magic")).IsTrue();

        var completionSummary = campaign.CompleteEncounter(
            "intro_summoning_practice",
            (int)EncounterOutcome.Victory
        );
        AssertThat(completionSummary["encounter_id"].AsString())
            .IsEqual("intro_summoning_practice");
        AssertThat(
                campaign
                    .GetEncounterCompletionSummary("intro_summoning_practice")["encounter_id"]
                    .AsString()
            )
            .IsEqual("intro_summoning_practice");

        var npcState = campaign.GetNpcQuestState("general_magic");
        AssertThat(npcState["quest_marker"].AsString()).IsEqual("?");
        var completion = campaign.RecordQuestNpcInteraction("general_magic");
        AssertThat(completion["completed"].AsBool()).IsTrue();

        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        AssertThat(progress.Quests.CompletedQuestIds).Contains("introduction_to_magic");
        AssertThat(progress.Academy.CompletedCourses)
            .Contains(Fateforged.Data.Academy.CourseIds.IntroductionToMagic101);
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var node = new T();
        _createdNodes.Add(node);
        return node;
    }
}
