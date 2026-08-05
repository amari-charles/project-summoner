using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Infrastructure.Persistence;

namespace Fateforged.Meta.Rewards;

public enum RewardRuntimeStatus
{
    Ready,
    Unavailable,
    Invalid,
    AlreadyClaimed,
}

public sealed record RewardResolutionContext
{
    public required SummonerId SummonerId { get; init; }
    public required ulong SummonerSeed { get; init; }
    public required RewardSourceContext Source { get; init; }
    public int ResolutionVersion { get; init; } = 1;
    public RewardContentCatalog Catalog { get; init; } = new();
    public IReadOnlySet<string> OwnedRewardKeys { get; init; } = new HashSet<string>();
}

public sealed record RewardResolutionResult
{
    public required RewardRuntimeStatus Status { get; init; }
    public ResolvedRewardOfferSnapshot? Snapshot { get; init; }
    public ImmutableArray<string> Errors { get; init; } = [];

    public static RewardResolutionResult Unavailable() =>
        new()
        {
            Status = RewardRuntimeStatus.Unavailable,
            Errors = ["Reward resolution is unavailable without a reward profile store."],
        };
}

public interface IRewardOptionSource
{
    Type DefinitionType { get; }

    RewardResolutionResult Resolve(
        RewardOfferDefinition offer,
        RewardOptionSourceDefinition source,
        RewardResolutionContext context
    );
}

public sealed class AuthoredRewardOptionSource : IRewardOptionSource
{
    public Type DefinitionType => typeof(AuthoredRewardOptionSourceDefinition);

    public RewardResolutionResult Resolve(
        RewardOfferDefinition offer,
        RewardOptionSourceDefinition source,
        RewardResolutionContext context
    )
    {
        if (source is not AuthoredRewardOptionSourceDefinition authored)
        {
            return Invalid($"Authored option source received {source.GetType().Name}.");
        }

        return RewardOptionResolution.ResolveCandidates(
            offer,
            authored.Options,
            context,
            randomize: false
        );
    }

    private static RewardResolutionResult Invalid(string error) =>
        new() { Status = RewardRuntimeStatus.Invalid, Errors = [error] };
}

public sealed class PoolRewardOptionSource : IRewardOptionSource
{
    public Type DefinitionType => typeof(PoolRewardOptionSourceDefinition);

    public RewardResolutionResult Resolve(
        RewardOfferDefinition offer,
        RewardOptionSourceDefinition source,
        RewardResolutionContext context
    )
    {
        if (source is not PoolRewardOptionSourceDefinition poolSource)
        {
            return new RewardResolutionResult
            {
                Status = RewardRuntimeStatus.Invalid,
                Errors = [$"Pool option source received {source.GetType().Name}."],
            };
        }

        if (!context.Catalog.Pools.TryGetValue(poolSource.PoolId, out var pool))
        {
            return new RewardResolutionResult
            {
                Status = RewardRuntimeStatus.Invalid,
                Errors = [$"Reward pool '{poolSource.PoolId}' was not found."],
            };
        }

        return RewardOptionResolution.ResolveCandidates(
            offer,
            pool.Options,
            context,
            randomize: true
        );
    }
}

public sealed class RewardResolver
{
    private readonly IReadOnlyDictionary<Type, IRewardOptionSource> _sources;

    public RewardResolver(IEnumerable<IRewardOptionSource> sources)
    {
        var registered = new Dictionary<Type, IRewardOptionSource>();
        foreach (var source in sources)
        {
            if (!registered.TryAdd(source.DefinitionType, source))
                throw new InvalidOperationException(
                    $"A reward option source is already registered for {source.DefinitionType.Name}."
                );
        }
        _sources = registered;
    }

    public RewardResolutionResult Resolve(
        RewardOfferDefinition offer,
        RewardResolutionContext context
    )
    {
        if (!_sources.TryGetValue(offer.OptionSource.GetType(), out var source))
        {
            return new RewardResolutionResult
            {
                Status = RewardRuntimeStatus.Invalid,
                Errors =
                [
                    $"No option-source implementation is registered for {offer.OptionSource.GetType().Name}.",
                ],
            };
        }

        return source.Resolve(offer, offer.OptionSource, context);
    }
}

