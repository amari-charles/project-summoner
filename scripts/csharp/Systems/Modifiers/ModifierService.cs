using Godot;
using System.Collections.Generic;
using ProjectSummoner.Services.Interfaces;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Central service for managing and applying modifiers.
/// Collects modifiers from registered providers, filters by conditions,
/// applies amplification, and provides them to targets for application.
/// </summary>
[GlobalClass]
public partial class ModifierService : Node, IModifierService
{
    public static ModifierService? Instance { get; private set; }

    private readonly Dictionary<string, IModifierProvider> _providers = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // =========================================================================
    // PROVIDER REGISTRATION (C# API)
    // =========================================================================

    /// <summary>
    /// Register a modifier provider.
    /// </summary>
    public void RegisterProvider(IModifierProvider provider)
    {
        if (_providers.ContainsKey(provider.ProviderId))
        {
            GD.PushWarning($"ModifierService: Provider '{provider.ProviderId}' already registered, replacing");
        }
        _providers[provider.ProviderId] = provider;
    }

    /// <summary>
    /// Register a provider with a custom ID.
    /// </summary>
    public void RegisterProvider(string providerId, IModifierProvider provider)
    {
        if (_providers.ContainsKey(providerId))
        {
            GD.PushWarning($"ModifierService: Provider '{providerId}' already registered, replacing");
        }
        _providers[providerId] = provider;
    }

    /// <summary>
    /// Unregister a provider by ID.
    /// </summary>
    public void UnregisterProvider(string providerId)
    {
        _providers.Remove(providerId);
    }

    /// <summary>
    /// Unregister a provider.
    /// </summary>
    public void UnregisterProvider(IModifierProvider provider)
    {
        _providers.Remove(provider.ProviderId);
    }

    /// <summary>
    /// Clear all providers.
    /// </summary>
    public void ClearProviders()
    {
        _providers.Clear();
    }

    // =========================================================================
    // MODIFIER COLLECTION (C# API)
    // =========================================================================

    /// <summary>
    /// Get all modifiers that apply to a target.
    /// </summary>
    public List<StatModifier> GetModifiers(ModifierContext context)
    {
        var allModifiers = new List<StatModifier>();

        // Collect from all providers
        foreach (var provider in _providers.Values)
        {
            var providerMods = provider.GetModifiers();
            allModifiers.AddRange(providerMods);
        }

        // Filter by conditions and instance scope
        var filtered = FilterModifiers(allModifiers, context);

        // Apply amplification
        ApplyAmplification(filtered);

        return filtered;
    }

    /// <summary>
    /// Get modifiers partitioned into static (always active) and triggered (conditional).
    /// Static modifiers are applied at spawn; triggered modifiers are stored and activated by combat events.
    /// </summary>
    public (List<StatModifier> Static, List<StatModifier> Triggered) GetModifiersPartitioned(ModifierContext context)
    {
        var allModifiers = GetModifiers(context);

        var staticMods = new List<StatModifier>();
        var triggeredMods = new List<StatModifier>();

        foreach (var mod in allModifiers)
        {
            if (mod.IsTriggered)
            {
                triggeredMods.Add(mod);
            }
            else
            {
                staticMods.Add(mod);
            }
        }

        return (staticMods, triggeredMods);
    }

    // =========================================================================
    // GDSCRIPT INTEROP (snake_case methods for GDScript callers)
    // =========================================================================

    /// <summary>
    /// Register a summoner modifier provider (factory method for GDScript).
    /// Called from game_controller_3d.gd
    /// </summary>
    public void register_summoner_provider(GodotObject summonerInstance, string summonerId)
    {
        var provider = new SummonerModifierProvider(summonerInstance, summonerId);
        RegisterProvider(provider);
    }

    /// <summary>
    /// Unregister a provider by ID (for GDScript).
    /// Called from game_controller_3d.gd
    /// </summary>
    public void unregister_provider(string providerId)
    {
        UnregisterProvider(providerId);
    }

