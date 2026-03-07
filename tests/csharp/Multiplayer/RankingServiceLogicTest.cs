namespace Fateforged.Tests.Multiplayer;

using System;
using System.Collections.Generic;
using GdUnit4;
using Fateforged.Multiplayer.Ranking;
using static GdUnit4.Assertions;

[TestSuite]
public class RankingServiceLogicTest
{
    #region MatchRecord Tests

    [TestCase]
    public void MatchRecord_DefaultValues()
    {
        var record = new MatchRecord();

        AssertThat(record.MatchId).IsEmpty();
        AssertThat(record.OpponentId).IsEmpty();
        AssertThat(record.Won).IsFalse();
        AssertThat(record.RatingBefore).IsEqual(0);
        AssertThat(record.RatingAfter).IsEqual(0);
        AssertThat(record.RatingChange).IsEqual(0);
        AssertThat(record.OpponentRatingBefore).IsEqual(0);
        AssertThat(record.DurationSeconds).IsEqual(0f);
        AssertThat(record.EndReason).IsEmpty();
        AssertThat(record.Timestamp).IsEqual(0L);
    }

    [TestCase]
    public void MatchRecord_StoresAllProperties()
    {
        var record = new MatchRecord
        {
            MatchId = "match-789",
            OpponentId = "opponent-user",
            Won = true,
            RatingBefore = 1200,
            RatingAfter = 1216,
            RatingChange = 16,
            OpponentRatingBefore = 1200,
            DurationSeconds = 120.5f,
            EndReason = "summoner_destroyed",
            Timestamp = 1707091200L
        };

        AssertThat(record.MatchId).IsEqual("match-789");
        AssertThat(record.OpponentId).IsEqual("opponent-user");
        AssertThat(record.Won).IsTrue();
        AssertThat(record.RatingBefore).IsEqual(1200);
        AssertThat(record.RatingAfter).IsEqual(1216);
        AssertThat(record.RatingChange).IsEqual(16);
        AssertThat(record.OpponentRatingBefore).IsEqual(1200);
        AssertThat(record.DurationSeconds).IsEqual(120.5f);
        AssertThat(record.EndReason).IsEqual("summoner_destroyed");
        AssertThat(record.Timestamp).IsEqual(1707091200L);
    }

    [TestCase]
    public void MatchRecord_RatingChangeConsistency()
    {
        var record = new MatchRecord
        {
            RatingBefore = 1200,
            RatingAfter = 1224,
            RatingChange = 24
        };

        AssertThat(record.RatingAfter - record.RatingBefore).IsEqual(record.RatingChange);
    }

    [TestCase]
    public void MatchRecord_NegativeRatingChange()
    {
        var record = new MatchRecord
        {
            Won = false,
            RatingBefore = 1200,
            RatingAfter = 1184,
            RatingChange = -16
        };

        AssertThat(record.Won).IsFalse();
        AssertThat(record.RatingChange).IsLess(0);
        AssertThat(record.RatingAfter).IsLess(record.RatingBefore);
    }

    #endregion

    #region MatchResult Tests

    [TestCase]
    public void MatchResult_DefaultValues()
    {
        var result = new MatchResult();

        AssertThat(result.MatchId).IsEmpty();
        AssertThat(result.WinnerUserId).IsEmpty();
        AssertThat(result.LoserUserId).IsEmpty();
        AssertThat(result.OpponentRating).IsEqual(EloCalculator.StartingElo);
        AssertThat(result.DurationSeconds).IsEqual(0f);
        AssertThat(result.EndReason).IsEqual("summoner_destroyed");
    }

    [TestCase]
    public void MatchResult_StoresAllProperties()
    {
        var result = new MatchResult
        {
            MatchId = "match-123",
            WinnerUserId = "winner-id",
            LoserUserId = "loser-id",
            OpponentRating = 1500,
            DurationSeconds = 120.5f,
            EndReason = "forfeit"
        };

        AssertThat(result.MatchId).IsEqual("match-123");
        AssertThat(result.WinnerUserId).IsEqual("winner-id");
        AssertThat(result.LoserUserId).IsEqual("loser-id");
        AssertThat(result.OpponentRating).IsEqual(1500);
        AssertThat(result.DurationSeconds).IsEqual(120.5f);
        AssertThat(result.EndReason).IsEqual("forfeit");
    }

    #endregion

    #region Win Rate Tests

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

        AssertThat(winRate).IsEqual(1.0f);
    }

    [TestCase]
    public void WinRateCalculation_HalfWins()
    {
        float winRate = CalculateWinRate(10, 5);

        AssertThat(winRate).IsEqual(0.5f);
    }

    [TestCase]
    public void WinRateCalculation_NoWins()
    {
        float winRate = CalculateWinRate(10, 0);

        AssertThat(winRate).IsEqual(0f);
    }

    [TestCase]
    public void WinRateCalculation_ThreeOutOfFour()
    {
        float winRate = CalculateWinRate(4, 3);

        AssertThat(winRate).IsEqual(0.75f);
    }

    #endregion

    #region Rating Clamping Tests

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

    #endregion

    #region Record Match Tests

    [TestCase]
    public void RecordMatch_WinCalculation()
    {
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
        int currentRating = 1000;
        int opponentRating = 1400;
        bool won = true;

        var (_, ratingChangeUpset) = SimulateRecordMatch(currentRating, opponentRating, won);
        var (_, ratingChangeEven) = SimulateRecordMatch(1000, 1000, true);

        AssertThat(ratingChangeUpset).IsGreater(ratingChangeEven);
    }

    [TestCase]
    public void RecordMatch_ExpectedWin()
    {
        int currentRating = 1400;
        int opponentRating = 1000;
        bool won = true;

        var (_, ratingChangeExpected) = SimulateRecordMatch(currentRating, opponentRating, won);
        var (_, ratingChangeEven) = SimulateRecordMatch(1400, 1400, true);

        AssertThat(ratingChangeExpected).IsLess(ratingChangeEven);
    }

    #endregion

    #region MatchEndReason Tests

    [TestCase]
    public void MatchEndReason_HasExpectedValues()
    {
        AssertThat((int)MatchEndReason.SummonerDestroyed).IsEqual(0);
        AssertThat((int)MatchEndReason.Forfeit).IsEqual(1);
        AssertThat((int)MatchEndReason.Disconnect).IsEqual(2);
        AssertThat((int)MatchEndReason.Timeout).IsEqual(3);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Mimics RankingService.GetWinRate() — returns 0.0-1.0 ratio.
    /// </summary>
    private static float CalculateWinRate(int totalMatches, int wins)
    {
        if (totalMatches == 0) return 0f;
        return (float)wins / totalMatches;
    }

    private static int ClampRating(int rating)
    {
        return Math.Clamp(rating, EloCalculator.EloFloor, EloCalculator.EloCeiling);
    }

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

    #endregion
}
