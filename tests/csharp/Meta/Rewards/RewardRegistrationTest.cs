namespace Fateforged.Tests.Meta.Rewards;

using System;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Data.Rewards;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardRegistrationTest
{
    [TestCase]
    public void DuplicateOptionSourceRegistrationFailsLoudly()
    {
        var threw = false;
        try
        {
            new RewardResolver(
                [new AuthoredRewardOptionSource(), new AuthoredRewardOptionSource()]
            );
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        AssertThat(threw).IsTrue();
    }

    [TestCase]
    public void URS_C19_BuiltInOptionSourceImplementationsDeclareDefinitionTypes()
    {
        IRewardOptionSource authored = new AuthoredRewardOptionSource();
        IRewardOptionSource pool = new PoolRewardOptionSource();

        AssertThat(authored.DefinitionType)
            .IsEqual(typeof(AuthoredRewardOptionSourceDefinition));
        AssertThat(pool.DefinitionType).IsEqual(typeof(PoolRewardOptionSourceDefinition));
    }

    [TestCase]
    public void URS_C19_GrantHandlerContractRequiresAnExplicitGrantType()
    {
        AssertThat(typeof(IRewardGrantHandler).GetProperty(nameof(IRewardGrantHandler.GrantType)))
            .IsNotNull();
        AssertThat(typeof(IRewardGrantHandler<>).IsGenericTypeDefinition).IsTrue();
    }

    [TestCase]
    public void URS_C19_UnregisteredGrantAndOptionSourceFailValidationOrResolution()
    {
        var option = new RewardOptionDefinition
        {
            Id = new RewardOptionId("unknown"),
            Grants =
            [
                new UnregisteredGrant
                {
                    Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
                },
            ],
        };
        var offer = new RewardOfferDefinition
        {
            Id = new RewardOfferId("unknown_source"),
            Selection = new RewardSelectionRule(),
            OptionSource = new UnregisteredSource { Options = [option] },
        };
        var validation = new RewardContentValidator(
            RewardGrantHandlerRegistry.CreateDefault().HandledGrantTypes
        ).Validate(
            new RewardContentCatalog(),
            [
                offer,
                offer with
                {
                    Id = new RewardOfferId("unknown_grant"),
                    OptionSource = new AuthoredRewardOptionSourceDefinition
                    {
                        Options = [option],
                    },
                },
            ]
        );
        var resolution = new RewardResolver([new AuthoredRewardOptionSource()]).Resolve(
            offer,
            new RewardResolutionContext
            {
                SummonerId = new Fateforged.Data.Summoners.SummonerId("summoner"),
                SummonerSeed = 1,
                Source = new Fateforged.Domain.Profile.Rewards.RewardSourceContext
                {
                    SourceType = "test",
                    SourceId = "source",
                },
            }
        );

        AssertThat(validation.Any(error => error.Contains("no handler"))).IsTrue();
        AssertThat(resolution.Status).IsEqual(RewardRuntimeStatus.Invalid);
    }

    private sealed record UnregisteredGrant : RewardGrantDefinition;

    private sealed record UnregisteredSource : RewardOptionSourceDefinition
    {
        public ImmutableArray<RewardOptionDefinition> Options { get; init; } = [];
    }
}
