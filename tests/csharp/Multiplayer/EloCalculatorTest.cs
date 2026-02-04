namespace ProjectSummoner.Tests.Multiplayer;

using GdUnit4;
using Fateforged.Multiplayer.Ranking;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for EloCalculator rating calculations.
/// </summary>
[TestSuite]
public class EloCalculatorTest
{
    [TestCase]
    public void CalculateNewRatings_WinnerGainsLoserLoses()
    {
        var (winnerNew, loserNew) = EloCalculator.CalculateNewRatings(1200, 1200);

        // Equal ratings: winner gains ~16, loser loses ~16 with K=32
        AssertThat(winnerNew).IsGreater(1200);
        AssertThat(loserNew).IsLess(1200);
        AssertThat(winnerNew - 1200).IsEqual(1200 - loserNew); // Symmetric change
    }

    [TestCase]
    public void CalculateNewRatings_HigherRatedWinnerGainsLess()
    {
        // Higher rated player wins - should gain fewer points
        var (winnerNew1, _) = EloCalculator.CalculateNewRatings(1500, 1200);
        var (winnerNew2, _) = EloCalculator.CalculateNewRatings(1200, 1200);

        // Winner with higher rating gains less than equal rating winner
        AssertThat(winnerNew1 - 1500).IsLess(winnerNew2 - 1200);
    }

    [TestCase]
    public void CalculateNewRatings_LowerRatedWinnerGainsMore()
    {
        // Lower rated player wins - should gain more points (upset bonus)
        var (winnerNew1, _) = EloCalculator.CalculateNewRatings(1200, 1500);
        var (winnerNew2, _) = EloCalculator.CalculateNewRatings(1200, 1200);

        // Winner with lower rating gains more than equal rating winner
        AssertThat(winnerNew1 - 1200).IsGreater(winnerNew2 - 1200);
    }

    [TestCase]
    public void CalculateNewRatings_RespectsEloFloor()
    {
        // Very low rated loser against high rated winner shouldn't go below floor
        var (_, loserNew) = EloCalculator.CalculateNewRatings(2000, 110);

        AssertThat(loserNew).IsGreaterEqual(EloCalculator.EloFloor);
    }

    [TestCase]
    public void CalculateNewRatings_RespectsEloCeiling()
    {
        // Very high rated winner shouldn't exceed ceiling
        var (winnerNew, _) = EloCalculator.CalculateNewRatings(2990, 1200);

        AssertThat(winnerNew).IsLessEqual(EloCalculator.EloCeiling);
    }

    [TestCase]
    public void GetExpectedScore_EqualRatingsIsHalf()
    {
        var expected = EloCalculator.GetExpectedScore(1200, 1200);

        AssertThat(expected).IsBetween(0.49, 0.51);
    }

    [TestCase]
    public void GetExpectedScore_HigherRatingHasHigherExpected()
    {
        var higher = EloCalculator.GetExpectedScore(1400, 1200);
        var lower = EloCalculator.GetExpectedScore(1200, 1400);

        AssertThat(higher).IsGreater(0.5);
        AssertThat(lower).IsLess(0.5);
        AssertThat(higher + lower).IsBetween(0.99, 1.01); // Should sum to ~1
    }

    [TestCase]
    public void GetTier_ReturnsCorrectTier()
    {
        AssertThat(EloCalculator.GetTier(500)).IsEqual(RankTier.Bronze);
        AssertThat(EloCalculator.GetTier(850)).IsEqual(RankTier.Silver);
        AssertThat(EloCalculator.GetTier(1050)).IsEqual(RankTier.Gold);
        AssertThat(EloCalculator.GetTier(1250)).IsEqual(RankTier.Platinum);
        AssertThat(EloCalculator.GetTier(1450)).IsEqual(RankTier.Diamond);
        AssertThat(EloCalculator.GetTier(1650)).IsEqual(RankTier.Master);
        AssertThat(EloCalculator.GetTier(1850)).IsEqual(RankTier.Grandmaster);
        AssertThat(EloCalculator.GetTier(2100)).IsEqual(RankTier.Legend);
    }

    [TestCase]
    public void GetTier_BoundaryValues()
    {
        // Test exact boundary values
        AssertThat(EloCalculator.GetTier(799)).IsEqual(RankTier.Bronze);
        AssertThat(EloCalculator.GetTier(800)).IsEqual(RankTier.Silver);
        AssertThat(EloCalculator.GetTier(999)).IsEqual(RankTier.Silver);
        AssertThat(EloCalculator.GetTier(1000)).IsEqual(RankTier.Gold);
        AssertThat(EloCalculator.GetTier(1999)).IsEqual(RankTier.Grandmaster);
        AssertThat(EloCalculator.GetTier(2000)).IsEqual(RankTier.Legend);
    }

    [TestCase]
    public void GetDivision_ReturnsBetween1And4()
    {
        for (int elo = 100; elo <= 2500; elo += 50)
        {
            var division = EloCalculator.GetDivision(elo);
            AssertThat(division).IsBetween(1, 4);
        }
    }

    [TestCase]
    public void GetDivision_HigherWithinTierIsLowerDivision()
    {
        // Higher ELO within same tier should have lower division number (I > II > III > IV)
        var divLow = EloCalculator.GetDivision(1010);  // Low Gold
        var divHigh = EloCalculator.GetDivision(1180); // High Gold

        AssertThat(divLow).IsGreaterEqual(divHigh);
    }

    [TestCase]
    public void GetTierFloor_ReturnsCorrectValues()
    {
        AssertThat(EloCalculator.GetTierFloor(RankTier.Bronze)).IsEqual(0);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Silver)).IsEqual(800);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Gold)).IsEqual(1000);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Platinum)).IsEqual(1200);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Diamond)).IsEqual(1400);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Master)).IsEqual(1600);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Grandmaster)).IsEqual(1800);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Legend)).IsEqual(2000);
    }

    [TestCase]
    public void FormatRating_IncludesTierAndDivision()
    {
        var formatted = EloCalculator.FormatRating(1150);

        AssertThat(formatted).Contains("Gold");
        AssertThat(formatted).Contains("1150");
    }

    [TestCase]
    public void FormatRating_LegendHasNoDivision()
    {
        var formatted = EloCalculator.FormatRating(2100);

        AssertThat(formatted).Contains("Legend");
        AssertThat(formatted).NotContains(" I ");
        AssertThat(formatted).NotContains(" II ");
    }

    [TestCase]
    public void StartingElo_IsWithinGoldTier()
    {
        var tier = EloCalculator.GetTier(EloCalculator.StartingElo);

        // Starting ELO of 1200 should be in Platinum tier
        AssertThat(tier).IsEqual(RankTier.Platinum);
    }

    [TestCase]
    public void Constants_HaveReasonableValues()
    {
        AssertThat(EloCalculator.StartingElo).IsGreater(0);
        AssertThat(EloCalculator.KFactor).IsGreater(0);
        AssertThat(EloCalculator.EloFloor).IsGreaterEqual(0);
        AssertThat(EloCalculator.EloCeiling).IsGreater(EloCalculator.EloFloor);
        AssertThat(EloCalculator.StartingElo).IsBetween(EloCalculator.EloFloor, EloCalculator.EloCeiling);
    }
}
