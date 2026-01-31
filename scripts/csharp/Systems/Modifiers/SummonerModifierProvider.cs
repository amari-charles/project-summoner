using Godot;
using System;
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
    /// <param name="summonerInstance">The GDScript SummonerInstance object. Must have get_all_trait_ids() method.</param>
    /// <param name="summonerId">The summoner's ID.</param>
    /// <exception cref="ArgumentNullException">Thrown if summonerInstance is null.</exception>
    /// <exception cref="ArgumentException">Thrown if summonerInstance lacks required methods or summonerId is empty.</exception>
    public SummonerModifierProvider(GodotObject summonerInstance, string summonerId)
    {
        if (summonerInstance == null)
            throw new ArgumentNullException(nameof(summonerInstance));
        if (string.IsNullOrEmpty(summonerId))
            throw new ArgumentException("Summoner ID cannot be null or empty", nameof(summonerId));
        if (!summonerInstance.HasMethod("get_all_trait_ids"))
            throw new ArgumentException("SummonerInstance must have get_all_trait_ids() method", nameof(summonerInstance));

        _summonerInstance = summonerInstance;
        _summonerId = summonerId;
    }

    public List<StatModifier> GetModifiers()
    {
        var modifiers = new List<StatModifier>();

        // Instance was validated at construction, but check validity in case it was freed
        if (!GodotObject.IsInstanceValid(_summonerInstance))
        {
            GD.PushWarning($"SummonerModifierProvider: SummonerInstance for '{_summonerId}' is no longer valid");
            return modifiers;
        }

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
