using System.Collections.Generic;
using ProjectSummoner.Stats;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Typed modifier class that replaces the GDScript Dictionary-based modifiers.
/// Represents a stat modification that can be applied to units.
/// </summary>
public class StatModifier
{
    /// <summary>
    /// Identifier for the source of this modifier (e.g., "trait_fire_affinity", "card_upgrades").
    /// </summary>
    public string Source { get; set; } = "";

    /// <summary>
    /// If set, this modifier only applies to the specified card instance.
    /// Used for card upgrade modifiers that are instance-scoped.
    /// </summary>
    public string? CardInstanceId { get; set; }

    /// <summary>
    /// Tags for filtering and amplification (e.g., ["fire", "melee"]).
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Conditions that must match for this modifier to apply.
    /// Key: condition name (e.g., "elemental_affinity")
    /// Value: required value
    /// </summary>
    public Dictionary<string, object> Conditions { get; set; } = new();

    /// <summary>
    /// Additive stat bonuses (applied first).
    /// Key: stat key (type-safe)
    /// Value: amount to add
    /// </summary>
    public Dictionary<StatKey, float> StatAdds { get; set; } = new();

    /// <summary>
    /// Multiplicative stat bonuses (applied after adds).
    /// Key: stat key (type-safe)
    /// Value: multiplier (1.0 = no change, 1.1 = +10%)
    /// </summary>
    public Dictionary<StatKey, float> StatMults { get; set; } = new();

    /// <summary>
    /// Boolean flags for special behaviors.
    /// Key: flag name (e.g., "immune_to_slow", "deals_fire_damage")
    /// Value: flag state
    /// </summary>
    public Dictionary<string, bool> Flags { get; set; } = new();

    /// <summary>
    /// If set, this modifier amplifies other modifiers with the specified tag.
    /// </summary>
    public string? AmplifyTag { get; set; }

    /// <summary>
    /// Amplification factor (multiplier applied to tagged modifiers).
    /// Only used when AmplifyTag is set.
    /// </summary>
    public float AmplifyFactor { get; set; } = 1.0f;

    // =========================================================================
    // TRIGGER FIELDS (for conditional/temporary effects)
    // =========================================================================

    /// <summary>
    /// When this modifier activates.
    /// Default is Always (static modifier applied at spawn).
    /// </summary>
    public TriggerCondition Trigger { get; set; } = TriggerCondition.Always;

    /// <summary>
    /// Threshold value for HP-based triggers (0.0 - 1.0 representing percentage).
    /// For BelowHpPercent: activates when HP falls below this percent.
    /// For AboveHpPercent: activates when HP is above this percent.
    /// </summary>
    public float TriggerThreshold { get; set; }

    /// <summary>
    /// How long the effect lasts after activation, in seconds.
    /// 0 means permanent (until condition no longer applies).
    /// </summary>
    public float TriggerDuration { get; set; }

    /// <summary>
    /// Minimum time between activations, in seconds.
    /// Prevents rapid re-triggering of the same effect.
    /// </summary>
    public float TriggerCooldown { get; set; }

    /// <summary>
    /// Returns true if this is a triggered modifier (not always active).
    /// </summary>
    public bool IsTriggered => Trigger != TriggerCondition.Always;

    /// <summary>
    /// Create a StatModifier from a Godot Dictionary (for interop with GDScript).
    /// </summary>
    public static StatModifier FromDictionary(Godot.Collections.Dictionary dict)
    {
        var modifier = new StatModifier
        {
            Source = dict.TryGetValue("source", out var source) ? source.AsString() : ""
        };

        if (dict.TryGetValue("card_instance_id", out var instanceId) && instanceId.VariantType != Godot.Variant.Type.Nil)
        {
            modifier.CardInstanceId = instanceId.AsString();
        }

        if (dict.TryGetValue("tags", out var tags) && tags.VariantType == Godot.Variant.Type.Array)
        {
            foreach (var tag in tags.AsGodotArray())
            {
                modifier.Tags.Add(tag.AsString());
            }
        }

        if (dict.TryGetValue("conditions", out var conditions) && conditions.VariantType == Godot.Variant.Type.Dictionary)
        {
            var condDict = conditions.AsGodotDictionary();
            foreach (var key in condDict.Keys)
            {
                modifier.Conditions[key.AsString()] = condDict[key].Obj!;
            }
        }

        if (dict.TryGetValue("stat_adds", out var statAdds) && statAdds.VariantType == Godot.Variant.Type.Dictionary)
        {
            var addsDict = statAdds.AsGodotDictionary();
            foreach (var key in addsDict.Keys)
            {
                var statKey = StatKeyExtensions.FromString(key.AsString());
                if (statKey.HasValue)
                {
                    modifier.StatAdds[statKey.Value] = addsDict[key].AsSingle();
                }
            }
        }

        if (dict.TryGetValue("stat_mults", out var statMults) && statMults.VariantType == Godot.Variant.Type.Dictionary)
        {
            var multsDict = statMults.AsGodotDictionary();
            foreach (var key in multsDict.Keys)
            {
                var statKey = StatKeyExtensions.FromString(key.AsString());
                if (statKey.HasValue)
                {
                    modifier.StatMults[statKey.Value] = multsDict[key].AsSingle();
                }
            }
        }

        if (dict.TryGetValue("flags", out var flags) && flags.VariantType == Godot.Variant.Type.Dictionary)
        {
            var flagsDict = flags.AsGodotDictionary();
            foreach (var key in flagsDict.Keys)
            {
                modifier.Flags[key.AsString()] = flagsDict[key].AsBool();
            }
        }

        if (dict.TryGetValue("amplify_tag", out var ampTag) && ampTag.VariantType != Godot.Variant.Type.Nil)
        {
            modifier.AmplifyTag = ampTag.AsString();
        }

        if (dict.TryGetValue("factor", out var factor))
        {
            modifier.AmplifyFactor = factor.AsSingle();
        }

        // Trigger fields
        if (dict.TryGetValue("trigger", out var trigger) && trigger.VariantType != Godot.Variant.Type.Nil)
        {
            var triggerStr = trigger.AsString();
            if (System.Enum.TryParse<TriggerCondition>(triggerStr, ignoreCase: true, out var triggerEnum))
            {
                modifier.Trigger = triggerEnum;
            }
        }

        if (dict.TryGetValue("trigger_threshold", out var threshold))
        {
            modifier.TriggerThreshold = threshold.AsSingle();
        }

        if (dict.TryGetValue("trigger_duration", out var duration))
        {
            modifier.TriggerDuration = duration.AsSingle();
        }

        if (dict.TryGetValue("trigger_cooldown", out var cooldown))
        {
            modifier.TriggerCooldown = cooldown.AsSingle();
        }

        return modifier;
    }

