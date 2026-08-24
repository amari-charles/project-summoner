using System.Collections.Immutable;
using Fateforged.Data.Quests;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Rewards;
using Godot.Collections;

namespace Fateforged.Meta.Quests;

public sealed class QuestRewardProcessor
{
    private readonly UniversalRewardRuntime _runtime;
    private readonly System.Func<SummonerId> _getActiveSummoner;
    private readonly RewardViewModelFactory _views = new();

    public QuestRewardProcessor(
        UniversalRewardRuntime runtime,
        System.Func<SummonerId> getActiveSummoner
    )
    {
        _runtime = runtime;
        _getActiveSummoner = getActiveSummoner;
    }

    public Array<Dictionary> GetPreviews(QuestDefinition quest)
    {
        var result = new Array<Dictionary>();
        foreach (var offer in quest.RewardOffers)
        {
            if (!TryResolve(quest, offer, earned: false, out var snapshot))
                continue;
            var state = _runtime.ProfileStore.GetRewardState();
            state.PendingSelections.TryGetValue(snapshot.ClaimId.Value, out var pending);
            state.ClaimReceipts.TryGetValue(snapshot.ClaimId.Value, out var receipt);
            result.Add(ToOffer(_views.Create(offer, snapshot, pending, receipt)));
        }
        return result;
    }

    public Dictionary Complete(QuestDefinition quest)
    {
        var grants = new Array<Dictionary>();
        var pendingOffers = new Array<Dictionary>();
        foreach (var offer in quest.RewardOffers)
        {
            if (!TryResolve(quest, offer, earned: true, out var snapshot))
                return [];

            if (snapshot.SelectionMode == RewardSelectionMode.PlayerChoice)
            {
                var state = _runtime.ProfileStore.GetRewardState();
                state.PendingSelections.TryGetValue(snapshot.ClaimId.Value, out var pending);
                state.ClaimReceipts.TryGetValue(snapshot.ClaimId.Value, out var receipt);
                pendingOffers.Add(ToOffer(_views.Create(offer, snapshot, pending, receipt)));
                continue;
            }

            var claim = _runtime.Claims.Claim(new RewardClaimRequest { ClaimId = snapshot.ClaimId });
            if (
                claim.Status is not RewardRuntimeStatus.Ready and not RewardRuntimeStatus.AlreadyClaimed
                || claim.Receipt == null
            )
                return [];
            if (claim.Status == RewardRuntimeStatus.Ready)
            {
                foreach (var grant in claim.Receipt.AppliedGrants)
                    grants.Add(ToGrant(RewardViewModelFactory.CreateGrant(grant)));
            }
        }

        return new Dictionary
        {
            ["quest_id"] = quest.Id,
            ["granted_rewards"] = grants,
            ["pending_reward_offers"] = pendingOffers,
        };
    }

    private bool TryResolve(
        QuestDefinition quest,
        RewardOfferDefinition offer,
        bool earned,
        out ResolvedRewardOfferSnapshot snapshot
    )
    {
        snapshot = null!;
        if (_runtime.Status != RewardRuntimeStatus.Ready)
            return false;
        var summonerId = _getActiveSummoner();
        var source = new RewardSourceContext
        {
            SourceType = "quest",
            SourceId = quest.Id,
            OccurrenceId = "completion",
        };
        var claimId = RewardIdentity.CreateClaimId(summonerId, source, offer.Id);
        var state = _runtime.ProfileStore.GetRewardState();
        if (state.ResolvedOffers.TryGetValue(claimId.Value, out var existing))
        {
            snapshot = existing;
            if (state.ClaimReceipts.ContainsKey(claimId.Value))
                return true;
            var existingPending =
                earned && snapshot.SelectionMode == RewardSelectionMode.PlayerChoice
                    ? new PendingRewardSelection
                    {
                        ClaimId = claimId,
                        ChooseCount = snapshot.ChooseCount,
                    }
                    : null;
            return existingPending == null
                || _runtime.ProfileStore.TryStoreResolvedOffer(snapshot, existingPending, out _);
        }

        ulong seed = 0;
        if (
            offer.OptionSource is PoolRewardOptionSourceDefinition
            && !_runtime.ProfileStore.TryGetOrCreateRewardSeed(summonerId, out seed, out _)
        )
            return false;
        var resolved = _runtime.Resolver.Resolve(
            offer,
            new RewardResolutionContext
            {
                SummonerId = summonerId,
                SummonerSeed = seed,
                Source = source,
                Catalog = _runtime.Catalog,
                OwnedRewardKeys = _runtime.ProfileStore.GetOwnedRewardKeys(summonerId),
            }
        );
        if (resolved.Status != RewardRuntimeStatus.Ready || resolved.Snapshot == null)
            return false;
        snapshot = resolved.Snapshot;
        var pending =
            earned && snapshot.SelectionMode == RewardSelectionMode.PlayerChoice
                ? new PendingRewardSelection
                {
                    ClaimId = snapshot.ClaimId,
                    ChooseCount = snapshot.ChooseCount,
                }
                : null;
        return _runtime.ProfileStore.TryStoreResolvedOffer(snapshot, pending, out _);
    }

    private static Dictionary ToOffer(RewardOfferViewModel offer)
    {
        var options = new Array<Dictionary>();
        foreach (var option in offer.Options)
        {
            var grants = new Array<Dictionary>();
            foreach (var grant in option.Grants)
                grants.Add(ToGrant(grant));
            options.Add(
                new Dictionary
                {
                    ["id"] = option.Id.Value,
                    ["label_key"] = option.LabelKey,
                    ["description_key"] = option.DescriptionKey,
                    ["grants"] = grants,
                    ["is_selected"] = option.IsSelected,
                }
            );
        }
        var result = new Dictionary
        {
            ["offer_id"] = offer.Id.Value,
            ["display_state"] = offer.DisplayState.ToString().ToLowerInvariant(),
            ["selection_mode"] = offer.SelectionMode.ToString().ToLowerInvariant(),
            ["choose_count"] = offer.ChooseCount,
            ["options"] = options,
        };
        if (offer.ClaimId.HasValue)
            result["claim_id"] = offer.ClaimId.Value.Value;
        if (options.Count > 0)
            result["label_key"] = options[0]["label_key"];
        return result;
    }

    private static Dictionary ToGrant(RewardGrantViewModel grant)
    {
        var result = new Dictionary
        {
            ["kind"] = grant.Kind,
            ["ownership_scope"] = grant.OwnershipScope.ToString(),
            ["target_id"] = grant.TargetId,
            ["id"] = grant.ContentId,
            ["amount"] = grant.Amount,
        };
        if (grant.Kind == "card")
        {
            result["card_id"] = grant.ContentId;
            result["rarity"] = grant.Rarity;
        }
        return result;
    }
}
