using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards.Effects.Concrete;
using ProjectSummoner.Cards.Formations;

namespace ProjectSummoner.Cards;

/// <summary>
/// Central registry of all card definitions.
/// Provides type-safe card lookup and query methods.
/// GDScript can call this via the CardCatalogBridge autoload.
/// </summary>
public static class CardCatalog
{
    // =========================================================================
    // CARD DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<string, CardDefinition> _cards = new()
    {
        // =====================================================================
        // SPELLS
        // =====================================================================

        ["fireball"] = new CardDefinition
        {
            Id = "fireball",
            Name = "Fireball",
            Description = "Unleash a devastating explosion of flame. Deals area damage to all enemies caught in the blast.",
            Rarity = Rarity.Rare,
            Type = CardType.Spell,
            ManaCost = 5,
            Cooldown = 2.0f,
            SummonTime = 0.0f,
            SpellDamage = 100.0f,
            SpellRadius = 10.0f,
            SpellDuration = 0.5f,
            ProjectileId = "fireball",
            SpellVfx = "fireball_spell",
            Tags = ["spell", "aoe", "damage"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Fire
        },

        ["rally"] = new CardDefinition
        {
            Id = "rally",
            Name = "Rally",
            Description = "Command nearby units to move to a target location and defend that zone until enemies are cleared.",
            Rarity = Rarity.Common,
            Type = CardType.Spell,
            ManaCost = 0,
            Cooldown = 1.0f,
            SummonTime = 0.0f,
            CommandType = CommandType.Rally,
            SelectionRadius = 8.0f,
            Tags = ["spell", "command", "tactical", "movement"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Neutral
        },

        ["guard"] = new CardDefinition
        {
            Id = "guard",
            Name = "Guard",
            Description = "Command nearby units to form a defensive formation for 25 seconds. Melee units protect ranged units in the back line.",
            Rarity = Rarity.Common,
            Type = CardType.Spell,
            ManaCost = 0,
            Cooldown = 1.0f,
            SummonTime = 0.0f,
            CommandType = CommandType.Guard,
            SelectionRadius = 8.0f,
            FormationDuration = 25.0f,
            Tags = ["spell", "command", "tactical", "formation"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Neutral
        },

        ["charge"] = new CardDefinition
        {
            Id = "charge",
            Name = "Charge",
            Description = "Command nearby units to launch a coordinated attack on the closest enemy (unit, structure, or base) to the target location for 30 seconds.",
            Rarity = Rarity.Common,
            Type = CardType.Spell,
            ManaCost = 0,
            Cooldown = 1.0f,
            SummonTime = 0.0f,
            CommandType = CommandType.Charge,
            SelectionRadius = 8.0f,
            Tags = ["spell", "command", "tactical", "focus_fire"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Neutral
        },

        // =====================================================================
        // FIRE ELEMENT UNITS
        // =====================================================================

        ["fire_elemental"] = new CardDefinition
        {
            Id = "fire_elemental",
            Name = "Fire Elemental",
            Description = "A floating spirit of pure flame. Hovers across the battlefield, burning all in its path.",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 3,
            Cooldown = 2.0f,
            SummonTime = 1.0f,
            UnitScenePath = "res://scenes/units/fire_elemental_3d.tscn",
            SpawnCount = 1,
            Formation = FormationPresets.StandardGrid,
            UnitType = UnitType.Melee,
            MaxHp = 60.0f,
            AttackDamage = 12.0f,
            AttackRange = 2.0f,
            AttackSpeed = 1.2f,
            MoveSpeed = 3.5f,
            AggroRadius = 20.0f,
            IsRanged = false,
            Tags = ["melee", "fire", "floating", "spirit"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Fire
        },

        ["fire_titan"] = new CardDefinition
        {
            Id = "fire_titan",
            Name = "Fire Titan",
            Description = "A colossal spirit of ancient flame. Towers over the battlefield, absorbing damage while scorching all who approach.",
            Rarity = Rarity.Epic,
            Type = CardType.Summon,
            ManaCost = 7,
            Cooldown = 3.0f,
            SummonTime = 2.0f,
            UnitScenePath = "res://scenes/units/fire_titan_3d.tscn",
            SpawnCount = 1,
            Formation = FormationPresets.StandardGrid,
            UnitType = UnitType.Melee,
            MaxHp = 300.0f,
            AttackDamage = 20.0f,
            AttackRange = 3.0f,
            AttackSpeed = 0.8f,
            MoveSpeed = 2.0f,
            AggroRadius = 20.0f,
            IsRanged = false,
            Tags = ["melee", "fire", "floating", "spirit", "tank", "giant"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Fire
        },

        ["fire_elemental_swarm"] = new CardDefinition
        {
            Id = "fire_elemental_swarm",
            Name = "Fire Swarm",
            Description = "Unleash a horde of flame spirits. Twelve smaller fire elementals surge forth to overwhelm the enemy.",
            Rarity = Rarity.Rare,
            Type = CardType.Summon,
            ManaCost = 7,
            Cooldown = 4.0f,
            SummonTime = 2.5f,
            UnitScenePath = "res://scenes/units/fire_elemental_3d.tscn",
            SpawnCount = 12,
            Formation = FormationPresets.TightSwarmGrid,
            UnitType = UnitType.Melee,
            MaxHp = 45.0f,
            AttackDamage = 9.0f,
            AttackRange = 2.0f,
            AttackSpeed = 1.2f,
            MoveSpeed = 3.5f,
            AggroRadius = 20.0f,
            IsRanged = false,
            Tags = ["melee", "fire", "floating", "spirit", "swarm"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Fire
        },

        ["fire_ant"] = new CardDefinition
        {
            Id = "fire_ant",
            Name = "Fire Ant",
            Description = "A swift and fierce fire ant. Scurries across the battlefield with blazing speed, overwhelming foes with relentless attacks.",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 2,
            Cooldown = 1.5f,
            SummonTime = 0.8f,
            UnitScenePath = "res://scenes/units/fire_ant_3d.tscn",
            SpawnCount = 1,
            Formation = FormationPresets.StandardGrid,
            UnitType = UnitType.Melee,
            MaxHp = 40.0f,
            AttackDamage = 8.0f,
            AttackRange = 1.8f,
            AttackSpeed = 1.5f,
            MoveSpeed = 4.5f,
            AggroRadius = 20.0f,
            IsRanged = false,
            Tags = ["melee", "fire", "insect", "fast"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Fire
        },

        ["fire_ant_swarm"] = new CardDefinition
        {
            Id = "fire_ant_swarm",
            Name = "Fire Ant Swarm",
            Description = "Release a colony of fire ants! Twenty tiny terrors surge forth in formation, overwhelming enemies with sheer numbers.",
            Rarity = Rarity.Epic,
            Type = CardType.Summon,
            ManaCost = 6,
            Cooldown = 4.0f,
            SummonTime = 2.0f,
            UnitScenePath = "res://scenes/units/fire_ant_3d.tscn",
            SpawnCount = 20,
            Formation = FormationPresets.FireAntSwarm,
            UnitType = UnitType.Melee,
            MaxHp = 40.0f,
            AttackDamage = 8.0f,
            AttackRange = 1.8f,
            AttackSpeed = 1.5f,
            MoveSpeed = 4.5f,
            AggroRadius = 20.0f,
            IsRanged = false,
            Tags = ["melee", "fire", "insect", "fast", "swarm"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Fire
        },

        // =====================================================================
        // EARTH ELEMENT UNITS
        // =====================================================================

        ["earth_sprite"] = new CardDefinition
        {
            Id = "earth_sprite",
            Name = "Earth Sprite",
            Description = "A gentle rock spirit with a sapling growing from its head. Sturdy and dependable, it protects nature with rocky determination.",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 3,
            Cooldown = 2.0f,
            SummonTime = 1.0f,
            UnitScenePath = "res://scenes/units/earth_sprite_3d.tscn",
            SpawnCount = 1,
            Formation = FormationPresets.StandardGrid,
            UnitType = UnitType.Melee,
            MaxHp = 150.0f,
            AttackDamage = 18.0f,
            AttackRange = 2.0f,
            AttackSpeed = 0.9f,
            MoveSpeed = 1.8f,
            AggroRadius = 20.0f,
            IsRanged = false,
            Tags = ["melee", "earth", "elemental", "nature"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Earth
        },

        ["rock"] = new CardDefinition
        {
            Id = "rock",
            Name = "Rock",
            Description = "A stationary target dummy for testing. Does not move or attack.",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 0,
            Cooldown = 0.5f,
            SummonTime = 0.0f,
            UnitScenePath = "res://scenes/units/rock_3d.tscn",
            SpawnCount = 1,
            Formation = FormationPresets.StandardGrid,
            UnitType = UnitType.Melee,
            MaxHp = 500.0f,
            AttackDamage = 0.0f,
            AttackRange = 2.0f,
            AttackSpeed = 0.0f,
            MoveSpeed = 0.0f,
            AggroRadius = 0.0f,
            IsRanged = false,
            Tags = ["melee", "earth", "test", "dummy", "stationary"],
            UnlockCondition = UnlockCondition.DevOnly,
            ElementalAffinity = Element.Earth
        },

        // =====================================================================
        // WIND ELEMENT UNITS
        // =====================================================================

        ["puff"] = new CardDefinition
        {
            Id = "puff",
            Name = "Puff",
            Description = "A mischievous cloud spirit that blows gusts of wind at its foes. Light and agile, it drifts across the battlefield.",
            Rarity = Rarity.Common,
            Type = CardType.Summon,
            ManaCost = 3,
            Cooldown = 2.0f,
            SummonTime = 1.0f,
            UnitScenePath = "res://scenes/units/puff_3d.tscn",
            SpawnCount = 1,
            Formation = FormationPresets.StandardGrid,
            UnitType = UnitType.Ranged,
            MaxHp = 80.0f,
            AttackDamage = 12.0f,
            AttackRange = 24.0f,
            AttackSpeed = 0.4f,
            MoveSpeed = 2.5f,
            AggroRadius = 24.0f,
            IsRanged = true,
            Tags = ["ranged", "wind", "elemental", "air"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Wind
        },

        ["cloud_swarm"] = new CardDefinition
        {
            Id = "cloud_swarm",
            Name = "Cloud Swarm",
            Description = "A swirling formation of cloud wisps. Six clouds drift together in pairs, overwhelming foes with their combined might.",
            Rarity = Rarity.Rare,
            Type = CardType.Summon,
            ManaCost = 5,
            Cooldown = 3.0f,
            SummonTime = 1.5f,
            UnitScenePath = "res://scenes/units/puff_3d.tscn",
            SpawnCount = 6,
            Formation = FormationPresets.CloudSwarm,
            UnitType = UnitType.Ranged,
            MaxHp = 80.0f,
            AttackDamage = 12.0f,
            AttackRange = 24.0f,
            AttackSpeed = 0.4f,
            MoveSpeed = 2.5f,
            AggroRadius = 24.0f,
            IsRanged = true,
            Tags = ["ranged", "wind", "elemental", "air", "swarm"],
            UnlockCondition = UnlockCondition.Default,
            ElementalAffinity = Element.Wind
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a card definition by ID. Returns null if not found.</summary>
    public static CardDefinition? GetCard(string id)
    {
        return _cards.GetValueOrDefault(id);
    }

    /// <summary>Check if a card exists in the catalog.</summary>
    public static bool HasCard(string id)
    {
        return _cards.ContainsKey(id);
    }

    /// <summary>Get all card IDs.</summary>
    public static string[] GetAllCardIds()
    {
        return [.. _cards.Keys];
    }

    /// <summary>Get all card definitions.</summary>
    public static CardDefinition[] GetAllCards()
    {
        return [.. _cards.Values];
    }

    /// <summary>Get card count.</summary>
    public static int Count => _cards.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get cards by rarity.</summary>
    public static CardDefinition[] GetCardsByRarity(Rarity rarity)
    {
        return _cards.Values.Where(c => c.Rarity == rarity).ToArray();
    }

    /// <summary>Get cards by type.</summary>
    public static CardDefinition[] GetCardsByType(CardType type)
    {
        return _cards.Values.Where(c => c.Type == type).ToArray();
    }

    /// <summary>Get cards by tag.</summary>
    public static CardDefinition[] GetCardsByTag(string tag)
    {
        return _cards.Values.Where(c => c.Tags.Contains(tag)).ToArray();
    }

    /// <summary>Get starter/default cards.</summary>
    public static CardDefinition[] GetStarterCards()
    {
        return _cards.Values.Where(c => c.UnlockCondition == UnlockCondition.Default).ToArray();
    }

    /// <summary>Get cards by elemental affinity.</summary>
    public static CardDefinition[] GetCardsByElement(Element element)
    {
        return _cards.Values.Where(c => c.ElementalAffinity == element).ToArray();
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    /// <summary>
    /// Convert a CardDefinition to a Godot Dictionary for GDScript consumption.
    /// Maintains compatibility with existing GDScript code expecting dictionary format.
    /// Enums are converted to lowercase strings to match GDScript constants.
    /// </summary>
    public static Godot.Collections.Dictionary ToDictionary(CardDefinition card)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["catalog_id"] = card.Id,
            ["card_name"] = card.Name,
            ["description"] = card.Description,
            ["rarity"] = card.Rarity.ToString().ToLowerInvariant(),
            ["card_type"] = (int)card.Type,
            ["mana_cost"] = card.ManaCost,
            ["cooldown"] = card.Cooldown,
            ["summon_time"] = card.SummonTime,
            ["unit_scene_path"] = card.UnitScenePath,
            ["spawn_count"] = card.SpawnCount,
            ["unit_type"] = card.UnitType.ToString().ToLowerInvariant(),
            ["max_hp"] = card.MaxHp,
            ["attack_damage"] = card.AttackDamage,
            ["attack_range"] = card.AttackRange,
            ["attack_speed"] = card.AttackSpeed,
            ["move_speed"] = card.MoveSpeed,
            ["aggro_radius"] = card.AggroRadius,
            ["is_ranged"] = card.IsRanged,
            ["projectile_scene_path"] = card.ProjectileScenePath,
            ["spell_damage"] = card.SpellDamage,
            ["spell_radius"] = card.SpellRadius,
            ["spell_duration"] = card.SpellDuration,
            ["projectile_id"] = card.ProjectileId,
            ["spell_vfx"] = card.SpellVfx,
            ["command_type"] = card.CommandType?.ToString().ToLowerInvariant() ?? "",
            ["selection_radius"] = card.SelectionRadius,
            ["formation_duration"] = card.FormationDuration,
            ["unlock_condition"] = UnlockConditionToString(card.UnlockCondition),
            ["card_icon_path"] = card.CardIconPath
        };

        // Convert tags array
        var tagsArray = new Godot.Collections.Array();
        foreach (var tag in card.Tags)
        {
            tagsArray.Add(tag);
        }
        dict["tags"] = tagsArray;

        // Categories dict for elemental affinity (matches GDScript structure)
        var categories = new Godot.Collections.Dictionary
        {
            ["elemental_affinity"] = card.ElementalAffinity.ToString().ToLowerInvariant()
        };
        dict["categories"] = categories;

        return dict;
    }

    /// <summary>Convert UnlockCondition enum to GDScript-compatible string.</summary>
    private static string UnlockConditionToString(UnlockCondition condition) => condition switch
    {
        UnlockCondition.Default => "default",
        UnlockCondition.DevOnly => "dev_only",
        _ => condition.ToString().ToLowerInvariant()
    };

    /// <summary>Get card as dictionary for GDScript. Returns empty dict if not found.</summary>
    public static Godot.Collections.Dictionary GetCardAsDict(string id)
    {
        var card = GetCard(id);
        return card != null ? ToDictionary(card) : new Godot.Collections.Dictionary();
    }

    /// <summary>Get all cards as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCardsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in _cards.Values)
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by rarity as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByRarityAsDict(string rarity)
    {
        if (!Enum.TryParse<Rarity>(rarity, ignoreCase: true, out var rarityEnum))
            return [];

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByRarity(rarityEnum))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by type as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByTypeAsDict(int type)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByType((CardType)type))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by tag as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByTagAsDict(string tag)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByTag(tag))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get starter cards as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetStarterCardsAsDict()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetStarterCards())
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }

    /// <summary>Get cards by element as dictionaries for GDScript.</summary>
    public static Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByElementAsDict(string element)
    {
        if (!Enum.TryParse<Element>(element, ignoreCase: true, out var elementEnum))
            return [];

        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var card in GetCardsByElement(elementEnum))
        {
            result.Add(ToDictionary(card));
        }
        return result;
    }
}
