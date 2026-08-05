namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Infrastructure.Persistence;
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
        AssertThat(economy.SpendCampaignGold(10, (string)summonerId)).IsTrue();
        AssertThat(economy.SpendCampaignGold(5, summonerId)).IsTrue();
        AssertThat(economy.GetCampaignGold(summonerId)).IsEqual(10);
    }

    [TestCase]
    public void EconomyService_StringAndTypedCampaignGoldOverloadsRejectInvalidAmounts()
    {
        var repo = CreateRepo("typed_facade_economy_invalid");
        var economy = CreateNode<EconomyService>();
        economy.InitForTesting(repo);
        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);

        economy.ClearCampaignGold(summonerId);
        economy.AddCampaignGold(0, (string)summonerId);
        economy.AddCampaignGold(-10, summonerId);
        AssertThat(economy.GetCampaignGold(summonerId)).IsEqual(0);
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

        string deckId = deckService.CreateDeck(
            "Typed Route Deck",
            granted.Select(id => id.Value).ToArray(),
            summonerId
        );
        AssertThat(string.IsNullOrEmpty(deckId)).IsFalse();
        AssertThat(deckService.GetDeck(deckId)!.SummonerId).IsEqual(summonerId);
        AssertThat(deckService.ValidateDeck(deckId)).IsTrue();
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
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
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
