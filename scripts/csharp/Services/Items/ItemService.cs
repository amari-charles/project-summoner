using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Traits;
using ProjectSummoner.Infrastructure.Persistence;
using ProjectSummoner.Services.Items.Handlers;
using ProjectSummoner.Stats;
using ProjectSummoner.Systems.Modifiers;
using ItemSlot = ProjectSummoner.Domain.Profile.Inventory.ItemSlot;
using ItemInstance = ProjectSummoner.Domain.Profile.Inventory.ItemInstance;

namespace ProjectSummoner.Services.Items;

/// <summary>
/// Item Service - Handles item granting, equipping, and inventory management.
///
/// Items are stored in ProfileData.Items as instances.
/// Equipped items are tracked per-summoner in SummonerInstance.EquippedItems.
///
/// Uses Facade + Handlers pattern for clean separation of concerns.
/// </summary>
[GlobalClass]
public partial class ItemService : Node
{
	public static ItemService? Instance { get; private set; }

	private IProfileRepository? _profileRepo;
	private ItemOwnershipHandler? _ownership;
	private ItemEquipmentHandler? _equipment;

	// =========================================================================
	// SIGNALS
	// =========================================================================

	[Signal]
	public delegate void ItemGrantedEventHandler(string instanceId, string catalogId);

	[Signal]
	public delegate void ItemEquippedEventHandler(string summonerId, string slot, string? itemInstanceId);

	[Signal]
	public delegate void ItemUnequippedEventHandler(string summonerId, string slot);

	// =========================================================================
	// LIFECYCLE
	// =========================================================================

	public override void _Ready()
	{
		Instance = this;
		Initialize();
	}

	private void Initialize()
	{
		GD.Print("ItemService: Initializing...");

		_profileRepo = ProfileRepository.Instance;

		if (_profileRepo == null)
		{
			GD.PushError("ItemService: ProfileRepository.Instance not available");
			return;
		}

		_ownership = new ItemOwnershipHandler(_profileRepo);
		_equipment = new ItemEquipmentHandler(_profileRepo, _ownership);

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
		_ownership = new ItemOwnershipHandler(repo);
		_equipment = new ItemEquipmentHandler(repo, _ownership);
	}

	// =========================================================================
	// ITEM GRANTING (delegates to ItemOwnershipHandler)
	// =========================================================================

	/// <summary>Grant an item to the player's inventory.</summary>
	public string? GrantItem(string catalogId, string? boundToSummonerId = null)
	{
		var instanceId = _ownership?.GrantItem(catalogId, boundToSummonerId);
		if (instanceId != null)
		{
			EmitSignal(SignalName.ItemGranted, instanceId, catalogId);
		}
		return instanceId;
	}

	// =========================================================================
	// EQUIPPING (delegates to ItemEquipmentHandler)
	// =========================================================================

	/// <summary>Equip an item to a summoner's slot.</summary>
	public bool EquipItem(string summonerId, string itemInstanceId, ItemSlot slot)
	{
		var success = _equipment?.EquipItem(summonerId, itemInstanceId, slot) ?? false;
		if (success)
		{
			EmitSignal(SignalName.ItemEquipped, summonerId, slot.ToString().ToLowerInvariant(), itemInstanceId);
		}
		return success;
	}

	/// <summary>Unequip an item from a summoner's slot.</summary>
	public bool UnequipItem(string summonerId, ItemSlot slot)
	{
		var success = _equipment?.UnequipItem(summonerId, slot) ?? false;
		if (success)
		{
			EmitSignal(SignalName.ItemUnequipped, summonerId, slot.ToString().ToLowerInvariant());
		}
		return success;
	}

	// =========================================================================
	// QUERIES - OWNERSHIP (delegates to ItemOwnershipHandler)
	// =========================================================================

	/// <summary>Get all AccountWide items (accessible by any summoner).</summary>
	public List<ItemInstance> GetAccountWideItems()
	{
		return _ownership?.GetAccountWideItems() ?? [];
	}

	/// <summary>Get SummonerBound items for a specific summoner.</summary>
	public List<ItemInstance> GetSummonerBoundItems(string summonerId)
	{
		return _ownership?.GetSummonerBoundItems(summonerId) ?? [];
	}

	/// <summary>Get all items owned by a summoner based on binding rules.</summary>
	public List<ItemInstance> GetOwnedItems(string summonerId)
	{
		return _ownership?.GetOwnedItems(summonerId) ?? [];
	}

	/// <summary>Get all items in the player's inventory.</summary>
	public List<ItemInstance> ListAllItems()
	{
		return _ownership?.ListAllItems() ?? [];
	}

	/// <summary>Get item instance by ID.</summary>
	public ItemInstance? GetItem(string instanceId)
	{
		return _ownership?.GetItem(instanceId);
	}

	/// <summary>Clear all items from inventory (for testing/debugging).</summary>
	public void ClearAllItems()
	{
		_ownership?.ClearAllItems();
	}

	// =========================================================================
	// QUERIES - EQUIPMENT (delegates to ItemEquipmentHandler)
	// =========================================================================

	/// <summary>Get equipped items for a summoner.</summary>
	public Dictionary<ItemSlot, string?> GetEquippedItems(string summonerId)
	{
		return _equipment?.GetEquippedItems(summonerId) ?? new Dictionary<ItemSlot, string?>
		{
			[ItemSlot.Wand] = null,
			[ItemSlot.Ring1] = null,
			[ItemSlot.Ring2] = null,
			[ItemSlot.Robes] = null
		};
	}

	/// <summary>Get all items available for a specific slot.</summary>
	public List<ItemInstance> ListItemsForSlot(ItemSlot slot, string summonerId)
	{
		return _equipment?.ListItemsForSlot(slot, summonerId) ?? [];
	}

