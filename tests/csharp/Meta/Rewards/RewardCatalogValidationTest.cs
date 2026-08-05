namespace Fateforged.Tests.Meta.Rewards;

using System;
using System.IO;
using Fateforged.Data.Rewards;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardCatalogValidationTest
{
    [TestCase]
    public void URS_C17_UnknownDiscriminatorFailsClosedWithFileError()
    {
        var directory = CreateDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(directory, "invalid.json"),
                """
                {
                  "pools": [{
                    "id": "invalid_pool",
                    "options": [{
                      "id": "invalid_option",
                      "grants": [{
                        "kind": "not_registered",
                        "target": { "scope": "account" }
                      }]
                    }]
                  }]
                }
                """
            );

            var result = Loader().Load(directory);

            AssertThat(result.IsReady).IsFalse();
            AssertThat(result.Errors).IsNotEmpty();
            AssertThat(result.Errors[0]).Contains("invalid.json");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase]
    public void URS_C09_C18_ValidDataOnlyPoolLoadsAndInvalidSelectionIsRejected()
    {
        var directory = CreateDirectory();
        try
        {
            File.WriteAllText(
                Path.Combine(directory, "starter.json"),
                """
                {
                  "pools": [{
                    "id": "starter_pool",
                    "category_key": "reward.category.starter",
                    "options": [{
                      "id": "fire_wisp",
                      "label_key": "reward.fire_wisp",
                      "grants": [{
                        "kind": "card",
                        "target": { "scope": "account" },
                        "card_id": "fire_wisp",
                        "count": 1
                      }]
                    }]
                  }]
                }
                """
            );
            var loader = Loader();
            var result = loader.Load(directory);
            var invalidOffer = new RewardOfferDefinition
            {
                Id = new RewardOfferId("invalid_offer"),
                Selection = new RewardSelectionRule { ShowCount = 1, ChooseCount = 2 },
                OptionSource = new PoolRewardOptionSourceDefinition
                {
                    PoolId = new UniversalRewardPoolId("starter_pool"),
                },
            };
            var validation = new RewardContentValidator(
                RewardGrantHandlerRegistry.CreateDefault().HandledGrantTypes
            ).Validate(result.Catalog, [invalidOffer]);

            AssertThat(result.IsReady).IsTrue();
            AssertThat(result.Catalog.Pools).HasSize(1);
            AssertThat(validation).IsNotEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static RewardContentLoader Loader() =>
        new(
            new RewardContentValidator(
                RewardGrantHandlerRegistry.CreateDefault().HandledGrantTypes
            )
        );

    private static string CreateDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"fateforged-reward-test-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(path);
        return path;
    }
}
