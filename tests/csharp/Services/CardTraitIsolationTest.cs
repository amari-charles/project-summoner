namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CardTraitIsolationTest
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
    public void UnlockingTrait_OnOneCardInstance_DoesNotUnlockSiblingInstance()
    {
        var repo = CreateRepo("card_trait_isolation");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var firstInstanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        var secondInstanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(firstInstanceId)).IsFalse();
        AssertThat(string.IsNullOrWhiteSpace(secondInstanceId)).IsFalse();

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(firstInstanceId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(secondInstanceId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        AssertThat(cardService.SpendCardTraitPoint(firstInstanceId, TraitIds.FireWispTwinFlame))
            .IsTrue();

        var firstCard = repo.GetCard(CardInstanceId.FromString(firstInstanceId));
        var secondCard = repo.GetCard(CardInstanceId.FromString(secondInstanceId));
        AssertThat(firstCard).IsNotNull();
        AssertThat(secondCard).IsNotNull();

        AssertThat(
                firstCard!.Traits.Contains(CardTraitId.FromString(TraitIds.FireWispTwinFlame))
            )
            .IsTrue();
        AssertThat(firstCard.UnspentTraitPoints).IsEqual(0);

        AssertThat(
                secondCard!.Traits.Contains(CardTraitId.FromString(TraitIds.FireWispTwinFlame))
            )
            .IsFalse();
        AssertThat(secondCard.UnspentTraitPoints).IsEqual(1);
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

        var node = new T { Name = $"{typeof(T).Name}_CardTraitIsolation_{Guid.NewGuid():N}" };
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }
}
