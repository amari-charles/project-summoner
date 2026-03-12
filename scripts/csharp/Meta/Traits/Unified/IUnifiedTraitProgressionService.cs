using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Summoners;

namespace Fateforged.Meta.Traits.Unified;

/// <summary>
/// Pass 2 contract for unified trait progression APIs across summoners and cards.
/// Implementations may be stubs until Pass 3 runtime behavior is complete.
/// </summary>
public interface IUnifiedTraitProgressionService
{
    UnifiedPointAmount GetUnspentTraitPoints(SummonerId summonerId);
    UnifiedPointAmount GrantTraitPoints(
        SummonerId summonerId,
        UnifiedPointAmount amount,
        UnifiedProgressionSource source
    );
    List<UnifiedTraitOffer> RollTraitOffers(
        SummonerId summonerId,
        UnifiedTraitOfferRequest request
    );
    bool SpendTraitPoint(SummonerId summonerId, UnifiedTraitId traitId);

    UnifiedPointAmount GetCardUnspentTraitPoints(CardInstanceId cardInstanceId);
    UnifiedPointAmount GrantCardTraitPoints(
        CardInstanceId cardInstanceId,
        UnifiedPointAmount amount,
        UnifiedProgressionSource source
    );
    List<UnifiedTraitOffer> RollCardTraitOffers(
        CardInstanceId cardInstanceId,
        UnifiedTraitOfferRequest request
    );
    bool SpendCardTraitPoint(CardInstanceId cardInstanceId, UnifiedTraitId traitId);
}
