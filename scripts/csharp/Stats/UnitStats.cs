using System.Collections.Generic;
using Godot;

namespace Fateforged.Stats;

/// <summary>
/// Type-safe container for unit stats.
/// Replaces Dictionary&lt;string, float&gt; for stat storage and manipulation.
/// Immutable record type - use With() methods to create modified copies.
/// </summary>
public record UnitStats
{
    public float MaxHp { get; init; } = 100f;
    public float AttackDamage { get; init; } = 10f;
    public float AttackSpeed { get; init; } = 1f;
    public float MoveSpeed { get; init; } = 3f;
    public float AttackRange { get; init; } = 2f;
    public float AggroRadius { get; init; } = 20f;
    public float CritChance { get; init; } = 0f;
    public float CritDamage { get; init; } = 1.5f;
    public float Armor { get; init; } = 0f;
    public float MagicResist { get; init; } = 0f;

    /// <summary>
    /// Creates UnitStats with default values.
    /// Note: Returns a new instance each time. For records, this is idiomatic
    /// as record equality is value-based, not reference-based.
    /// </summary>
    public static UnitStats Default => new();

    /// <summary>
    /// Gets a stat value by StatKey.
    /// </summary>
    public float Get(StatKey key) => key switch
    {
        StatKey.MaxHp => MaxHp,
        StatKey.AttackDamage => AttackDamage,
        StatKey.AttackSpeed => AttackSpeed,
        StatKey.MoveSpeed => MoveSpeed,
        StatKey.AttackRange => AttackRange,
        StatKey.AggroRadius => AggroRadius,
        StatKey.CritChance => CritChance,
        StatKey.CritDamage => CritDamage,
        StatKey.Armor => Armor,
        StatKey.MagicResist => MagicResist,
        _ => 0f
    };

    /// <summary>
    /// Creates a new UnitStats with one stat modified.
    /// </summary>
    public UnitStats With(StatKey key, float value) => key switch
    {
        StatKey.MaxHp => this with { MaxHp = value },
        StatKey.AttackDamage => this with { AttackDamage = value },
        StatKey.AttackSpeed => this with { AttackSpeed = value },
        StatKey.MoveSpeed => this with { MoveSpeed = value },
        StatKey.AttackRange => this with { AttackRange = value },
        StatKey.AggroRadius => this with { AggroRadius = value },
        StatKey.CritChance => this with { CritChance = value },
        StatKey.CritDamage => this with { CritDamage = value },
        StatKey.Armor => this with { Armor = value },
        StatKey.MagicResist => this with { MagicResist = value },
        _ => this
    };

    /// <summary>
    /// Creates a new UnitStats with a stat multiplied by a factor.
    /// </summary>
    public UnitStats WithMultiplied(StatKey key, float multiplier) =>
        With(key, Get(key) * multiplier);

    /// <summary>
    /// Creates a new UnitStats with a stat increased by an amount.
    /// </summary>
    public UnitStats WithAdded(StatKey key, float amount) =>
        With(key, Get(key) + amount);

    /// <summary>
    /// Creates UnitStats from a C# dictionary.
    /// Accepts both snake_case and PascalCase keys.
    /// </summary>
    public static UnitStats FromDictionary(Dictionary<string, float>? dict)
    {
        if (dict == null || dict.Count == 0)
            return Default;

        return new UnitStats
        {
            MaxHp = GetFloat(dict, "max_hp", "MaxHp", 100f),
            AttackDamage = GetFloat(dict, "attack_damage", "AttackDamage", 10f),
            AttackSpeed = GetFloat(dict, "attack_speed", "AttackSpeed", 1f),
            MoveSpeed = GetFloat(dict, "move_speed", "MoveSpeed", 3f),
            AttackRange = GetFloat(dict, "attack_range", "AttackRange", 2f),
            AggroRadius = GetFloat(dict, "aggro_radius", "AggroRadius", 20f),
            CritChance = GetFloat(dict, "crit_chance", "CritChance", 0f),
            CritDamage = GetFloat(dict, "crit_damage", "CritDamage", 1.5f),
            Armor = GetFloat(dict, "armor", "Armor", 0f),
            MagicResist = GetFloat(dict, "magic_resist", "MagicResist", 0f)
        };
    }

    /// <summary>
    /// Creates UnitStats from a Godot Dictionary.
    /// Used for GDScript interop. Converts to C# dictionary internally.
    /// </summary>
    public static UnitStats FromGodotDictionary(Godot.Collections.Dictionary? dict)
    {
        if (dict == null)
            return Default;

        var csharpDict = new Dictionary<string, float>();
        foreach (var key in dict.Keys)
        {
            var keyStr = key.AsString();
            var value = dict[key];
            if (value.VariantType == Variant.Type.Float || value.VariantType == Variant.Type.Int)
            {
                csharpDict[keyStr] = value.AsSingle();
            }
        }
        return FromDictionary(csharpDict);
    }

