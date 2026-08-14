namespace Fateforged.Tests.Meta.Progression;

using System;
using System.Collections.Generic;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Domain.Progression;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Progression;
using Fateforged.Meta.Summoner;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class ProgressionAuthorityServiceTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void BeginCampaignBattleAttempt_UsesResolvedSummonerWhenPersistedSelectionIsEmpty()
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId($"progression_authority_resolved_summoner_{Guid.NewGuid():N}"));
        repo.ResetProfile();
        repo.SaveSummonerInstance(new SummonerInstance { SummonerId = SummonerIds.Mei });

        AssertThat(repo.GetProfileMetadata()!.Meta.SelectedSummoner).IsEmpty();

        var selection = CreateNode<SummonerSelectionService>();
        selection.InitForTesting(repo);
        AssertThat(selection.GetActiveSummonerId()).IsEqual((string)SummonerIds.Mei);

        var authority = new RecordingAuthority();
        var service = CreateNode<ProgressionAuthorityService>();
        service.InitForTesting(authority);

        service.BeginCampaignBattleAttempt(CampaignIds.TestArena, new BattleId("debug_arena"));

        AssertThat(authority.LastStartRequest).IsNotNull();
        AssertThat(authority.LastStartRequest!.SummonerId).IsEqual(SummonerIds.Mei);
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var node = new T { Name = $"{typeof(T).Name}_{Guid.NewGuid():N}" };
        tree.Root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }

    private sealed class RecordingAuthority : IProgressionAuthority
    {
        public StartBattleAttemptRequest? LastStartRequest { get; private set; }

        public ProgressionAuthorityResult StartBattleAttempt(StartBattleAttemptRequest request)
        {
            LastStartRequest = request;
            return ProgressionAuthorityResult.Unavailable("stub");
        }

        public ProgressionAuthorityResult CompleteBattleAttempt(
            CompleteBattleAttemptRequest request
        ) => ProgressionAuthorityResult.Unavailable("stub");

        public ProgressionAuthorityResult GetBattleRewards(BattleAttemptId attemptId) =>
            ProgressionAuthorityResult.Unavailable("stub");

        public ProgressionAuthorityResult GetPendingBattleRewards(SummonerId summonerId) =>
            ProgressionAuthorityResult.Unavailable("stub");

        public ProgressionAuthorityResult ClaimBattleReward(BattleRewardClaimRequest request) =>
            ProgressionAuthorityResult.Unavailable("stub");
    }
}
