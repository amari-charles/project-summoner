namespace Fateforged.Tests.Meta.Rewards;

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardResolverTest
{
    [TestCase]
    public void URS_C02_C05_C06_OptionSourcesResolveAuthoredAndPooledSnapshots()
    {
        var resolver = new RewardResolver(
            [new AuthoredRewardOptionSource(), new PoolRewardOptionSource()]
        );
        var context = CreateContext();

        var authored = resolver.Resolve(
            CreateOffer(
                new AuthoredRewardOptionSourceDefinition { Options = [Option("authored")] }
            ),
            context
        );
        var poolId = new UniversalRewardPoolId("foundation_pool");
        var pooled = resolver.Resolve(
            CreateOffer(
                new PoolRewardOptionSourceDefinition
                {
                    PoolId = poolId,
                }
            ),
            context with
            {
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
                            Options = [Option("pooled")],
                        }
                    ),
                },
            }
        );

        AssertThat(authored.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(authored.Snapshot!.Options[0].Id.Value).IsEqual("authored");
        AssertThat(pooled.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(pooled.Snapshot!.Options[0].Id.Value).IsEqual("pooled");
    }

    [TestCase]
    public void URS_C02_C08_C09_SelectionAndOwnershipFilteringAreEnforced()
    {
        var options = new[] { CardOption("a"), CardOption("b"), CardOption("c") };
        var offer = CreateOffer(
            new AuthoredRewardOptionSourceDefinition { Options = [.. options] }
        ) with
        {
            Selection = new RewardSelectionRule
            {
                Mode = RewardSelectionMode.PlayerChoice,
                ShowCount = 3,
                ChooseCount = 1,
            },
            Eligibility = new RewardEligibilityDefinition
            {
                DuplicatePolicy = RewardDuplicatePolicy.ExcludeOwned,
            },
        };
        var resolver = new RewardResolver([new AuthoredRewardOptionSource()]);
        var all = resolver.Resolve(offer, CreateContext());
        var filtered = resolver.Resolve(
            offer,
            CreateContext() with
            {
                OwnedRewardKeys = new HashSet<string> { "card:a", "card:b" },
            }
        );
        var impossible = resolver.Resolve(
            offer with
            {
                Selection = offer.Selection with { ChooseCount = 2 },
            },
            CreateContext() with
            {
                OwnedRewardKeys = new HashSet<string> { "card:a", "card:b" },
            }
        );

        AssertThat(all.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(all.Snapshot!.Options).HasSize(3);
        AssertThat(filtered.Status).IsEqual(RewardRuntimeStatus.Ready);
        AssertThat(filtered.Snapshot!.Options).HasSize(1);
        AssertThat(filtered.Snapshot.Options[0].Id.Value).IsEqual("c");
        AssertThat(impossible.Status).IsEqual(RewardRuntimeStatus.Invalid);
        AssertThat(impossible.Snapshot).IsNull();
    }

    private static RewardOfferDefinition CreateOffer(RewardOptionSourceDefinition source) =>
        new()
        {
            Id = new RewardOfferId("test_offer"),
            Selection = new RewardSelectionRule(),
            OptionSource = source,
        };

    private static RewardOptionDefinition Option(string id) =>
        new() { Id = new RewardOptionId(id) };

    private static RewardOptionDefinition CardOption(string id) =>
        new()
        {
            Id = new RewardOptionId(id),
            Grants =
            [
                new CardRewardGrantDefinition
                {
                    CardId = new CardId(id),
                    Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
                },
            ],
        };

    private static RewardResolutionContext CreateContext() =>
        new()
        {
            SummonerId = new SummonerId("summoner_test"),
            SummonerSeed = 42,
            Source = new RewardSourceContext
            {
                SourceType = "academy_activity",
                SourceId = "test_quest",
                OccurrenceId = "lesson_1",
            },
        };
}