    /// <summary>
    /// Converts to a Godot Dictionary with snake_case keys.
    /// Used for GDScript interop.
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        return new Godot.Collections.Dictionary
        {
            ["max_hp"] = MaxHp,
            ["attack_damage"] = AttackDamage,
            ["attack_speed"] = AttackSpeed,
            ["move_speed"] = MoveSpeed,
            ["attack_range"] = AttackRange,
            ["aggro_radius"] = AggroRadius,
            ["crit_chance"] = CritChance,
            ["crit_damage"] = CritDamage,
            ["armor"] = Armor,
            ["magic_resist"] = MagicResist
        };
    }

    /// <summary>
    /// Converts to a C# Dictionary with StatKey keys.
    /// </summary>
    public Dictionary<StatKey, float> ToStatKeyDictionary()
    {
        return new Dictionary<StatKey, float>
        {
            [StatKey.MaxHp] = MaxHp,
            [StatKey.AttackDamage] = AttackDamage,
            [StatKey.AttackSpeed] = AttackSpeed,
            [StatKey.MoveSpeed] = MoveSpeed,
            [StatKey.AttackRange] = AttackRange,
            [StatKey.AggroRadius] = AggroRadius,
            [StatKey.CritChance] = CritChance,
            [StatKey.CritDamage] = CritDamage,
            [StatKey.Armor] = Armor,
            [StatKey.MagicResist] = MagicResist
        };
    }

    /// <summary>
    /// Applies a single modifier to these stats and returns new UnitStats.
    /// Order: (base + adds) * mults
    /// </summary>
    public UnitStats WithModifier(StatModifier? modifier)
    {
        if (modifier == null)
            return this;
        return WithModifiers([modifier]);
    }

    /// <summary>
    /// Applies modifiers to these stats and returns new UnitStats.
    /// Order: (base + adds) * mults
    /// </summary>
    public UnitStats WithModifiers(List<StatModifier>? modifiers)
    {
        if (modifiers == null || modifiers.Count == 0)
            return this;

        // Collect adds and mults per stat
        var adds = new Dictionary<StatKey, float>();
        var mults = new Dictionary<StatKey, float>();

        foreach (var mod in modifiers)
        {
            // Process additive modifiers (StatKey-keyed)
            foreach (var (statKey, value) in mod.StatAdds)
            {
                adds.TryAdd(statKey, 0f);
                adds[statKey] += value;
            }

            // Process multiplicative modifiers (StatKey-keyed)
            foreach (var (statKey, value) in mod.StatMults)
            {
                mults.TryAdd(statKey, 1f);
                mults[statKey] *= value;
            }
        }

        // Apply: (base + adds) * mults
        var result = this;
        foreach (var key in StatKeyExtensions.CoreUnitStats)
        {
            var baseValue = Get(key);
            var addValue = adds.GetValueOrDefault(key, 0f);
            var multValue = mults.GetValueOrDefault(key, 1f);
            var finalValue = (baseValue + addValue) * multValue;
            result = result.With(key, finalValue);
        }

        return result;
    }

    /// <summary>
    /// Applies upgrade multipliers to these stats and returns new UnitStats.
    /// Upgrade multipliers are multiplicative (e.g., 1.1 = +10%).
    /// </summary>
    public UnitStats WithUpgradeMultipliers(Dictionary<string, float>? multipliers)
    {
        if (multipliers == null || multipliers.Count == 0)
            return this;

        var result = this;
        foreach (var (statString, mult) in multipliers)
        {
            var key = StatKeyExtensions.FromString(statString);
            if (key.HasValue)
            {
                result = result.WithMultiplied(key.Value, mult);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies override values, replacing stats with explicit values.
    /// </summary>
    public UnitStats WithOverrides(Dictionary<string, float>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return this;

        var result = this;
        foreach (var (keyString, value) in overrides)
        {
            var statKey = StatKeyExtensions.FromString(keyString);
            if (statKey.HasValue)
            {
                result = result.With(statKey.Value, value);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies override values from a Godot Dictionary.
    /// Used for GDScript interop.
    /// </summary>
    public UnitStats WithGodotOverrides(Godot.Collections.Dictionary? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return this;

        var csharpDict = new Dictionary<string, float>();
        foreach (var key in overrides.Keys)
        {
            var keyStr = key.AsString();
            var value = overrides[key];
            if (value.VariantType == Variant.Type.Float || value.VariantType == Variant.Type.Int)
            {
                csharpDict[keyStr] = value.AsSingle();
            }
        }
        return WithOverrides(csharpDict);
    }

    public override string ToString() =>
        $"UnitStats(HP={MaxHp}, ATK={AttackDamage}, SPD={AttackSpeed}, MOV={MoveSpeed}, RNG={AttackRange}, AGG={AggroRadius}, CRIT={CritChance:P0}/{CritDamage:F1}x, ARM={Armor}, MR={MagicResist})";

    private static float GetFloat(Dictionary<string, float> dict, string snakeKey, string pascalKey, float defaultValue)
    {
        if (dict.TryGetValue(snakeKey, out var snakeValue))
            return snakeValue;
        if (dict.TryGetValue(pascalKey, out var pascalValue))
            return pascalValue;
        return defaultValue;
    }
}
