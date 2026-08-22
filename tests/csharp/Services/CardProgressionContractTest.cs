namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Cards.Handlers;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CardProgressionContractTest
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
    public void GetCardProgressionInfoDict_ExposesUiContractFieldsWithExpectedValues()
    {
        var repo = CreateRepo("card_progression_contract_fields");
        var service = CreateNode<CardService>();
        service.InitForTesting(repo);

        var instanceId = service.GrantCard(CardIds.FireWisp, "common");
        AssertThat(instanceId).IsNotEqual("");
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(instanceId),
                    new CardUpdate
                    {
                        Level = 1,
                        Xp = 15,
                        UnspentTraitPoints = 0,
                    }
                )
            )
            .IsTrue();

        var info = service.GetCardProgressionInfoDict(instanceId);
        AssertThat(info.ContainsKey("xp")).IsTrue();
        AssertThat(info.ContainsKey("xp_for_next_level")).IsTrue();
        AssertThat(info.ContainsKey("xp_progress")).IsTrue();
        AssertThat(info.ContainsKey("can_level_up")).IsFalse();
        AssertThat(info.ContainsKey("level_up_resource_cost")).IsFalse();
        AssertThat(info.ContainsKey("has_level_up_resource_cost")).IsFalse();

        AssertThat(info["xp"].AsInt32()).IsEqual(15);
        AssertThat(info["xp_for_next_level"].AsInt32()).IsEqual(30);
        var progress = (float)info["xp_progress"].AsDouble();
        AssertThat(Math.Abs(progress - 0.5f)).IsLess(0.001f);
    }

    [TestCase]
    public void CardRarityScaling_Level1CostDiff_IsPreserved()
    {
        var repo = CreateRepo("card_progression_contract_rarity");
        var service = CreateNode<CardService>();
        service.InitForTesting(repo);

        var commonId = service.GrantCard(CardIds.FireWisp, "common");
        var rareId = service.GrantCard(CardIds.FireWisp, "rare");

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(commonId),
                    new CardUpdate { Level = 1, Xp = 0 }
                )
            )
            .IsTrue();
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(rareId),
                    new CardUpdate { Level = 1, Xp = 0 }
                )
            )
            .IsTrue();

        var commonInfo = service.GetCardProgressionInfoDict(commonId);
        var rareInfo = service.GetCardProgressionInfoDict(rareId);

        AssertThat(commonInfo["xp_for_next_level"].AsInt32()).IsEqual(30);
        AssertThat(rareInfo["xp_for_next_level"].AsInt32()).IsEqual(45);
    }

    [TestCase]
    public void CardMaxLevel_NoOp_AndUiContractRemainStable()
    {
        var repo = CreateRepo("card_progression_contract_max");
        var service = CreateNode<CardService>();
        service.InitForTesting(repo);

        var instanceId = service.GrantCard(CardIds.FireWisp, "common");
        var typedId = CardInstanceId.FromString(instanceId);
        AssertThat(
                repo.UpdateCard(
                    typedId,
                    new CardUpdate
                    {
                        Level = CardProgressionHandler.MaxLevel,
                        Xp = 999,
                        UnspentTraitPoints = 2,
                    }
                )
            )
            .IsTrue();

        AssertThat(service.GrantXp(instanceId, 100)).IsEqual(999);

        var info = service.GetCardProgressionInfoDict(instanceId);
        AssertThat(info["xp_for_next_level"].AsInt32()).IsEqual(0);
        AssertThat((float)info["xp_progress"].AsDouble()).IsEqual(1f);

        var card = repo.GetCard(typedId);
        AssertThat(card).IsNotNull();
        AssertThat(card!.Level).IsEqual(CardProgressionHandler.MaxLevel);
        AssertThat(card.Xp).IsEqual(999);
        AssertThat(card.UnspentTraitPoints).IsEqual(2);
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

        var node = new T { Name = $"{typeof(T).Name}_CardProgressionContract_{Guid.NewGuid():N}" };
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }
}