    /// <summary>
    /// Register a card modifier provider (factory method for GDScript).
    /// </summary>
    public void register_card_provider(string cardInstanceId)
    {
        var provider = new CardModifierProvider(cardInstanceId);
        RegisterProvider(provider);
    }

    /// <summary>
    /// Register an item modifier provider (factory method for GDScript).
    /// Called from game_controller_3d.gd alongside summoner provider.
    /// </summary>
    public void register_item_provider(string summonerId)
    {
        var provider = new ItemModifierProvider(summonerId);
        RegisterProvider(provider);
    }

    // =========================================================================
    // MODIFIER APPLICATION
    // =========================================================================

    /// <summary>
    /// Apply modifiers to base stats and return modified stats.
    /// </summary>
    public static ModifiedStats ApplyModifiers(BaseStats baseStats, List<StatModifier> modifiers)
    {
        // Phase 1: Collect all additive bonuses
        float hpAdd = 0f, damageAdd = 0f, speedAdd = 0f, moveSpeedAdd = 0f;

        // Phase 2: Collect all multiplicative bonuses
        float hpMult = 1f, damageMult = 1f, speedMult = 1f, moveSpeedMult = 1f;

        // Phase 3: Collect all flags
        var flags = new Dictionary<string, bool>();

        foreach (var mod in modifiers)
        {
            // Process stat_adds
            if (mod.StatAdds.TryGetValue("max_hp", out var hp)) hpAdd += hp;
            if (mod.StatAdds.TryGetValue("attack_damage", out var dmg)) damageAdd += dmg;
            if (mod.StatAdds.TryGetValue("attack_speed", out var spd)) speedAdd += spd;
            if (mod.StatAdds.TryGetValue("move_speed", out var mvSpd)) moveSpeedAdd += mvSpd;

            // Process stat_mults
            if (mod.StatMults.TryGetValue("max_hp", out var hpM)) hpMult *= hpM;
            if (mod.StatMults.TryGetValue("attack_damage", out var dmgM)) damageMult *= dmgM;
            if (mod.StatMults.TryGetValue("attack_speed", out var spdM)) speedMult *= spdM;
            if (mod.StatMults.TryGetValue("move_speed", out var mvSpdM)) moveSpeedMult *= mvSpdM;

            // Process flags
            foreach (var kvp in mod.Flags)
            {
                flags[kvp.Key] = kvp.Value;
            }
        }

        // Apply formula: (base + adds) * mults
        return new ModifiedStats
        {
            MaxHp = (baseStats.MaxHp + hpAdd) * hpMult,
            AttackDamage = (baseStats.AttackDamage + damageAdd) * damageMult,
            AttackSpeed = (baseStats.AttackSpeed + speedAdd) * speedMult,
            MoveSpeed = (baseStats.MoveSpeed + moveSpeedAdd) * moveSpeedMult,
            Flags = flags
        };
    }

    // =========================================================================
    // FILTERING
    // =========================================================================

    private static List<StatModifier> FilterModifiers(List<StatModifier> modifiers, ModifierContext context)
    {
        var filtered = new List<StatModifier>();
        var cardInstanceId = context.CardInstanceId ?? "";

        foreach (var mod in modifiers)
        {
            // Instance-scoped modifiers only apply to matching card
            if (!string.IsNullOrEmpty(mod.CardInstanceId))
            {
                if (mod.CardInstanceId != cardInstanceId)
                    continue; // Skip - wrong card instance
            }

            // Condition-based filtering
            if (MatchesConditions(mod, context))
            {
                filtered.Add(mod);
            }
        }

        return filtered;
    }

