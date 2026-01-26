using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Infrastructure.Persistence;
using ItemInstance = ProjectSummoner.Domain.Profile.Inventory.ItemInstance;

namespace ProjectSummoner.Services.Items.Handlers;

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
    public string? GrantItem(string catalogId, string? boundToSummonerId = null)
    {
        var definition = ItemCatalog.GetItem(catalogId);
        if (definition == null)
        {
            GD.PushError($"ItemOwnershipHandler: Unknown item catalog ID: {catalogId}");
            return null;
        }

        // Check if player already owns this item type
        var existingItems = _profileRepo.ListItems();
        var existingItem = existingItems.FirstOrDefault(i => i.CatalogId == catalogId);
        if (existingItem != null)
        {
            GD.Print($"ItemOwnershipHandler: Player already owns '{catalogId}' (instance: {existingItem.Id}), skipping grant");
            return existingItem.Id;
        }

        // Create new item instance with simple sequential ID
        var instanceId = $"item_{existingItems.Count + 1:D3}";
        var instance = new ItemInstance
        {
            Id = instanceId,
            CatalogId = catalogId,
            BoundToSummonerId = definition.Binding == ItemBinding.SummonerBound ? boundToSummonerId : null,
            EquippedBySummonerId = null,
            EquippedSlot = null
        };

        // Add to profile
        existingItems.Add(instance);
        _profileRepo.SaveItems(existingItems);

        GD.Print($"ItemOwnershipHandler: Granted item '{catalogId}' (instance: {instanceId})");
        return instanceId;
    }

    /// <summary>
    /// Grant an item from a legacy boon ID (used during migration).
    /// </summary>
    public string? GrantItemFromBoon(string boonId, string summonerId)
    {
        var itemId = ItemCatalog.GetItemIdForBoon(boonId);
        if (itemId == null)
        {
            GD.PushWarning($"ItemOwnershipHandler: No item mapping for boon '{boonId}'");
            return null;
        }

        return GrantItem(itemId, summonerId);
    }

    // =========================================================================
    // QUERIES - OWNERSHIP
    // =========================================================================

    /// <summary>Get all AccountWide items (accessible by any summoner).</summary>
    public List<ItemInstance> GetAccountWideItems()
    {
        return _profileRepo.ListItems()
            .Where(item => ItemCatalog.GetItem(item.CatalogId)?.Binding == ItemBinding.AccountWide)
            .ToList();
    }

    /// <summary>Get SummonerBound items for a specific summoner.</summary>
    public List<ItemInstance> GetSummonerBoundItems(string summonerId)
    {
        return _profileRepo.ListItems()
            .Where(item => ItemCatalog.GetItem(item.CatalogId)?.Binding == ItemBinding.SummonerBound
                && item.BoundToSummonerId == summonerId)
            .ToList();
    }

    /// <summary>Get all items owned by a summoner based on binding rules.</summary>
    public List<ItemInstance> GetOwnedItems(string summonerId)
    {
        return GetAccountWideItems()
            .Concat(GetSummonerBoundItems(summonerId))
            .ToList();
    }

    /// <summary>Get all items in the player's inventory.</summary>
    public List<ItemInstance> ListAllItems()
    {
        return _profileRepo.ListItems();
    }

    /// <summary>Get item instance by ID.</summary>
    public ItemInstance? GetItem(string instanceId)
    {
        return _profileRepo.ListItems().FirstOrDefault(i => i.Id == instanceId);
    }

    /// <summary>Clear all items from inventory (for testing/debugging).</summary>
    public void ClearAllItems()
    {
        _profileRepo.SaveItems([]);
        GD.Print("ItemOwnershipHandler: Cleared all items from inventory");
    }
}
