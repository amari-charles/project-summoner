namespace Fateforged.Tests.Meta.Rewards;

using System.Collections.Immutable;
using System.Linq;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardDeterminismTest
{
    [TestCase]
    public void URS_D01_D02_D04_SeedAndStableContextDetermineRepeatablePoolOrder()
    {
        var resolver = Resolver();
        var offer = Offer();
        var first = resolver.Resolve(offer, Context(seed: 42, occurrence: "lesson"));
        var repeated = resolver.Resolve(offer, Context(seed: 42, occurrence: "lesson"));
        var otherSeed = resolver.Resolve(offer, Context(seed: 9001, occurrence: "lesson"));
        var otherContext = resolver.Resolve(offer, Context(seed: 42, occurrence: "exam"));

        AssertThat(Ids(first)).IsEqual(Ids(repeated));
        AssertThat(Ids(otherSeed)).IsNotEqual(Ids(first));
        AssertThat(Ids(otherContext)).IsNotEqual(Ids(first));
    }

    [TestCase]
    public void URS_D03_PoolEnumerationOrderIsCanonicalizedBeforeSampling()
    {
        var original = Options();
        var reversed = original.Reverse().ToImmutableArray();
        var first = Resolver().Resolve(Offer(), Context(options: original));
        var second = Resolver().Resolve(Offer(), Context(options: reversed));

        AssertThat(Ids(first)).IsEqual(Ids(second));
    }

    [TestCase]
    public void StableClaimIdentityDoesNotCollideWhenFieldsContainDelimiters()
    {
        var summonerId = new SummonerId("summoner");
        var offerId = new RewardOfferId("offer");
        var first = RewardIdentity.CreateClaimId(
            summonerId,
            new RewardSourceContext
            {
                SourceType = "academy:activity",
                SourceId = "course",
                OccurrenceId = "lesson",
            },
            offerId
        );
        var second = RewardIdentity.CreateClaimId(
            summonerId,
            new RewardSourceContext
            {
                SourceType = "academy",
                SourceId = "activity:course",
                OccurrenceId = "lesson",
            },
            offerId
        );

        AssertThat(first).IsNotEqual(second);
        AssertThat(first.Value).StartsWith("reward:v1:");
    }

    private static RewardResolver Resolver() =>
        new([new AuthoredRewardOptionSource(), new PoolRewardOptionSource()]);

    private static RewardOfferDefinition Offer()
    {
        var poolId = new UniversalRewardPoolId("determinism_pool");
        return new RewardOfferDefinition
        {
            Id = new RewardOfferId("determinism_offer"),
            Selection = new RewardSelectionRule
            {
                Mode = RewardSelectionMode.PlayerChoice,
                ShowCount = 6,
                ChooseCount = 1,
            },
            OptionSource = new PoolRewardOptionSourceDefinition { PoolId = poolId },
        };
    }

    private static RewardResolutionContext Context(
        ulong seed = 42,
        string occurrence = "lesson",
        ImmutableArray<RewardOptionDefinition>? options = null
    )
    {
        var poolId = new UniversalRewardPoolId("determinism_pool");
        return new RewardResolutionContext
        {
            SummonerId = new SummonerId("summoner_test"),
            SummonerSeed = seed,
            Source = new RewardSourceContext
            {
                SourceType = "academy_activity",
                SourceId = "course",
                OccurrenceId = occurrence,
            },
            Catalog = new RewardContentCatalog
            {
                Pools = ImmutableDictionary<
                    UniversalRewardPoolId,
                    Fateforged.Data.Rewards.RewardPoolDefinition
                >.Empty.Add(
                    poolId,
                    new Fateforged.Data.Rewards.RewardPoolDefinition
                    {
                        Id = poolId,
                        Options = options ?? Options(),
                    }
                ),
            },
        };
    }

    private static ImmutableArray<RewardOptionDefinition> Options() =>
    [
        Option("a"),
        Option("b"),
        Option("c"),
        Option("d"),
        Option("e"),
        Option("f"),
    ];

    private static RewardOptionDefinition Option(string id) =>
        new() { Id = new RewardOptionId(id) };

    private static string Ids(RewardResolutionResult result)
    {
        AssertThat(result.Status).IsEqual(RewardRuntimeStatus.Ready);
        return string.Join(",", result.Snapshot!.Options.Select(option => option.Id.Value));
    }
}