internal static class RewardOptionResolution
{
    public static RewardResolutionResult ResolveCandidates(
        RewardOfferDefinition offer,
        ImmutableArray<RewardOptionDefinition> candidates,
        RewardResolutionContext context,
        bool randomize
    )
    {
        if (!offer.Id.HasValue)
            return Invalid("Reward offer ID is required.");

        if (offer.Selection.ShowCount <= 0 || offer.Selection.ChooseCount <= 0)
            return Invalid("Reward showCount and chooseCount must both be positive.");

        if (offer.Selection.ChooseCount > offer.Selection.ShowCount)
            return Invalid("Reward chooseCount cannot exceed showCount.");

        var distinct = candidates
            .GroupBy(option => option.Id.Value, StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(option => IsEligible(option, offer.Eligibility, context.OwnedRewardKeys))
            .ToList();

        if (
            distinct.Count < offer.Selection.ChooseCount
            && offer.Eligibility.FallbackToDuplicatesWhenInsufficient
        )
        {
            distinct = candidates
                .GroupBy(option => option.Id.Value, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
        }

        if (distinct.Count < offer.Selection.ChooseCount)
        {
            return Invalid(
                $"Reward offer '{offer.Id}' has {distinct.Count} eligible options but requires {offer.Selection.ChooseCount}."
            );
        }

        var shownCount = Math.Min(offer.Selection.ShowCount, distinct.Count);
        if (randomize)
        {
            distinct.Sort(
                (left, right) => StringComparer.Ordinal.Compare(left.Id.Value, right.Id.Value)
            );
            var random = DeterministicRewardRandom.FromContext(offer.Id, context);
            for (var i = distinct.Count - 1; i > 0; i--)
            {
                var j = random.NextInt(i + 1);
                (distinct[i], distinct[j]) = (distinct[j], distinct[i]);
            }
        }

        var claimId = RewardIdentity.CreateClaimId(context.SummonerId, context.Source, offer.Id);
        return new RewardResolutionResult
        {
            Status = RewardRuntimeStatus.Ready,
            Snapshot = new ResolvedRewardOfferSnapshot
            {
                ClaimId = claimId,
                OfferId = offer.Id,
                Source = context.Source,
                SummonerId = context.SummonerId,
                ResolutionVersion = context.ResolutionVersion,
                SelectionMode = offer.Selection.Mode,
                ChooseCount = offer.Selection.ChooseCount,
                Options = distinct
                    .Take(shownCount)
                    .Select(option =>
                        RewardTargetMaterializer.ForSummoner(option, context.SummonerId)
                    )
                    .ToImmutableArray(),
            },
        };
    }

    private static bool IsEligible(
        RewardOptionDefinition option,
        RewardEligibilityDefinition eligibility,
        IReadOnlySet<string> ownedKeys
    )
    {
        if (eligibility.DuplicatePolicy == RewardDuplicatePolicy.Allow)
            return true;

        return option.Grants.All(grant =>
        {
            var key = RewardOwnershipKey.ForGrant(grant);
            return string.IsNullOrEmpty(key) || !ownedKeys.Contains(key);
        });
    }

    private static RewardResolutionResult Invalid(string error) =>
        new() { Status = RewardRuntimeStatus.Invalid, Errors = [error] };
}

internal static class RewardTargetMaterializer
{
    public static RewardOptionDefinition ForSummoner(
        RewardOptionDefinition option,
        SummonerId summonerId
    ) =>
        option with
        {
            Grants = option
                .Grants.Select(grant => WithResolvedTarget(grant, summonerId))
                .ToImmutableArray(),
        };

    private static RewardGrantDefinition WithResolvedTarget(
        RewardGrantDefinition grant,
        SummonerId summonerId
    )
    {
        if (grant.Target.TargetId != "$summoner")
            return grant;

        var target = grant.Target with { TargetId = (string)summonerId };
        return grant switch
        {
            CardRewardGrantDefinition value => value with { Target = target },
            ResourceRewardGrantDefinition value => value with { Target = target },
            ItemRewardGrantDefinition value => value with { Target = target },
            SummonerUnlockRewardGrantDefinition value => value with { Target = target },
            CosmeticRewardGrantDefinition value => value with { Target = target },
            EmoteRewardGrantDefinition value => value with { Target = target },
            SummonerExperienceRewardGrantDefinition value => value with { Target = target },
            CardExperienceRewardGrantDefinition value => value with { Target = target },
            SummonerTraitRewardGrantDefinition value => value with { Target = target },
            CardTraitRewardGrantDefinition value => value with { Target = target },
            AcademyProgressFlagRewardGrantDefinition value => value with { Target = target },
            _ => grant,
        };
    }
}

public static class RewardIdentity
{
    public static RewardClaimId CreateClaimId(
        SummonerId summonerId,
        RewardSourceContext source,
        RewardOfferId offerId
    )
    {
        var identity = new StringBuilder();
        AppendField(identity, (string)summonerId);
        AppendField(identity, source.SourceType);
        AppendField(identity, source.SourceId);
        AppendField(identity, source.OccurrenceId);
        AppendField(identity, offerId.Value);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToString()));
        return new RewardClaimId($"reward:v1:{Convert.ToHexString(hash).ToLowerInvariant()}");
    }

