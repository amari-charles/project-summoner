using Godot;
using System.Collections.Generic;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Provides unit modifiers from summoner traits.
/// Reads unit modifiers from TraitCatalog based on the summoner's traits.
///
/// Replaces GDScript summoner_modifier_provider.gd
/// </summary>
public class SummonerModifierProvider : IModifierProvider
{
    private readonly GodotObject _summonerInstance;
    private readonly string _summonerId;

    public string ProviderId => $"summoner_{_summonerId}";

    /// <summary>
    /// Create a provider for a GDScript SummonerInstance.
    /// </summary>
    public SummonerModifierProvider(GodotObject summonerInstance, string summonerId)
    {
        _summonerInstance = summonerInstance;
        _summonerId = summonerId;
    }

    public List<StatModifier> GetModifiers()
    {
        var modifiers = new List<StatModifier>();

        if (_summonerInstance == null || !GodotObject.IsInstanceValid(_summonerInstance))
            return modifiers;

        // Get TraitCatalog autoload
        var mainLoop = Engine.GetMainLoop();
        if (mainLoop is not SceneTree sceneTree)
            return modifiers;

        var traitCatalog = sceneTree.Root.GetNodeOrNull("/root/TraitCatalog");
        if (traitCatalog == null)
        {
            GD.PushWarning("SummonerModifierProvider: TraitCatalog not found");
            return modifiers;
        }

        if (!traitCatalog.HasMethod("get_unit_modifiers_for_trait"))
        {
            GD.PushWarning("SummonerModifierProvider: TraitCatalog.get_unit_modifiers_for_trait() not available");
            return modifiers;
        }

        // Get all trait IDs from the summoner instance
        if (!_summonerInstance.HasMethod("get_all_trait_ids"))
            return modifiers;

        var traitIdsVar = _summonerInstance.Call("get_all_trait_ids");
        if (traitIdsVar.VariantType != Variant.Type.Array)
            return modifiers;

        var traitIds = traitIdsVar.AsGodotArray();

        // Collect unit modifiers from all summoner traits
        foreach (var traitIdVar in traitIds)
        {
            string traitId = traitIdVar.AsString();
            var traitMods = traitCatalog.Call("get_unit_modifiers_for_trait", traitId);

            if (traitMods.VariantType != Variant.Type.Array)
                continue;

            // Convert each trait modifier dictionary to StatModifier
            foreach (var modVar in traitMods.AsGodotArray())
            {
                if (modVar.VariantType != Variant.Type.Dictionary)
                    continue;

                var modifier = StatModifier.FromDictionary(modVar.AsGodotDictionary());
                modifiers.Add(modifier);
            }
        }

        return modifiers;
    }
}
