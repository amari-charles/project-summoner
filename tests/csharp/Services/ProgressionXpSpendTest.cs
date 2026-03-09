namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Cards.Handlers;
using Fateforged.Meta.Summoner;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class ProgressionXpSpendTest
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
    public void CardLevelUp_ConsumesXpAndStopsWhenLeftoverIsInsufficient()
    {
        var repo = CreateRepo("progression_xp_spend_card_single");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = CardInstanceId.FromString(cardService.GrantCard(CardIds.FireWisp, "common"));
        AssertThat(instanceId).IsNotEqual(CardInstanceId.None);
        AssertThat(repo.UpdateCard(instanceId, new CardUpdate
        {
            Level = 1,
            Xp = 30,
            UnspentTraitPoints = 0
        })).IsTrue();

        var progression = new CardProgressionHandler(repo);
        AssertThat(progression.CanLevelUp(instanceId)).IsTrue();
        AssertThat(progression.LevelUpCard(instanceId)).IsTrue();
        AssertThat(progression.CanLevelUp(instanceId)).IsFalse();

        var card = repo.GetCard(instanceId);
        AssertThat(card).IsNotNull();
        AssertThat(card!.Level).IsEqual(2);
        AssertThat(card.Xp).IsEqual(0);
        AssertThat(card.UnspentTraitPoints).IsEqual(1);
    }

    [TestCase]
    public void CardLevelUp_CarriesOverExcessXpAcrossMultipleLevels()
    {
        var repo = CreateRepo("progression_xp_spend_card_multi");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = CardInstanceId.FromString(cardService.GrantCard(CardIds.FireWisp, "common"));
        AssertThat(instanceId).IsNotEqual(CardInstanceId.None);
        AssertThat(repo.UpdateCard(instanceId, new CardUpdate
        {
            Level = 1,
            Xp = 80,
            UnspentTraitPoints = 0
        })).IsTrue();

        var progression = new CardProgressionHandler(repo);
        AssertThat(progression.LevelUpCard(instanceId)).IsTrue(); // -30 XP => 50
        AssertThat(progression.LevelUpCard(instanceId)).IsTrue(); // -45 XP => 5
        AssertThat(progression.CanLevelUp(instanceId)).IsFalse();

        var card = repo.GetCard(instanceId);
        AssertThat(card).IsNotNull();
        AssertThat(card!.Level).IsEqual(3);
        AssertThat(card.Xp).IsEqual(5);
        AssertThat(card.UnspentTraitPoints).IsEqual(2);
    }

    [TestCase]
    public void SummonerLevelUp_ConsumesXpAndCarriesOverRemainder()
    {
        var repo = CreateRepo("progression_xp_spend_summoner");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 1;
        summoner.Xp = 260;
        summoner.UnspentTraitPoints = 0;
        AssertThat(repo.SaveSummonerInstance(summoner)).IsTrue();

        AssertThat(service.LevelUpSummoner(summonerId)).IsTrue(); // -100 XP => 160
        AssertThat(service.LevelUpSummoner(summonerId)).IsTrue(); // -150 XP => 10
        AssertThat(service.CanLevelUp(summonerId)).IsFalse();

        var updated = repo.GetSummonerInstance(summonerId);
        AssertThat(updated).IsNotNull();
        AssertThat(updated!.Level).IsEqual(3);
        AssertThat(updated.Xp).IsEqual(10);
        AssertThat(updated.UnspentTraitPoints).IsEqual(2);
    }

    [TestCase]
    public void SummonerLevelUp_SingleLevel_ConsumesExactXpAndGrantsOneTraitPoint()
    {
        var repo = CreateRepo("progression_xp_spend_summoner_single");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 1;
        summoner.Xp = 100;
        summoner.UnspentTraitPoints = 0;
        AssertThat(repo.SaveSummonerInstance(summoner)).IsTrue();

        AssertThat(service.LevelUpSummoner(summonerId)).IsTrue();
        AssertThat(service.LevelUpSummoner(summonerId)).IsFalse();

        var updated = repo.GetSummonerInstance(summonerId);
        AssertThat(updated).IsNotNull();
        AssertThat(updated!.Level).IsEqual(2);
        AssertThat(updated.Xp).IsEqual(0);
        AssertThat(updated.UnspentTraitPoints).IsEqual(1);
    }

    [TestCase]
    public void CardLevelUp_FailurePaths_DoNotMutateStateOrGrantTraitPoints()
    {
        var repo = CreateRepo("progression_xp_spend_card_failures");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = CardInstanceId.FromString(cardService.GrantCard(CardIds.FireWisp, "common"));
        AssertThat(instanceId).IsNotEqual(CardInstanceId.None);
        AssertThat(repo.UpdateCard(instanceId, new CardUpdate
        {
            Level = 1,
            Xp = 29,
            UnspentTraitPoints = 0
        })).IsTrue();

        var progression = new CardProgressionHandler(repo);
        AssertThat(progression.LevelUpCard(instanceId)).IsFalse();

        var beforeMax = repo.GetCard(instanceId);
        AssertThat(beforeMax).IsNotNull();
        AssertThat(beforeMax!.Level).IsEqual(1);
        AssertThat(beforeMax.Xp).IsEqual(29);
        AssertThat(beforeMax.UnspentTraitPoints).IsEqual(0);

        AssertThat(repo.UpdateCard(instanceId, new CardUpdate
        {
            Level = CardProgressionHandler.MaxLevel,
            Xp = 999,
            UnspentTraitPoints = 3
        })).IsTrue();
        AssertThat(progression.LevelUpCard(instanceId)).IsFalse();

        var afterMax = repo.GetCard(instanceId);
        AssertThat(afterMax).IsNotNull();
        AssertThat(afterMax!.Level).IsEqual(CardProgressionHandler.MaxLevel);
        AssertThat(afterMax.Xp).IsEqual(999);
        AssertThat(afterMax.UnspentTraitPoints).IsEqual(3);
    }

    [TestCase]
    public void SummonerLevelUp_FailurePaths_DoNotMutateStateOrGrantTraitPoints()
    {
        var repo = CreateRepo("progression_xp_spend_summoner_failures");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 1;
        summoner.Xp = 99;
        summoner.UnspentTraitPoints = 0;
        AssertThat(repo.SaveSummonerInstance(summoner)).IsTrue();

        AssertThat(service.LevelUpSummoner(summonerId)).IsFalse();

        var beforeMax = repo.GetSummonerInstance(summonerId);
        AssertThat(beforeMax).IsNotNull();
        AssertThat(beforeMax!.Level).IsEqual(1);
        AssertThat(beforeMax.Xp).IsEqual(99);
        AssertThat(beforeMax.UnspentTraitPoints).IsEqual(0);

        beforeMax.Level = SummonerProgressionService.MaxLevel;
        beforeMax.Xp = 999;
        beforeMax.UnspentTraitPoints = 4;
        AssertThat(repo.SaveSummonerInstance(beforeMax)).IsTrue();
        AssertThat(service.LevelUpSummoner(summonerId)).IsFalse();

        var afterMax = repo.GetSummonerInstance(summonerId);
        AssertThat(afterMax).IsNotNull();
        AssertThat(afterMax!.Level).IsEqual(SummonerProgressionService.MaxLevel);
        AssertThat(afterMax.Xp).IsEqual(999);
        AssertThat(afterMax.UnspentTraitPoints).IsEqual(4);
    }

    private ProfileRepository CreateRepo(string profileId)
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId(profileId));
        repo.ResetProfile();
        return repo;
    }

    private T CreateNode<T>() where T : Node, new()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var node = new T { Name = $"{typeof(T).Name}_ProgressionXpSpend_{Guid.NewGuid():N}" };
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
            repo.SaveSummonerInstance(new SummonerInstance
            {
                SummonerId = candidate
            });
        }

        return candidate;
    }
}
