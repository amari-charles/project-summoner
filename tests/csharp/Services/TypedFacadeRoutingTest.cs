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
using Fateforged.Meta.Rewards;
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
        campaignService.SetPendingReward(battleId, Fateforged.Data.Events.RewardType.Fixed.ToStringId(), 0);

        var pendingBefore = campaignService.GetPendingReward();
        AssertThat(pendingBefore.ContainsKey("battle_id")).IsTrue();
        AssertThat(pendingBefore["battle_id"].AsString()).IsEqual(battleId);

        campaignService.ClaimPendingReward();

        AssertThat(campaignService.IsBattleCompleted(battleId)).IsTrue();
        AssertThat(campaignService.GetPendingReward().Count).IsEqual(0);
    }

    [TestCase]
    public void CampaignService_ClaimPendingReward_GrantsFlexibleChoiceAndCampaignGold()
    {
        var repo = CreateRepo("typed_facade_campaign_rewards");
        var campaignService = CreateNode<CampaignService>();
        campaignService.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        campaignService.SetActiveSummonerGetter(Callable.From(() => (string)summonerId));
        campaignService.SetCollectionCallbacks(Callable.From<string, string, string>((catalogId, rarity) =>
        {
            var ids = repo.GrantCards(new[] { (new CardId(catalogId), rarity) });
            return ids.Length > 0 ? ids[0].Value : "";
        }));
        campaignService.InitializeCatalogs();

        string battleId = (string)EventIds.FirstTrial;
        campaignService.SetPendingReward(battleId, Fateforged.Data.Events.RewardType.Flexible.ToStringId(), 1);

        var granted = campaignService.ClaimPendingReward();

        var campaignProgress = repo.GetCampaignProgress(summonerId);
        var ownedCardCatalogIds = repo.ListCards().Select(c => (string)c.CatalogId).ToArray();

        AssertThat(campaignService.IsBattleCompleted(battleId)).IsTrue();
        AssertThat(campaignService.GetPendingReward().Count).IsEqual(0);
        AssertThat(campaignProgress.Gold).IsEqual(30);
        AssertThat(ownedCardCatalogIds).Contains((string)CardIds.Puff);
        AssertThat(granted.ContainsKey("instance_ids")).IsTrue();
        AssertThat(granted.GetValueOrDefault("campaign_gold", 0).AsInt32()).IsEqual(30);
    }

    [TestCase]
    public void MissionCompletionFlow_VictorySpecChoiceClaim_CompletesAndGrantsExpectedReward()
    {
        var repo = CreateRepo("typed_facade_mission_flow");
        var campaignService = CreateNode<CampaignService>();
        campaignService.InitForTesting(repo);

        var rewardService = CreateNode<RewardService>();
        rewardService.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        campaignService.SetActiveSummonerGetter(Callable.From(() => (string)summonerId));
        campaignService.SetCollectionCallbacks(Callable.From<string, string, string>((catalogId, rarity) =>
        {
            var ids = repo.GrantCards(new[] { (new CardId(catalogId), rarity) });
            return ids.Length > 0 ? ids[0].Value : "";
        }));
        campaignService.InitializeCatalogs();

        string battleId = (string)EventIds.FirstTrial;

        // Simulate reward screen request after victory.
        var spec = rewardService.GetBattleRewardSpecAsDict(battleId, isCompleted: false, chosenIndex: -1);
        var goldReward = spec.GetValueOrDefault("gold_reward", 0).AsInt32();
        var cardOptions = spec.GetValueOrDefault("card_options", new Godot.Collections.Array()).AsGodotArray();
        bool requiresChoice = spec.GetValueOrDefault("requires_choice", false).AsBool();

        AssertThat(requiresChoice).IsTrue();
        AssertThat(cardOptions.Count).IsGreater(0);

        var chosenIndex = 0;
        var chosenOption = cardOptions[chosenIndex].AsGodotDictionary();
        var chosenCatalogId = chosenOption.GetValueOrDefault("catalog_id", "").AsString();
        AssertThat(string.IsNullOrEmpty(chosenCatalogId)).IsFalse();

        // Simulate pending reward lifecycle used by reward screen.
        campaignService.SetPendingReward(battleId, Fateforged.Data.Events.RewardType.Flexible.ToStringId(), -1);
        campaignService.UpdatePendingChoice(chosenIndex, chosenCatalogId);

        var granted = campaignService.ClaimPendingReward();
        var progress = repo.GetCampaignProgress(summonerId);

        AssertThat(campaignService.IsBattleCompleted(battleId)).IsTrue();
        AssertThat(campaignService.GetPendingReward().Count).IsEqual(0);
        AssertThat(progress.Gold).IsEqual(goldReward);
        AssertThat(granted.GetValueOrDefault("catalog_id", "").AsString()).IsEqual(chosenCatalogId);
        AssertThat(granted.GetValueOrDefault("campaign_gold", 0).AsInt32()).IsEqual(goldReward);
    }

    [TestCase]
    public void MissionCompletionFlow_ChoiceCatalogIdPreventsIndexDriftAfterCollectionChanges()
    {
        var repo = CreateRepo("typed_facade_mission_drift");
        var campaignService = CreateNode<CampaignService>();
        campaignService.InitForTesting(repo);

        var rewardService = CreateNode<RewardService>();
        rewardService.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        campaignService.SetActiveSummonerGetter(Callable.From(() => (string)summonerId));
        campaignService.SetCollectionCallbacks(Callable.From<string, string, string>((catalogId, rarity) =>
        {
            var ids = repo.GrantCards(new[] { (new CardId(catalogId), rarity) });
            return ids.Length > 0 ? ids[0].Value : "";
        }));
        campaignService.InitializeCatalogs();

        string battleId = (string)EventIds.FirstTrial;
        var spec = rewardService.GetBattleRewardSpecAsDict(battleId, isCompleted: false, chosenIndex: -1);
        var options = spec.GetValueOrDefault("card_options", new Godot.Collections.Array()).AsGodotArray();
        AssertThat(options.Count).IsGreater(1);

        var chosenIndex = 1;
        var chosenCatalogId = options[chosenIndex].AsGodotDictionary().GetValueOrDefault("catalog_id", "").AsString();
        AssertThat(string.IsNullOrEmpty(chosenCatalogId)).IsFalse();

        campaignService.SetPendingReward(battleId, Fateforged.Data.Events.RewardType.Flexible.ToStringId(), -1);
        campaignService.UpdatePendingChoice(chosenIndex, chosenCatalogId);

        // Change collection before claim; this can reshuffle filtered flexible options.
        repo.GrantCards(new[] { (new CardId((string)CardIds.FireWisp), "common") });

        var granted = campaignService.ClaimPendingReward();
        AssertThat(granted.GetValueOrDefault("catalog_id", "").AsString()).IsEqual(chosenCatalogId);
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
