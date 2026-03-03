namespace Fateforged.Tests.Multiplayer;

using System;
using GdUnit4;
using Fateforged.Multiplayer.Ranking;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for RankingService logic that doesn't require scene tree.
/// Note: Full integration testing requires Godot scene tree and ProfileRepo.
/// </summary>
[TestSuite]
public class RankingServiceLogicTest
{
    [TestCase]
    public void MatchHistoryEntry_DefaultValues()
    {
        var entry = new MatchHistoryEntry();

        AssertThat(entry.MatchId).IsEmpty();
        AssertThat(entry.OpponentId).IsEmpty();
        AssertThat(entry.Won).IsFalse();
        AssertThat(entry.RatingBefore).IsEqual(0);
        AssertThat(entry.RatingAfter).IsEqual(0);
        AssertThat(entry.RatingChange).IsEqual(0);
        AssertThat(entry.PlayedAt).IsEqual(default(DateTime));
    }

    [TestCase]
    public void MatchHistoryEntry_StoresAllProperties()
    {
        var playedAt = new DateTime(2026, 2, 4, 12, 0, 0);
        var entry = new MatchHistoryEntry
        {
            MatchId = "match-789",
            OpponentId = "opponent-user",
            Won = true,
            RatingBefore = 1200,
            RatingAfter = 1216,
            RatingChange = 16,
            PlayedAt = playedAt
        };

        AssertThat(entry.MatchId).IsEqual("match-789");
        AssertThat(entry.OpponentId).IsEqual("opponent-user");
        AssertThat(entry.Won).IsTrue();
        AssertThat(entry.RatingBefore).IsEqual(1200);
        AssertThat(entry.RatingAfter).IsEqual(1216);
        AssertThat(entry.RatingChange).IsEqual(16);
        AssertThat(entry.PlayedAt).IsEqual(playedAt);
    }

    [TestCase]
    public void MatchHistoryEntry_RatingChangeConsistency()
    {
        // Verify rating change matches before/after difference
        var entry = new MatchHistoryEntry
        {
            RatingBefore = 1200,
            RatingAfter = 1224,
            RatingChange = 24
        };

        AssertThat(entry.RatingAfter - entry.RatingBefore).IsEqual(entry.RatingChange);
    }

    [TestCase]
    public void MatchHistoryEntry_NegativeRatingChange()
    {
        // Verify loss scenario
        var entry = new MatchHistoryEntry
        {
            Won = false,
            RatingBefore = 1200,
            RatingAfter = 1184,
            RatingChange = -16
        };

        AssertThat(entry.Won).IsFalse();
        AssertThat(entry.RatingChange).IsLess(0);
        AssertThat(entry.RatingAfter).IsLess(entry.RatingBefore);
    }

    [TestCase]
    public void WinRateCalculation_ZeroMatches()
    {
        float winRate = CalculateWinRate(0, 0);

        AssertThat(winRate).IsEqual(0f);
    }

    [TestCase]
    public void WinRateCalculation_AllWins()
    {
        float winRate = CalculateWinRate(10, 10);

        AssertThat(winRate).IsEqual(100f);
    }

    [TestCase]
    public void WinRateCalculation_HalfWins()
    {
        float winRate = CalculateWinRate(10, 5);

        AssertThat(winRate).IsEqual(50f);
    }

    [TestCase]
    public void WinRateCalculation_NoWins()
    {
        float winRate = CalculateWinRate(10, 0);

        AssertThat(winRate).IsEqual(0f);
    }

    [TestCase]
    public void RatingClamping_AtFloor()
    {
        int clamped = ClampRating(50);

        AssertThat(clamped).IsEqual(EloCalculator.EloFloor);
    }

    [TestCase]
    public void RatingClamping_AtCeiling()
    {
        int clamped = ClampRating(5000);

        AssertThat(clamped).IsEqual(EloCalculator.EloCeiling);
    }

    [TestCase]
    public void RatingClamping_InRange()
    {
        int clamped = ClampRating(1500);

        AssertThat(clamped).IsEqual(1500);
    }

    [TestCase]
    public void RecordMatch_WinCalculation()
    {
        // Simulate recording a win
        int currentRating = 1200;
        int opponentRating = 1200;
        bool won = true;

        var (newRating, ratingChange) = SimulateRecordMatch(currentRating, opponentRating, won);

        AssertThat(newRating).IsGreater(currentRating);
        AssertThat(ratingChange).IsGreater(0);
    }

    [TestCase]
    public void RecordMatch_LossCalculation()
    {
        // Simulate recording a loss
        int currentRating = 1200;
        int opponentRating = 1200;
        bool won = false;

        var (newRating, ratingChange) = SimulateRecordMatch(currentRating, opponentRating, won);

        AssertThat(newRating).IsLess(currentRating);
        AssertThat(ratingChange).IsLess(0);
    }

    [TestCase]
    public void RecordMatch_UpsetWin()
    {
        // Lower rated player beats higher rated - should gain more points
        int currentRating = 1000;
        int opponentRating = 1400;
        bool won = true;

        var (_, ratingChangeUpset) = SimulateRecordMatch(currentRating, opponentRating, won);

        // Compare to even match
        var (_, ratingChangeEven) = SimulateRecordMatch(1000, 1000, true);

        AssertThat(ratingChangeUpset).IsGreater(ratingChangeEven);
    }

    [TestCase]
    public void RecordMatch_ExpectedWin()
    {
        // Higher rated player beats lower rated - should gain fewer points
        int currentRating = 1400;
        int opponentRating = 1000;
        bool won = true;

        var (_, ratingChangeExpected) = SimulateRecordMatch(currentRating, opponentRating, won);

        // Compare to even match
        var (_, ratingChangeEven) = SimulateRecordMatch(1400, 1400, true);

        AssertThat(ratingChangeExpected).IsLess(ratingChangeEven);
    }

    /// <summary>
    /// Helper method that mimics RankingService.GetWinRate() logic.
    /// </summary>
    private static float CalculateWinRate(int totalMatches, int wins)
    {
        if (totalMatches == 0) return 0f;
        return (float)wins / totalMatches * 100f;
    }

    /// <summary>
    /// Helper method that mimics RankingService.SetRating() clamping.
    /// </summary>
    private static int ClampRating(int rating)
    {
        return Math.Clamp(rating, EloCalculator.EloFloor, EloCalculator.EloCeiling);
    }

    /// <summary>
    /// Simulates RankingService.RecordMatch() rating calculation.
    /// Returns (newRating, ratingChange).
    /// </summary>
    private static (int newRating, int ratingChange) SimulateRecordMatch(
        int currentRating, int opponentRating, bool won)
    {
        int newRating;
        if (won)
        {
            var (winnerNew, _) = EloCalculator.CalculateNewRatings(currentRating, opponentRating);
            newRating = winnerNew;
        }
        else
        {
            var (_, loserNew) = EloCalculator.CalculateNewRatings(opponentRating, currentRating);
            newRating = loserNew;
        }

        return (newRating, newRating - currentRating);
    }
}
