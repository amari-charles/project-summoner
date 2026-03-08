namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Deck;
using Fateforged.Meta.Economy;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class TypedFacadeRoutingTest
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
    public void EconomyService_StringAndTypedCampaignGoldOverloadsStayInSync()
    {
        var repo = CreateRepo("typed_facade_economy");
        var economy = CreateNode<EconomyService>();
        economy.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);

        economy.ClearCampaignGold(summonerId);
        economy.AddCampaignGold(25, summonerId);

        AssertThat(economy.GetCampaignGold((string)summonerId)).IsEqual(25);
        AssertThat(economy.GetCampaignGold(summonerId)).IsEqual(25);

        bool spentViaString = economy.SpendCampaignGold(10, (string)summonerId);
        bool spentViaTyped = economy.SpendCampaignGold(5, summonerId);

        AssertThat(spentViaString).IsTrue();
        AssertThat(spentViaTyped).IsTrue();
        AssertThat(economy.GetCampaignGold(summonerId)).IsEqual(10);
    }

    [TestCase]
    public void EconomyService_StringAndTypedCampaignGoldOverloadsMatchOnNonPositiveAddAndUnaffordableSpend()
    {
        var repo = CreateRepo("typed_facade_economy_invalid");
        var economy = CreateNode<EconomyService>();
        economy.InitForTesting(repo);
        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);

        economy.ClearCampaignGold(summonerId);
        economy.AddCampaignGold(0, (string)summonerId);
        economy.AddCampaignGold(-10, summonerId);

        AssertThat(economy.GetCampaignGold((string)summonerId)).IsEqual(0);
        AssertThat(economy.GetCampaignGold(summonerId)).IsEqual(0);
        AssertThat(economy.SpendCampaignGold(1, (string)summonerId)).IsFalse();
        AssertThat(economy.SpendCampaignGold(1, summonerId)).IsFalse();
    }

    [TestCase]
    public void DeckService_StringFacadeRoutesToTypedHandlers()
    {
        var repo = CreateRepo("typed_facade_deck");
        var deckService = CreateNode<DeckService>();
        deckService.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var granted = repo.GrantCards(new[] { (new CardId("fire_wisp"), "common") });
        string[] cardIds = granted.Select(id => id.Value).ToArray();

        string deckId = deckService.CreateDeck("Typed Route Deck", cardIds, summonerId);
        AssertThat(string.IsNullOrEmpty(deckId)).IsFalse();

        var deck = deckService.GetDeck(deckId);
        AssertThat(deck).IsNotNull();
        AssertThat(deck!.SummonerId).IsEqual(summonerId);

        AssertThat(deckService.ValidateDeck(deckId)).IsTrue();
        AssertThat(deckService.ListDecksForSummoner((string)summonerId).Any(d => d.Id.Value == deckId)).IsTrue();
        AssertThat(deckService.SetDeckSummoner(deckId, (string)summonerId)).IsTrue();
    }

    [TestCase]
    public void CampaignService_ClaimPendingReward_CompletesBattleAndClearsPending()
    {
        var repo = CreateRepo("typed_facade_campaign");
        var campaignService = CreateNode<CampaignService>();
        campaignService.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        campaignService.SetActiveSummonerGetter(Callable.From(() => (string)summonerId));
        campaignService.InitializeCatalogs();

        var battles = campaignService.GetAllBattles();
        AssertThat(battles.Count).IsGreater(0);

        var firstBattle = battles[0];
        string battleId = firstBattle["id"].AsString();
        campaignService.SetPendingReward(battleId, RewardType.Fixed.ToStringId(), 0);

        var pendingBefore = campaignService.GetPendingReward();
        AssertThat(pendingBefore.ContainsKey("battle_id")).IsTrue();
        AssertThat(pendingBefore["battle_id"].AsString()).IsEqual(battleId);

        campaignService.ClaimPendingReward();

        AssertThat(campaignService.IsBattleCompleted(battleId)).IsTrue();
        AssertThat(campaignService.GetPendingReward().Count).IsEqual(0);
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

        var node = new T { Name = $"{typeof(T).Name}_TypedFacade_{Guid.NewGuid():N}" };
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }

    private static SummonerId EnsureUnlockedSummoner(IProfileRepository repo, SummonerId candidate)
    {
        if (!repo.IsSummonerUnlocked(candidate))
            repo.UnlockSummoner(candidate);
        return candidate;
    }
}
