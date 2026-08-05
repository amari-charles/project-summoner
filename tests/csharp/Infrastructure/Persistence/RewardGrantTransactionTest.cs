namespace Fateforged.Tests.Infrastructure.Persistence;

using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class RewardGrantTransactionTest
{
    [TestCase]
    public void URS_C14_ProfileRepositoryOwnsAtomicRewardTransactionFactory()
    {
        AssertThat(typeof(IRewardProfileStore).IsAssignableFrom(typeof(ProfileRepository)))
            .IsTrue();
        AssertThat(
                typeof(IRewardGrantTransaction).GetMethod(
                    nameof(IRewardGrantTransaction.TryStageReceipt)
                )
            )
            .IsNotNull();
        AssertThat(
                typeof(IRewardGrantTransaction).GetMethod(nameof(IRewardGrantTransaction.Commit))
            )
            .IsNotNull();
    }
}
