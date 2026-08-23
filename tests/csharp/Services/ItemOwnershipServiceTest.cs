namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Summoners;
using ItemSlot = Fateforged.Domain.Profile.Inventory.ItemSlot;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Items;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class ItemOwnershipServiceTest
{
    private readonly List<Node> _nodes = [];

    [AfterTest]
    public void Cleanup()
    {
        foreach (var node in _nodes)
        {
            if (GodotObject.IsInstanceValid(node))
                node.Free();
        }
        _nodes.Clear();
    }

    [TestCase]
    public void NormalGrantRequiresOwnerAndEquipmentIsIsolated()
    {
        var (repo, service) = CreateSubject();
        EnsureSummoner(repo, SummonerIds.Cole);
        EnsureSummoner(repo, SummonerIds.Selene);

        AssertThat(service.GrantItem(ItemIds.TrainingBlade, null)).IsNull();
        var instanceId = service.GrantItemToSummoner(ItemIds.TrainingBlade, SummonerIds.Cole);

        AssertThat(instanceId).IsNotEmpty();
        AssertThat(service.GetOwnedItems(SummonerIds.Cole)).HasSize(1);
        AssertThat(service.GetOwnedItems(SummonerIds.Selene)).IsEmpty();
        AssertThat(service.EquipItemStr(SummonerIds.Cole, instanceId, "wand")).IsTrue();
        AssertThat(service.EquipItemStr(SummonerIds.Selene, instanceId, "wand")).IsFalse();
    }

    [TestCase]
    public void ExplicitSharedEventItemIsVisibleToBothSummoners()
    {
        var (repo, service) = CreateSubject();
        EnsureSummoner(repo, SummonerIds.Cole);
        EnsureSummoner(repo, SummonerIds.Selene);

        var instanceId = service.GrantSharedEventItem(ItemIds.VeteransMedal);

        AssertThat(instanceId).IsNotEmpty();
        AssertThat(service.GetOwnedItems(SummonerIds.Cole)).HasSize(1);
        AssertThat(service.GetOwnedItems(SummonerIds.Selene)).HasSize(1);
        AssertThat(service.GetOwnedItems(SummonerIds.Cole)[0].BoundToSummonerId).IsNull();
    }

    [TestCase]
    public void ClearRemovesInventoryAndEquipmentBindings()
    {
        var (repo, service) = CreateSubject();
        EnsureSummoner(repo, SummonerIds.Cole);
        var instanceId = service.GrantItemToSummoner(ItemIds.TrainingBlade, SummonerIds.Cole);
        AssertThat(service.EquipItemStr(SummonerIds.Cole, instanceId, "wand")).IsTrue();

        service.ClearAllItems();

        AssertThat(service.ListAllItems()).IsEmpty();
        AssertThat(service.GetEquippedItems(SummonerIds.Cole)[ItemSlot.Wand])
            .IsNull();
    }

    private (ProfileRepository repo, ItemService service) CreateSubject()
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId($"item_ownership_{Guid.NewGuid():N}"));
        repo.ResetProfile();
        var service = CreateNode<ItemService>();
        service.InitForTesting(repo);
        return (repo, service);
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var node = new T { Name = $"{typeof(T).Name}_{Guid.NewGuid():N}" };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(node);
        _nodes.Add(node);
        return node;
    }

    private static void EnsureSummoner(IProfileRepository repo, SummonerId id)
    {
        if (!repo.IsSummonerUnlocked(id))
            repo.UnlockSummoner(id);
        if (repo.GetSummonerInstance(id) == null)
            repo.SaveSummonerInstance(new SummonerInstance { SummonerId = id });
    }
}
