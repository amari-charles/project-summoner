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

        foreach (var innateTraitId in SummonerCatalog.GetSummoner(summonerId)!.InnateTraitIds)
        {
            AssertThat(FindNode(vm, "one_off_nodes", innateTraitId)).IsNotNull();
        }

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
    public void CardTreeViewModel_UsesExplicitNativeCoreInsteadOfGlobalStatPool()
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
        const string fireWispRootId = "__card_core_root__:fire_wisp";

        var coreRoot = FindNode(summonVm, "progression_nodes", fireWispRootId);
        AssertThat(coreRoot).IsNotNull();
        var coreRootNode = coreRoot!;
        AssertThat(ReadBool(coreRootNode, "is_owned")).IsTrue();
        AssertThat(ReadInt(coreRootNode, "depth")).IsEqual(0);

        var twinFlame = FindNode(
            summonVm,
            "progression_nodes",
            TraitIds.FireWispTwinFlame
        );
        AssertThat(twinFlame).IsNotNull();
        var twinFlameNode = twinFlame!;
        AssertThat(ReadInt(twinFlameNode, "depth")).IsEqual(1);
        AssertThat(ReadStringArrayContains(twinFlameNode, "prerequisites", fireWispRootId))
            .IsTrue();
        AssertThat(
                FindNode(summonVm, "progression_nodes", TraitIds.FireWispCondensedFlame)
            )
            .IsNotNull();
        AssertThat(FindNode(summonVm, "progression_nodes", TraitIds.Power)).IsNull();
        AssertThat(FindNode(summonVm, "progression_nodes", TraitIds.Fortitude)).IsNull();
        AssertThat(FindNode(spellVm, "progression_nodes", TraitIds.Power)).IsNull();
        AssertThat(ReadNodeCount(spellVm, "progression_nodes")).IsEqual(1);
        AssertThat(
                FindNode(spellVm, "progression_nodes", "__card_core_root__:mana_bolt")
            )
            .IsNotNull();
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

        var detail = traitTree.GetTraitNodeDetail("card", cardId, TraitIds.FireWispTwinFlame);
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

        var result = traitTree.TryUnlockTrait("card", cardId, TraitIds.FireWispTwinFlame);
        AssertThat(ReadBool(result, "success")).IsTrue();

        var card = repo.GetCard(CardInstanceId.FromString(cardId));
        AssertThat(card).IsNotNull();
        AssertThat(card!.UnspentTraitPoints).IsEqual(0);
        AssertThat(
                card.Traits.Contains(CardTraitId.FromString(TraitIds.FireWispTwinFlame))
            )
            .IsTrue();
        AssertThat(cardService.GetTraitSpawnCountBonus(cardId)).IsEqual(1);
        var statModifiers = cardService.GetTraitStatModifiers(cardId);
        AssertThat(statModifiers["max_hp"]).IsEqualApprox(0.65f, 0.001f);
        AssertThat(statModifiers["attack_damage"]).IsEqualApprox(0.65f, 0.001f);

        var detail = traitTree.GetTraitNodeDetail(
            "card",
            cardId,
            TraitIds.FireWispTwinFlame
        );
        AssertThat(ReadString(detail, "state")).IsEqual("owned");
    }

    [TestCase]
    public void CardCoreChoice_PermanentlyClosesAndHidesAlternativeBranch()
    {
        var repo = CreateRepo("trait_tree_service_card_core_exclusivity");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);
        var traitTree = CreateNode<TraitTreeService>();

        var cardId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(cardId),
                    new CardUpdate { Level = 4, UnspentTraitPoints = 2 }
                )
            )
            .IsTrue();

        var before = traitTree.GetCardTreeViewModel(cardId);
        AssertThat(
                FindNode(before, "progression_nodes", TraitIds.FireWispTwinFlame)
            )
            .IsNotNull();
        AssertThat(
                FindNode(before, "progression_nodes", TraitIds.FireWispCondensedFlame)
            )
            .IsNotNull();

        var result = traitTree.TryUnlockTrait("card", cardId, TraitIds.FireWispTwinFlame);
        AssertThat(ReadBool(result, "success")).IsTrue();

        var after = traitTree.GetCardTreeViewModel(cardId);
        AssertThat(
                FindNode(after, "progression_nodes", TraitIds.FireWispTwinFlame)
            )
            .IsNotNull();
        AssertThat(
                FindNode(after, "progression_nodes", TraitIds.FireWispDancingEmbers)
            )
            .IsNotNull();
        AssertThat(
                FindNode(after, "progression_nodes", TraitIds.FireWispCondensedFlame)
            )
            .IsNull();
        AssertThat(
                FindNode(after, "progression_nodes", TraitIds.FireWispBlazingCore)
            )
            .IsNull();

        var rejected = traitTree.TryUnlockTrait(
            "card",
            cardId,
            TraitIds.FireWispCondensedFlame
        );
        AssertThat(ReadBool(rejected, "success")).IsFalse();
    }

    [TestCase]
    public void CardProgression_RejectsGenericStatTraitOutsideAuthoredCore()
    {
        var repo = CreateRepo("trait_tree_service_card_rejects_generic_stat");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var cardId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(cardId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        AssertThat(cardService.SpendCardTraitPoint(cardId, TraitIds.Power)).IsFalse();
        var card = repo.GetCard(CardInstanceId.FromString(cardId));
        AssertThat(card).IsNotNull();
        AssertThat(card!.UnspentTraitPoints).IsEqual(1);
        AssertThat(card.Traits).IsEmpty();
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

    private static int ReadNodeCount(Godot.Collections.Dictionary viewModel, string key)
    {
        return viewModel.TryGetValue(key, out var nodesVar)
            && nodesVar.VariantType == Variant.Type.Array
            ? nodesVar.AsGodotArray().Count
            : 0;
    }

    private static bool ReadStringArrayContains(
        Godot.Collections.Dictionary dict,
        string key,
        string expected
    )
    {
        if (
            !dict.TryGetValue(key, out var value)
            || value.VariantType != Variant.Type.Array
        )
            return false;

        return value.AsGodotArray().Any(item => item.AsString() == expected);
    }

    private static int ReadInt(
        Godot.Collections.Dictionary dict,
        string key,
        int fallback = 0
    )
    {
        return dict.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Int
            ? value.AsInt32()
            : fallback;
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
