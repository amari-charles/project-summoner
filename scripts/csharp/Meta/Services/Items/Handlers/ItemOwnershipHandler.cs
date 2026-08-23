using System.Collections.Generic;
using System.Linq;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Infrastructure.Persistence;
using Godot;
using ItemInstance = Fateforged.Domain.Profile.Inventory.ItemInstance;

namespace Fateforged.Meta.Items.Handlers;

/// <summary>
/// Handles item ownership operations: granting, removing, and querying items.
/// </summary>
public class ItemOwnershipHandler
{
    private readonly IProfileRepository _profileRepo;

    public ItemOwnershipHandler(IProfileRepository profileRepo)
    {
        _profileRepo = profileRepo;
    }

    // =========================================================================
    // ITEM GRANTING
    // =========================================================================

    /// <summary>
    /// Grant an item to the player's inventory.
    /// Returns the new item instance ID, or null if failed.
    /// </summary>
    public string? GrantItem(string catalogId, string? boundToSummonerId)
    {
        var definition = ItemCatalog.GetItem(catalogId);
        if (definition == null)
        {
            GD.PushError($"ItemOwnershipHandler: Unknown item catalog ID: {catalogId}");
            return null;
        }

        if (definition.Binding == ItemBinding.SummonerBound && string.IsNullOrWhiteSpace(boundToSummonerId))
        {
            GD.PushError($"ItemOwnershipHandler: Summoner-owned item '{catalogId}' requires a target summoner");
            return null;
        }
        if (definition.Binding == ItemBinding.AccountWide && !definition.IsEventExclusive)
        {
            GD.PushError($"ItemOwnershipHandler: Account-wide item '{catalogId}' must be explicitly event-exclusive");
            return null;
        }

        // Check if player already owns this item type
        var existingItems = _profileRepo.ListItems();
        var typedCatalogId = new ItemId(catalogId);
        var typedOwner = string.IsNullOrWhiteSpace(boundToSummonerId)
            ? (SummonerId?)null
            : new SummonerId(boundToSummonerId);
        var existingItem = existingItems.FirstOrDefault(i =>
            i.CatalogId == typedCatalogId
            && (definition.Binding == ItemBinding.AccountWide || i.BoundToSummonerId == typedOwner)
        );
        if (existingItem != null)
        {
            GD.Print(
                $"ItemOwnershipHandler: Player already owns '{catalogId}' (instance: {existingItem.Id}), skipping grant"
            );
            return (string)existingItem.Id;
        }

        // Create new item instance with simple sequential ID
        var instanceId = $"item_{existingItems.Count + 1:D3}";
        var instance = new ItemInstance
        {
            Id = new ItemId(instanceId),
            CatalogId = typedCatalogId,
            BoundToSummonerId = definition.Binding == ItemBinding.SummonerBound ? typedOwner : null,
            EquippedBySummonerId = null,
            EquippedSlot = null,
        };

        // Add to profile
        existingItems.Add(instance);
        _profileRepo.SaveItems(existingItems);

        GD.Print($"ItemOwnershipHandler: Granted item '{catalogId}' (instance: {instanceId})");
        return instanceId;
    }

    // =========================================================================
    // QUERIES - OWNERSHIP
    // =========================================================================

    /// <summary>Get all AccountWide items (accessible by any summoner).</summary>
    public List<ItemInstance> GetAccountWideItems()
    {
        return _profileRepo
            .ListItems()
            .Where(item => ItemCatalog.GetItem(item.CatalogId)?.Binding == ItemBinding.AccountWide)
            .ToList();
    }

    /// <summary>Get SummonerBound items for a specific summoner.</summary>
    public List<ItemInstance> GetSummonerBoundItems(string summonerId)
    {
        var typedSummonerId = new SummonerId(summonerId);
        return _profileRepo
            .ListItems()
            .Where(item =>
                ItemCatalog.GetItem(item.CatalogId)?.Binding == ItemBinding.SummonerBound
                && item.BoundToSummonerId == typedSummonerId
            )
            .ToList();
    }

    /// <summary>Get all items owned by a summoner based on binding rules.</summary>
    public List<ItemInstance> GetOwnedItems(string summonerId)
    {
        return GetAccountWideItems().Concat(GetSummonerBoundItems(summonerId)).ToList();
    }

    /// <summary>Get all items in the player's inventory.</summary>
    public List<ItemInstance> ListAllItems()
    {
        return _profileRepo.ListItems();
    }

    /// <summary>Get item instance by ID.</summary>
    public ItemInstance? GetItem(string instanceId)
    {
        var typedInstanceId = new ItemId(instanceId);
        return _profileRepo.ListItems().FirstOrDefault(i => i.Id == typedInstanceId);
    }

    /// <summary>Clear all items from inventory (for testing/debugging).</summary>
    public void ClearAllItems()
    {
        foreach (var summoner in _profileRepo.GetAllSummonerInstances())
        {
            foreach (var slot in summoner.EquippedItems.Keys.ToArray())
                summoner.EquippedItems[slot] = null;
            _profileRepo.SaveSummonerInstance(summoner);
        }
        _profileRepo.SaveItems([]);
        GD.Print("ItemOwnershipHandler: Cleared all items from inventory");
    }
}
