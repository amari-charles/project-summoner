namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Summoner;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SummonerProgressionServiceTest
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
    public void GetXpForLevel_Thresholds_AreStableForSharedCoreWiring()
    {
        AssertThat(SummonerProgressionService.GetXpForLevel(1)).IsEqual(0);
        AssertThat(SummonerProgressionService.GetXpForLevel(2)).IsEqual(100);
        AssertThat(SummonerProgressionService.GetXpForLevel(3)).IsEqual(250);
    }

    [TestCase]
    public void GetSummonerProgressionInfo_ExposesUiContractFieldsWithExpectedValues()
    {
        var repo = CreateRepo("summoner_progression_contract_fields");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 1;
        summoner.Xp = 60;
        summoner.UnspentTraitPoints = 0;
        AssertThat(repo.SaveSummonerInstance(summoner)).IsTrue();

        var info = service.GetSummonerProgressionInfo(summonerId);
        AssertThat(info.ContainsKey("xp")).IsTrue();
        AssertThat(info.ContainsKey("xp_for_next_level")).IsTrue();
        AssertThat(info.ContainsKey("xp_progress")).IsTrue();

        AssertThat(info["xp"].AsInt32()).IsEqual(60);
        AssertThat(info["xp_for_next_level"].AsInt32()).IsEqual(100);

        var progress = (float)info["xp_progress"].AsDouble();
        AssertThat(Math.Abs(progress - 0.6f)).IsLess(0.001f);
    }

    [TestCase]
    public void SummonerThresholdPolicy_LevelToLevelCost_IsPreserved()
    {
        var repo = CreateRepo("summoner_progression_contract_thresholds");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 2;
        summoner.Xp = 0;
        AssertThat(repo.SaveSummonerInstance(summoner)).IsTrue();

        var info = service.GetSummonerProgressionInfo(summonerId);
        AssertThat(info["xp_for_next_level"].AsInt32()).IsEqual(150);
        AssertThat(info["xp_to_next_level"].AsInt32()).IsEqual(150);
    }

    [TestCase]
    public void SummonerMaxLevel_NoOp_AndUiContractRemainStable()
    {
        var repo = CreateRepo("summoner_progression_contract_max");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = SummonerProgressionService.MaxLevel;
        summoner.Xp = 999;
        summoner.UnspentTraitPoints = 3;
        AssertThat(repo.SaveSummonerInstance(summoner)).IsTrue();

        AssertThat(service.GrantSummonerXp(summonerId, 100)).IsEqual(999);

        var info = service.GetSummonerProgressionInfo(summonerId);
        AssertThat(info["xp_for_next_level"].AsInt32()).IsEqual(0);
        AssertThat((float)info["xp_progress"].AsDouble()).IsEqual(1f);

        var updated = repo.GetSummonerInstance(summonerId);
        AssertThat(updated).IsNotNull();
        AssertThat(updated!.Level).IsEqual(SummonerProgressionService.MaxLevel);
        AssertThat(updated.Xp).IsEqual(999);
        AssertThat(updated.UnspentTraitPoints).IsEqual(3);
    }

    private ProfileRepository CreateRepo(string profileId)
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId(profileId));
        repo.ResetProfile();
        return repo;
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var node = new T { Name = $"{typeof(T).Name}_SummonerProgressionTest_{Guid.NewGuid():N}" };
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }

    private static SummonerId EnsureUnlockedSummoner(IProfileRepository repo, SummonerId candidate)
    {
        if (!repo.IsSummonerUnlocked(candidate))
            repo.UnlockSummoner(candidate);

        if (repo.GetSummonerInstance(candidate) == null)
        {
            repo.SaveSummonerInstance(new SummonerInstance { SummonerId = candidate });
        }

        return candidate;
    }
}
