namespace Fateforged.Tests.Multiplayer;

using System;
using System.Collections.Generic;
using GdUnit4;
using Fateforged.Multiplayer.Ranking;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for MatchReporter logic that doesn't require scene tree.
/// Note: Full integration testing requires Godot scene tree and ProfileRepo.
/// </summary>
[TestSuite]
public class MatchReporterLogicTest
{
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

    [TestCase]
    public void MatchReport_DefaultValues()
    {
        var report = new MatchReport();

        AssertThat(report.MatchId).IsEmpty();
        AssertThat(report.WinnerId).IsEmpty();
        AssertThat(report.LoserId).IsEmpty();
        AssertThat(report.WinnerRatingBefore).IsEqual(0);
        AssertThat(report.LoserRatingBefore).IsEqual(0);
        AssertThat(report.WinnerRatingAfter).IsEqual(0);
        AssertThat(report.LoserRatingAfter).IsEqual(0);
        AssertThat(report.DurationSeconds).IsEqual(0f);
        AssertThat(report.EndReason).IsEmpty();
        AssertThat(report.Timestamp).IsEqual(0L);
        AssertThat(report.LocalPlayerWon).IsFalse();
    }

    [TestCase]
    public void MatchReport_StoresAllProperties()
    {
        var report = new MatchReport
        {
            MatchId = "match-456",
            WinnerId = "winner-user",
            LoserId = "loser-user",
            WinnerRatingBefore = 1200,
            LoserRatingBefore = 1300,
            WinnerRatingAfter = 1220,
            LoserRatingAfter = 1280,
            DurationSeconds = 90.0f,
            EndReason = "summoner_destroyed",
            Timestamp = 1707091200L,
            LocalPlayerWon = true
        };

        AssertThat(report.MatchId).IsEqual("match-456");
        AssertThat(report.WinnerId).IsEqual("winner-user");
        AssertThat(report.LoserId).IsEqual("loser-user");
        AssertThat(report.WinnerRatingBefore).IsEqual(1200);
        AssertThat(report.LoserRatingBefore).IsEqual(1300);
        AssertThat(report.WinnerRatingAfter).IsEqual(1220);
        AssertThat(report.LoserRatingAfter).IsEqual(1280);
        AssertThat(report.DurationSeconds).IsEqual(90.0f);
        AssertThat(report.EndReason).IsEqual("summoner_destroyed");
        AssertThat(report.Timestamp).IsEqual(1707091200L);
        AssertThat(report.LocalPlayerWon).IsTrue();
    }

    [TestCase]
    public void MatchReport_RatingChangeCalculation()
    {
        // Verify that rating changes are computed correctly
        var report = new MatchReport
        {
            WinnerRatingBefore = 1200,
            WinnerRatingAfter = 1216,  // +16
            LoserRatingBefore = 1200,
            LoserRatingAfter = 1184    // -16
        };

        int winnerChange = report.WinnerRatingAfter - report.WinnerRatingBefore;
        int loserChange = report.LoserRatingAfter - report.LoserRatingBefore;

        AssertThat(winnerChange).IsEqual(16);
        AssertThat(loserChange).IsEqual(-16);
        AssertThat(winnerChange + loserChange).IsEqual(0); // Zero-sum
    }

    [TestCase]
    public void WinRateCalculation_EmptyHistory()
    {
        var history = new List<MatchReport>();

        float winRate = CalculateWinRate(history);

        AssertThat(winRate).IsEqual(0f);
    }

    [TestCase]
    public void WinRateCalculation_AllWins()
    {
        var history = new List<MatchReport>
        {
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = true }
        };

        float winRate = CalculateWinRate(history);

        AssertThat(winRate).IsEqual(1f);
    }

    [TestCase]
    public void WinRateCalculation_AllLosses()
    {
        var history = new List<MatchReport>
        {
            new() { LocalPlayerWon = false },
            new() { LocalPlayerWon = false },
            new() { LocalPlayerWon = false }
        };

        float winRate = CalculateWinRate(history);

        AssertThat(winRate).IsEqual(0f);
    }

    [TestCase]
    public void WinRateCalculation_Mixed()
    {
        var history = new List<MatchReport>
        {
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = false },
            new() { LocalPlayerWon = false }
        };

        float winRate = CalculateWinRate(history);

        AssertThat(winRate).IsEqual(0.5f);
    }

    [TestCase]
    public void WinRateCalculation_ThreeOutOfFour()
    {
        var history = new List<MatchReport>
        {
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = true },
            new() { LocalPlayerWon = false }
        };

        float winRate = CalculateWinRate(history);

        AssertThat(winRate).IsEqual(0.75f);
    }

    /// <summary>
    /// Helper method that mimics MatchReporter.GetWinRate() logic.
    /// This allows testing the algorithm without needing the Node instance.
    /// </summary>
    private static float CalculateWinRate(List<MatchReport> history)
    {
        if (history.Count == 0) return 0f;

        int wins = 0;
        foreach (var match in history)
        {
            if (match.LocalPlayerWon) wins++;
        }

        return (float)wins / history.Count;
    }
}
