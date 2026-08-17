namespace Fateforged.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
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
        AssertThat(quest.Dialogue.Responses).HasSize(1);
        AssertThat(quest.Dialogue.Responses[0].Action).IsEqual("accept_quest");

        var encounter = EncounterCatalog.Find(quest.Steps[1].EncounterId);
        AssertThat(encounter).IsNotNull();
        AssertThat(encounter!.ExecutionKind).IsEqual(EncounterExecutionKind.Battle);
    }

    [TestCase]
    public void IntroductionQuest_CanAdoptExistingAcademyEnrollmentWithoutChargingTwice()
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId("generic_quest_existing_enrollment"));
        repo.ResetProfile();
        if (!repo.IsSummonerUnlocked(SummonerIds.Cole))
            repo.UnlockSummoner(SummonerIds.Cole);

        var campaign = CreateNode<CampaignService>();
        campaign.InitForTesting(repo);
        campaign.SetActiveSummonerGetter(Callable.From(() => (string)SummonerIds.Cole));

        AssertThat(
                campaign.EnrollAcademyCourse(
                    (string)Fateforged.Data.Academy.CourseIds.IntroductionToMagic101
                )
            )
            .IsTrue();
        AssertThat(repo.GetCampaignProgress(SummonerIds.Cole).Academy.RemainingEnrollments)
            .IsEqual(2);

        AssertThat(campaign.AcceptQuest("introduction_to_magic")).IsTrue();
        AssertThat(repo.GetCampaignProgress(SummonerIds.Cole).Academy.RemainingEnrollments)
            .IsEqual(2);
        var active = campaign.GetGenericQuestJournalState()["active"].AsGodotArray();
        AssertThat(active).HasSize(1);
        AssertThat(active[0].AsGodotDictionary()["current_target_id"].AsString())
            .IsEqual("practice_grounds");
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
        AssertThat(completionSummary["granted_rewards"].AsGodotArray()).IsEmpty();
        AssertThat(repo.GetCardCount(CardIds.MagicBolt)).IsEqual(0);

        var npcState = campaign.GetNpcQuestState("general_magic");
        AssertThat(npcState["quest_marker"].AsString()).IsEqual("?");
        var completion = campaign.RecordQuestNpcInteraction("general_magic");
        AssertThat(completion["completed"].AsBool()).IsTrue();
        var questCompletionRewards = campaign
            .GetLastAcademyCompletionSummary()["granted_rewards"]
            .AsGodotArray();
        AssertThat(questCompletionRewards).HasSize(1);
        AssertThat(questCompletionRewards[0].AsGodotDictionary()["card_id"].AsString())
            .IsEqual((string)CardIds.MagicBolt);
        AssertThat(repo.GetCardCount(CardIds.MagicBolt)).IsEqual(1);

        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        AssertThat(progress.Quests.CompletedQuestIds).Contains("introduction_to_magic");
        AssertThat(progress.Academy.CompletedCourses)
            .Contains(Fateforged.Data.Academy.CourseIds.IntroductionToMagic101);

        var followUpState = campaign.GetNpcQuestState("general_magic");
        var followUpOpportunities = followUpState["opportunities"].AsGodotArray();
        AssertThat(followUpOpportunities).HasSize(2);
        AssertThat(
                followUpOpportunities
                    .Select(value => value.AsGodotDictionary()["id"].AsString())
                    .ToArray()
            )
            .Contains("summoning_basics")
            .Contains("practical_spellcraft");
    }

    [TestCase]
    public void FoundationFocus_UnlocksAfterIntroductionAndCommitsToOneExclusivePath()
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId("generic_quest_foundation_focus"));
        repo.ResetProfile();
        if (!repo.IsSummonerUnlocked(SummonerIds.Cole))
            repo.UnlockSummoner(SummonerIds.Cole);

        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        progress.Quests.CompletedQuestIds.Add("introduction_to_magic");
        progress.Academy.CompletedCourses.Add(
            Fateforged.Data.Academy.CourseIds.IntroductionToMagic101
        );
        progress.Academy.RemainingEnrollments = 2;
        repo.UpdateCampaignProgress(SummonerIds.Cole, progress);

        var campaign = CreateNode<CampaignService>();
        campaign.InitForTesting(repo);
        campaign.SetActiveSummonerGetter(Callable.From(() => (string)SummonerIds.Cole));

        AssertThat(campaign.AcceptQuest("summoning_basics")).IsTrue();
        AssertThat(campaign.AcceptQuest("practical_spellcraft")).IsFalse();

        var npcState = campaign.GetNpcQuestState("general_magic");
        AssertThat(npcState["opportunities"].AsGodotArray()).IsEmpty();
        var active = npcState["active"].AsGodotArray();
        AssertThat(active).HasSize(1);
        AssertThat(active[0].AsGodotDictionary()["id"].AsString()).IsEqual("summoning_basics");

        var worldResult = campaign.RecordQuestWorldInteraction("practice_grounds");
        AssertThat(worldResult["current_step"].AsGodotDictionary()["encounter_id"].AsString())
            .IsEqual("summoning_basics_practice");
        AssertThat(campaign.GetEncounterPreparationState("summoning_basics_practice")).IsNotEmpty();

        var practiceRetry = campaign.RecordQuestWorldInteraction("practice_grounds");
        AssertThat(practiceRetry["advanced"].AsBool()).IsFalse();
        AssertThat(practiceRetry["current_step"].AsGodotDictionary()["encounter_id"].AsString())
            .IsEqual("summoning_basics_practice");

        AssertThat(
                campaign.CompleteEncounter(
                    "summoning_basics_practice",
                    (int)EncounterOutcome.Victory
                )
            )
            .IsNotEmpty();
        var assessmentLaunch = campaign.RecordQuestWorldInteraction("practice_grounds");
        AssertThat(assessmentLaunch["current_step"].AsGodotDictionary()["encounter_id"].AsString())
            .IsEqual("summoning_basics_assessment");

        var assessmentRetry = campaign.RecordQuestWorldInteraction("practice_grounds");
        AssertThat(assessmentRetry["advanced"].AsBool()).IsFalse();
        AssertThat(assessmentRetry["current_step"].AsGodotDictionary()["encounter_id"].AsString())
            .IsEqual("summoning_basics_assessment");

        var assessmentSummary = campaign.CompleteEncounter(
            "summoning_basics_assessment",
            (int)EncounterOutcome.Victory
        );
        AssertThat(assessmentSummary["granted_rewards"].AsGodotArray()).IsEmpty();
        AssertThat(assessmentSummary["completed_course"].AsBool()).IsFalse();
        AssertThat(repo.GetCardCount(CardIds.FireWisp)).IsEqual(0);

        var turnIn = campaign.RecordQuestNpcInteraction("general_magic");
        AssertThat(turnIn["completed"].AsBool()).IsTrue();
        var questCompletionSummary = campaign.GetLastAcademyCompletionSummary();
        var rewards = questCompletionSummary["granted_rewards"].AsGodotArray();
        AssertThat(rewards).HasSize(1);
        AssertThat(rewards[0].AsGodotDictionary()["card_id"].AsString())
            .IsEqual((string)CardIds.FireWisp);
        AssertThat(repo.GetCardCount(CardIds.FireWisp)).IsEqual(1);
        AssertThat(repo.GetCampaignProgress(SummonerIds.Cole).Quests.CompletedQuestIds)
            .Contains("summoning_basics");
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var node = new T();
        _createdNodes.Add(node);
        return node;
    }
}
