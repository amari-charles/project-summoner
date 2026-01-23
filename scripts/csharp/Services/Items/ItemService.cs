using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Profile;
using ProjectSummoner.Data.Traits;
using ProjectSummoner.Services.Profile;

namespace ProjectSummoner.Services.Items;

/// <summary>
/// Item Service - Handles item granting, equipping, and inventory management.
///
/// Items are stored in ProfileData.Items as instances.
/// Equipped items are tracked per-summoner in SummonerInstanceData.EquippedItems.
/// </summary>
[GlobalClass]
public partial class ItemService : Node
{
    public static ItemService? Instance { get; private set; }

    [Signal]
    public delegate void ItemGrantedEventHandler(string instanceId, string catalogId);

    [Signal]
    public delegate void ItemEquippedEventHandler(string summonerId, string slot, string? itemInstanceId);

    [Signal]
    public delegate void ItemUnequippedEventHandler(string summonerId, string slot);

    private IProfileRepository? _profileRepo;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(Initialize));
    }

    private void Initialize()
    {
        GD.Print("ItemService: Initializing...");

        _profileRepo = ProfileRepositoryBridge.Instance;

        if (_profileRepo == null)
        {
            GD.PushError("ItemService: ProfileRepositoryBridge.Instance not available");
            return;
        }

        GD.Print("ItemService: Ready");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Initialize for testing with mock dependencies.</summary>
    public void InitForTesting(IProfileRepository repo)
    {
        ArgumentNullException.ThrowIfNull(repo);
        _profileRepo = repo;
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
        if (_profileRepo == null) return null;

        var definition = ItemCatalog.GetItem(catalogId);
        if (definition == null)
        {
            GD.PushError($"ItemService: Unknown item catalog ID: {catalogId}");
            return null;
        }

        // Check if player already owns this item type
        var existingItems = _profileRepo.ListItems();
        var existingItem = existingItems.FirstOrDefault(i => i.CatalogId == catalogId);
        if (existingItem != null)
        {
            GD.Print($"ItemService: Player already owns '{catalogId}' (instance: {existingItem.Id}), skipping grant");
            return existingItem.Id;
        }

        // Create new item instance with simple sequential ID
        var instanceId = $"item_{existingItems.Count + 1:D3}";
        var instance = new ItemInstanceData
        {
            Id = instanceId,
            CatalogId = catalogId,
            BoundToSummonerId = definition.Binding == ItemBinding.SummonerBound ? boundToSummonerId : null,
            EquippedBySummonerId = null,
            EquippedSlot = null
        };

        // Add to profile (reuse existingItems from duplicate check)
        existingItems.Add(instance);
        _profileRepo.SaveItems(existingItems);

        GD.Print($"ItemService: Granted item '{catalogId}' (instance: {instanceId})");
        EmitSignal(SignalName.ItemGranted, instanceId, catalogId);

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
            GD.PushWarning($"ItemService: No item mapping for boon '{boonId}'");
            return null;
        }

        return GrantItem(itemId, summonerId);
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
        if (_profileRepo == null) return false;

        // Get item instance
        var items = _profileRepo.ListItems();
        var item = items.FirstOrDefault(i => i.Id == itemInstanceId);
        if (item == null)
        {
            GD.PushError($"ItemService: Item instance not found: {itemInstanceId}");
            return false;
        }

        // Verify item definition exists and matches slot
        var definition = ItemCatalog.GetItem(item.CatalogId);
        if (definition == null)
        {
            GD.PushError($"ItemService: Item definition not found: {item.CatalogId}");
            return false;
        }

        if (definition.Slot != slot)
        {
            GD.PushError($"ItemService: Item '{item.CatalogId}' cannot be equipped to slot '{slot}' (requires '{definition.Slot}')");
            return false;
        }

        // Check binding restrictions
        if (definition.Binding == ItemBinding.SummonerBound && item.BoundToSummonerId != summonerId)
        {
            GD.PushError($"ItemService: Item '{itemInstanceId}' is bound to summoner '{item.BoundToSummonerId}', cannot equip to '{summonerId}'");
            return false;
        }

        // Check if item is already equipped by another summoner
        if (item.EquippedBySummonerId != null && item.EquippedBySummonerId != summonerId)
        {
            GD.PushError($"ItemService: Item '{itemInstanceId}' is already equipped by '{item.EquippedBySummonerId}'");
            return false;
        }

        // Get summoner instance
        var summoner = _profileRepo.GetSummonerInstance(summonerId);
        if (summoner == null)
        {
            GD.PushError($"ItemService: Summoner not found: {summonerId}");
            return false;
        }

        var slotKey = slot.ToString().ToLowerInvariant();

        // Unequip current item in slot (if any)
        if (summoner.EquippedItems.TryGetValue(slotKey, out var currentItemId) && currentItemId != null)
        {
            var currentItem = items.FirstOrDefault(i => i.Id == currentItemId);
            if (currentItem != null)
            {
                currentItem.EquippedBySummonerId = null;
                currentItem.EquippedSlot = null;
            }
        }

        // Equip the new item
        item.EquippedBySummonerId = summonerId;
        item.EquippedSlot = slotKey;
        summoner.EquippedItems[slotKey] = itemInstanceId;

        // Save changes
        _profileRepo.SaveItems(items);
        _profileRepo.SaveSummonerInstance(summoner);

        GD.Print($"ItemService: Equipped item '{item.CatalogId}' to {summonerId}'s {slot} slot");
        EmitSignal(SignalName.ItemEquipped, summonerId, slotKey, itemInstanceId);

        return true;
    }

    /// <summary>
    /// Unequip an item from a summoner's slot.
    /// Returns true if successful.
    /// </summary>
    public bool UnequipItem(string summonerId, ItemSlot slot)
    {
        if (_profileRepo == null) return false;

        var summoner = _profileRepo.GetSummonerInstance(summonerId);
        if (summoner == null)
        {
            GD.PushError($"ItemService: Summoner not found: {summonerId}");
            return false;
        }

        var slotKey = slot.ToString().ToLowerInvariant();

        if (!summoner.EquippedItems.TryGetValue(slotKey, out var itemInstanceId) || itemInstanceId == null)
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
        summoner.EquippedItems[slotKey] = null;
        _profileRepo.SaveSummonerInstance(summoner);

        GD.Print($"ItemService: Unequipped item from {summonerId}'s {slot} slot");
        EmitSignal(SignalName.ItemUnequipped, summonerId, slotKey);

        return true;
    }

    // =========================================================================
    // QUERIES - OWNERSHIP
    // =========================================================================

    /// <summary>
    /// Get all AccountWide items (accessible by any summoner).
    /// </summary>
    public List<ItemInstanceData> GetAccountWideItems()
    {
        if (_profileRepo == null) return [];

        return _profileRepo.ListItems()
            .Where(item => ItemCatalog.GetItem(item.CatalogId)?.Binding == ItemBinding.AccountWide)
            .ToList();
    }

    /// <summary>
    /// Get SummonerBound items for a specific summoner.
    /// </summary>
    public List<ItemInstanceData> GetSummonerBoundItems(string summonerId)
    {
        if (_profileRepo == null) return [];

        return _profileRepo.ListItems()
            .Where(item => ItemCatalog.GetItem(item.CatalogId)?.Binding == ItemBinding.SummonerBound
                && item.BoundToSummonerId == summonerId)
            .ToList();
    }

    /// <summary>
    /// Get all items owned by a summoner based on binding rules.
    /// Returns AccountWide items + SummonerBound items bound to this summoner.
    /// </summary>
    public List<ItemInstanceData> GetOwnedItems(string summonerId)
    {
        return GetAccountWideItems()
            .Concat(GetSummonerBoundItems(summonerId))
            .ToList();
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
            [ItemSlot.Weapon] = null,
            [ItemSlot.Ring1] = null,
            [ItemSlot.Ring2] = null,
            [ItemSlot.Vestments] = null
        };

        if (_profileRepo == null) return result;

        var summoner = _profileRepo.GetSummonerInstance(summonerId);
        if (summoner == null) return result;

        foreach (var (slotKey, instanceId) in summoner.EquippedItems)
        {
            if (Enum.TryParse<ItemSlot>(slotKey, ignoreCase: true, out var slot))
            {
                result[slot] = instanceId;
            }
        }

        return result;
    }

    /// <summary>
    /// Get all items available for a specific slot.
    /// Uses GetOwnedItems for ownership filtering, then filters by slot and equip status.
    /// </summary>
    public List<ItemInstanceData> ListItemsForSlot(ItemSlot slot, string summonerId)
    {
        var result = new List<ItemInstanceData>();

        foreach (var item in GetOwnedItems(summonerId))
        {
            var definition = ItemCatalog.GetItem(item.CatalogId);
            if (definition == null) continue;

            // Filter by slot
            if (definition.Slot != slot) continue;

            // Filter out items equipped by another summoner
            if (!string.IsNullOrEmpty(item.EquippedBySummonerId) && item.EquippedBySummonerId != summonerId) continue;

            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Get all items in the player's inventory.
    /// </summary>
    public List<ItemInstanceData> ListAllItems()
    {
        if (_profileRepo == null) return [];
        return _profileRepo.ListItems();
    }

    /// <summary>
    /// Clear all items from inventory (for testing/debugging).
    /// </summary>
    public void ClearAllItems()
    {
        if (_profileRepo == null) return;
        _profileRepo.SaveItems([]);
        GD.Print("ItemService: Cleared all items from inventory");
    }

    /// <summary>
    /// Get item instance by ID.
    /// </summary>
    public ItemInstanceData? GetItem(string instanceId)
    {
        if (_profileRepo == null) return null;
        return _profileRepo.ListItems().FirstOrDefault(i => i.Id == instanceId);
    }

    // =========================================================================
    // MODIFIERS (for stat computation)
    // =========================================================================

    /// <summary>
    /// Get all modifiers from equipped items for a summoner.
    /// </summary>
    public List<TraitModifier> GetEquippedItemModifiers(string summonerId)
    {
        var modifiers = new List<TraitModifier>();

        if (_profileRepo == null) return modifiers;

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

    // =========================================================================
    // GODOT INTEROP
    // =========================================================================

    /// <summary>Get equipped items as dictionary for GDScript.</summary>
    public Godot.Collections.Dictionary GetEquippedItemsDict(string summonerId)
    {
        var result = new Godot.Collections.Dictionary();
        var equipped = GetEquippedItems(summonerId);

        foreach (var (slot, instanceId) in equipped)
        {
            result[slot.ToString().ToLowerInvariant()] = instanceId ?? "";
        }

        return result;
    }

    /// <summary>Get owned items as array of dictionaries for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetOwnedItemsDict(string summonerId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var item in GetOwnedItems(summonerId))
        {
            var definition = ItemCatalog.GetItem(item.CatalogId);
            if (definition == null) continue;

            var dict = ItemCatalog.ToDictionary(definition);
            dict["instance_id"] = item.Id;
            dict["equipped_by"] = item.EquippedBySummonerId ?? "";
            dict["bound_to"] = item.BoundToSummonerId ?? "";
            result.Add(dict);
        }

        return result;
    }

    /// <summary>Get items for slot as array of dictionaries for GDScript.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> ListItemsForSlotDict(string slotName, string summonerId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (!Enum.TryParse<ItemSlot>(slotName, ignoreCase: true, out var slot))
            return result;

        foreach (var item in ListItemsForSlot(slot, summonerId))
        {
            var definition = ItemCatalog.GetItem(item.CatalogId);
            if (definition == null) continue;

            var dict = ItemCatalog.ToDictionary(definition);
            dict["instance_id"] = item.Id;
            dict["equipped_by"] = item.EquippedBySummonerId ?? "";
            result.Add(dict);
        }

        return result;
    }

    /// <summary>Equip item from GDScript (slot as string).</summary>
    public bool EquipItemStr(string summonerId, string itemInstanceId, string slotName)
    {
        if (!Enum.TryParse<ItemSlot>(slotName, ignoreCase: true, out var slot))
        {
            GD.PushError($"ItemService: Invalid slot name: {slotName}");
            return false;
        }
        return EquipItem(summonerId, itemInstanceId, slot);
    }

    /// <summary>Unequip item from GDScript (slot as string).</summary>
    public bool UnequipItemStr(string summonerId, string slotName)
    {
        if (!Enum.TryParse<ItemSlot>(slotName, ignoreCase: true, out var slot))
        {
            GD.PushError($"ItemService: Invalid slot name: {slotName}");
            return false;
        }
        return UnequipItem(summonerId, slot);
    }
}
