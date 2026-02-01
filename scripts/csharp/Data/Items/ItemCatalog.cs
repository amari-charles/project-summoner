using System.Linq;
using Godot;

namespace ProjectSummoner.Data.Items;

/// <summary>
/// Central registry of all item definitions.
/// Provides query methods and GDScript interop.
/// Uses ItemDefinitions as the source of truth.
/// </summary>
public static class ItemCatalog
{
    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get an item definition by ID. Returns null if not found.</summary>
    public static ItemDefinition? GetItem(ItemId id) => ItemDefinitions.Get(id);

    /// <summary>Get an item definition by string ID. Returns null if not found.</summary>
    public static ItemDefinition? GetItem(string id) => ItemDefinitions.Get(id);

    /// <summary>Check if an item exists in the catalog.</summary>
    public static bool HasItem(ItemId id) => ItemDefinitions.Has(id);

    /// <summary>Check if an item exists in the catalog by string ID.</summary>
    public static bool HasItem(string id) => ItemDefinitions.Has(id);

    /// <summary>Get all item IDs.</summary>
    public static string[] GetAllItemIds() => ItemDefinitions.All.Select(i => (string)i.Id).ToArray();

    /// <summary>Get all item definitions.</summary>
    public static ItemDefinition[] GetAllItems() => [.. ItemDefinitions.All];

    /// <summary>Get item count.</summary>
    public static int Count => ItemDefinitions.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get items by slot.</summary>
    public static ItemDefinition[] GetItemsBySlot(ItemSlot slot) =>
        ItemDefinitions.All.Where(i => i.Slot == slot).ToArray();

    /// <summary>Get items by rarity.</summary>
    public static ItemDefinition[] GetItemsByRarity(string rarity) =>
        ItemDefinitions.All.Where(i => i.Rarity == rarity).ToArray();

    /// <summary>Get items by binding type.</summary>
    public static ItemDefinition[] GetItemsByBinding(ItemBinding binding) =>
        ItemDefinitions.All.Where(i => i.Binding == binding).ToArray();

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>Convert an ItemDefinition to a Godot Dictionary for GDScript consumption.</summary>
    public static Godot.Collections.Dictionary ToDictionary(ItemDefinition item)
    {
        var modifiersArray = new Godot.Collections.Array();
        foreach (var mod in item.Modifiers)
        {
            var modDict = new Godot.Collections.Dictionary();
            if (!string.IsNullOrEmpty(mod.Stat))
            {
                modDict["stat"] = mod.Stat;
                modDict["type"] = mod.Type;
                modDict["value"] = mod.Value;
            }
            modifiersArray.Add(modDict);
        }

        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = (string)item.Id,
            ["name_key"] = item.NameKey,
            ["description_key"] = item.DescriptionKey,
            ["slot"] = item.Slot.ToString().ToLowerInvariant(),
            ["binding"] = item.Binding.ToString(),
            ["rarity"] = item.Rarity,
            ["modifiers"] = modifiersArray
        };

        if (!string.IsNullOrEmpty(item.IconPath))
            dict["icon_path"] = item.IconPath;

        return dict;
    }

    /// <summary>Get item as dictionary for GDScript. Returns empty dict if not found.</summary>
    public static Godot.Collections.Dictionary GetItemAsDict(string id)
    {
        var item = GetItem(id);
        return item != null ? ToDictionary(item) : new Godot.Collections.Dictionary();
    }

    /// <summary>Get all items as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllItemsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var item in ItemDefinitions.All)
        {
            result.Add(ToDictionary(item));
        }
        return result;
    }

    /// <summary>Get items by slot as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetItemsBySlotAsDict(string slotName)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        if (System.Enum.TryParse<ItemSlot>(slotName, ignoreCase: true, out var slot))
        {
            foreach (var item in GetItemsBySlot(slot))
            {
                result.Add(ToDictionary(item));
            }
        }
        return result;
    }
}
