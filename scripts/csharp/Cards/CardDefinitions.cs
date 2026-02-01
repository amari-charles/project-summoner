using System.Collections.Generic;
using ProjectSummoner.Cards.Effects.Concrete;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Cards.Spawning;
using ProjectSummoner.Constants;
using ProjectSummoner.Projectiles;
using ProjectSummoner.Systems.Modifiers;
using ProjectSummoner.Vfx;

namespace ProjectSummoner.Cards;

/// <summary>
/// Central registry of all card definitions as static readonly fields.
/// Provides type-safe card definitions and lookup methods.
/// Follows the same pattern as UnitDefinitions for consistency.
/// </summary>
public static class CardDefinitions
{
    // =========================================================================
    // SPELLS
    // =========================================================================

    public static readonly CardDefinition Fireball = new()
    {
        Id = CardIds.Fireball,
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
        ProjectileId = ProjectileIds.Fireball,
        SpellVfx = VfxIds.FireballSpell,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.AreaOfEffect,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition Rally = new()
    {
        Id = CardIds.Rally,
        Name = "Rally",
        Description = "Command nearby units to move to a target location and defend that zone until enemies are cleared.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 0,
        Cooldown = 1.0f,
        SummonTime = 0.0f,
        CommandType = CommandType.Rally,
        SelectionRadius = 8.0f,
        SpellCategory = SpellCategory.Command,
        SpellTargeting = SpellTargeting.SelectionRadius,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition Guard = new()
    {
        Id = CardIds.Guard,
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
        SpellCategory = SpellCategory.Command,
        SpellTargeting = SpellTargeting.SelectionRadius,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition Charge = new()
    {
        Id = CardIds.Charge,
        Name = "Charge",
        Description = "Command nearby units to launch a coordinated attack on the closest enemy (unit, structure, or base) to the target location for 30 seconds.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 0,
        Cooldown = 1.0f,
        SummonTime = 0.0f,
        CommandType = CommandType.Charge,
        SelectionRadius = 8.0f,
        SpellCategory = SpellCategory.Command,
        SpellTargeting = SpellTargeting.SelectionRadius,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition ManaBolt = new()
    {
        Id = CardIds.ManaBolt,
        Name = "Mana Bolt",
        Description = "Fire a bolt of arcane energy at the nearest enemy.",
        Rarity = Rarity.Common,
        Type = CardType.Spell,
        ManaCost = 3,
        Cooldown = 1.5f,
        SummonTime = 0.0f,
        SpellDamage = 60.0f,
        ProjectileId = ProjectileIds.ManaBolt,
        SpellCategory = SpellCategory.Damage,
        SpellTargeting = SpellTargeting.SingleTarget,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Neutral
    };

    // =========================================================================
    // WISPS (Basic starter units for each element)
    // =========================================================================

    public static readonly CardDefinition FireWisp = new()
    {
        Id = CardIds.FireWisp,
        Name = "Fire Wisp",
        Description = "A teardrop of living flame. Drifts across the battlefield, burning all in its path.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.FireWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire
    };

    public static readonly CardDefinition WaterWisp = new()
    {
        Id = CardIds.WaterWisp,
        Name = "Water Wisp",
        Description = "A teardrop of living water. Flows across the battlefield with fluid grace.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water
    };

    public static readonly CardDefinition WindWisp = new()
    {
        Id = CardIds.WindWisp,
        Name = "Wind Wisp",
        Description = "A teardrop of swirling wind. Darts across the battlefield with elusive speed.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WindWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind
    };

    public static readonly CardDefinition EarthWisp = new()
    {
        Id = CardIds.EarthWisp,
        Name = "Earth Wisp",
        Description = "A teardrop of compacted stone. Moves with sturdy determination across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth
    };

    public static readonly CardDefinition LightningWisp = new()
    {
        Id = CardIds.LightningWisp,
        Name = "Lightning Wisp",
        Description = "A teardrop of crackling energy. Strikes across the battlefield with shocking speed.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.LightningWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Lightning
    };

    public static readonly CardDefinition LifeWisp = new()
    {
        Id = CardIds.LifeWisp,
        Name = "Life Wisp",
        Description = "A teardrop of living essence. Glows warmly as it drifts across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.LifeWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Life
    };

    public static readonly CardDefinition DeathWisp = new()
    {
        Id = CardIds.DeathWisp,
        Name = "Death Wisp",
        Description = "A teardrop of spectral essence. Flickers between visible and ethereal as it haunts the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.DeathWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Death
    };

    public static readonly CardDefinition ShadowWisp = new()
    {
        Id = CardIds.ShadowWisp,
        Name = "Shadow Wisp",
        Description = "A teardrop of living shadow. Shifts and fades as it stalks across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.ShadowWisp,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Shadow
    };

    public static readonly CardDefinition FireWispSwarm = new()
    {
        Id = CardIds.FireWispSwarm,
        Name = "Fire Wisp Swarm",
        Description = "Unleash a horde of flame wisps. Twelve smaller fire wisps surge forth to overwhelm the enemy.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 7,
        Cooldown = 4.0f,
        SummonTime = 2.5f,
        UnitId = UnitIds.FireWisp,
        UnitModifier = new StatModifier
        {
            Source = "card_swarm_variant",
            StatMults = new Dictionary<string, float>
            {
                ["max_hp"] = 0.75f,
                ["attack_damage"] = 0.75f
            }
        },
        SpawnCount = 12,
        Formation = FormationPresets.TightSwarmGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental,
        Roles = SummonRole.Swarm,
        VisualTraits = VisualTrait.UsesWispVisuals,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived
    };

    // =========================================================================
    // FIRE ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition FireTitan = new()
    {
        Id = CardIds.FireTitan,
        Name = "Fire Titan",
        Description = "A colossal spirit of ancient flame. Towers over the battlefield, absorbing damage while scorching all who approach.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 7,
        Cooldown = 3.0f,
        SummonTime = 2.0f,
        UnitId = UnitIds.FireTitan,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Spirit,
        Roles = SummonRole.Tank | SummonRole.Giant,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition FireAnt = new()
    {
        Id = CardIds.FireAnt,
        Name = "Fire Ant",
        Description = "A swift and fierce fire ant. Scurries across the battlefield with blazing speed, overwhelming foes with relentless attacks.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 2,
        Cooldown = 1.5f,
        SummonTime = 0.8f,
        UnitId = UnitIds.FireAnt,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Insect,
        Roles = SummonRole.Fast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition FireAntSwarm = new()
    {
        Id = CardIds.FireAntSwarm,
        Name = "Fire Ant Swarm",
        Description = "Release a colony of fire ants! Twenty tiny terrors surge forth in formation, overwhelming enemies with sheer numbers.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 6,
        Cooldown = 4.0f,
        SummonTime = 2.0f,
        UnitId = UnitIds.FireAnt,
        SpawnCount = 20,
        Formation = FormationPresets.FireAntSwarm,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Insect,
        Roles = SummonRole.Fast | SummonRole.Swarm,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire,
        Flags = CardFlags.Archived
    };

    public static readonly CardDefinition FireBoar = new()
    {
        Id = CardIds.FireBoar,
        Name = "Fire Boar",
        Description = "A charging bruiser wreathed in flame. Barrels through enemies with reckless aggression.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.2f,
        UnitId = UnitIds.FireBoar,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire
    };

    public static readonly CardDefinition FireSpider = new()
    {
        Id = CardIds.FireSpider,
        Name = "Fire Spider",
        Description = "A skittering hunter that spins webs of flame. Its sticky projectiles slow enemies caught in its trap.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.FireSpider,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Insect,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Fire
    };

    // =========================================================================
    // EARTH ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition Pebbloom = new()
    {
        Id = CardIds.Pebbloom,
        Name = "Pebbloom",
        Description = "A sturdy creature native to the elemental plane of earth. Pebblooms carry saplings that they nurture with elemental energy.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthSprite,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Elemental | CreatureType.Nature,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth
    };

    public static readonly CardDefinition Rock = new()
    {
        Id = CardIds.Rock,
        Name = "Rock",
        Description = "A stationary target dummy for testing. Does not move or attack.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 0,
        Cooldown = 0.5f,
        SummonTime = 0.0f,
        UnitId = UnitIds.Rock,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        Roles = SummonRole.Stationary,
        Flags = CardFlags.DevOnly | CardFlags.Dummy | CardFlags.Archived,
        UnlockCondition = UnlockCondition.DevOnly,
        ElementalAffinity = Element.Earth
    };

    public static readonly CardDefinition StoneApe = new()
    {
        Id = CardIds.StoneApe,
        Name = "Stone Ape",
        Description = "A massive gorilla made of living rock. Slow and deliberate, but devastating in close combat.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 6,
        Cooldown = 3.0f,
        SummonTime = 1.5f,
        UnitId = UnitIds.StoneApe,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        Roles = SummonRole.Tank,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth
    };

    public static readonly CardDefinition EarthRockThrower = new()
    {
        Id = CardIds.EarthRockThrower,
        Name = "Rock Thrower",
        Description = "A tiny creature with impossible strength. Hurls boulders larger than itself at distant foes.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 4,
        Cooldown = 2.5f,
        SummonTime = 1.0f,
        UnitId = UnitIds.EarthRockThrower,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Elemental,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Earth
    };

    // =========================================================================
    // WIND ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition Puff = new()
    {
        Id = CardIds.Puff,
        Name = "Puff",
        Description = "A mischievous cloud spirit that blows gusts of wind at its foes. Light and agile, it drifts across the battlefield.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.Puff,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Aerial,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind
    };

    public static readonly CardDefinition CloudSwarm = new()
    {
        Id = CardIds.CloudSwarm,
        Name = "Cloud Swarm",
        Description = "A swirling formation of cloud wisps. Six clouds drift together in pairs, overwhelming foes with their combined might.",
        Rarity = Rarity.Rare,
        Type = CardType.Summon,
        ManaCost = 5,
        Cooldown = 3.0f,
        SummonTime = 1.5f,
        UnitId = UnitIds.Puff,
        SpawnCount = 6,
        Formation = FormationPresets.CloudSwarm,
        UnitType = UnitType.Ranged,
        IsRanged = true,
        CreatureTypes = CreatureType.Aerial,
        Roles = SummonRole.Swarm,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Wind,
        Flags = CardFlags.Archived
    };

    // =========================================================================
    // WATER ELEMENT UNITS
    // =========================================================================

    public static readonly CardDefinition WaterFrog = new()
    {
        Id = CardIds.WaterFrog,
        Name = "Water Frog",
        Description = "A pudgy amphibian with a lightning-fast tongue. Strikes from a distance with surprising reach, snatching enemies before they can react.",
        Rarity = Rarity.Common,
        Type = CardType.Summon,
        ManaCost = 3,
        Cooldown = 2.0f,
        SummonTime = 1.0f,
        UnitId = UnitIds.WaterFrog,
        SpawnCount = 1,
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Amphibian,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water
    };

    public static readonly CardDefinition MamaDuck = new()
    {
        Id = CardIds.MamaDuck,
        Name = "Mama Duck",
        Description = "A protective mother duck and her ducklings. Mama fights in melee while her babies pepper foes with water bullets.",
        Rarity = Rarity.Epic,
        Type = CardType.Summon,
        ManaCost = 5,
        Cooldown = 3.0f,
        SummonTime = 1.5f,
        // SummonSpec replaces UnitId/SpawnCount for multi-unit spawning
        // Note: SummonTime/Cooldown come from CardDefinition, not SummonSpec
        Summon = new SummonSpec
        {
            Units =
            [
                new UnitSpawnEntry
                {
                    UnitId = UnitIds.MamaDuck,
                    Count = 1
                },
                new UnitSpawnEntry
                {
                    UnitId = UnitIds.Duckling,
                    Count = 3,
                    Placement = SpawnPlacement.BehindLeader,
                    FollowsIndex = 0,  // Ducklings follow mama's targeting
                    PlacementOffset = 1.5f
                }
            ]
        },
        // Legacy fields kept for UI display compatibility
        UnitId = UnitIds.MamaDuck,
        SpawnCount = 4,  // Total: 1 mama + 3 ducklings
        Formation = FormationPresets.StandardGrid,
        UnitType = UnitType.Melee,
        IsRanged = false,
        CreatureTypes = CreatureType.Beast,
        UnlockCondition = UnlockCondition.Default,
        ElementalAffinity = Element.Water
    };

    // =========================================================================
    // LOOKUP
    // =========================================================================

    /// <summary>
    /// Lookup dictionary built from static fields.
    /// Uses CardId (which implicitly converts to string) as key.
    /// </summary>
    private static readonly Dictionary<string, CardDefinition> _lookup = new()
    {
        // Spells
        [CardIds.Fireball] = Fireball,
        [CardIds.Rally] = Rally,
        [CardIds.Guard] = Guard,
        [CardIds.Charge] = Charge,
        [CardIds.ManaBolt] = ManaBolt,

        // Wisps
        [CardIds.FireWisp] = FireWisp,
        [CardIds.WaterWisp] = WaterWisp,
        [CardIds.WindWisp] = WindWisp,
        [CardIds.EarthWisp] = EarthWisp,
        [CardIds.LightningWisp] = LightningWisp,
        [CardIds.LifeWisp] = LifeWisp,
        [CardIds.DeathWisp] = DeathWisp,
        [CardIds.ShadowWisp] = ShadowWisp,
        [CardIds.FireWispSwarm] = FireWispSwarm,

        // Fire units
        [CardIds.FireTitan] = FireTitan,
        [CardIds.FireAnt] = FireAnt,
        [CardIds.FireAntSwarm] = FireAntSwarm,
        [CardIds.FireBoar] = FireBoar,
        [CardIds.FireSpider] = FireSpider,

        // Earth units
        [CardIds.Pebbloom] = Pebbloom,
        [CardIds.Rock] = Rock,
        [CardIds.StoneApe] = StoneApe,
        [CardIds.EarthRockThrower] = EarthRockThrower,

        // Wind units
        [CardIds.Puff] = Puff,
        [CardIds.CloudSwarm] = CloudSwarm,

        // Water units
        [CardIds.WaterFrog] = WaterFrog,
        [CardIds.MamaDuck] = MamaDuck
    };

    /// <summary>Get a card definition by ID. Returns null if not found.</summary>
    public static CardDefinition? Get(CardId id) => _lookup.GetValueOrDefault(id);

    /// <summary>Get a card definition by string ID. Returns null if not found.</summary>
    public static CardDefinition? Get(string id) => _lookup.GetValueOrDefault(id);

    /// <summary>Try to get a card definition by ID.</summary>
    public static bool TryGet(CardId id, out CardDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Try to get a card definition by string ID.</summary>
    public static bool TryGet(string id, out CardDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Check if a card exists.</summary>
    public static bool Has(CardId id) => _lookup.ContainsKey(id);

    /// <summary>Check if a card exists by string ID.</summary>
    public static bool Has(string id) => _lookup.ContainsKey(id);

    /// <summary>Get all card definitions.</summary>
    public static IReadOnlyCollection<CardDefinition> All => _lookup.Values;

    /// <summary>Get all card IDs.</summary>
    public static IReadOnlyCollection<string> AllIds => _lookup.Keys;

    /// <summary>Get card count.</summary>
    public static int Count => _lookup.Count;
}
