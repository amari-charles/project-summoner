namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Services.Traits;
using Fateforged.Meta.Summoner;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class TraitTreeServiceTest
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
    public void SummonerTreeViewModel_ProvidesStateMatrixAndUnlocksThroughService()
    {
        var repo = CreateRepo("trait_tree_service_summoner");
        var progression = CreateNode<SummonerProgressionService>();
        progression.InitForTesting(repo);

        var traitTree = CreateNode<TraitTreeService>();

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 2;
        summoner.UnspentTraitPoints = 1;
        repo.SaveSummonerInstance(summoner);

        var vm = traitTree.GetSummonerTreeViewModel(summonerId);
        AssertThat(vm.Count).IsGreater(0);

        var coleSoulStrength = FindNode(vm, "progression_nodes", TraitIds.ColeSoulStrengthI);
        AssertThat(coleSoulStrength).IsNotNull();
        var coleSoulStrengthNode = coleSoulStrength!;
        AssertThat(ReadString(coleSoulStrengthNode, "state")).IsEqual("available");
        AssertThat(ReadBool(coleSoulStrengthNode, "can_unlock")).IsTrue();

        // Legacy complex summoner traits are no longer in the level-up offer pool.
        var berserker = FindNode(vm, "progression_nodes", "trait_berserker");
        AssertThat(berserker).IsNull();

        var detail = traitTree.GetTraitNodeDetail(
            "summoner",
            summonerId,
            TraitIds.ColeSoulStrengthI
        );
        AssertThat(detail.Count).IsGreater(0);
        AssertThat(ReadString(detail, "name")).IsNotEmpty();
        AssertThat(ReadString(detail, "description")).IsNotEmpty();
        AssertThat(ReadBool(detail, "unlock_button_visible")).IsTrue();
        AssertThat(ReadBool(detail, "unlock_button_enabled")).IsTrue();

        var unlockResult = traitTree.TryUnlockTrait(
            "summoner",
            summonerId,
            TraitIds.ColeSoulStrengthI
        );
        AssertThat(ReadBool(unlockResult, "success")).IsTrue();

        var updated = repo.GetSummonerInstance(summonerId);
        AssertThat(updated).IsNotNull();
        AssertThat(updated!.UnspentTraitPoints).IsEqual(0);
        AssertThat(updated.AcquiredTraitIds.Contains(TraitIds.ColeSoulStrengthI)).IsTrue();
    }

    [TestCase]
    public void CardTreeViewModel_RespectsOwnerTypeFiltering()
    {
        var repo = CreateRepo("trait_tree_service_card_owner_filter");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);
        var traitTree = CreateNode<TraitTreeService>();

        var summonCardId = cardService.GrantCard(CardIds.FireWisp, "common");
        var spellCardId = cardService.GrantCard(CardIds.ManaBolt, "common");
        AssertThat(string.IsNullOrWhiteSpace(summonCardId)).IsFalse();
        AssertThat(string.IsNullOrWhiteSpace(spellCardId)).IsFalse();

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(summonCardId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(spellCardId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        var summonVm = traitTree.GetCardTreeViewModel(summonCardId);
        var spellVm = traitTree.GetCardTreeViewModel(spellCardId);

        AssertThat(FindNode(summonVm, "progression_nodes", TraitIds.Power)).IsNotNull();
        AssertThat(FindNode(spellVm, "progression_nodes", TraitIds.Power)).IsNull();
    }

    [TestCase]
    public void CardTraitNodeDetail_ShowsNameDescriptionAndDisabledUnlockWhenNoPoints()
    {
        var repo = CreateRepo("trait_tree_service_card_detail");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);
        var traitTree = CreateNode<TraitTreeService>();

        var cardId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(cardId)).IsFalse();

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(cardId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 0 }
                )
            )
            .IsTrue();

        var detail = traitTree.GetTraitNodeDetail("card", cardId, TraitIds.Power);
        AssertThat(detail.Count).IsGreater(0);
        AssertThat(ReadString(detail, "name")).IsNotEmpty();
        AssertThat(ReadString(detail, "description")).IsNotEmpty();
        AssertThat(ReadBool(detail, "unlock_button_visible")).IsTrue();
        AssertThat(ReadBool(detail, "unlock_button_enabled")).IsFalse();
        AssertThat(ReadString(detail, "unlock_blocked_reason")).Contains("trait point");
    }

    [TestCase]
    public void TryUnlockTrait_CardFlow_SpendsPointAndAppliesTrait()
    {
        var repo = CreateRepo("trait_tree_service_card_unlock");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);
        var traitTree = CreateNode<TraitTreeService>();

        var cardId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(cardId)).IsFalse();

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(cardId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        var result = traitTree.TryUnlockTrait("card", cardId, TraitIds.Power);
        AssertThat(ReadBool(result, "success")).IsTrue();

        var card = repo.GetCard(CardInstanceId.FromString(cardId));
        AssertThat(card).IsNotNull();
        AssertThat(card!.UnspentTraitPoints).IsEqual(0);
        AssertThat(card.Traits.Contains(CardTraitId.FromString(TraitIds.Power))).IsTrue();

        var detail = traitTree.GetTraitNodeDetail("card", cardId, TraitIds.Power);
        AssertThat(ReadString(detail, "state")).IsEqual("owned");
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

        var node = new T { Name = $"{typeof(T).Name}_TraitTreeServiceTest_{Guid.NewGuid():N}" };
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

    private static Godot.Collections.Dictionary? FindNode(
        Godot.Collections.Dictionary viewModel,
        string key,
        string traitId
    )
    {
        if (
            !viewModel.TryGetValue(key, out var nodesVar)
            || nodesVar.VariantType != Variant.Type.Array
        )
            return null;

        foreach (var entry in nodesVar.AsGodotArray())
        {
            if (entry.VariantType != Variant.Type.Dictionary)
                continue;

            var node = entry.AsGodotDictionary();
            if (ReadString(node, "id") == traitId)
                return node;
        }

        return null;
    }

    private static string ReadString(
        Godot.Collections.Dictionary dict,
        string key,
        string fallback = ""
    )
    {
        return dict.TryGetValue(key, out var value) ? value.AsString() : fallback;
    }

    private static bool ReadBool(
        Godot.Collections.Dictionary dict,
        string key,
        bool fallback = false
    )
    {
        return dict.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Bool
            ? value.AsBool()
            : fallback;
    }
}
