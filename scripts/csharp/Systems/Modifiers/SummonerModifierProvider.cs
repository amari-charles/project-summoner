using Godot;
using System.Collections.Generic;
using ProjectSummoner.Data.Traits;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Provides unit modifiers from summoner traits.
/// Reads unit modifiers directly from C# TraitCatalog based on the summoner's traits.
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

        // Get all trait IDs from the summoner instance (still in GDScript)
        if (!_summonerInstance.HasMethod("get_all_trait_ids"))
            return modifiers;

        var traitIdsVar = _summonerInstance.Call("get_all_trait_ids");
        if (traitIdsVar.VariantType != Variant.Type.Array)
            return modifiers;

        var traitIds = traitIdsVar.AsGodotArray();

        // Collect unit modifiers from all summoner traits using C# TraitCatalog directly
        foreach (var traitIdVar in traitIds)
        {
            string traitId = traitIdVar.AsString();

            var traitModifiers = TraitCatalog.GetUnitModifiersForTrait(traitId);
            modifiers.AddRange(traitModifiers);
        }

        return modifiers;
    }
}
