using Godot;
using System.Collections.Generic;
using ProjectSummoner.Services.Interfaces;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Central service for managing and applying modifiers.
/// Replaces the GDScript modifier_system.gd.
///
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
    // PROVIDER REGISTRATION
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

    // =========================================================================
    // GDSCRIPT INTEROP (snake_case methods)
    // =========================================================================

    /// <summary>
    /// Get modifiers for GDScript callers.
    /// Matches the signature of modifier_system.gd.get_modifiers_for()
    /// </summary>
    public Godot.Collections.Array get_modifiers_for(
        string targetType,
        Godot.Collections.Dictionary categories,
        Godot.Collections.Dictionary context)
    {
        var modContext = ModifierContext.FromDictionaries(categories, context);
        modContext.TargetType = targetType;

        var modifiers = GetModifiers(modContext);

        // Convert to Godot Array of Dictionaries
        var result = new Godot.Collections.Array();
        foreach (var mod in modifiers)
        {
            result.Add(mod.ToDictionary());
        }
        return result;
    }

    /// <summary>
    /// Register a GDScript provider (duck-typed).
    /// </summary>
    public void register_provider(string providerId, GodotObject provider)
    {
        var wrapper = new GDScriptProviderWrapper(providerId, provider);
        RegisterProvider(providerId, wrapper);
    }

    /// <summary>
    /// Unregister a provider by ID.
    /// </summary>
    public void unregister_provider(string providerId)
    {
        UnregisterProvider(providerId);
    }

    /// <summary>
    /// Clear all providers.
    /// </summary>
    public void clear_providers()
    {
        ClearProviders();
    }

    // =========================================================================
    // PROVIDER FACTORIES (for GDScript)
    // =========================================================================

    /// <summary>
    /// Register a summoner modifier provider (factory method for GDScript).
    /// Creates a SummonerModifierProvider internally since GDScript can't instantiate C# classes.
    /// </summary>
    public void register_summoner_provider(GodotObject summonerInstance, string summonerId)
    {
        var provider = new SummonerModifierProvider(summonerInstance, summonerId);
        RegisterProvider(provider);
    }

    /// <summary>
    /// Register a card modifier provider (factory method for GDScript).
    /// Creates a CardModifierProvider internally since GDScript can't instantiate C# classes.
    /// </summary>
    public void register_card_provider(string cardInstanceId)
    {
        var provider = new CardModifierProvider(cardInstanceId);
        RegisterProvider(provider);
    }

    // =========================================================================
    // MODIFIER APPLICATION
    // =========================================================================

    /// <summary>
    /// Apply modifiers to base stats and return modified stats.
    /// Consolidates the logic from Unit3D.InitializeWithModifiers.
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

    /// <summary>
    /// Apply modifiers from a Godot Array (for GDScript interop).
    /// </summary>
    public static ModifiedStats ApplyModifiersFromArray(BaseStats baseStats, Godot.Collections.Array modifiers)
    {
        var typedModifiers = new List<StatModifier>();
        foreach (var mod in modifiers)
        {
            if (mod.VariantType == Variant.Type.Dictionary)
            {
                typedModifiers.Add(StatModifier.FromDictionary(mod.AsGodotDictionary()));
            }
        }
        return ApplyModifiers(baseStats, typedModifiers);
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

        // String comparison fallback
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

    public void debug_print_providers()
    {
        GD.Print("=== ModifierService Debug ===");
        GD.Print($"Registered providers: {_providers.Count}");
        foreach (var providerId in _providers.Keys)
        {
            GD.Print($"  - {providerId}");
        }
    }

    public void debug_print_modifiers(Godot.Collections.Dictionary categories)
    {
        var context = new ModifierContext();
        foreach (var key in categories.Keys)
        {
            context.Categories[key.AsString()] = categories[key].Obj!;
        }

        var modifiers = GetModifiers(context);
        GD.Print($"=== Modifiers for categories: {categories} ===");
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

/// <summary>
/// Wrapper for GDScript providers to implement IModifierProvider.
/// </summary>
internal class GDScriptProviderWrapper : IModifierProvider
{
    private readonly string _providerId;
    private readonly GodotObject _provider;

    public string ProviderId => _providerId;

    public GDScriptProviderWrapper(string providerId, GodotObject provider)
    {
        _providerId = providerId;
        _provider = provider;
    }

    public List<StatModifier> GetModifiers()
    {
        var result = new List<StatModifier>();

        if (_provider == null || !GodotObject.IsInstanceValid(_provider))
            return result;

        if (!_provider.HasMethod("get_modifiers"))
            return result;

        var mods = _provider.Call("get_modifiers");
        if (mods.VariantType != Variant.Type.Array)
            return result;

        foreach (var mod in mods.AsGodotArray())
        {
            if (mod.VariantType == Variant.Type.Dictionary)
            {
                result.Add(StatModifier.FromDictionary(mod.AsGodotDictionary()));
            }
        }

        return result;
    }
}