	/// <summary>Get all modifiers from equipped items for a summoner.</summary>
	public List<TraitModifier> GetEquippedItemModifiers(string summonerId)
	{
		return _equipment?.GetEquippedItemModifiers(summonerId) ?? [];
	}

	/// <summary>
	/// Get equipped item modifiers as StatModifiers for the modifier system.
	/// This converts TraitModifiers to StatModifiers that can be applied to units.
	/// </summary>
	public List<StatModifier> GetEquippedItemStatModifiers(string summonerId)
	{
		var traitModifiers = GetEquippedItemModifiers(summonerId);
		var statModifiers = new List<StatModifier>();

		foreach (var traitMod in traitModifiers)
		{
			var statMod = ConvertTraitModifierToStatModifier(traitMod, summonerId);
			if (statMod != null)
			{
				statModifiers.Add(statMod);
			}
		}

		return statModifiers;
	}

	/// <summary>
	/// Convert a TraitModifier to a StatModifier.
	/// Maps the trait modifier format to the stat modifier format used by ModifierService.
	/// </summary>
	private static StatModifier? ConvertTraitModifierToStatModifier(TraitModifier traitMod, string summonerId)
	{
		var statMod = new StatModifier
		{
			Source = traitMod.Source ?? $"item_{summonerId}"
		};

		// Copy conditions
		if (traitMod.Conditions != null)
		{
			foreach (var kvp in traitMod.Conditions)
			{
				statMod.Conditions[kvp.Key] = kvp.Value;
			}
		}

		// Copy stat multipliers (already typed as StatKey)
		if (traitMod.StatMults != null)
		{
			foreach (var kvp in traitMod.StatMults)
			{
				statMod.StatMults[kvp.Key] = kvp.Value;
			}
		}

		// Copy stat adds (already typed as StatKey)
		if (traitMod.StatAdds != null)
		{
			foreach (var kvp in traitMod.StatAdds)
			{
				statMod.StatAdds[kvp.Key] = kvp.Value;
			}
		}

		// Convert summoner stat format (Stat/Type/Value) to unit modifier format
		if (traitMod.HasSummonerStat)
		{
			// Map item stat to unit stat (some don't have unit equivalents)
			var unitStat = MapItemStatToUnitStat(traitMod.Stat!.Value);
			if (unitStat.HasValue)
			{
				if (traitMod.Type == ModifierType.Percent)
				{
					// Convert percent to multiplier (5% -> 1.05)
					statMod.StatMults[unitStat.Value] = 1.0f + (traitMod.Value / 100.0f);
				}
				else // Flat
				{
					statMod.StatAdds[unitStat.Value] = traitMod.Value;
				}
			}
		}

		// Only return if the modifier has any actual stats to apply
		if (statMod.StatAdds.Count == 0 && statMod.StatMults.Count == 0 && statMod.Flags.Count == 0)
		{
			return null;
		}

		return statMod;
	}

	/// <summary>
	/// Map item stat keys to unit stat keys.
	/// Some item stats don't have unit equivalents (e.g., GoldBonus).
	/// Returns null for stats that don't apply to units.
	/// </summary>
	private static StatKey? MapItemStatToUnitStat(StatKey itemStat)
	{
		return itemStat switch
		{
			StatKey.MaxHealth => StatKey.MaxHp,
			StatKey.DamageBonus => StatKey.AttackDamage,
			StatKey.AttackSpeed => StatKey.AttackSpeed,
			StatKey.MoveSpeed => StatKey.MoveSpeed,
			StatKey.MaxHp => StatKey.MaxHp,
			StatKey.AttackDamage => StatKey.AttackDamage,
			// These don't apply to units, return null
			StatKey.GoldBonus => null,
			StatKey.XpBonus => null,
			StatKey.ManaRegen => null,
			// Elemental bonuses don't apply directly to unit stats
			StatKey.FireDamageBonus => null,
			StatKey.WaterDamageBonus => null,
			StatKey.WindDamageBonus => null,
			StatKey.EarthDamageBonus => null,
			StatKey.LightningDamageBonus => null,
			StatKey.DeathDamageBonus => null,
			StatKey.HealingBonus => null,
			StatKey.CastSpeed => null,
			StatKey.DamageReduction => null,
			StatKey.Lifesteal => null,
			// Pass through core unit stats
			_ => itemStat
		};
	}

	// =========================================================================
	// GODOT INTEROP
	// =========================================================================

	/// <summary>
	/// Get equipped item stat modifiers as array of dictionaries for GDScript.
	/// Each dictionary contains the StatModifier fields (source, stat_adds, stat_mults, etc.)
	/// </summary>
	public Godot.Collections.Array<Godot.Collections.Dictionary> GetEquippedItemStatModifiersDict(string summonerId)
	{
		var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
		var modifiers = GetEquippedItemStatModifiers(summonerId);

		foreach (var modifier in modifiers)
		{
			result.Add(modifier.ToDictionary());
		}

		return result;
	}

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
			dict["instance_id"] = (string)item.Id;
			dict["equipped_by"] = item.EquippedBySummonerId != null ? (string)item.EquippedBySummonerId : "";
			dict["bound_to"] = item.BoundToSummonerId != null ? (string)item.BoundToSummonerId : "";
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
			dict["instance_id"] = (string)item.Id;
			dict["equipped_by"] = item.EquippedBySummonerId != null ? (string)item.EquippedBySummonerId : "";
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

	// =========================================================================
	// HELPERS
	// =========================================================================

	/// <summary>Convert string to slot enum, returns null if invalid.</summary>
	public static ItemSlot? StringToSlot(string slotName)
	{
		return Enum.TryParse<ItemSlot>(slotName, ignoreCase: true, out var slot) ? slot : null;
	}
}
