using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;

namespace Fateforged.Data.Traits;

/// <summary>
/// Declares the authored, natural development path for each Card catalog entry.
/// Core membership is explicit: broad eligibility tags must never turn the
/// global trait pool into a Card's Core path.
/// </summary>
public static class CardCoreCatalog
{
    private static readonly Dictionary<string, TraitId[]> CoreTraitIdsByCard = new(
        StringComparer.Ordinal
    )
    {
        [CardIds.FireWisp.Value] =
        [
            TraitIds.FireWispTwinFlame,
            TraitIds.FireWispDancingEmbers,
            TraitIds.FireWispCondensedFlame,
            TraitIds.FireWispBlazingCore,
        ],
        [CardIds.WaterWisp.Value] =
        [
            TraitIds.Fortitude,
            TraitIds.FortitudeII,
            TraitIds.FortitudeIII,
            TraitIds.FortitudeIV,
            TraitIds.Warding,
            TraitIds.WardingII,
            TraitIds.WardingIII,
            TraitIds.WardingIV,
        ],
        [CardIds.WindWisp.Value] =
        [
            TraitIds.Swiftness,
            TraitIds.SwiftnessII,
            TraitIds.SwiftnessIII,
            TraitIds.SwiftnessIV,
            TraitIds.Agility,
            TraitIds.AgilityII,
            TraitIds.AgilityIII,
            TraitIds.AgilityIV,
        ],
        [CardIds.EarthWisp.Value] =
        [
            TraitIds.Fortitude,
            TraitIds.FortitudeII,
            TraitIds.FortitudeIII,
            TraitIds.FortitudeIV,
            TraitIds.Plating,
            TraitIds.PlatingII,
            TraitIds.PlatingIII,
            TraitIds.PlatingIV,
        ],
    };

    public static IReadOnlyList<TraitId> GetCoreTraitIds(CardId cardCatalogId) =>
        CoreTraitIdsByCard.TryGetValue(cardCatalogId.Value, out var traitIds) ? traitIds : [];

    public static TraitDefinition[] GetCoreTraits(CardId cardCatalogId) =>
        GetCoreTraitIds(cardCatalogId)
            .Select(traitId =>
                TraitCatalog.GetTrait(traitId)
                    ?? throw new InvalidOperationException(
                        $"Card Core for '{cardCatalogId}' references unknown trait '{traitId}'"
                    )
            )
            .ToArray();

    public static bool Contains(CardId cardCatalogId, TraitId traitId) =>
        GetCoreTraitIds(cardCatalogId).Contains(traitId);
}