    private static void AppendField(StringBuilder identity, string value) =>
        identity.Append(value.Length).Append(':').Append(value);
}

public static class RewardOwnershipKey
{
    public static string ForGrant(RewardGrantDefinition grant) =>
        grant switch
        {
            CardRewardGrantDefinition card => $"card:{card.CardId}",
            ItemRewardGrantDefinition item => $"item:{item.ItemId}",
            SummonerUnlockRewardGrantDefinition summoner => $"summoner:{summoner.SummonerId}",
            CosmeticRewardGrantDefinition cosmetic => $"cosmetic:{cosmetic.CosmeticId}",
            EmoteRewardGrantDefinition emote => $"emote:{emote.EmoteId}",
            SummonerTraitRewardGrantDefinition trait => $"summoner_trait:{trait.TraitId}",
            CardTraitRewardGrantDefinition trait => $"card_trait:{trait.TraitId}",
            AcademyProgressFlagRewardGrantDefinition flag => $"academy_flag:{flag.FlagId}",
            _ => "",
        };
}

public struct DeterministicRewardRandom
{
    public const int CurrentVersion = 1;
    private ulong _state;

    private DeterministicRewardRandom(ulong seed)
    {
        _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    }

    public static DeterministicRewardRandom FromContext(
        RewardOfferId offerId,
        RewardResolutionContext context
    )
    {
        var input = string.Join(
            "|",
            CurrentVersion,
            context.ResolutionVersion,
            context.SummonerSeed,
            context.Source.SourceType,
            context.Source.SourceId,
            context.Source.OccurrenceId,
            offerId.Value
        );
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new DeterministicRewardRandom(BinaryPrimitives.ReadUInt64LittleEndian(hash));
    }

    public int NextInt(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));

        return (int)(NextUInt64() % (uint)exclusiveMax);
    }

    private ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}

public sealed record RewardGrantContext
{
    public required RewardClaimId ClaimId { get; init; }
    public required RewardSourceContext Source { get; init; }
}

public sealed record RewardGrantPreparation
{
    public bool IsValid { get; init; }
    public IRewardGrantMutation? Mutation { get; init; }
    public ImmutableArray<string> Errors { get; init; } = [];
}

public interface IRewardGrantMutation
{
    bool TryApply(ProfileData profile, out string error);
}

public interface IRewardGrantHandler
{
    Type GrantType { get; }

    RewardGrantPreparation Prepare(RewardGrantDefinition grant, RewardGrantContext context);
}

public interface IRewardGrantHandler<in TGrant> : IRewardGrantHandler
    where TGrant : RewardGrantDefinition
{
    RewardGrantPreparation Prepare(TGrant grant, RewardGrantContext context);
}

public abstract class RewardGrantHandler<TGrant> : IRewardGrantHandler<TGrant>
    where TGrant : RewardGrantDefinition
{
    public Type GrantType => typeof(TGrant);

    public abstract RewardGrantPreparation Prepare(TGrant grant, RewardGrantContext context);

    RewardGrantPreparation IRewardGrantHandler.Prepare(
        RewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (grant is not TGrant typedGrant)
        {
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = [$"Handler for {typeof(TGrant).Name} received {grant.GetType().Name}."],
            };
        }

        return Prepare(typedGrant, context);
    }
}

public interface IRewardGrantTransaction
{
    bool IsAvailable { get; }
    bool TryStage(IRewardGrantMutation mutation, out string error);
    bool TryStageReceipt(RewardClaimReceipt receipt, out string error);
    RewardTransactionCommitResult Commit();
}

public interface IRewardGrantTransactionFactory
{
    IRewardGrantTransaction BeginRewardTransaction();
}