    /// <summary>
    /// Convert to a Godot Dictionary (for interop with GDScript).
    /// </summary>
    public Godot.Collections.Dictionary ToDictionary()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["source"] = Source
        };

        if (!string.IsNullOrEmpty(CardInstanceId))
        {
            dict["card_instance_id"] = CardInstanceId;
        }

        if (Tags.Count > 0)
        {
            var tagsArray = new Godot.Collections.Array();
            foreach (var tag in Tags)
            {
                tagsArray.Add(tag);
            }
            dict["tags"] = tagsArray;
        }

        if (Conditions.Count > 0)
        {
            var condDict = new Godot.Collections.Dictionary();
            foreach (var kvp in Conditions)
            {
                condDict[kvp.Key] = ObjectToVariant(kvp.Value);
            }
            dict["conditions"] = condDict;
        }

        if (StatAdds.Count > 0)
        {
            var addsDict = new Godot.Collections.Dictionary();
            foreach (var kvp in StatAdds)
            {
                addsDict[kvp.Key.ToSnakeCase()] = kvp.Value;
            }
            dict["stat_adds"] = addsDict;
        }

        if (StatMults.Count > 0)
        {
            var multsDict = new Godot.Collections.Dictionary();
            foreach (var kvp in StatMults)
            {
                multsDict[kvp.Key.ToSnakeCase()] = kvp.Value;
            }
            dict["stat_mults"] = multsDict;
        }

        if (Flags.Count > 0)
        {
            var flagsDict = new Godot.Collections.Dictionary();
            foreach (var kvp in Flags)
            {
                flagsDict[kvp.Key] = kvp.Value;
            }
            dict["flags"] = flagsDict;
        }

        if (!string.IsNullOrEmpty(AmplifyTag))
        {
            dict["amplify_tag"] = AmplifyTag;
            dict["factor"] = AmplifyFactor;
        }

        // Trigger fields (only include if not default)
        if (Trigger != TriggerCondition.Always)
        {
            dict["trigger"] = Trigger.ToString();
        }

        if (TriggerThreshold > 0)
        {
            dict["trigger_threshold"] = TriggerThreshold;
        }

        if (TriggerDuration > 0)
        {
            dict["trigger_duration"] = TriggerDuration;
        }

        if (TriggerCooldown > 0)
        {
            dict["trigger_cooldown"] = TriggerCooldown;
        }

        return dict;
    }

    /// <summary>
    /// Convert a C# object to a Godot Variant safely.
    /// Handles common types that Godot supports.
    /// </summary>
    private static Godot.Variant ObjectToVariant(object? value)
    {
        return value switch
        {
            null => default,
            string s => s,
            int i => i,
            float f => f,
            double d => (float)d,
            bool b => b,
            long l => l,
            Godot.GodotObject obj => obj,
            Godot.Variant v => v,
            _ => value.ToString() ?? ""
        };
    }
}

/// <summary>
/// Result of applying modifiers to base stats.
/// </summary>
public struct ModifiedStats
{
    public float MaxHp;
    public float AttackDamage;
    public float AttackSpeed;
    public float MoveSpeed;
    public Dictionary<string, bool> Flags;

    public static ModifiedStats Default => new()
    {
        MaxHp = 100f,
        AttackDamage = 10f,
        AttackSpeed = 1f,
        MoveSpeed = 3f,
        Flags = new Dictionary<string, bool>()
    };
}

/// <summary>
/// Base stats before modifier application.
/// </summary>
public struct BaseStats
{
    public float MaxHp;
    public float AttackDamage;
    public float AttackSpeed;
    public float MoveSpeed;
}
