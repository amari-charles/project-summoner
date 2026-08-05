using System.Collections.Immutable;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Domain.Progression;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using Fateforged.Meta.Rewards;
using Godot;
using GdDict = Godot.Collections.Dictionary;

namespace Fateforged.Meta.Progression;

/// <summary>
/// Godot composition root and GDScript adapter for the provider-neutral authority.
/// </summary>
[GlobalClass]
public partial class ProgressionAuthorityService : Node
{
    public static ProgressionAuthorityService? Instance { get; private set; }

    public IProgressionAuthority Authority { get; private set; } = null!;
    public BattleOutcomeCoordinator OutcomeCoordinator { get; private set; } = null!;

    public override void _Ready()
    {
        Instance = this;
        var repository = ProfileRepository.Instance;
        if (repository == null)
        {
            SetAuthority(new UnavailableProgressionAuthority("Profile repository unavailable."));
            return;
        }

        var rewards =
            RewardService.Instance?.UniversalRuntime ?? UniversalRewardRuntime.CreateUnavailable();
        SetAuthority(new LocalProgressionAuthority(repository, rewards));
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitForTesting(IProgressionAuthority authority) => SetAuthority(authority);

    public ProgressionAuthorityResult BeginCampaignBattleAttempt(
        CampaignId campaignId,
        BattleId battleId
    )
    {
        var profile = ProfileRepository.Instance?.GetProfileMetadata();
        if (profile == null)
            return ProgressionAuthorityResult.Unavailable("Profile metadata unavailable.");

        var summonerId = new SummonerId(profile.Meta.SelectedSummoner);
        var deck = string.IsNullOrWhiteSpace(profile.Meta.SelectedDeck)
            ? null
            : ProfileRepository.Instance?.GetDeck(
                new Fateforged.Meta.Deck.DeckId(profile.Meta.SelectedDeck)
            );

        return Authority.StartBattleAttempt(
            new StartBattleAttemptRequest
            {
                SummonerId = summonerId,
                CampaignId = campaignId,
                BattleId = battleId,
                DeckCardInstanceIds = deck?.CardInstanceIds.ToImmutableArray() ?? [],
            }
        );
    }

    public ProgressionAuthorityResult ReportBattleOutcome(
        BattleAttemptId attemptId,
        BattleTerminalOutcome outcome
    ) => OutcomeCoordinator.Report(attemptId, outcome);

    /// <summary>GDScript boundary used by campaign launch.</summary>
    public GdDict StartCampaignBattleAttempt(string campaignId, string battleId) =>
        ToDictionary(
            BeginCampaignBattleAttempt(
                CampaignId.FromString(campaignId),
                BattleId.FromString(battleId)
            )
        );

    public GdDict GetProgressionAuthorityStatus() =>
        new()
        {
            ["status"] = Authority is UnavailableProgressionAuthority ? "unavailable" : "ready",
            ["can_start_battle"] = Authority is not UnavailableProgressionAuthority,
            ["can_complete_battle"] = Authority is not UnavailableProgressionAuthority,
        };

    public GdDict GetBattleRewards(string attemptId) =>
        ToDictionary(Authority.GetBattleRewards(BattleAttemptId.FromString(attemptId)));

    public GdDict GetPendingBattleRewards()
    {
        var profile = ProfileRepository.Instance?.GetProfileMetadata();
        var summonerId = new SummonerId(profile?.Meta.SelectedSummoner ?? "");
        return ToDictionary(Authority.GetPendingBattleRewards(summonerId));
    }

    public GdDict ClaimBattleReward(
        string attemptId,
        string claimId,
        Godot.Collections.Array<string> selectedOptionIds
    ) =>
        ToDictionary(
            Authority.ClaimBattleReward(
                new BattleRewardClaimRequest
                {
                    AttemptId = BattleAttemptId.FromString(attemptId),
                    ClaimId = new RewardClaimId(claimId),
                    SelectedOptionIds = selectedOptionIds
                        .Select(value => new RewardOptionId(value))
                        .ToImmutableArray(),
                }
            )
        );

    private void SetAuthority(IProgressionAuthority authority)
    {
        Authority = authority;
        OutcomeCoordinator = new BattleOutcomeCoordinator(authority);
    }

    private static GdDict ToDictionary(ProgressionAuthorityResult result)
    {
        var attemptId = result.Attempt?.AttemptId.Value ?? result.Completion?.AttemptId.Value ?? "";
        var offers = new Godot.Collections.Array<GdDict>();
        foreach (var offer in result.RewardOffers)
        {
            var options = new Godot.Collections.Array<GdDict>();
            foreach (var option in offer.Options)
            {
                var grants = new Godot.Collections.Array<GdDict>();
                foreach (var grant in option.Grants)
                    grants.Add(
                        new GdDict
                        {
                            ["kind"] = grant.Kind,
                            ["ownership_scope"] = grant
                                .OwnershipScope.ToString()
                                .ToLowerInvariant(),
                            ["target_id"] = grant.TargetId,
                            ["content_id"] = grant.ContentId,
                            ["rarity"] = grant.Rarity,
                            ["amount"] = grant.Amount,
                        }
                    );
                options.Add(
                    new GdDict
                    {
                        ["id"] = option.Id.Value,
                        ["label_key"] = option.LabelKey,
                        ["description_key"] = option.DescriptionKey,
                        ["is_selected"] = option.IsSelected,
                        ["grants"] = grants,
                    }
                );
            }
            offers.Add(
                new GdDict
                {
                    ["id"] = offer.Id.Value,
                    ["claim_id"] = offer.ClaimId?.Value ?? "",
                    ["display_state"] = offer.DisplayState.ToString().ToLowerInvariant(),
                    ["selection_mode"] = offer.SelectionMode.ToString().ToLowerInvariant(),
                    ["choose_count"] = offer.ChooseCount,
                    ["options"] = options,
                }
            );
        }
        return new GdDict
        {
            ["is_success"] = result.IsSuccess,
            ["status"] = result.Status.ToString().ToLowerInvariant(),
            ["attempt_id"] = attemptId,
            ["reward_offers"] = offers,
            ["errors"] = new Godot.Collections.Array<string>(result.Errors),
        };
    }

    private sealed class UnavailableProgressionAuthority : IProgressionAuthority
    {
        private readonly string _error;

        public UnavailableProgressionAuthority(string error)
        {
            _error = error;
        }

        public ProgressionAuthorityResult StartBattleAttempt(StartBattleAttemptRequest request) =>
            ProgressionAuthorityResult.Unavailable(_error);

        public ProgressionAuthorityResult CompleteBattleAttempt(
            CompleteBattleAttemptRequest request
        ) => ProgressionAuthorityResult.Unavailable(_error);

        public ProgressionAuthorityResult GetBattleRewards(BattleAttemptId attemptId) =>
            ProgressionAuthorityResult.Unavailable(_error);

        public ProgressionAuthorityResult GetPendingBattleRewards(SummonerId summonerId) =>
            ProgressionAuthorityResult.Unavailable(_error);

        public ProgressionAuthorityResult ClaimBattleReward(BattleRewardClaimRequest request) =>
            ProgressionAuthorityResult.Unavailable(_error);
    }
}