public readonly record struct RewardTransactionCommitResult(bool Committed, string Error)
{
    public static RewardTransactionCommitResult Unavailable(string error) => new(false, error);
}

public sealed record RewardClaimRequest
{
    public required RewardClaimId ClaimId { get; init; }
    public ImmutableArray<RewardOptionId> SelectedOptionIds { get; init; } = [];
}

public sealed record RewardClaimResult
{
    public required RewardRuntimeStatus Status { get; init; }
    public RewardClaimReceipt? Receipt { get; init; }
    public ImmutableArray<string> Errors { get; init; } = [];
}

public sealed class RewardClaimService
{
    private readonly IRewardProfileStore _profileStore;
    private readonly RewardGrantHandlerRegistry _handlers;

    public RewardClaimService(IRewardProfileStore profileStore, RewardGrantHandlerRegistry handlers)
    {
        _profileStore = profileStore;
        _handlers = handlers;
    }

    public RewardClaimResult Claim(RewardClaimRequest request)
    {
        var state = _profileStore.GetRewardState();
        if (state.ClaimReceipts.TryGetValue(request.ClaimId.Value, out var existingReceipt))
        {
            return new RewardClaimResult
            {
                Status = RewardRuntimeStatus.AlreadyClaimed,
                Receipt = existingReceipt,
            };
        }

        if (!state.ResolvedOffers.TryGetValue(request.ClaimId.Value, out var snapshot))
            return Invalid($"Resolved reward claim '{request.ClaimId}' was not found.");

        var selectedIds =
            snapshot.SelectionMode == RewardSelectionMode.Automatic
                ? snapshot.Options.Select(option => option.Id).ToImmutableArray()
                : request.SelectedOptionIds;

        if (
            snapshot.SelectionMode == RewardSelectionMode.PlayerChoice
            && selectedIds.Length != snapshot.ChooseCount
        )
        {
            return Invalid(
                $"Reward claim requires exactly {snapshot.ChooseCount} selected options."
            );
        }

        if (selectedIds.Distinct().Count() != selectedIds.Length)
            return Invalid("Reward claim contains duplicate option IDs.");

        var selected = new List<RewardOptionDefinition>();
        foreach (var selectedId in selectedIds)
        {
            var option = snapshot.Options.FirstOrDefault(candidate => candidate.Id == selectedId);
            if (option == null)
                return Invalid($"Reward option '{selectedId}' is not part of this claim.");
            selected.Add(option);
        }

        var grantContext = new RewardGrantContext
        {
            ClaimId = request.ClaimId,
            Source = snapshot.Source,
        };
        var preparations = new List<RewardGrantPreparation>();
        var appliedGrants = selected.SelectMany(option => option.Grants).ToImmutableArray();
        foreach (var grant in appliedGrants)
        {
            var preparation = _handlers.Prepare(grant, grantContext);
            if (!preparation.IsValid || preparation.Mutation == null)
                return Invalid(preparation.Errors);
            preparations.Add(preparation);
        }

        var receipt = new RewardClaimReceipt
        {
            ClaimId = request.ClaimId,
            ClaimedOptionIds = selectedIds,
            AppliedGrants = appliedGrants,
        };
        var transaction = _profileStore.BeginRewardTransaction();
        foreach (var preparation in preparations)
        {
            if (!transaction.TryStage(preparation.Mutation!, out var stageError))
                return Invalid(stageError);
        }
        if (!transaction.TryStageReceipt(receipt, out var receiptError))
            return Invalid(receiptError);

        var commit = transaction.Commit();
        if (!commit.Committed)
        {
            var refreshed = _profileStore.GetRewardState();
            if (refreshed.ClaimReceipts.TryGetValue(request.ClaimId.Value, out existingReceipt))
            {
                return new RewardClaimResult
                {
                    Status = RewardRuntimeStatus.AlreadyClaimed,
                    Receipt = existingReceipt,
                };
            }
            return Invalid(commit.Error);
        }

        return new RewardClaimResult { Status = RewardRuntimeStatus.Ready, Receipt = receipt };
    }

    private static RewardClaimResult Invalid(string error) =>
        new() { Status = RewardRuntimeStatus.Invalid, Errors = [error] };

    private static RewardClaimResult Invalid(IEnumerable<string> errors) =>
        new() { Status = RewardRuntimeStatus.Invalid, Errors = [.. errors] };
}
