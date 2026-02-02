using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Data.Traits;
using ProjectSummoner.Infrastructure.Persistence;
using ItemSlot = ProjectSummoner.Domain.Profile.Inventory.ItemSlot;
using ItemInstance = ProjectSummoner.Domain.Profile.Inventory.ItemInstance;

namespace ProjectSummoner.Services.Items.Handlers;

/// <summary>
/// Handles item equipment operations: equipping, unequipping, and equipment queries.
/// </summary>
public class ItemEquipmentHandler
{
    private readonly IProfileRepository _profileRepo;
    private readonly ItemOwnershipHandler _ownership;

    public ItemEquipmentHandler(IProfileRepository profileRepo, ItemOwnershipHandler ownership)
    {
        _profileRepo = profileRepo;
        _ownership = ownership;
    }

    // =========================================================================
    // EQUIPPING
    // =========================================================================

    /// <summary>
    /// Equip an item to a summoner's slot.
    /// Returns true if successful.
    /// </summary>
    public bool EquipItem(string summonerId, string itemInstanceId, ItemSlot slot)
    {
        // Get item instance
        var items = _profileRepo.ListItems();
        var typedItemId = new ItemId(itemInstanceId);
        var typedSummonerId = new SummonerId(summonerId);
        var item = items.FirstOrDefault(i => i.Id == typedItemId);
        if (item == null)
        {
            GD.PushError($"ItemEquipmentHandler: Item instance not found: {itemInstanceId}");
            return false;
        }

        // Verify item definition exists and matches slot
        var definition = ItemCatalog.GetItem(item.CatalogId);
        if (definition == null)
        {
            GD.PushError($"ItemEquipmentHandler: Item definition not found: {item.CatalogId}");
            return false;
        }

        if ((int)definition.Slot != (int)slot)
        {
            GD.PushError($"ItemEquipmentHandler: Item '{item.CatalogId}' cannot be equipped to slot '{slot}' (requires '{definition.Slot}')");
            return false;
        }

        // Check binding restrictions
        if (definition.Binding == ItemBinding.SummonerBound && item.BoundToSummonerId != typedSummonerId)
        {
            GD.PushError($"ItemEquipmentHandler: Item '{itemInstanceId}' is bound to summoner '{item.BoundToSummonerId}', cannot equip to '{summonerId}'");
            return false;
        }

        // Check if item is already equipped by another summoner
        if (item.EquippedBySummonerId != null && item.EquippedBySummonerId != typedSummonerId)
        {
            GD.PushError($"ItemEquipmentHandler: Item '{itemInstanceId}' is already equipped by '{item.EquippedBySummonerId}'");
            return false;
        }

        // Get summoner instance
        var summoner = _profileRepo.GetSummonerInstance(typedSummonerId);
        if (summoner == null)
        {
            GD.PushError($"ItemEquipmentHandler: Summoner not found: {summonerId}");
            return false;
        }

        // Unequip current item in slot (if any)
        if (summoner.EquippedItems.TryGetValue(slot, out var currentItemId) && currentItemId != null)
        {
            var currentItem = items.FirstOrDefault(i => i.Id == currentItemId);
            if (currentItem != null)
            {
                currentItem.EquippedBySummonerId = null;
                currentItem.EquippedSlot = null;
            }
        }

        // Equip the new item
        item.EquippedBySummonerId = typedSummonerId;
        item.EquippedSlot = slot;
        summoner.EquippedItems[slot] = typedItemId;

        // Save changes
        _profileRepo.SaveItems(items);
        _profileRepo.SaveSummonerInstance(summoner);

        GD.Print($"ItemEquipmentHandler: Equipped item '{item.CatalogId}' to {summonerId}'s {slot} slot");
        return true;
    }

    /// <summary>
    /// Unequip an item from a summoner's slot.
    /// Returns true if successful.
    /// </summary>
    public bool UnequipItem(string summonerId, ItemSlot slot)
    {
        var summoner = _profileRepo.GetSummonerInstance(new SummonerId(summonerId));
        if (summoner == null)
        {
            GD.PushError($"ItemEquipmentHandler: Summoner not found: {summonerId}");
            return false;
        }

        if (!summoner.EquippedItems.TryGetValue(slot, out var itemInstanceId) || itemInstanceId == null)
        {
            // Slot is already empty
            return true;
        }

        // Update item instance
        var items = _profileRepo.ListItems();
        var item = items.FirstOrDefault(i => i.Id == itemInstanceId);
        if (item != null)
        {
            item.EquippedBySummonerId = null;
            item.EquippedSlot = null;
            _profileRepo.SaveItems(items);
        }

        // Update summoner
        summoner.EquippedItems[slot] = null;
        _profileRepo.SaveSummonerInstance(summoner);

        GD.Print($"ItemEquipmentHandler: Unequipped item from {summonerId}'s {slot} slot");
        return true;
    }

    // =========================================================================
    // QUERIES - EQUIPMENT
    // =========================================================================

    /// <summary>
    /// Get equipped items for a summoner.
    /// Returns a dictionary of slot -> item instance ID (or null if empty).
    /// </summary>
    public Dictionary<ItemSlot, string?> GetEquippedItems(string summonerId)
    {
        var result = new Dictionary<ItemSlot, string?>
        {
            [ItemSlot.Wand] = null,
            [ItemSlot.Ring1] = null,
            [ItemSlot.Ring2] = null,
            [ItemSlot.Robes] = null
        };

        var summoner = _profileRepo.GetSummonerInstance(new SummonerId(summonerId));
        if (summoner == null) return result;

        foreach (var (slot, instanceId) in summoner.EquippedItems)
        {
            result[slot] = instanceId;
        }

        return result;
    }

    /// <summary>
    /// Get all items available for a specific slot.
    /// Uses ownership handler for filtering, then filters by slot and equip status.
    /// </summary>
    public List<ItemInstance> ListItemsForSlot(ItemSlot slot, string summonerId)
    {
        var result = new List<ItemInstance>();

        foreach (var item in _ownership.GetOwnedItems(summonerId))
        {
            var definition = ItemCatalog.GetItem(item.CatalogId);
            if (definition == null) continue;

            // Filter by slot
            if ((int)definition.Slot != (int)slot) continue;

            // Filter out items equipped by another summoner
            if (!string.IsNullOrEmpty(item.EquippedBySummonerId) && item.EquippedBySummonerId != summonerId) continue;

            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Get all modifiers from equipped items for a summoner.
    /// </summary>
    public List<TraitModifier> GetEquippedItemModifiers(string summonerId)
    {
        var modifiers = new List<TraitModifier>();

        var equipped = GetEquippedItems(summonerId);
        var items = _profileRepo.ListItems();

        foreach (var (slot, instanceId) in equipped)
        {
            if (instanceId == null) continue;

            var instance = items.FirstOrDefault(i => i.Id == instanceId);
            if (instance == null) continue;

            var definition = ItemCatalog.GetItem(instance.CatalogId);
            if (definition == null) continue;

            modifiers.AddRange(definition.Modifiers);
        }

        return modifiers;
    }
}