    private static bool MatchesConditions(StatModifier modifier, ModifierContext context)
    {
        // If no conditions, modifier always applies
        if (modifier.Conditions.Count == 0)
            return true;

        // Check each condition
        foreach (var kvp in modifier.Conditions)
        {
            var conditionKey = kvp.Key;
            var requiredValue = kvp.Value;

            if (!context.Categories.TryGetValue(conditionKey, out var actualValue))
                return false;

            // Handle array values (tags)
            if (actualValue is Godot.Collections.Array actualArray)
            {
                bool found = false;
                foreach (var item in actualArray)
                {
                    if (item.Obj?.Equals(requiredValue) == true ||
                        item.AsString() == requiredValue?.ToString())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            // Handle Element objects (elemental_affinity)
            else if (conditionKey == "elemental_affinity")
            {
                if (!MatchesElement(actualValue, requiredValue))
                    return false;
            }
            // Direct comparison
            else
            {
                if (!ValuesEqual(actualValue, requiredValue))
                    return false;
            }
        }

        return true;
    }

    private static bool MatchesElement(object? actual, object? required)
    {
        if (actual == null || required == null)
            return false;

        // Handle Element objects with matches_affinity method
        if (actual is GodotObject actualObj && actualObj.HasMethod("matches_affinity"))
        {
            // Convert required to appropriate Variant type
            Variant requiredVar;
            if (required is string reqStr)
                requiredVar = reqStr;
            else if (required is GodotObject reqObj)
                requiredVar = reqObj;
            else
                requiredVar = required.ToString() ?? "";

            return actualObj.Call("matches_affinity", requiredVar).AsBool();
        }

        // String comparison
        return actual.ToString() == required.ToString();
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        return a.Equals(b) || a.ToString() == b.ToString();
    }

    // =========================================================================
    // AMPLIFICATION
    // =========================================================================

    private static void ApplyAmplification(List<StatModifier> modifiers)
    {
        // Step 1: Find all amplifiers and calculate total amplification per tag
        var amplifiers = new Dictionary<string, float>();

        foreach (var mod in modifiers)
        {
            if (!string.IsNullOrEmpty(mod.AmplifyTag))
            {
                if (!amplifiers.ContainsKey(mod.AmplifyTag))
                    amplifiers[mod.AmplifyTag] = 1.0f;
                amplifiers[mod.AmplifyTag] *= mod.AmplifyFactor;
            }
        }

        if (amplifiers.Count == 0)
            return; // No amplifiers, nothing to do

        // Step 2: Apply amplification to tagged modifiers
        foreach (var mod in modifiers)
        {
            // Don't amplify amplifiers themselves
            if (!string.IsNullOrEmpty(mod.AmplifyTag))
                continue;

            // Calculate total amplification for this modifier's tags
            float totalAmp = 1.0f;
            foreach (var tag in mod.Tags)
            {
                if (amplifiers.TryGetValue(tag, out var amp))
                    totalAmp *= amp;
            }

            if (totalAmp == 1.0f)
                continue; // No amplification needed

            // Amplify additive bonuses
            var addKeys = new List<string>(mod.StatAdds.Keys);
            foreach (var stat in addKeys)
            {
                mod.StatAdds[stat] *= totalAmp;
            }

            // Amplify multiplicative bonuses (amplify the bonus, not the base)
            var multKeys = new List<string>(mod.StatMults.Keys);
            foreach (var stat in multKeys)
            {
                float bonus = mod.StatMults[stat] - 1.0f;
                bonus *= totalAmp;
                mod.StatMults[stat] = 1.0f + bonus;
            }
        }
    }

    // =========================================================================
    // DEBUGGING
    // =========================================================================

    public void DebugPrintProviders()
    {
        GD.Print("=== ModifierService Debug ===");
        GD.Print($"Registered providers: {_providers.Count}");
        foreach (var providerId in _providers.Keys)
        {
            GD.Print($"  - {providerId}");
        }
    }

    public void DebugPrintModifiers(ModifierContext context)
    {
        var modifiers = GetModifiers(context);
        GD.Print($"=== Modifiers for context ===");
        GD.Print($"Total modifiers: {modifiers.Count}");
        foreach (var mod in modifiers)
        {
            GD.Print($"  - Source: {mod.Source}");
            GD.Print($"    Tags: [{string.Join(", ", mod.Tags)}]");
            GD.Print($"    Stat mults: {string.Join(", ", mod.StatMults)}");
            GD.Print($"    Stat adds: {string.Join(", ", mod.StatAdds)}");
        }
    }
}
