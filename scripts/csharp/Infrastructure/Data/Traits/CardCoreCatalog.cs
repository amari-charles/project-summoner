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
    };

    public static IReadOnlyList<TraitId> GetCoreTraitIds(CardId cardCatalogId) =>
        CoreTraitIdsByCard.TryGetValue(cardCatalogId.Value, out var traitIds) ? traitIds : [];

    public static TraitDefinition[] GetCoreTraits(CardId cardCatalogId) =>
        GetCoreTraitIds(cardCatalogId)
            .Select(TraitCatalog.GetTrait)
            .Where(definition => definition != null)
            .Cast<TraitDefinition>()
            .ToArray();

    public static bool Contains(CardId cardCatalogId, TraitId traitId) =>
        GetCoreTraitIds(cardCatalogId).Contains(traitId);
}
