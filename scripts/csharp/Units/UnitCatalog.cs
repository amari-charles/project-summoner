using System.Collections.Generic;
using ProjectSummoner.Constants;
using ProjectSummoner.Stats;

namespace ProjectSummoner.Units;

/// <summary>
/// Defines what a unit type IS - its base stats and scene path.
/// This is the single source of truth for unit identity.
/// See docs/technical/unit-stat-pipeline.md for architecture details.
/// </summary>
public record UnitDefinition
{
    /// <summary>Unique identifier for this unit type (type-safe).</summary>
    public UnitId Id { get; init; } = UnitId.None;

    /// <summary>Path to the unit's scene file.</summary>
    public string ScenePath { get; init; } = "";

    /// <summary>Base stats for this unit type.</summary>
    public UnitStats BaseStats { get; init; } = UnitStats.Default;
}

/// <summary>
/// Central registry of all unit type definitions.
/// Provides the base stats that cards can modify.
/// See docs/technical/unit-stat-pipeline.md for architecture details.
/// </summary>
public static class UnitCatalog
{
    // =========================================================================
    // UNIT DEFINITIONS
    // =========================================================================

    private static readonly Dictionary<UnitId, UnitDefinition> _units = new()
    {
        // =====================================================================
        // WISPS (Basic starter units for each element)
        // =====================================================================

        [UnitIds.FireWisp] = new UnitDefinition
        {
            Id = UnitIds.FireWisp,
            ScenePath = "res://scenes/units/fire_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 60f,
                AttackDamage = 12f,
                AttackRange = 3.0f,
                AttackSpeed = 1.2f,
                MoveSpeed = 3.5f,
                AggroRadius = 20f
            }
        },

