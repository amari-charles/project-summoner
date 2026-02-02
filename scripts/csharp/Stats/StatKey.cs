namespace ProjectSummoner.Stats;

/// <summary>
/// Type-safe enumeration of all stat keys used in the modifier system.
/// Use this instead of string keys for compile-time validation.
/// </summary>
public enum StatKey
{
    // =========================================================================
    // CORE UNIT STATS
    // =========================================================================

    /// <summary>Maximum hit points.</summary>
    MaxHp,

    /// <summary>Attack damage dealt per hit.</summary>
    AttackDamage,

    /// <summary>Attacks per second.</summary>
    AttackSpeed,

    /// <summary>Movement speed.</summary>
    MoveSpeed,

    /// <summary>Attack range in units.</summary>
    AttackRange,

    /// <summary>Aggro detection radius.</summary>
    AggroRadius,

    /// <summary>Critical hit chance (0.0 - 1.0).</summary>
    CritChance,

    /// <summary>Critical hit damage multiplier.</summary>
    CritDamage,

    /// <summary>Physical damage reduction.</summary>
    Armor,

    /// <summary>Elemental/magical damage reduction.</summary>
    MagicResist,

    // =========================================================================
    // ELEMENTAL DAMAGE BONUSES (summoner stats that affect units)
    // =========================================================================

    /// <summary>Bonus damage for fire element attacks.</summary>
    FireDamageBonus,

    /// <summary>Bonus damage for water element attacks.</summary>
    WaterDamageBonus,

    /// <summary>Bonus damage for wind element attacks.</summary>
    WindDamageBonus,

    /// <summary>Bonus damage for earth element attacks.</summary>
    EarthDamageBonus,

    /// <summary>Bonus damage for lightning element attacks.</summary>
    LightningDamageBonus,

    /// <summary>Bonus damage for life element attacks.</summary>
    LifeDamageBonus,

    /// <summary>Bonus damage for death element attacks.</summary>
    DeathDamageBonus,

    /// <summary>Bonus damage for shadow element attacks.</summary>
    ShadowDamageBonus,

    // =========================================================================
    // SUMMONER SURVIVAL/UTILITY STATS
    // =========================================================================

    /// <summary>Healing effectiveness bonus.</summary>
    HealingBonus,

    /// <summary>Life steal percentage.</summary>
    Lifesteal,

    /// <summary>Flat damage reduction.</summary>
    DamageReduction,

    /// <summary>Mana regeneration rate.</summary>
    ManaRegen,

    /// <summary>Casting speed multiplier.</summary>
    CastSpeed,

    /// <summary>Maximum health (alias for MaxHp, used in some contexts).</summary>
    MaxHealth,

    // =========================================================================
    // COMBAT EFFECTS
    // =========================================================================

    /// <summary>Health restored on kill.</summary>
    HealOnKill,

    /// <summary>Generic damage bonus multiplier.</summary>
    DamageBonus,

    // =========================================================================
    // PROGRESSION/ECONOMY STATS
    // =========================================================================

    /// <summary>Bonus gold from all sources.</summary>
    GoldBonus,

    /// <summary>Bonus experience from all sources.</summary>
    XpBonus
}

/// <summary>
/// Extension methods for StatKey conversion and validation.
/// </summary>
public static class StatKeyExtensions
{
    /// <summary>
    /// Converts a StatKey to its snake_case string representation.
    /// Used for GDScript interop and dictionary keys.
    /// </summary>
    public static string ToSnakeCase(this StatKey key) => key switch
    {
        // Core unit stats
        StatKey.MaxHp => "max_hp",
        StatKey.AttackDamage => "attack_damage",
        StatKey.AttackSpeed => "attack_speed",
        StatKey.MoveSpeed => "move_speed",
        StatKey.AttackRange => "attack_range",
        StatKey.AggroRadius => "aggro_radius",
        StatKey.CritChance => "crit_chance",
        StatKey.CritDamage => "crit_damage",
        StatKey.Armor => "armor",
        StatKey.MagicResist => "magic_resist",

        // Elemental damage bonuses
        StatKey.FireDamageBonus => "fire_damage_bonus",
        StatKey.WaterDamageBonus => "water_damage_bonus",
        StatKey.WindDamageBonus => "wind_damage_bonus",
        StatKey.EarthDamageBonus => "earth_damage_bonus",
        StatKey.LightningDamageBonus => "lightning_damage_bonus",
        StatKey.LifeDamageBonus => "life_damage_bonus",
        StatKey.DeathDamageBonus => "death_damage_bonus",
        StatKey.ShadowDamageBonus => "shadow_damage_bonus",

        // Summoner survival/utility stats
        StatKey.HealingBonus => "healing_bonus",
        StatKey.Lifesteal => "lifesteal",
        StatKey.DamageReduction => "damage_reduction",
        StatKey.ManaRegen => "mana_regen",
        StatKey.CastSpeed => "cast_speed",
        StatKey.MaxHealth => "max_health",

        // Combat effects
        StatKey.HealOnKill => "heal_on_kill",
        StatKey.DamageBonus => "damage_bonus",

        // Progression/economy stats
        StatKey.GoldBonus => "gold_bonus",
        StatKey.XpBonus => "xp_bonus",

        _ => key.ToString().ToLowerInvariant()
    };

