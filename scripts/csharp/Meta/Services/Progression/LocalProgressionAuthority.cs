using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Progression;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Domain.Progression;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Rewards;

namespace Fateforged.Meta.Progression;

/// <summary>
/// Durable local implementation of the provider-neutral progression port.
/// Local saves are intentionally not trusted as a security boundary; moving to a
/// server authority only replaces this adapter, not battle or reward callers.
/// </summary>
public sealed class LocalProgressionAuthority : IProgressionAuthority
{
    private const string BattleXpSource = "authored_battle_xp";
    private const string FirstClearSource = "authored_battle_first_clear";
    private readonly object _gate = new();
    private readonly IProgressionProfileStore _profileStore;
    private readonly RewardViewModelFactory _viewModels = new();

    public UniversalRewardRuntime RewardRuntime { get; }

    public LocalProgressionAuthority(
        IProgressionProfileStore profileStore,
        UniversalRewardRuntime rewardRuntime
    )
    {
        _profileStore = profileStore;
        RewardRuntime = rewardRuntime;
    }

    public ProgressionAuthorityResult StartBattleAttempt(StartBattleAttemptRequest request)
    {
        lock (_gate)
        {
            if (RewardRuntime.Status != RewardRuntimeStatus.Ready)
                return ProgressionAuthorityResult.Unavailable("Reward runtime is unavailable.");
            if (!request.SummonerId.HasValue || !request.BattleId.HasValue)
                return ProgressionAuthorityResult.Invalid("Summoner and battle IDs are required.");

            var battle = EventCatalog.GetEvent<BattleEventDefinition>(
                new EventId(request.BattleId.Value)
            );
            if (battle == null)
                return ProgressionAuthorityResult.Invalid(
                    $"Battle '{request.BattleId}' was not found."
                );

            var snapshot = _profileStore.GetProgressionSnapshot();
            if (!snapshot.UnlockedSummoners.Contains(request.SummonerId))
                return ProgressionAuthorityResult.Invalid(
                    $"Summoner '{request.SummonerId}' is not unlocked."
                );
            if (snapshot.SummonerInstances.All(value => value.SummonerId != request.SummonerId))
                return ProgressionAuthorityResult.Invalid(
                    $"Summoner '{request.SummonerId}' was not found."
                );

            var deck = request.DeckId.HasValue
                ? snapshot.Decks.FirstOrDefault(value => value.Id == request.DeckId)
                : null;
            if (battle.RequiresDeck && deck == null)
                return ProgressionAuthorityResult.Invalid("The selected deck was not found.");
            if (deck != null && deck.SummonerId != request.SummonerId)
                return ProgressionAuthorityResult.Invalid(
                    "The selected deck does not belong to the battle summoner."
                );

            var distinctCards = battle.RequiresDeck
                ? deck!.CardInstanceIds.Distinct().ToList()
                : [];
            if (distinctCards.Any(id => snapshot.Collection.All(card => card.Id != id)))
                return ProgressionAuthorityResult.Invalid(
                    "The battle deck contains an unowned card instance."
                );

            var seed = snapshot.Rewards.RewardSeedBySummoner.TryGetValue(
                request.SummonerId.Value,
                out var existingSeed
            )
                ? existingSeed
                : NewNonZeroSeed();
            var firstClearSnapshots = new List<ResolvedRewardOfferSnapshot>();
            var alreadyCompleted =
                snapshot.SummonerProgressMap.TryGetValue(
                    request.SummonerId.Value,
                    out var progressSnapshot
                ) && progressSnapshot.CompletedBattles.Contains(request.BattleId);
            if (!alreadyCompleted)
            {
                var source = FirstClearContext(request.BattleId);
                foreach (var offer in battle.FirstClearRewardOffers)
                {
                    var resolved = Resolve(snapshot, offer, request.SummonerId, source, seed);
                    if (resolved.Status != RewardRuntimeStatus.Ready || resolved.Snapshot == null)
                        return ProgressionAuthorityResult.Invalid(
                            string.Join(" ", resolved.Errors)
                        );
                    firstClearSnapshots.Add(resolved.Snapshot);
                }
            }

            var attempt = new BattleAttempt
            {
                AttemptId = CreateAttemptId(),
                SummonerId = request.SummonerId,
                BattleId = request.BattleId,
                DeckId = battle.RequiresDeck ? request.DeckId : Fateforged.Meta.Deck.DeckId.None,
                DeckCardInstanceIds = distinctCards,
                CardXpReward = battle.CardXpReward,
                SummonerXpReward = battle.SummonerXpReward,
                FirstClearRewardSnapshots = firstClearSnapshots,
                StartedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            var mutation = new ProgressionMutation(profile =>
            {
                if (!profile.UnlockedSummoners.Contains(request.SummonerId))
                    return Failure("Summoner became unavailable before battle start.");
                if (
                    profile.Rewards.RewardSeedBySummoner.TryGetValue(
                        request.SummonerId.Value,
                        out var committedSeed
                    )
                    && committedSeed != seed
                )
                    return Failure("The summoner reward seed changed before battle start.");
                if (battle.RequiresDeck)
                {
                    var committedDeck = profile.Decks.FirstOrDefault(value =>
                        value.Id == request.DeckId && value.SummonerId == request.SummonerId
                    );
                    if (committedDeck == null)
                        return Failure("The selected deck changed before battle start.");
                    var committedCards = committedDeck.CardInstanceIds.Distinct().ToHashSet();
                    if (
                        committedCards.Count != distinctCards.Count
                        || !committedCards.SetEquals(distinctCards)
                    )
                        return Failure("The selected deck changed before battle start.");
                }
                if (distinctCards.Any(id => profile.Collection.All(card => card.Id != id)))
                    return Failure("The battle deck changed before battle start.");

                var progress = GetOrCreateProgress(profile, request.SummonerId);
                if (progress.ActiveBattleAttempt is { } stale)
                {
                    progress.BattleAttemptCompletions.TryAdd(
                        stale.AttemptId.Value,
                        new BattleAttemptCompletion
                        {
                            AttemptId = stale.AttemptId,
                            Outcome = BattleTerminalOutcome.Abandoned,
                            CompletedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        }
                    );
                }

                progress.ActiveBattleAttempt = attempt;
                profile.Rewards.RewardSeedBySummoner.TryAdd(request.SummonerId.Value, seed);
                return Success();
            });
            if (!_profileStore.TryCommitProgression([mutation], out var error))
                return ProgressionAuthorityResult.Unavailable(error);

            return new ProgressionAuthorityResult
            {
                Status = ProgressionAuthorityStatus.Ready,
                Attempt = attempt,
            };
        }
    }

    public ProgressionAuthorityResult CompleteBattleAttempt(CompleteBattleAttemptRequest request)
    {
        lock (_gate)
        {
            if (RewardRuntime.Status != RewardRuntimeStatus.Ready)
                return ProgressionAuthorityResult.Unavailable("Reward runtime is unavailable.");
            if (!request.AttemptId.HasValue)
                return ProgressionAuthorityResult.Invalid("Battle attempt ID is required.");

            var profile = _profileStore.GetProgressionSnapshot();
            if (TryFindCompletion(profile, request.AttemptId, out var existing, out _))
            {
                if (existing!.Outcome != request.Outcome)
                    return ProgressionAuthorityResult.Invalid(
                        "The attempt already has a different terminal outcome."
                    );
                return BuildResult(profile, existing, ProgressionAuthorityStatus.AlreadyCompleted);
            }

            if (
                !TryFindActiveAttempt(profile, request.AttemptId, out var attempt, out var progress)
            )
                return ProgressionAuthorityResult.Invalid(
                    "The active battle attempt was not found."
                );

            var completion = new BattleAttemptCompletion
            {
                AttemptId = request.AttemptId,
                Outcome = request.Outcome,
                CompletedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            var mutations = new List<IRewardGrantMutation>();
            mutations.Add(
                new ProgressionMutation(candidate =>
                {
                    var candidateProgress = GetOrCreateProgress(candidate, attempt!.SummonerId);
                    if (
                        candidateProgress.BattleAttemptCompletions.ContainsKey(
                            request.AttemptId.Value
                        )
                    )
                        return Failure("The attempt was completed by another request.");
                    return candidateProgress.ActiveBattleAttempt?.AttemptId == request.AttemptId
                        ? Success()
                        : Failure("The active battle attempt changed before completion.");
                })
            );
            var snapshots = new List<ResolvedRewardOfferSnapshot>();
            var pendings = new List<PendingRewardSelection>();
            var receipts = new List<RewardClaimReceipt>();

            if (request.Outcome == BattleTerminalOutcome.Victory)
            {
                var xp = BuildXpOffer(attempt!);
                if (
                    xp != null
                    && !TryPrepareAutomaticOffer(
                        profile,
                        xp,
                        attempt!,
                        mutations,
                        snapshots,
                        receipts,
                        out var xpError
                    )
                )
                    return ProgressionAuthorityResult.Invalid(xpError);

                if (!progress!.CompletedBattles.Contains(attempt!.BattleId))
                {
                    foreach (var rewardSnapshot in attempt.FirstClearRewardSnapshots)
                    {
                        if (rewardSnapshot.SelectionMode == RewardSelectionMode.Automatic)
                        {
                            if (
                                !TryPrepareAutomaticSnapshot(
                                    rewardSnapshot,
                                    mutations,
                                    snapshots,
                                    receipts,
                                    out var rewardError
                                )
                            )
                                return ProgressionAuthorityResult.Invalid(rewardError);
                        }
                        else
                        {
                            snapshots.Add(rewardSnapshot);
                            pendings.Add(
                                new PendingRewardSelection
                                {
                                    ClaimId = rewardSnapshot.ClaimId,
                                    ChooseCount = rewardSnapshot.ChooseCount,
                                }
                            );
                        }
                    }
                }
            }

            completion.ClaimIds.AddRange(snapshots.Select(value => value.ClaimId));
            completion.PendingClaimIds.AddRange(pendings.Select(value => value.ClaimId));
            mutations.Add(
                new ProgressionMutation(candidate =>
                {
                    var candidateProgress = GetOrCreateProgress(candidate, attempt!.SummonerId);
                    if (
                        candidateProgress.BattleAttemptCompletions.TryGetValue(
                            request.AttemptId.Value,
                            out var prior
                        )
                    )
                        return prior.Outcome == request.Outcome
                            ? Success()
                            : Failure("The attempt already has a different terminal outcome.");
                    if (candidateProgress.ActiveBattleAttempt?.AttemptId != request.AttemptId)
                        return Failure("The active battle attempt changed before completion.");

                    foreach (var resolved in snapshots)
                        candidate.Rewards.ResolvedOffers[resolved.ClaimId.Value] = resolved;
                    foreach (var pending in pendings)
                        candidate.Rewards.PendingSelections[pending.ClaimId.Value] = pending;
                    foreach (var receipt in receipts)
                        candidate.Rewards.ClaimReceipts[receipt.ClaimId.Value] = receipt;

                    candidateProgress.ActiveBattleAttempt = null;
                    candidateProgress.BattleAttemptCompletions[request.AttemptId.Value] =
                        completion;
                    if (
                        request.Outcome == BattleTerminalOutcome.Victory
                        && !candidateProgress.CompletedBattles.Contains(attempt.BattleId)
                    )
                        candidateProgress.CompletedBattles.Add(attempt.BattleId);
                    return Success();
                })
            );

            if (!_profileStore.TryCommitProgression(mutations, out var commitError))
            {
                var refreshed = _profileStore.GetProgressionSnapshot();
                if (
                    TryFindCompletion(refreshed, request.AttemptId, out existing, out _)
                    && existing!.Outcome == request.Outcome
                )
                    return BuildResult(
                        refreshed,
                        existing,
                        ProgressionAuthorityStatus.AlreadyCompleted
                    );
                return ProgressionAuthorityResult.Unavailable(commitError);
            }

            return BuildResult(
                _profileStore.GetProgressionSnapshot(),
                completion,
                ProgressionAuthorityStatus.Ready
            );
        }
    }

    public ProgressionAuthorityResult GetBattleRewards(BattleAttemptId attemptId)
    {
        lock (_gate)
        {
            if (RewardRuntime.Status != RewardRuntimeStatus.Ready)
                return ProgressionAuthorityResult.Unavailable("Reward runtime is unavailable.");
            var profile = _profileStore.GetProgressionSnapshot();
            if (!TryFindCompletion(profile, attemptId, out var completion, out _))
                return ProgressionAuthorityResult.Invalid("Battle completion was not found.");
            return BuildResult(profile, completion!, ProgressionAuthorityStatus.Ready);
        }
    }

    public ProgressionAuthorityResult GetPendingBattleRewards(
        Fateforged.Data.Summoners.SummonerId summonerId
    )
    {
        lock (_gate)
        {
            if (RewardRuntime.Status != RewardRuntimeStatus.Ready)
                return ProgressionAuthorityResult.Unavailable("Reward runtime is unavailable.");
            var profile = _profileStore.GetProgressionSnapshot();
            if (!profile.SummonerProgressMap.TryGetValue(summonerId.Value, out var progress))
                return ProgressionAuthorityResult.Invalid("Summoner progress was not found.");
            var completion = progress
                .BattleAttemptCompletions.Values.Where(value => value.PendingClaimIds.Count > 0)
                .OrderByDescending(value => value.CompletedAtUnixSeconds)
                .FirstOrDefault();
            return completion == null
                ? ProgressionAuthorityResult.Invalid("No pending battle reward was found.")
                : BuildResult(profile, completion, ProgressionAuthorityStatus.Ready);
        }
    }

    public ProgressionAuthorityResult ClaimBattleReward(BattleRewardClaimRequest request)
    {
        lock (_gate)
        {
            if (RewardRuntime.Status != RewardRuntimeStatus.Ready)
                return ProgressionAuthorityResult.Unavailable("Reward runtime is unavailable.");
            var profile = _profileStore.GetProgressionSnapshot();
            if (
                !TryFindCompletion(profile, request.AttemptId, out var completion, out _)
                || completion!.Outcome != BattleTerminalOutcome.Victory
            )
                return ProgressionAuthorityResult.Invalid(
                    "A victorious battle completion was not found."
                );
            if (!completion.PendingClaimIds.Contains(request.ClaimId))
            {
                if (
                    profile.Rewards.ClaimReceipts.TryGetValue(request.ClaimId.Value, out var prior)
                    && completion.ClaimIds.Contains(request.ClaimId)
                )
                    return new ProgressionAuthorityResult
                    {
                        Status = ProgressionAuthorityStatus.AlreadyCompleted,
                        Completion = completion,
                        ClaimReceipt = prior,
                        RewardOffers = BuildOffers(profile, completion),
                    };
                return ProgressionAuthorityResult.Invalid(
                    "The reward claim does not belong to this attempt."
                );
            }
            if (
                !profile.Rewards.ResolvedOffers.TryGetValue(request.ClaimId.Value, out var snapshot)
            )
                return ProgressionAuthorityResult.Invalid(
                    "The resolved reward offer was not found."
                );

            if (
                !TrySelect(
                    snapshot,
                    request.SelectedOptionIds,
                    out var selected,
                    out var selectionError
                )
            )
                return ProgressionAuthorityResult.Invalid(selectionError);
            var grants = selected.SelectMany(value => value.Grants).ToImmutableArray();
            var mutations = new List<IRewardGrantMutation>();
            mutations.Add(
                new ProgressionMutation(candidate =>
                {
                    if (candidate.Rewards.ClaimReceipts.ContainsKey(request.ClaimId.Value))
                        return Failure("The reward was claimed by another request.");
                    return
                        TryFindCompletion(candidate, request.AttemptId, out var current, out _)
                        && current!.PendingClaimIds.Contains(request.ClaimId)
                        ? Success()
                        : Failure("The pending reward changed before it was claimed.");
                })
            );
            var context = new RewardGrantContext
            {
                ClaimId = request.ClaimId,
                Source = snapshot.Source,
            };
            foreach (var grant in grants)
            {
                var prepared = RewardRuntime.Handlers.Prepare(grant, context);
                if (!prepared.IsValid || prepared.Mutation == null)
                    return ProgressionAuthorityResult.Invalid(string.Join(" ", prepared.Errors));
                mutations.Add(prepared.Mutation);
            }

            var selectedIds = selected.Select(value => value.Id).ToImmutableArray();
            var receipt = new RewardClaimReceipt
            {
                ClaimId = request.ClaimId,
                ClaimedOptionIds = selectedIds,
                AppliedGrants = grants,
            };
            mutations.Add(
                new ProgressionMutation(candidate =>
                {
                    if (candidate.Rewards.ClaimReceipts.ContainsKey(request.ClaimId.Value))
                        return Success();
                    if (
                        !TryFindCompletion(
                            candidate,
                            request.AttemptId,
                            out var candidateCompletion,
                            out _
                        ) || !candidateCompletion!.PendingClaimIds.Remove(request.ClaimId)
                    )
                        return Failure("The pending reward changed before it was claimed.");
                    candidate.Rewards.PendingSelections.Remove(request.ClaimId.Value);
                    candidate.Rewards.ClaimReceipts[request.ClaimId.Value] = receipt;
                    return Success();
                })
            );
            if (!_profileStore.TryCommitProgression(mutations, out var error))
            {
                var refreshed = _profileStore.GetProgressionSnapshot();
                if (
                    refreshed.Rewards.ClaimReceipts.TryGetValue(
                        request.ClaimId.Value,
                        out var existingReceipt
                    )
                )
                    return new ProgressionAuthorityResult
                    {
                        Status = ProgressionAuthorityStatus.AlreadyCompleted,
                        Completion = completion,
                        ClaimReceipt = existingReceipt,
                        RewardOffers = BuildOffers(refreshed, completion),
                    };
                return ProgressionAuthorityResult.Unavailable(error);
            }

            return new ProgressionAuthorityResult
            {
                Status = ProgressionAuthorityStatus.Ready,
                Completion = completion with
                {
                    PendingClaimIds = completion
                        .PendingClaimIds.Where(id => id != request.ClaimId)
                        .ToList(),
                },
                ClaimReceipt = receipt,
                RewardOffers = BuildOffers(_profileStore.GetProgressionSnapshot(), completion),
            };
        }
    }

    private bool TryPrepareAutomaticOffer(
        ProfileData profile,
        RewardOfferDefinition offer,
        BattleAttempt attempt,
        List<IRewardGrantMutation> mutations,
        List<ResolvedRewardOfferSnapshot> snapshots,
        List<RewardClaimReceipt> receipts,
        out string error
    )
    {
        var source = new RewardSourceContext
        {
            SourceType = BattleXpSource,
            SourceId = attempt.BattleId.Value,
            OccurrenceId = attempt.AttemptId.Value,
        };
        return TryResolveAutomatic(
            profile,
            offer,
            attempt,
            source,
            mutations,
            snapshots,
            receipts,
            out error
        );
    }

    private bool TryPrepareAutomaticSnapshot(
        ResolvedRewardOfferSnapshot snapshot,
        List<IRewardGrantMutation> mutations,
        List<ResolvedRewardOfferSnapshot> snapshots,
        List<RewardClaimReceipt> receipts,
        out string error
    )
    {
        var selected = snapshot.Options;
        var grants = selected.SelectMany(value => value.Grants).ToImmutableArray();
        var context = new RewardGrantContext
        {
            ClaimId = snapshot.ClaimId,
            Source = snapshot.Source,
        };
        foreach (var grant in grants)
        {
            var prepared = RewardRuntime.Handlers.Prepare(grant, context);
            if (!prepared.IsValid || prepared.Mutation == null)
            {
                error = string.Join(" ", prepared.Errors);
                return false;
            }
            mutations.Add(prepared.Mutation);
        }
        snapshots.Add(snapshot);
        receipts.Add(
            new RewardClaimReceipt
            {
                ClaimId = snapshot.ClaimId,
                ClaimedOptionIds = selected.Select(value => value.Id).ToImmutableArray(),
                AppliedGrants = grants,
            }
        );
        error = "";
        return true;
    }

    private bool TryResolveAutomatic(
        ProfileData profile,
        RewardOfferDefinition offer,
        BattleAttempt attempt,
        RewardSourceContext source,
        List<IRewardGrantMutation> mutations,
        List<ResolvedRewardOfferSnapshot> snapshots,
        List<RewardClaimReceipt> receipts,
        out string error
    )
    {
        var resolved = Resolve(profile, offer, attempt.SummonerId, source);
        if (resolved.Status != RewardRuntimeStatus.Ready || resolved.Snapshot == null)
        {
            error = string.Join(" ", resolved.Errors);
            return false;
        }
        var selected = resolved.Snapshot.Options;
        var grants = selected.SelectMany(value => value.Grants).ToImmutableArray();
        var context = new RewardGrantContext
        {
            ClaimId = resolved.Snapshot.ClaimId,
            Source = source,
        };
        foreach (var grant in grants)
        {
            var prepared = RewardRuntime.Handlers.Prepare(grant, context);
            if (!prepared.IsValid || prepared.Mutation == null)
            {
                error = string.Join(" ", prepared.Errors);
                return false;
            }
            mutations.Add(prepared.Mutation);
        }
        snapshots.Add(resolved.Snapshot);
        receipts.Add(
            new RewardClaimReceipt
            {
                ClaimId = resolved.Snapshot.ClaimId,
                ClaimedOptionIds = selected.Select(value => value.Id).ToImmutableArray(),
                AppliedGrants = grants,
            }
        );
        error = "";
        return true;
    }

    private RewardResolutionResult Resolve(
        ProfileData profile,
        RewardOfferDefinition offer,
        Fateforged.Data.Summoners.SummonerId summonerId,
        RewardSourceContext source,
        ulong? explicitSeed = null
    ) =>
        RewardRuntime.Resolver.Resolve(
            offer,
            new RewardResolutionContext
            {
                SummonerId = summonerId,
                SummonerSeed =
                    explicitSeed
                    ?? (
                        profile.Rewards.RewardSeedBySummoner.TryGetValue(
                            summonerId.Value,
                            out var seed
                        )
                            ? seed
                            : 1
                    ),
                Source = source,
                Catalog = RewardRuntime.Catalog,
                OwnedRewardKeys = RewardRuntime.ProfileStore.GetOwnedRewardKeys(summonerId),
            }
        );

    private static RewardSourceContext FirstClearContext(BattleId battleId) =>
        new() { SourceType = FirstClearSource, SourceId = battleId.Value };

    private static RewardOfferDefinition? BuildXpOffer(BattleAttempt attempt)
    {
        var grants = ImmutableArray.CreateBuilder<RewardGrantDefinition>();
        if (attempt.SummonerXpReward > 0)
            grants.Add(
                new SummonerExperienceRewardGrantDefinition
                {
                    Target = new RewardOwnershipTarget(
                        RewardOwnershipScope.Summoner,
                        attempt.SummonerId.Value
                    ),
                    Amount = attempt.SummonerXpReward,
                }
            );
        if (attempt.CardXpReward > 0)
            foreach (var cardId in attempt.DeckCardInstanceIds.Distinct())
                grants.Add(
                    new CardExperienceRewardGrantDefinition
                    {
                        Target = new RewardOwnershipTarget(
                            RewardOwnershipScope.CardInstance,
                            cardId.Value
                        ),
                        Amount = attempt.CardXpReward,
                    }
                );
        if (grants.Count == 0)
            return null;
        return AutomaticOffer(
            new RewardOfferId($"battle:{attempt.BattleId.Value}:xp"),
            new RewardOptionId("xp"),
            grants.ToImmutable()
        );
    }

    private static RewardOfferDefinition AutomaticOffer(
        RewardOfferId offerId,
        RewardOptionId optionId,
        ImmutableArray<RewardGrantDefinition> grants
    ) =>
        new()
        {
            Id = offerId,
            Selection = new RewardSelectionRule { Mode = RewardSelectionMode.Automatic },
            OptionSource = new AuthoredRewardOptionSourceDefinition
            {
                Options = [new RewardOptionDefinition { Id = optionId, Grants = grants }],
            },
        };

    private ProgressionAuthorityResult BuildResult(
        ProfileData profile,
        BattleAttemptCompletion completion,
        ProgressionAuthorityStatus status
    ) =>
        new()
        {
            Status = status,
            Completion = completion,
            ProgressionGrants = BuildProgressionGrants(profile, completion),
            RewardOffers = BuildOffers(profile, completion),
        };

    private static ImmutableArray<RewardGrantViewModel> BuildProgressionGrants(
        ProfileData profile,
        BattleAttemptCompletion completion
    ) =>
        completion
            .ClaimIds.Where(id =>
                profile.Rewards.ResolvedOffers.TryGetValue(id.Value, out var value)
                && value.Source.SourceType == BattleXpSource
                && profile.Rewards.ClaimReceipts.ContainsKey(id.Value)
            )
            .SelectMany(id => profile.Rewards.ClaimReceipts[id.Value].AppliedGrants)
            .Select(RewardViewModelFactory.CreateGrant)
            .ToImmutableArray();

    private ImmutableArray<RewardOfferViewModel> BuildOffers(
        ProfileData profile,
        BattleAttemptCompletion completion
    ) =>
        completion
            .ClaimIds.Where(id =>
                profile.Rewards.ResolvedOffers.TryGetValue(id.Value, out var value)
                && value.Source.SourceType == FirstClearSource
            )
            .Select(id =>
            {
                var snapshot = profile.Rewards.ResolvedOffers[id.Value];
                profile.Rewards.PendingSelections.TryGetValue(id.Value, out var pending);
                profile.Rewards.ClaimReceipts.TryGetValue(id.Value, out var receipt);
                return _viewModels.Create(snapshot, pending, receipt);
            })
            .ToImmutableArray();

    private static bool TrySelect(
        ResolvedRewardOfferSnapshot snapshot,
        ImmutableArray<RewardOptionId> requested,
        out ImmutableArray<RewardOptionDefinition> selected,
        out string error
    )
    {
        var ids =
            snapshot.SelectionMode == RewardSelectionMode.Automatic
                ? snapshot.Options.Select(value => value.Id).ToImmutableArray()
                : requested;
        if (ids.Length != snapshot.ChooseCount || ids.Distinct().Count() != ids.Length)
        {
            selected = [];
            error = $"Reward claim requires exactly {snapshot.ChooseCount} distinct options.";
            return false;
        }
        var values = ids.Select(id => snapshot.Options.FirstOrDefault(value => value.Id == id))
            .ToArray();
        if (values.Any(value => value == null))
        {
            selected = [];
            error = "A selected reward option is not part of this claim.";
            return false;
        }
        selected = values.Cast<RewardOptionDefinition>().ToImmutableArray();
        error = "";
        return true;
    }

    private static bool TryFindActiveAttempt(
        ProfileData profile,
        BattleAttemptId attemptId,
        out BattleAttempt? attempt,
        out SummonerProgress? progress
    )
    {
        foreach (var value in profile.SummonerProgressMap.Values)
            if (value.ActiveBattleAttempt?.AttemptId == attemptId)
            {
                attempt = value.ActiveBattleAttempt;
                progress = value;
                return true;
            }
        attempt = null;
        progress = null;
        return false;
    }

    private static bool TryFindCompletion(
        ProfileData profile,
        BattleAttemptId attemptId,
        out BattleAttemptCompletion? completion,
        out SummonerProgress? progress
    )
    {
        foreach (var value in profile.SummonerProgressMap.Values)
            if (value.BattleAttemptCompletions.TryGetValue(attemptId.Value, out completion))
            {
                progress = value;
                return true;
            }
        completion = null;
        progress = null;
        return false;
    }

    private static SummonerProgress GetOrCreateProgress(
        ProfileData profile,
        Fateforged.Data.Summoners.SummonerId summonerId
    )
    {
        if (!profile.SummonerProgressMap.TryGetValue(summonerId.Value, out var progress))
            profile.SummonerProgressMap[summonerId.Value] = progress = new SummonerProgress();
        return progress;
    }

    private static BattleAttemptId CreateAttemptId() =>
        new(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant());

    private static ulong NewNonZeroSeed()
    {
        var seed = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(sizeof(ulong)));
        return seed == 0 ? 1 : seed;
    }

    private static (bool success, string error) Success() => (true, "");

    private static (bool success, string error) Failure(string error) => (false, error);

    private sealed class ProgressionMutation : IRewardGrantMutation
    {
        private readonly Func<ProfileData, (bool success, string error)> _apply;

        public ProgressionMutation(Func<ProfileData, (bool success, string error)> apply) =>
            _apply = apply;

        public bool TryApply(ProfileData profile, out string error)
        {
            var result = _apply(profile);
            error = result.error;
            return result.success;
        }
    }
}