        [UnitIds.WaterWisp] = new UnitDefinition
        {
            Id = UnitIds.WaterWisp,
            ScenePath = "res://scenes/units/water_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 65f,
                AttackDamage = 10f,
                AttackRange = 3.0f,
                AttackSpeed = 1.1f,
                MoveSpeed = 3.2f,
                AggroRadius = 20f
            }
        },

        [UnitIds.WindWisp] = new UnitDefinition
        {
            Id = UnitIds.WindWisp,
            ScenePath = "res://scenes/units/wind_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 50f,
                AttackDamage = 10f,
                AttackRange = 3.0f,
                AttackSpeed = 1.4f,
                MoveSpeed = 4.0f,
                AggroRadius = 20f
            }
        },

        [UnitIds.EarthWisp] = new UnitDefinition
        {
            Id = UnitIds.EarthWisp,
            ScenePath = "res://scenes/units/earth_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 80f,
                AttackDamage = 14f,
                AttackRange = 3.0f,
                AttackSpeed = 0.9f,
                MoveSpeed = 2.8f,
                AggroRadius = 20f
            }
        },

        [UnitIds.LightningWisp] = new UnitDefinition
        {
            Id = UnitIds.LightningWisp,
            ScenePath = "res://scenes/units/lightning_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 45f,
                AttackDamage = 15f,
                AttackRange = 3.0f,
                AttackSpeed = 1.5f,
                MoveSpeed = 4.2f,
                AggroRadius = 20f
            }
        },

        [UnitIds.LifeWisp] = new UnitDefinition
        {
            Id = UnitIds.LifeWisp,
            ScenePath = "res://scenes/units/life_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 70f,
                AttackDamage = 8f,
                AttackRange = 3.0f,
                AttackSpeed = 1.0f,
                MoveSpeed = 3.0f,
                AggroRadius = 20f
            }
        },

        [UnitIds.DeathWisp] = new UnitDefinition
        {
            Id = UnitIds.DeathWisp,
            ScenePath = "res://scenes/units/death_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 55f,
                AttackDamage = 14f,
                AttackRange = 3.0f,
                AttackSpeed = 1.1f,
                MoveSpeed = 3.0f,
                AggroRadius = 20f
            }
        },

        [UnitIds.ShadowWisp] = new UnitDefinition
        {
            Id = UnitIds.ShadowWisp,
            ScenePath = "res://scenes/units/shadow_wisp_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 50f,
                AttackDamage = 12f,
                AttackRange = 3.0f,
                AttackSpeed = 1.3f,
                MoveSpeed = 3.8f,
                AggroRadius = 20f
            }
        },

        // =====================================================================
        // FIRE ELEMENT UNITS
        // =====================================================================

        [UnitIds.FireTitan] = new UnitDefinition
        {
            Id = UnitIds.FireTitan,
            ScenePath = "res://scenes/units/fire_titan_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 300f,
                AttackDamage = 20f,
                AttackRange = 5.0f,
                AttackSpeed = 0.8f,
                MoveSpeed = 2.0f,
                AggroRadius = 20f
            }
        },

        [UnitIds.FireAnt] = new UnitDefinition
        {
            Id = UnitIds.FireAnt,
            ScenePath = "res://scenes/units/fire_ant_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 40f,
                AttackDamage = 8f,
                AttackRange = 3.0f,
                AttackSpeed = 1.5f,
                MoveSpeed = 4.5f,
                AggroRadius = 20f
            }
        },

        [UnitIds.FireBoar] = new UnitDefinition
        {
            Id = UnitIds.FireBoar,
            ScenePath = "res://scenes/units/fire_boar_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 120f,
                AttackDamage = 18f,
                AttackRange = 3.5f,
                AttackSpeed = 0.8f,
                MoveSpeed = 2.5f,
                AggroRadius = 20f
            }
        },

        [UnitIds.FireSpider] = new UnitDefinition
        {
            Id = UnitIds.FireSpider,
            ScenePath = "res://scenes/units/fire_spider_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 50f,
                AttackDamage = 10f,
                AttackRange = 18f,
                AttackSpeed = 0.6f,
                MoveSpeed = 3.5f,
                AggroRadius = 20f
            }
        },

        // =====================================================================
        // EARTH ELEMENT UNITS
        // =====================================================================

        [UnitIds.EarthSprite] = new UnitDefinition
        {
            Id = UnitIds.EarthSprite,
            ScenePath = "res://scenes/units/earth_sprite_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 150f,
                AttackDamage = 18f,
                AttackRange = 3.0f,
                AttackSpeed = 0.9f,
                MoveSpeed = 1.8f,
                AggroRadius = 20f
            }
        },

        [UnitIds.Rock] = new UnitDefinition
        {
            Id = UnitIds.Rock,
            ScenePath = "res://scenes/units/rock_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 500f,
                AttackDamage = 0f,
                AttackRange = 3.0f,
                AttackSpeed = 0f,
                MoveSpeed = 0f,
                AggroRadius = 0f
            }
        },

        [UnitIds.StoneApe] = new UnitDefinition
        {
            Id = UnitIds.StoneApe,
            ScenePath = "res://scenes/units/stone_ape_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 200f,
                AttackDamage = 25f,
                AttackRange = 4.0f,
                AttackSpeed = 0.6f,
                MoveSpeed = 1.8f,
                AggroRadius = 20f
            }
        },

        [UnitIds.EarthRockThrower] = new UnitDefinition
        {
            Id = UnitIds.EarthRockThrower,
            ScenePath = "res://scenes/units/earth_rock_thrower_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 45f,
                AttackDamage = 22f,
                AttackRange = 22f,
                AttackSpeed = 0.4f,
                MoveSpeed = 2.0f,
                AggroRadius = 22f
            }
        },

        // =====================================================================
        // WIND ELEMENT UNITS
        // =====================================================================

        [UnitIds.Puff] = new UnitDefinition
        {
            Id = UnitIds.Puff,
            ScenePath = "res://scenes/units/puff_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 80f,
                AttackDamage = 12f,
                AttackRange = 24f,
                AttackSpeed = 0.4f,
                MoveSpeed = 2.5f,
                AggroRadius = 24f
            }
        },

        // =====================================================================
        // WATER ELEMENT UNITS
        // =====================================================================

        [UnitIds.WaterFrog] = new UnitDefinition
        {
            Id = UnitIds.WaterFrog,
            ScenePath = "res://scenes/units/water_frog_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 70f,
                AttackDamage = 15f,
                AttackRange = 5.0f,  // Extended range for tongue attack
                AttackSpeed = 1.0f,
                MoveSpeed = 2.5f,
                AggroRadius = 20f
            }
        },

        [UnitIds.MamaDuck] = new UnitDefinition
        {
            Id = UnitIds.MamaDuck,
            ScenePath = "res://scenes/units/mama_duck_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 100f,
                AttackDamage = 12f,
                AttackRange = 3.5f,
                AttackSpeed = 0.9f,
                MoveSpeed = 2.8f,
                AggroRadius = 20f
            }
        },

        [UnitIds.Duckling] = new UnitDefinition
        {
            Id = UnitIds.Duckling,
            ScenePath = "res://scenes/units/duckling_3d.tscn",
            BaseStats = new UnitStats
            {
                MaxHp = 25f,
                AttackDamage = 8f,
                AttackRange = 16f,
                AttackSpeed = 0.8f,
                MoveSpeed = 3.2f,
                AggroRadius = 20f
            }
        }
    };

    // =========================================================================
    // LOOKUP METHODS (Type-safe UnitId)
    // =========================================================================

    /// <summary>Get a unit definition by ID. Returns null if not found.</summary>
    public static UnitDefinition? GetUnit(UnitId id)
    {
        return _units.GetValueOrDefault(id);
    }

    /// <summary>Check if a unit type exists in the catalog.</summary>
    public static bool HasUnit(UnitId id)
    {
        return _units.ContainsKey(id);
    }

    /// <summary>Get base stats for a unit type. Returns default stats if not found.</summary>
    public static UnitStats GetBaseStats(UnitId unitId)
    {
        return GetUnit(unitId)?.BaseStats ?? UnitStats.Default;
    }

    /// <summary>Get the scene path for a unit type. Returns empty string if not found.</summary>
    public static string GetScenePath(UnitId unitId)
    {
        return GetUnit(unitId)?.ScenePath ?? "";
    }

    // =========================================================================
    // LOOKUP METHODS (String - for backwards compatibility during migration)
    // =========================================================================

    /// <summary>Get a unit definition by string ID. Returns null if not found.</summary>
    public static UnitDefinition? GetUnit(string id)
    {
        return GetUnit(new UnitId(id));
    }

    /// <summary>Check if a unit type exists in the catalog.</summary>
    public static bool HasUnit(string id)
    {
        return HasUnit(new UnitId(id));
    }

    /// <summary>Get base stats for a unit type. Returns default stats if not found.</summary>
    public static UnitStats GetBaseStats(string unitId)
    {
        return GetBaseStats(new UnitId(unitId));
    }

    /// <summary>Get the scene path for a unit type. Returns empty string if not found.</summary>
    public static string GetScenePath(string unitId)
    {
        return GetScenePath(new UnitId(unitId));
    }

    // =========================================================================
    // ENUMERATION METHODS
    // =========================================================================

    /// <summary>Get all unit type IDs.</summary>
    public static UnitId[] GetAllUnitIds()
    {
        return [.. _units.Keys];
    }

    /// <summary>Get all unit definitions.</summary>
    public static UnitDefinition[] GetAllUnits()
    {
        return [.. _units.Values];
    }

    /// <summary>Get unit count.</summary>
    public static int Count => _units.Count;
}