    /// <summary>
    /// Converts a StatKey to its PascalCase property name.
    /// Used for C# property access.
    /// </summary>
    public static string ToPascalCase(this StatKey key) => key.ToString();

    /// <summary>
    /// Parses a string to a StatKey. Returns null if the string is not a valid stat key.
    /// Accepts both snake_case and PascalCase formats.
    /// </summary>
    public static StatKey? FromString(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return null;

        // Normalize: lowercase and remove underscores
        var normalized = s.ToLowerInvariant().Replace("_", "");

        return normalized switch
        {
            // Core unit stats
            "maxhp" => StatKey.MaxHp,
            "attackdamage" => StatKey.AttackDamage,
            "attackspeed" => StatKey.AttackSpeed,
            "movespeed" => StatKey.MoveSpeed,
            "attackrange" => StatKey.AttackRange,
            "aggroradius" => StatKey.AggroRadius,
            "critchance" => StatKey.CritChance,
            "critdamage" => StatKey.CritDamage,
            "armor" => StatKey.Armor,
            "magicresist" => StatKey.MagicResist,

            // Elemental damage bonuses
            "firedamagebonus" => StatKey.FireDamageBonus,
            "waterdamagebonus" => StatKey.WaterDamageBonus,
            "winddamagebonus" => StatKey.WindDamageBonus,
            "earthdamagebonus" => StatKey.EarthDamageBonus,
            "lightningdamagebonus" => StatKey.LightningDamageBonus,
            "lifedamagebonus" => StatKey.LifeDamageBonus,
            "deathdamagebonus" => StatKey.DeathDamageBonus,
            "shadowdamagebonus" => StatKey.ShadowDamageBonus,

            // Summoner survival/utility stats
            "healingbonus" => StatKey.HealingBonus,
            "lifesteal" => StatKey.Lifesteal,
            "damagereduction" => StatKey.DamageReduction,
            "manaregen" => StatKey.ManaRegen,
            "castspeed" => StatKey.CastSpeed,
            "maxhealth" => StatKey.MaxHealth,

            // Combat effects
            "healonkill" => StatKey.HealOnKill,
            "damagebonus" => StatKey.DamageBonus,

            // Progression/economy stats
            "goldbonus" => StatKey.GoldBonus,
            "xpbonus" => StatKey.XpBonus,

            _ => null
        };
    }

    /// <summary>
    /// Checks if a string represents a valid stat key.
    /// </summary>
    public static bool IsValidStatKey(string? s) => FromString(s) != null;

    /// <summary>
    /// Gets the default value for a stat key.
    /// Returns 0 for stats that don't have meaningful defaults.
    /// </summary>
    public static float GetDefault(this StatKey key) => key switch
    {
        StatKey.MaxHp => 100f,
        StatKey.AttackDamage => 10f,
        StatKey.AttackSpeed => 1f,
        StatKey.MoveSpeed => 3f,
        StatKey.AttackRange => 2f,
        StatKey.AggroRadius => 20f,
        StatKey.CritChance => 0f,
        StatKey.CritDamage => 1.5f,
        StatKey.Armor => 0f,
        StatKey.MagicResist => 0f,
        StatKey.MaxHealth => 100f,
        _ => 0f
    };

    /// <summary>
    /// Gets all core unit stats (the ones directly applied to units).
    /// </summary>
    public static StatKey[] CoreUnitStats => new[]
    {
        StatKey.MaxHp,
        StatKey.AttackDamage,
        StatKey.AttackSpeed,
        StatKey.MoveSpeed,
        StatKey.AttackRange,
        StatKey.AggroRadius,
        StatKey.CritChance,
        StatKey.CritDamage,
        StatKey.Armor,
        StatKey.MagicResist
    };

    /// <summary>
    /// Gets all stat keys as an array.
    /// </summary>
    public static StatKey[] All => (StatKey[])System.Enum.GetValues(typeof(StatKey));

    /// <summary>
    /// Maps alternate stat names to their canonical StatKey equivalent.
    /// For example, "max_health" -> MaxHp for unit context.
    /// Returns null if no mapping exists.
    /// </summary>
    public static StatKey? MapToUnitStat(this StatKey key) => key switch
    {
        StatKey.MaxHealth => StatKey.MaxHp,
        StatKey.DamageBonus => StatKey.AttackDamage,
        _ => key
    };
}
