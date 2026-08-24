using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Stats;

namespace Fateforged.Data.Traits;

/// <summary>
/// Central registry of all trait definitions as static readonly fields.
/// Provides type-safe trait definitions and lookup methods.
/// Follows the same pattern as UnitDefinitions/CardDefinitions for consistency.
/// </summary>
public static class TraitDefinitions
{
    // =========================================================================
    // INNATE TRAITS - Fire Summoner
    // =========================================================================

    public static readonly TraitDefinition FireAffinity = new()
    {
        Id = TraitIds.FireAffinity,
        NameKey = "trait.fire_affinity.name",
        DescriptionKey = "trait.fire_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Fire],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.FireDamageBonus,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FireAffinity,
                Conditions = new() { ["elemental_affinity"] = "fire" },
                StatMults = new() { [StatKey.AttackDamage] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition BurningSpirit = new()
    {
        Id = TraitIds.BurningSpirit,
        NameKey = "trait.burning_spirit.name",
        DescriptionKey = "trait.burning_spirit.description",
        Category = TraitCategory.Combat,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Fire],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.FireDamageBonus,
                Type = ModifierType.Percent,
                Value = 5.0f,
            },
        ],
    };

    // =========================================================================
    // INNATE TRAITS - Water Summoner
    // =========================================================================

    public static readonly TraitDefinition WaterAffinity = new()
    {
        Id = TraitIds.WaterAffinity,
        NameKey = "trait.water_affinity.name",
        DescriptionKey = "trait.water_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Water],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.WaterDamageBonus,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WaterAffinity,
                Conditions = new() { ["elemental_affinity"] = "water" },
                StatMults = new() { [StatKey.AttackDamage] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition TidalResilience = new()
    {
        Id = TraitIds.TidalResilience,
        NameKey = "trait.tidal_resilience.name",
        DescriptionKey = "trait.tidal_resilience.description",
        Category = TraitCategory.Defense,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Water],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.MaxHealth,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
        ],
    };

    // =========================================================================
    // INNATE TRAITS - Wind Summoner
    // =========================================================================

    public static readonly TraitDefinition WindAffinity = new()
    {
        Id = TraitIds.WindAffinity,
        NameKey = "trait.wind_affinity.name",
        DescriptionKey = "trait.wind_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Wind],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.WindDamageBonus,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WindAffinity,
                Conditions = new() { ["elemental_affinity"] = "wind" },
                StatMults = new() { [StatKey.AttackDamage] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition SwiftCasting = new()
    {
        Id = TraitIds.SwiftCasting,
        NameKey = "trait.swift_casting.name",
        DescriptionKey = "trait.swift_casting.description",
        Category = TraitCategory.Utility,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Wind],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.CastSpeed,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
        ],
    };

    // =========================================================================
    // INNATE TRAITS - Earth Summoner
    // =========================================================================

    public static readonly TraitDefinition EarthAffinity = new()
    {
        Id = TraitIds.EarthAffinity,
        NameKey = "trait.earth_affinity.name",
        DescriptionKey = "trait.earth_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Earth],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.EarthDamageBonus,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.EarthAffinity,
                Conditions = new() { ["elemental_affinity"] = "earth" },
                StatMults = new() { [StatKey.AttackDamage] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition StoneFortitude = new()
    {
        Id = TraitIds.StoneFortitude,
        NameKey = "trait.stone_fortitude.name",
        DescriptionKey = "trait.stone_fortitude.description",
        Category = TraitCategory.Defense,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Earth],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.SoulStrength,
                Type = ModifierType.Flat,
                Value = 5.0f,
            },
        ],
    };

    // =========================================================================
    // INNATE TRAITS - Lightning Summoner
    // =========================================================================

    public static readonly TraitDefinition LightningAffinity = new()
    {
        Id = TraitIds.LightningAffinity,
        NameKey = "trait.lightning_affinity.name",
        DescriptionKey = "trait.lightning_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Lightning],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.LightningDamageBonus,
                Type = ModifierType.Percent,
                Value = 15.0f,
            },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LightningAffinity,
                Conditions = new() { ["elemental_affinity"] = "lightning" },
                StatMults = new() { [StatKey.AttackDamage] = 1.15f },
            },
        ],
    };

    // =========================================================================
    // INNATE TRAITS - Life Summoner
    // =========================================================================

    public static readonly TraitDefinition LifeAffinity = new()
    {
        Id = TraitIds.LifeAffinity,
        NameKey = "trait.life_affinity.name",
        DescriptionKey = "trait.life_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Life],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.HealingBonus,
                Type = ModifierType.Percent,
                Value = 15.0f,
            },
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LifeAffinity,
                Conditions = new() { ["elemental_affinity"] = "life" },
                StatMults = new() { [StatKey.MaxHp] = 1.10f },
            },
        ],
    };

    // =========================================================================
    // INNATE TRAITS - Death Summoner
    // =========================================================================

    public static readonly TraitDefinition DeathAffinity = new()
    {
        Id = TraitIds.DeathAffinity,
        NameKey = "trait.death_affinity.name",
        DescriptionKey = "trait.death_affinity.description",
        Category = TraitCategory.Elemental,
        IsInnate = true,
        Tags = [TraitTags.Summoner, TraitTags.Death],
        Modifiers =
        [
            new TraitModifier
            {
                Stat = StatKey.DeathDamageBonus,
                Type = ModifierType.Percent,
                Value = 10.0f,
            },
            new TraitModifier
            {
                Stat = StatKey.Lifesteal,
                Type = ModifierType.Percent,
                Value = 5.0f,
            },
        ],
    };

    // =========================================================================
    // ACQUIRABLE TRAITS - Summoner Identity Lines
    // =========================================================================

    // Cole: Soul Strength + Cast Speed
    public static readonly TraitDefinition ColeSoulStrengthI = BuildSummonerIdentityTrait(
        TraitIds.ColeSoulStrengthI,
        "trait.cole_soul_strength_i.name",
        "trait.cole_soul_strength_i.description",
        TraitCategory.Defense,
        TraitTags.Cole,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 1.0f
    );

    public static readonly TraitDefinition ColeSoulStrengthII = BuildSummonerIdentityTrait(
        TraitIds.ColeSoulStrengthII,
        "trait.cole_soul_strength_ii.name",
        "trait.cole_soul_strength_ii.description",
        TraitCategory.Defense,
        TraitTags.Cole,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.ColeSoulStrengthI,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 2.0f
    );

    public static readonly TraitDefinition ColeSoulStrengthIII = BuildSummonerIdentityTrait(
        TraitIds.ColeSoulStrengthIII,
        "trait.cole_soul_strength_iii.name",
        "trait.cole_soul_strength_iii.description",
        TraitCategory.Defense,
        TraitTags.Cole,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.ColeSoulStrengthII,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 3.0f
    );

    public static readonly TraitDefinition ColeSoulStrengthIV = BuildSummonerIdentityTrait(
        TraitIds.ColeSoulStrengthIV,
        "trait.cole_soul_strength_iv.name",
        "trait.cole_soul_strength_iv.description",
        TraitCategory.Defense,
        TraitTags.Cole,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.ColeSoulStrengthIII,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 4.0f
    );

    public static readonly TraitDefinition ColeCastSpeedI = BuildSummonerIdentityTrait(
        TraitIds.ColeCastSpeedI,
        "trait.cole_cast_speed_i.name",
        "trait.cole_cast_speed_i.description",
        TraitCategory.Utility,
        TraitTags.Cole,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 5.0f
    );

    public static readonly TraitDefinition ColeCastSpeedII = BuildSummonerIdentityTrait(
        TraitIds.ColeCastSpeedII,
        "trait.cole_cast_speed_ii.name",
        "trait.cole_cast_speed_ii.description",
        TraitCategory.Utility,
        TraitTags.Cole,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.ColeCastSpeedI,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 10.0f
    );

    public static readonly TraitDefinition ColeCastSpeedIII = BuildSummonerIdentityTrait(
        TraitIds.ColeCastSpeedIII,
        "trait.cole_cast_speed_iii.name",
        "trait.cole_cast_speed_iii.description",
        TraitCategory.Utility,
        TraitTags.Cole,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.ColeCastSpeedII,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 15.0f
    );

    public static readonly TraitDefinition ColeCastSpeedIV = BuildSummonerIdentityTrait(
        TraitIds.ColeCastSpeedIV,
        "trait.cole_cast_speed_iv.name",
        "trait.cole_cast_speed_iv.description",
        TraitCategory.Utility,
        TraitTags.Cole,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.ColeCastSpeedIII,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 20.0f
    );

    // Selene: Health + Max Mana
    public static readonly TraitDefinition SeleneHealthI = BuildSummonerIdentityTrait(
        TraitIds.SeleneHealthI,
        "trait.selene_health_i.name",
        "trait.selene_health_i.description",
        TraitCategory.Defense,
        TraitTags.Selene,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 100.0f
    );

    public static readonly TraitDefinition SeleneHealthII = BuildSummonerIdentityTrait(
        TraitIds.SeleneHealthII,
        "trait.selene_health_ii.name",
        "trait.selene_health_ii.description",
        TraitCategory.Defense,
        TraitTags.Selene,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.SeleneHealthI,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 200.0f
    );

    public static readonly TraitDefinition SeleneHealthIII = BuildSummonerIdentityTrait(
        TraitIds.SeleneHealthIII,
        "trait.selene_health_iii.name",
        "trait.selene_health_iii.description",
        TraitCategory.Defense,
        TraitTags.Selene,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.SeleneHealthII,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 300.0f
    );

    public static readonly TraitDefinition SeleneHealthIV = BuildSummonerIdentityTrait(
        TraitIds.SeleneHealthIV,
        "trait.selene_health_iv.name",
        "trait.selene_health_iv.description",
        TraitCategory.Defense,
        TraitTags.Selene,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.SeleneHealthIII,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 400.0f
    );

    public static readonly TraitDefinition SeleneMaxManaI = BuildSummonerIdentityTrait(
        TraitIds.SeleneMaxManaI,
        "trait.selene_max_mana_i.name",
        "trait.selene_max_mana_i.description",
        TraitCategory.Utility,
        TraitTags.Selene,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 10.0f
    );

    public static readonly TraitDefinition SeleneMaxManaII = BuildSummonerIdentityTrait(
        TraitIds.SeleneMaxManaII,
        "trait.selene_max_mana_ii.name",
        "trait.selene_max_mana_ii.description",
        TraitCategory.Utility,
        TraitTags.Selene,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.SeleneMaxManaI,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 20.0f
    );

    public static readonly TraitDefinition SeleneMaxManaIII = BuildSummonerIdentityTrait(
        TraitIds.SeleneMaxManaIII,
        "trait.selene_max_mana_iii.name",
        "trait.selene_max_mana_iii.description",
        TraitCategory.Utility,
        TraitTags.Selene,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.SeleneMaxManaII,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 30.0f
    );

    public static readonly TraitDefinition SeleneMaxManaIV = BuildSummonerIdentityTrait(
        TraitIds.SeleneMaxManaIV,
        "trait.selene_max_mana_iv.name",
        "trait.selene_max_mana_iv.description",
        TraitCategory.Utility,
        TraitTags.Selene,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.SeleneMaxManaIII,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 40.0f
    );

    // Mei: Cast Speed + Max Mana
    public static readonly TraitDefinition MeiCastSpeedI = BuildSummonerIdentityTrait(
        TraitIds.MeiCastSpeedI,
        "trait.mei_cast_speed_i.name",
        "trait.mei_cast_speed_i.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 5.0f
    );

    public static readonly TraitDefinition MeiCastSpeedII = BuildSummonerIdentityTrait(
        TraitIds.MeiCastSpeedII,
        "trait.mei_cast_speed_ii.name",
        "trait.mei_cast_speed_ii.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.MeiCastSpeedI,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 10.0f
    );

    public static readonly TraitDefinition MeiCastSpeedIII = BuildSummonerIdentityTrait(
        TraitIds.MeiCastSpeedIII,
        "trait.mei_cast_speed_iii.name",
        "trait.mei_cast_speed_iii.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.MeiCastSpeedII,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 15.0f
    );

    public static readonly TraitDefinition MeiCastSpeedIV = BuildSummonerIdentityTrait(
        TraitIds.MeiCastSpeedIV,
        "trait.mei_cast_speed_iv.name",
        "trait.mei_cast_speed_iv.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.MeiCastSpeedIII,
        stat: StatKey.CastSpeed,
        modifierType: ModifierType.Percent,
        value: 20.0f
    );

    public static readonly TraitDefinition MeiMaxManaI = BuildSummonerIdentityTrait(
        TraitIds.MeiMaxManaI,
        "trait.mei_max_mana_i.name",
        "trait.mei_max_mana_i.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 10.0f
    );

    public static readonly TraitDefinition MeiMaxManaII = BuildSummonerIdentityTrait(
        TraitIds.MeiMaxManaII,
        "trait.mei_max_mana_ii.name",
        "trait.mei_max_mana_ii.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.MeiMaxManaI,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 20.0f
    );

    public static readonly TraitDefinition MeiMaxManaIII = BuildSummonerIdentityTrait(
        TraitIds.MeiMaxManaIII,
        "trait.mei_max_mana_iii.name",
        "trait.mei_max_mana_iii.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.MeiMaxManaII,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 30.0f
    );

    public static readonly TraitDefinition MeiMaxManaIV = BuildSummonerIdentityTrait(
        TraitIds.MeiMaxManaIV,
        "trait.mei_max_mana_iv.name",
        "trait.mei_max_mana_iv.description",
        TraitCategory.Utility,
        TraitTags.Mei,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.MeiMaxManaIII,
        stat: StatKey.MaxMana,
        modifierType: ModifierType.Flat,
        value: 40.0f
    );

    // Teo: Health + Soul Strength
    public static readonly TraitDefinition TeoHealthI = BuildSummonerIdentityTrait(
        TraitIds.TeoHealthI,
        "trait.teo_health_i.name",
        "trait.teo_health_i.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 100.0f
    );

    public static readonly TraitDefinition TeoHealthII = BuildSummonerIdentityTrait(
        TraitIds.TeoHealthII,
        "trait.teo_health_ii.name",
        "trait.teo_health_ii.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.TeoHealthI,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 200.0f
    );

    public static readonly TraitDefinition TeoHealthIII = BuildSummonerIdentityTrait(
        TraitIds.TeoHealthIII,
        "trait.teo_health_iii.name",
        "trait.teo_health_iii.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.TeoHealthII,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 300.0f
    );

    public static readonly TraitDefinition TeoHealthIV = BuildSummonerIdentityTrait(
        TraitIds.TeoHealthIV,
        "trait.teo_health_iv.name",
        "trait.teo_health_iv.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.TeoHealthIII,
        stat: StatKey.MaxHealth,
        modifierType: ModifierType.Flat,
        value: 400.0f
    );

    public static readonly TraitDefinition TeoSoulStrengthI = BuildSummonerIdentityTrait(
        TraitIds.TeoSoulStrengthI,
        "trait.teo_soul_strength_i.name",
        "trait.teo_soul_strength_i.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 2,
        prerequisiteTraitId: null,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 1.0f
    );

    public static readonly TraitDefinition TeoSoulStrengthII = BuildSummonerIdentityTrait(
        TraitIds.TeoSoulStrengthII,
        "trait.teo_soul_strength_ii.name",
        "trait.teo_soul_strength_ii.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 3,
        prerequisiteTraitId: TraitIds.TeoSoulStrengthI,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 2.0f
    );

    public static readonly TraitDefinition TeoSoulStrengthIII = BuildSummonerIdentityTrait(
        TraitIds.TeoSoulStrengthIII,
        "trait.teo_soul_strength_iii.name",
        "trait.teo_soul_strength_iii.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 5,
        prerequisiteTraitId: TraitIds.TeoSoulStrengthII,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 3.0f
    );

    public static readonly TraitDefinition TeoSoulStrengthIV = BuildSummonerIdentityTrait(
        TraitIds.TeoSoulStrengthIV,
        "trait.teo_soul_strength_iv.name",
        "trait.teo_soul_strength_iv.description",
        TraitCategory.Defense,
        TraitTags.Teo,
        minLevel: 7,
        prerequisiteTraitId: TraitIds.TeoSoulStrengthIII,
        stat: StatKey.SoulStrength,
        modifierType: ModifierType.Flat,
        value: 4.0f
    );

    // =========================================================================
    // CARD-NATIVE CORE - Fire Wisp
    // =========================================================================

    private const string FireWispCoreFormGroup = "fire_wisp_core_form";
    private const string FireWispTwinOption = "twin_flame";
    private const string FireWispCondensedOption = "condensed_flame";

    public static readonly TraitDefinition FireWispTwinFlame = new()
    {
        Id = TraitIds.FireWispTwinFlame,
        NameKey = "trait.fire_wisp_twin_flame.name",
        DescriptionKey = "trait.fire_wisp_twin_flame.description",
        Category = TraitCategory.Special,
        Tags = [TraitTags.Summon, TraitTags.Fire],
        AllowedCardCatalogIds = [CardIds.FireWisp.Value],
        MinLevel = 2,
        ExclusiveGroupId = FireWispCoreFormGroup,
        ExclusiveOptionId = FireWispTwinOption,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FireWispTwinFlame,
                StatAdds = new() { [StatKey.UnitCount] = 1f },
                StatMults = new() { [StatKey.MaxHp] = 0.65f, [StatKey.AttackDamage] = 0.65f },
            },
        ],
    };

    public static readonly TraitDefinition FireWispDancingEmbers = new()
    {
        Id = TraitIds.FireWispDancingEmbers,
        NameKey = "trait.fire_wisp_dancing_embers.name",
        DescriptionKey = "trait.fire_wisp_dancing_embers.description",
        Category = TraitCategory.Combat,
        Tags = [TraitTags.Summon, TraitTags.Fire],
        AllowedCardCatalogIds = [CardIds.FireWisp.Value],
        MinLevel = 4,
        Prerequisites = [TraitIds.FireWispTwinFlame],
        ExclusiveGroupId = FireWispCoreFormGroup,
        ExclusiveOptionId = FireWispTwinOption,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FireWispDancingEmbers,
                StatMults = new()
                {
                    [StatKey.AttackSpeed] = 1.10f,
                    [StatKey.MoveSpeed] = 1.08f,
                },
            },
        ],
    };

    public static readonly TraitDefinition FireWispCondensedFlame = new()
    {
        Id = TraitIds.FireWispCondensedFlame,
        NameKey = "trait.fire_wisp_condensed_flame.name",
        DescriptionKey = "trait.fire_wisp_condensed_flame.description",
        Category = TraitCategory.Special,
        Tags = [TraitTags.Summon, TraitTags.Fire],
        AllowedCardCatalogIds = [CardIds.FireWisp.Value],
        MinLevel = 2,
        ExclusiveGroupId = FireWispCoreFormGroup,
        ExclusiveOptionId = FireWispCondensedOption,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FireWispCondensedFlame,
                StatMults = new() { [StatKey.MaxHp] = 1.20f, [StatKey.AttackDamage] = 1.15f },
            },
        ],
    };

    public static readonly TraitDefinition FireWispBlazingCore = new()
    {
        Id = TraitIds.FireWispBlazingCore,
        NameKey = "trait.fire_wisp_blazing_core.name",
        DescriptionKey = "trait.fire_wisp_blazing_core.description",
        Category = TraitCategory.Combat,
        Tags = [TraitTags.Summon, TraitTags.Fire],
        AllowedCardCatalogIds = [CardIds.FireWisp.Value],
        MinLevel = 4,
        Prerequisites = [TraitIds.FireWispCondensedFlame],
        ExclusiveGroupId = FireWispCoreFormGroup,
        ExclusiveOptionId = FireWispCondensedOption,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FireWispBlazingCore,
                StatMults = new()
                {
                    [StatKey.MaxHp] = 1.08f,
                    [StatKey.AttackDamage] = 1.12f,
                },
            },
        ],
    };

    // =========================================================================
    // SUMMON TRAITS - Global Pool (available to all summons)
    // =========================================================================

    public static readonly TraitDefinition Fortitude = new()
    {
        Id = TraitIds.Fortitude,
        NameKey = "trait.fortitude.name",
        DescriptionKey = "trait.fortitude.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Fortitude,
                StatMults = new() { [StatKey.MaxHp] = 1.08f },
            },
        ],
    };

    public static readonly TraitDefinition FortitudeII = new()
    {
        Id = TraitIds.FortitudeII,
        NameKey = "trait.fortitude_ii.name",
        DescriptionKey = "trait.fortitude_ii.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Fortitude],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FortitudeII,
                StatMults = new() { [StatKey.MaxHp] = 1.12f },
            },
        ],
    };

    public static readonly TraitDefinition FortitudeIII = new()
    {
        Id = TraitIds.FortitudeIII,
        NameKey = "trait.fortitude_iii.name",
        DescriptionKey = "trait.fortitude_iii.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.FortitudeII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FortitudeIII,
                StatMults = new() { [StatKey.MaxHp] = 1.16f },
            },
        ],
    };

    public static readonly TraitDefinition FortitudeIV = new()
    {
        Id = TraitIds.FortitudeIV,
        NameKey = "trait.fortitude_iv.name",
        DescriptionKey = "trait.fortitude_iv.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.FortitudeIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FortitudeIV,
                StatMults = new() { [StatKey.MaxHp] = 1.20f },
            },
        ],
    };

    public static readonly TraitDefinition Power = new()
    {
        Id = TraitIds.Power,
        NameKey = "trait.power.name",
        DescriptionKey = "trait.power.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Power,
                StatMults = new() { [StatKey.AttackDamage] = 1.06f },
            },
        ],
    };

    public static readonly TraitDefinition PowerII = new()
    {
        Id = TraitIds.PowerII,
        NameKey = "trait.power_ii.name",
        DescriptionKey = "trait.power_ii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Power],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.PowerII,
                StatMults = new() { [StatKey.AttackDamage] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition PowerIII = new()
    {
        Id = TraitIds.PowerIII,
        NameKey = "trait.power_iii.name",
        DescriptionKey = "trait.power_iii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.PowerII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.PowerIII,
                StatMults = new() { [StatKey.AttackDamage] = 1.14f },
            },
        ],
    };

    public static readonly TraitDefinition PowerIV = new()
    {
        Id = TraitIds.PowerIV,
        NameKey = "trait.power_iv.name",
        DescriptionKey = "trait.power_iv.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.PowerIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.PowerIV,
                StatMults = new() { [StatKey.AttackDamage] = 1.18f },
            },
        ],
    };

    public static readonly TraitDefinition Swiftness = new()
    {
        Id = TraitIds.Swiftness,
        NameKey = "trait.swiftness.name",
        DescriptionKey = "trait.swiftness.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Swiftness,
                StatMults = new() { [StatKey.AttackSpeed] = 1.05f },
            },
        ],
    };

    public static readonly TraitDefinition SwiftnessII = new()
    {
        Id = TraitIds.SwiftnessII,
        NameKey = "trait.swiftness_ii.name",
        DescriptionKey = "trait.swiftness_ii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Swiftness],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SwiftnessII,
                StatMults = new() { [StatKey.AttackSpeed] = 1.08f },
            },
        ],
    };

    public static readonly TraitDefinition SwiftnessIII = new()
    {
        Id = TraitIds.SwiftnessIII,
        NameKey = "trait.swiftness_iii.name",
        DescriptionKey = "trait.swiftness_iii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.SwiftnessII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SwiftnessIII,
                StatMults = new() { [StatKey.AttackSpeed] = 1.11f },
            },
        ],
    };

    public static readonly TraitDefinition SwiftnessIV = new()
    {
        Id = TraitIds.SwiftnessIV,
        NameKey = "trait.swiftness_iv.name",
        DescriptionKey = "trait.swiftness_iv.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.SwiftnessIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SwiftnessIV,
                StatMults = new() { [StatKey.AttackSpeed] = 1.14f },
            },
        ],
    };

    public static readonly TraitDefinition Agility = new()
    {
        Id = TraitIds.Agility,
        NameKey = "trait.agility.name",
        DescriptionKey = "trait.agility.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Agility,
                StatMults = new() { [StatKey.MoveSpeed] = 1.05f },
            },
        ],
    };

    public static readonly TraitDefinition AgilityII = new()
    {
        Id = TraitIds.AgilityII,
        NameKey = "trait.agility_ii.name",
        DescriptionKey = "trait.agility_ii.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Agility],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.AgilityII,
                StatMults = new() { [StatKey.MoveSpeed] = 1.08f },
            },
        ],
    };

    public static readonly TraitDefinition AgilityIII = new()
    {
        Id = TraitIds.AgilityIII,
        NameKey = "trait.agility_iii.name",
        DescriptionKey = "trait.agility_iii.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.AgilityII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.AgilityIII,
                StatMults = new() { [StatKey.MoveSpeed] = 1.11f },
            },
        ],
    };

    public static readonly TraitDefinition AgilityIV = new()
    {
        Id = TraitIds.AgilityIV,
        NameKey = "trait.agility_iv.name",
        DescriptionKey = "trait.agility_iv.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.AgilityIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.AgilityIV,
                StatMults = new() { [StatKey.MoveSpeed] = 1.14f },
            },
        ],
    };

    public static readonly TraitDefinition Reach = new()
    {
        Id = TraitIds.Reach,
        NameKey = "trait.reach.name",
        DescriptionKey = "trait.reach.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Reach,
                StatMults = new() { [StatKey.AttackRange] = 1.05f },
            },
        ],
    };

    public static readonly TraitDefinition ReachII = new()
    {
        Id = TraitIds.ReachII,
        NameKey = "trait.reach_ii.name",
        DescriptionKey = "trait.reach_ii.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Reach],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.ReachII,
                StatMults = new() { [StatKey.AttackRange] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition ReachIII = new()
    {
        Id = TraitIds.ReachIII,
        NameKey = "trait.reach_iii.name",
        DescriptionKey = "trait.reach_iii.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.ReachII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.ReachIII,
                StatMults = new() { [StatKey.AttackRange] = 1.15f },
            },
        ],
    };

    public static readonly TraitDefinition ReachIV = new()
    {
        Id = TraitIds.ReachIV,
        NameKey = "trait.reach_iv.name",
        DescriptionKey = "trait.reach_iv.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.ReachIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.ReachIV,
                StatMults = new() { [StatKey.AttackRange] = 1.20f },
            },
        ],
    };

    public static readonly TraitDefinition Plating = new()
    {
        Id = TraitIds.Plating,
        NameKey = "trait.plating.name",
        DescriptionKey = "trait.plating.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Plating,
                StatAdds = new() { [StatKey.Armor] = 4f },
            },
        ],
    };

    public static readonly TraitDefinition PlatingII = new()
    {
        Id = TraitIds.PlatingII,
        NameKey = "trait.plating_ii.name",
        DescriptionKey = "trait.plating_ii.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Plating],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.PlatingII,
                StatAdds = new() { [StatKey.Armor] = 8f },
            },
        ],
    };

    public static readonly TraitDefinition PlatingIII = new()
    {
        Id = TraitIds.PlatingIII,
        NameKey = "trait.plating_iii.name",
        DescriptionKey = "trait.plating_iii.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.PlatingII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.PlatingIII,
                StatAdds = new() { [StatKey.Armor] = 12f },
            },
        ],
    };

    public static readonly TraitDefinition PlatingIV = new()
    {
        Id = TraitIds.PlatingIV,
        NameKey = "trait.plating_iv.name",
        DescriptionKey = "trait.plating_iv.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.PlatingIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.PlatingIV,
                StatAdds = new() { [StatKey.Armor] = 16f },
            },
        ],
    };

    public static readonly TraitDefinition Warding = new()
    {
        Id = TraitIds.Warding,
        NameKey = "trait.warding.name",
        DescriptionKey = "trait.warding.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Warding,
                StatAdds = new() { [StatKey.MagicResist] = 4f },
            },
        ],
    };

    public static readonly TraitDefinition WardingII = new()
    {
        Id = TraitIds.WardingII,
        NameKey = "trait.warding_ii.name",
        DescriptionKey = "trait.warding_ii.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Warding],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WardingII,
                StatAdds = new() { [StatKey.MagicResist] = 8f },
            },
        ],
    };

    public static readonly TraitDefinition WardingIII = new()
    {
        Id = TraitIds.WardingIII,
        NameKey = "trait.warding_iii.name",
        DescriptionKey = "trait.warding_iii.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.WardingII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WardingIII,
                StatAdds = new() { [StatKey.MagicResist] = 12f },
            },
        ],
    };

    public static readonly TraitDefinition WardingIV = new()
    {
        Id = TraitIds.WardingIV,
        NameKey = "trait.warding_iv.name",
        DescriptionKey = "trait.warding_iv.description",
        Category = TraitCategory.Defense,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.WardingIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.WardingIV,
                StatAdds = new() { [StatKey.MagicResist] = 16f },
            },
        ],
    };

    public static readonly TraitDefinition Soulforce = new()
    {
        Id = TraitIds.Soulforce,
        NameKey = "trait.soulforce.name",
        DescriptionKey = "trait.soulforce.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Soulforce,
                StatAdds = new() { [StatKey.SoulStrength] = 1f },
            },
        ],
    };

    public static readonly TraitDefinition SoulforceII = new()
    {
        Id = TraitIds.SoulforceII,
        NameKey = "trait.soulforce_ii.name",
        DescriptionKey = "trait.soulforce_ii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Soulforce],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SoulforceII,
                StatAdds = new() { [StatKey.SoulStrength] = 2f },
            },
        ],
    };

    public static readonly TraitDefinition SoulforceIII = new()
    {
        Id = TraitIds.SoulforceIII,
        NameKey = "trait.soulforce_iii.name",
        DescriptionKey = "trait.soulforce_iii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.SoulforceII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SoulforceIII,
                StatAdds = new() { [StatKey.SoulStrength] = 3f },
            },
        ],
    };

    public static readonly TraitDefinition SoulforceIV = new()
    {
        Id = TraitIds.SoulforceIV,
        NameKey = "trait.soulforce_iv.name",
        DescriptionKey = "trait.soulforce_iv.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.SoulforceIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.SoulforceIV,
                StatAdds = new() { [StatKey.SoulStrength] = 4f },
            },
        ],
    };

    public static readonly TraitDefinition Arcana = new()
    {
        Id = TraitIds.Arcana,
        NameKey = "trait.arcana.name",
        DescriptionKey = "trait.arcana.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Arcana,
                StatMults = new() { [StatKey.DamageBonus] = 1.05f },
            },
        ],
    };

    public static readonly TraitDefinition ArcanaII = new()
    {
        Id = TraitIds.ArcanaII,
        NameKey = "trait.arcana_ii.name",
        DescriptionKey = "trait.arcana_ii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Arcana],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.ArcanaII,
                StatMults = new() { [StatKey.DamageBonus] = 1.10f },
            },
        ],
    };

    public static readonly TraitDefinition ArcanaIII = new()
    {
        Id = TraitIds.ArcanaIII,
        NameKey = "trait.arcana_iii.name",
        DescriptionKey = "trait.arcana_iii.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.ArcanaII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.ArcanaIII,
                StatMults = new() { [StatKey.DamageBonus] = 1.15f },
            },
        ],
    };

    public static readonly TraitDefinition ArcanaIV = new()
    {
        Id = TraitIds.ArcanaIV,
        NameKey = "trait.arcana_iv.name",
        DescriptionKey = "trait.arcana_iv.description",
        Category = TraitCategory.Combat,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.ArcanaIII],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.ArcanaIV,
                StatMults = new() { [StatKey.DamageBonus] = 1.20f },
            },
        ],
    };

    public static readonly TraitDefinition Legion = new()
    {
        Id = TraitIds.Legion,
        NameKey = "trait.legion.name",
        DescriptionKey = "trait.legion.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 2,
        AllowedRarities = ["common", "rare", "epic"],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.Legion,
                StatAdds = new() { [StatKey.UnitCount] = 1f },
            },
        ],
    };

    public static readonly TraitDefinition LegionII = new()
    {
        Id = TraitIds.LegionII,
        NameKey = "trait.legion_ii.name",
        DescriptionKey = "trait.legion_ii.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 3,
        Prerequisites = [TraitIds.Legion],
        AllowedRarities = ["common", "rare", "epic"],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LegionII,
                StatAdds = new() { [StatKey.UnitCount] = 1f },
            },
        ],
    };

    public static readonly TraitDefinition LegionIII = new()
    {
        Id = TraitIds.LegionIII,
        NameKey = "trait.legion_iii.name",
        DescriptionKey = "trait.legion_iii.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 5,
        Prerequisites = [TraitIds.LegionII],
        AllowedRarities = ["common", "rare"],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LegionIII,
                StatAdds = new() { [StatKey.UnitCount] = 1f },
            },
        ],
    };

    public static readonly TraitDefinition LegionIV = new()
    {
        Id = TraitIds.LegionIV,
        NameKey = "trait.legion_iv.name",
        DescriptionKey = "trait.legion_iv.description",
        Category = TraitCategory.Utility,
        IsInnate = false,
        Tags = [TraitTags.Summon, TraitTags.Global],
        MinLevel = 7,
        Prerequisites = [TraitIds.LegionIII],
        AllowedRarities = ["common"],
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.LegionIV,
                StatAdds = new() { [StatKey.UnitCount] = 1f },
            },
        ],
    };

    // =========================================================================
    // SPECIAL TRAITS - Granted by specific game events
    // =========================================================================

    /// <summary>
    /// Fortune Favors the Bold: +10% damage to all attacks.
    /// Granted when the player selects random summoner at profile setup.
    /// </summary>
    public static readonly TraitDefinition FortuneFavorsTheBold = new()
    {
        Id = TraitIds.FortuneFavorsTheBold,
        NameKey = "trait.fortune_favors_the_bold.name",
        DescriptionKey = "trait.fortune_favors_the_bold.description",
        Category = TraitCategory.Special,
        IsInnate = false,
        AcquisitionMode = TraitAcquisitionMode.GrantedOnly,
        Tags = [TraitTags.Summoner],
        MinLevel = 1,
        MaxLevel = 99,
        Modifiers =
        [
            new TraitModifier
            {
                Target = "unit",
                Source = TraitIds.FortuneFavorsTheBold,
                StatMults = new() { [StatKey.AttackDamage] = 1.10f },
            },
        ],
    };

    private static TraitDefinition BuildSummonerIdentityTrait(
        TraitId id,
        string nameKey,
        string descriptionKey,
        TraitCategory category,
        string summonerTag,
        int minLevel,
        TraitId? prerequisiteTraitId,
        StatKey stat,
        ModifierType modifierType,
        float value
    )
    {
        var prerequisites = prerequisiteTraitId.HasValue
            ? new[] { prerequisiteTraitId.Value.Value }
            : [];

        return new TraitDefinition
        {
            Id = id,
            NameKey = nameKey,
            DescriptionKey = descriptionKey,
            Category = category,
            IsInnate = false,
            Tags = [TraitTags.Summoner],
            RequiredTags = [summonerTag],
            MinLevel = minLevel,
            Prerequisites = prerequisites,
            Modifiers =
            [
                new TraitModifier
                {
                    Stat = stat,
                    Type = modifierType,
                    Value = value,
                },
            ],
        };
    }

    // =========================================================================
    // LOOKUP
    // =========================================================================

    private static readonly Dictionary<string, TraitDefinition> _lookup = new()
    {
        // Innate - Fire
        [TraitIds.FireAffinity] = FireAffinity,
        [TraitIds.BurningSpirit] = BurningSpirit,

        // Innate - Water
        [TraitIds.WaterAffinity] = WaterAffinity,
        [TraitIds.TidalResilience] = TidalResilience,

        // Innate - Wind
        [TraitIds.WindAffinity] = WindAffinity,
        [TraitIds.SwiftCasting] = SwiftCasting,

        // Innate - Earth
        [TraitIds.EarthAffinity] = EarthAffinity,
        [TraitIds.StoneFortitude] = StoneFortitude,

        // Innate - Lightning
        [TraitIds.LightningAffinity] = LightningAffinity,

        // Innate - Life
        [TraitIds.LifeAffinity] = LifeAffinity,

        // Innate - Death
        [TraitIds.DeathAffinity] = DeathAffinity,

        // Acquirable - Summoner Identity Lines
        [TraitIds.ColeSoulStrengthI] = ColeSoulStrengthI,
        [TraitIds.ColeSoulStrengthII] = ColeSoulStrengthII,
        [TraitIds.ColeSoulStrengthIII] = ColeSoulStrengthIII,
        [TraitIds.ColeSoulStrengthIV] = ColeSoulStrengthIV,
        [TraitIds.ColeCastSpeedI] = ColeCastSpeedI,
        [TraitIds.ColeCastSpeedII] = ColeCastSpeedII,
        [TraitIds.ColeCastSpeedIII] = ColeCastSpeedIII,
        [TraitIds.ColeCastSpeedIV] = ColeCastSpeedIV,
        [TraitIds.SeleneHealthI] = SeleneHealthI,
        [TraitIds.SeleneHealthII] = SeleneHealthII,
        [TraitIds.SeleneHealthIII] = SeleneHealthIII,
        [TraitIds.SeleneHealthIV] = SeleneHealthIV,
        [TraitIds.SeleneMaxManaI] = SeleneMaxManaI,
        [TraitIds.SeleneMaxManaII] = SeleneMaxManaII,
        [TraitIds.SeleneMaxManaIII] = SeleneMaxManaIII,
        [TraitIds.SeleneMaxManaIV] = SeleneMaxManaIV,
        [TraitIds.MeiCastSpeedI] = MeiCastSpeedI,
        [TraitIds.MeiCastSpeedII] = MeiCastSpeedII,
        [TraitIds.MeiCastSpeedIII] = MeiCastSpeedIII,
        [TraitIds.MeiCastSpeedIV] = MeiCastSpeedIV,
        [TraitIds.MeiMaxManaI] = MeiMaxManaI,
        [TraitIds.MeiMaxManaII] = MeiMaxManaII,
        [TraitIds.MeiMaxManaIII] = MeiMaxManaIII,
        [TraitIds.MeiMaxManaIV] = MeiMaxManaIV,
        [TraitIds.TeoHealthI] = TeoHealthI,
        [TraitIds.TeoHealthII] = TeoHealthII,
        [TraitIds.TeoHealthIII] = TeoHealthIII,
        [TraitIds.TeoHealthIV] = TeoHealthIV,
        [TraitIds.TeoSoulStrengthI] = TeoSoulStrengthI,
        [TraitIds.TeoSoulStrengthII] = TeoSoulStrengthII,
        [TraitIds.TeoSoulStrengthIII] = TeoSoulStrengthIII,
        [TraitIds.TeoSoulStrengthIV] = TeoSoulStrengthIV,

        // Card-native Core - Fire Wisp
        [TraitIds.FireWispTwinFlame] = FireWispTwinFlame,
        [TraitIds.FireWispDancingEmbers] = FireWispDancingEmbers,
        [TraitIds.FireWispCondensedFlame] = FireWispCondensedFlame,
        [TraitIds.FireWispBlazingCore] = FireWispBlazingCore,

        // Summon Traits
        [TraitIds.Fortitude] = Fortitude,
        [TraitIds.FortitudeII] = FortitudeII,
        [TraitIds.FortitudeIII] = FortitudeIII,
        [TraitIds.FortitudeIV] = FortitudeIV,
        [TraitIds.Power] = Power,
        [TraitIds.PowerII] = PowerII,
        [TraitIds.PowerIII] = PowerIII,
        [TraitIds.PowerIV] = PowerIV,
        [TraitIds.Swiftness] = Swiftness,
        [TraitIds.SwiftnessII] = SwiftnessII,
        [TraitIds.SwiftnessIII] = SwiftnessIII,
        [TraitIds.SwiftnessIV] = SwiftnessIV,
        [TraitIds.Agility] = Agility,
        [TraitIds.AgilityII] = AgilityII,
        [TraitIds.AgilityIII] = AgilityIII,
        [TraitIds.AgilityIV] = AgilityIV,
        [TraitIds.Reach] = Reach,
        [TraitIds.ReachII] = ReachII,
        [TraitIds.ReachIII] = ReachIII,
        [TraitIds.ReachIV] = ReachIV,
        [TraitIds.Plating] = Plating,
        [TraitIds.PlatingII] = PlatingII,
        [TraitIds.PlatingIII] = PlatingIII,
        [TraitIds.PlatingIV] = PlatingIV,
        [TraitIds.Warding] = Warding,
        [TraitIds.WardingII] = WardingII,
        [TraitIds.WardingIII] = WardingIII,
        [TraitIds.WardingIV] = WardingIV,
        [TraitIds.Soulforce] = Soulforce,
        [TraitIds.SoulforceII] = SoulforceII,
        [TraitIds.SoulforceIII] = SoulforceIII,
        [TraitIds.SoulforceIV] = SoulforceIV,
        [TraitIds.Arcana] = Arcana,
        [TraitIds.ArcanaII] = ArcanaII,
        [TraitIds.ArcanaIII] = ArcanaIII,
        [TraitIds.ArcanaIV] = ArcanaIV,
        [TraitIds.Legion] = Legion,
        [TraitIds.LegionII] = LegionII,
        [TraitIds.LegionIII] = LegionIII,
        [TraitIds.LegionIV] = LegionIV,

        // Special Traits
        [TraitIds.FortuneFavorsTheBold] = FortuneFavorsTheBold,
    };

    /// <summary>Get a trait definition by ID. Returns null if not found.</summary>
    public static TraitDefinition? Get(TraitId id) => _lookup.GetValueOrDefault(id);

    /// <summary>Get a trait definition by string ID. Returns null if not found.</summary>
    public static TraitDefinition? Get(string id) => _lookup.GetValueOrDefault(id);

    /// <summary>Try to get a trait definition by ID.</summary>
    public static bool TryGet(TraitId id, out TraitDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Try to get a trait definition by string ID.</summary>
    public static bool TryGet(string id, out TraitDefinition? definition)
    {
        return _lookup.TryGetValue(id, out definition);
    }

    /// <summary>Check if a trait exists.</summary>
    public static bool Has(TraitId id) => _lookup.ContainsKey(id);

    /// <summary>Check if a trait exists by string ID.</summary>
    public static bool Has(string id) => _lookup.ContainsKey(id);

    /// <summary>Get all trait definitions.</summary>
    public static IReadOnlyCollection<TraitDefinition> All => _lookup.Values;

    /// <summary>Get all trait IDs.</summary>
    public static IReadOnlyCollection<string> AllIds => _lookup.Keys;

    /// <summary>Get trait count.</summary>
    public static int Count => _lookup.Count;
}
