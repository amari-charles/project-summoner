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

        // Create new item instance
        var instanceId = Guid.NewGuid().ToString();
        var instance = new ItemInstanceData
        {
            Id = instanceId,
            CatalogId = catalogId,
            BoundToSummonerId = definition.Binding == ItemBinding.SummonerBound ? boundToSummonerId : null,
            EquippedBySummonerId = null,
            EquippedSlot = null
        };

        // Add to profile
        var items = _profileRepo.ListItems();
        items.Add(instance);
        _profileRepo.SaveItems(items);

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
    // QUERIES
    // =========================================================================

    /// <summary>
    /// Get equipped items for a summoner.
    /// Returns a dictionary of slot -> item instance ID (or null if empty).
    /// </summary>
    public Dictionary<ItemSlot, string?> GetEquippedItems(string summonerId)
    {
        var result = new Dictionary<ItemSlot, string?>
        {
            [ItemSlot.Grimoire] = null,
            [ItemSlot.Weapon] = null,
            [ItemSlot.Ring] = null,
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
    /// For AccountWide items, returns all unequipped items of that slot.
    /// For SummonerBound items, filters by bound summoner.
    /// </summary>
    public List<ItemInstanceData> ListItemsForSlot(ItemSlot slot, string summonerId)
    {
        if (_profileRepo == null) return [];

        var items = _profileRepo.ListItems();
        var result = new List<ItemInstanceData>();

        foreach (var item in items)
        {
            var definition = ItemCatalog.GetItem(item.CatalogId);
            if (definition == null || definition.Slot != slot) continue;

            // Check binding
            if (definition.Binding == ItemBinding.SummonerBound)
            {
                if (item.BoundToSummonerId != summonerId) continue;
            }

            // Check if already equipped by another summoner
            if (item.EquippedBySummonerId != null && item.EquippedBySummonerId != summonerId) continue;

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
