using System;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// ELO rating calculation for ranked matches.
/// Uses standard ELO formula with configurable K-factor.
/// </summary>
public static class EloCalculator
{
    /// <summary>
    /// Starting ELO for new players.
    /// </summary>
    public const int StartingElo = 1200;

    /// <summary>
    /// K-factor determines how much ratings change per match.
    /// Higher = more volatile, Lower = more stable.
    /// 32 is standard for chess beginners, 16 for masters.
    /// </summary>
    public const int KFactor = 32;

    /// <summary>
    /// Minimum ELO floor to prevent negative ratings.
    /// </summary>
    public const int EloFloor = 100;

    /// <summary>
    /// Maximum ELO ceiling (optional, for display purposes).
    /// </summary>
    public const int EloCeiling = 3000;

    /// <summary>
    /// Calculate new ratings after a match.
    /// </summary>
    /// <param name="winnerElo">Winner's current ELO</param>
    /// <param name="loserElo">Loser's current ELO</param>
    /// <returns>Tuple of (newWinnerElo, newLoserElo)</returns>
    public static (int WinnerNew, int LoserNew) CalculateNewRatings(int winnerElo, int loserElo)
    {
        return CalculateNewRatings(winnerElo, loserElo, KFactor);
    }

    /// <summary>
    /// Calculate new ratings with custom K-factor.
    /// </summary>
    public static (int WinnerNew, int LoserNew) CalculateNewRatings(int winnerElo, int loserElo, int kFactor)
    {
        var expectedWinner = GetExpectedScore(winnerElo, loserElo);
        var expectedLoser = 1.0 - expectedWinner;

        // Winner gets actualScore = 1.0, loser gets 0.0
        var winnerChange = (int)Math.Round(kFactor * (1.0 - expectedWinner));
        var loserChange = (int)Math.Round(kFactor * (0.0 - expectedLoser));

        var newWinnerElo = Math.Clamp(winnerElo + winnerChange, EloFloor, EloCeiling);
        var newLoserElo = Math.Clamp(loserElo + loserChange, EloFloor, EloCeiling);

        return (newWinnerElo, newLoserElo);
    }

    /// <summary>
    /// Calculate expected score (probability of winning) for player A against player B.
    /// </summary>
    /// <param name="playerElo">Player A's ELO</param>
    /// <param name="opponentElo">Player B's ELO</param>
    /// <returns>Expected score between 0 and 1</returns>
    public static double GetExpectedScore(int playerElo, int opponentElo)
    {
        return 1.0 / (1.0 + Math.Pow(10, (opponentElo - playerElo) / 400.0));
    }

    /// <summary>
    /// Get the rank tier for a given ELO.
    /// </summary>
    public static RankTier GetTier(int elo)
    {
        return elo switch
        {
            < 800 => RankTier.Bronze,
            < 1000 => RankTier.Silver,
            < 1200 => RankTier.Gold,
            < 1400 => RankTier.Platinum,
            < 1600 => RankTier.Diamond,
            < 1800 => RankTier.Master,
            < 2000 => RankTier.Grandmaster,
            _ => RankTier.Legend
        };
    }

    /// <summary>
    /// Get the division within a tier (1-4, where 1 is highest).
    /// </summary>
    public static int GetDivision(int elo)
    {
        var tier = GetTier(elo);
        var tierFloor = GetTierFloor(tier);
        var tierCeiling = GetTierCeiling(tier);
        var tierRange = tierCeiling - tierFloor;
        var positionInTier = elo - tierFloor;

        // Divide tier into 4 divisions
        var divisionSize = tierRange / 4;
        if (divisionSize == 0) return 1;

        var division = 4 - (positionInTier / divisionSize);
        return Math.Clamp(division, 1, 4);
    }

    /// <summary>
    /// Get ELO floor for a tier.
    /// </summary>
    public static int GetTierFloor(RankTier tier)
    {
        return tier switch
        {
            RankTier.Bronze => 0,
            RankTier.Silver => 800,
            RankTier.Gold => 1000,
            RankTier.Platinum => 1200,
            RankTier.Diamond => 1400,
            RankTier.Master => 1600,
            RankTier.Grandmaster => 1800,
            RankTier.Legend => 2000,
            _ => 0
        };
    }

    /// <summary>
    /// Get ELO ceiling for a tier.
    /// </summary>
    public static int GetTierCeiling(RankTier tier)
    {
        return tier switch
        {
            RankTier.Bronze => 799,
            RankTier.Silver => 999,
            RankTier.Gold => 1199,
            RankTier.Platinum => 1399,
            RankTier.Diamond => 1599,
            RankTier.Master => 1799,
            RankTier.Grandmaster => 1999,
            RankTier.Legend => EloCeiling,
            _ => EloCeiling
        };
    }

    /// <summary>
    /// Format ELO with tier and division for display.
    /// Example: "Gold II (1150)"
    /// </summary>
    public static string FormatRating(int elo)
    {
        var tier = GetTier(elo);
        var division = GetDivision(elo);
        var divisionNumeral = division switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => ""
        };

        // Legend tier doesn't have divisions
        if (tier == RankTier.Legend)
            return $"Legend ({elo})";

        return $"{tier} {divisionNumeral} ({elo})";
    }
}

/// <summary>
/// Rank tiers from lowest to highest.
/// </summary>
public enum RankTier
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3,
    Diamond = 4,
    Master = 5,
    Grandmaster = 6,
    Legend = 7
}
