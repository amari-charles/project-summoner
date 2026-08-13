namespace Fateforged.Tests.Multiplayer;

using Fateforged.Multiplayer.Ranking;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class EloCalculatorTest
{
    [TestCase]
    public void CalculateNewRatings_WinnerGainsLoserLoses()
    {
        var (winnerNew, loserNew) = EloCalculator.CalculateNewRatings(1200, 1200);

        AssertThat(winnerNew).IsGreater(1200);
        AssertThat(loserNew).IsLess(1200);
        AssertThat(winnerNew - 1200).IsEqual(1200 - loserNew);
    }

    [TestCase]
    public void CalculateNewRatings_HigherRatedWinnerGainsLess()
    {
        var (winnerNew1, _) = EloCalculator.CalculateNewRatings(1500, 1200);
        var (winnerNew2, _) = EloCalculator.CalculateNewRatings(1200, 1200);

        AssertThat(winnerNew1 - 1500).IsLess(winnerNew2 - 1200);
    }

    [TestCase]
    public void CalculateNewRatings_LowerRatedWinnerGainsMore()
    {
        var (winnerNew1, _) = EloCalculator.CalculateNewRatings(1200, 1500);
        var (winnerNew2, _) = EloCalculator.CalculateNewRatings(1200, 1200);

        AssertThat(winnerNew1 - 1200).IsGreater(winnerNew2 - 1200);
    }

    [TestCase]
    public void CalculateNewRatings_RespectsEloFloor()
    {
        var (_, loserNew) = EloCalculator.CalculateNewRatings(2000, 110);

        AssertThat(loserNew).IsGreaterEqual(EloCalculator.EloFloor);
    }

    [TestCase]
    public void CalculateNewRatings_RespectsEloCeiling()
    {
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
        AssertThat(higher + lower).IsBetween(0.99, 1.01);
    }

    [TestCase]
    public void GetTier_ReturnsCorrectTier()
    {
        AssertThat(EloCalculator.GetTier(500)).IsEqual(RankTier.Unbound);
        AssertThat(EloCalculator.GetTier(850)).IsEqual(RankTier.Apprentice);
        AssertThat(EloCalculator.GetTier(1050)).IsEqual(RankTier.Adept);
        AssertThat(EloCalculator.GetTier(1250)).IsEqual(RankTier.Mage);
        AssertThat(EloCalculator.GetTier(1450)).IsEqual(RankTier.Archmage);
        AssertThat(EloCalculator.GetTier(1650)).IsEqual(RankTier.Sage);
    }

    [TestCase]
    public void GetTier_BoundaryValues()
    {
        AssertThat(EloCalculator.GetTier(799)).IsEqual(RankTier.Unbound);
        AssertThat(EloCalculator.GetTier(800)).IsEqual(RankTier.Apprentice);
        AssertThat(EloCalculator.GetTier(999)).IsEqual(RankTier.Apprentice);
        AssertThat(EloCalculator.GetTier(1000)).IsEqual(RankTier.Adept);
        AssertThat(EloCalculator.GetTier(1199)).IsEqual(RankTier.Adept);
        AssertThat(EloCalculator.GetTier(1200)).IsEqual(RankTier.Mage);
        AssertThat(EloCalculator.GetTier(1399)).IsEqual(RankTier.Mage);
        AssertThat(EloCalculator.GetTier(1400)).IsEqual(RankTier.Archmage);
        AssertThat(EloCalculator.GetTier(1599)).IsEqual(RankTier.Archmage);
        AssertThat(EloCalculator.GetTier(1600)).IsEqual(RankTier.Sage);
        AssertThat(EloCalculator.GetTier(2500)).IsEqual(RankTier.Sage);
    }

    [TestCase]
    public void GetTier_NeverReturnsFateforged()
    {
        // GetTier is purely ELO-based, never returns Fateforged
        for (int elo = 100; elo <= 3000; elo += 100)
        {
            AssertThat(EloCalculator.GetTier(elo)).IsNotEqual(RankTier.Fateforged);
        }
    }

    [TestCase]
    public void GetDisplayTier_ReturnsFateforgedWhenTopTwenty()
    {
        AssertThat(EloCalculator.GetDisplayTier(1800, true)).IsEqual(RankTier.Fateforged);
        AssertThat(EloCalculator.GetDisplayTier(1200, true)).IsEqual(RankTier.Fateforged);
    }

    [TestCase]
    public void GetDisplayTier_ReturnsEloTierWhenNotTopTwenty()
    {
        AssertThat(EloCalculator.GetDisplayTier(1800, false)).IsEqual(RankTier.Sage);
        AssertThat(EloCalculator.GetDisplayTier(1200, false)).IsEqual(RankTier.Mage);
        AssertThat(EloCalculator.GetDisplayTier(500, false)).IsEqual(RankTier.Unbound);
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
        var divLow = EloCalculator.GetDivision(1210); // Low Mage
        var divHigh = EloCalculator.GetDivision(1380); // High Mage

        AssertThat(divLow).IsGreaterEqual(divHigh);
    }

    [TestCase]
    public void GetTierFloor_ReturnsCorrectValues()
    {
        AssertThat(EloCalculator.GetTierFloor(RankTier.Unbound)).IsEqual(0);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Apprentice)).IsEqual(800);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Adept)).IsEqual(1000);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Mage)).IsEqual(1200);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Archmage)).IsEqual(1400);
        AssertThat(EloCalculator.GetTierFloor(RankTier.Sage)).IsEqual(1600);
    }

    [TestCase]
    public void GetTierCeiling_ReturnsCorrectValues()
    {
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Unbound)).IsEqual(799);
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Apprentice)).IsEqual(999);
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Adept)).IsEqual(1199);
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Mage)).IsEqual(1399);
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Archmage)).IsEqual(1599);
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Sage)).IsEqual(EloCalculator.EloCeiling);
        AssertThat(EloCalculator.GetTierCeiling(RankTier.Fateforged))
            .IsEqual(EloCalculator.EloCeiling);
    }

    [TestCase(800, 0)]
    [TestCase(925, 125)]
    [TestCase(1000, 0)]
    [TestCase(1050, 50)]
    [TestCase(1600, 0)]
    [TestCase(1825, 225)]
    public void GetLeaguePoints_ReturnsProgressAboveCurrentTierFloor(int rating, int expectedLp)
    {
        AssertThat(EloCalculator.GetLeaguePoints(rating)).IsEqual(expectedLp);
    }

    [TestCase]
    public void FormatRating_IncludesTierAndDivision()
    {
        var formatted = EloCalculator.FormatRating(1250);

        AssertThat(formatted).Contains("Mage");
        AssertThat(formatted).Contains("1250");
    }

    [TestCase]
    public void FormatRating_FateforgedHasNoDivision()
    {
        var formatted = EloCalculator.FormatRating(2100, true);

        AssertThat(formatted).Contains("Fateforged");
        AssertThat(formatted).NotContains(" I ");
        AssertThat(formatted).NotContains(" II ");
    }

    [TestCase]
    public void FormatRating_NonTopTwentyShowsSage()
    {
        var formatted = EloCalculator.FormatRating(2100, false);

        AssertThat(formatted).Contains("Sage");
        AssertThat(formatted).NotContains("Fateforged");
    }

    [TestCase]
    public void StartingElo_IsWithinApprenticeTier()
    {
        var tier = EloCalculator.GetTier(EloCalculator.StartingElo);

        AssertThat(tier).IsEqual(RankTier.Apprentice);
    }

    [TestCase]
    public void Constants_HaveReasonableValues()
    {
        AssertThat(EloCalculator.StartingElo).IsGreater(0);
        AssertThat(EloCalculator.KFactor).IsGreater(0);
        AssertThat(EloCalculator.EloFloor).IsGreaterEqual(0);
        AssertThat(EloCalculator.EloCeiling).IsGreater(EloCalculator.EloFloor);
        AssertThat(EloCalculator.StartingElo)
            .IsBetween(EloCalculator.EloFloor, EloCalculator.EloCeiling);
    }
}
