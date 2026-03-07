using System;

namespace Fateforged.Multiplayer.Ranking;

/// <summary>
/// ELO rating calculation for ranked matches.
/// Uses standard ELO formula with static K-factor of 32.
/// </summary>
public static class EloCalculator
{
    /// <summary>
    /// Starting ELO for new players (Mage floor).
    /// </summary>
    public const int StartingElo = 1200;

    /// <summary>
    /// K-factor determines how much ratings change per match.
    /// Static K=32 for all players.
    /// </summary>
    public const int KFactor = 32;

    /// <summary>
    /// Minimum ELO floor to prevent negative ratings.
    /// </summary>
    public const int EloFloor = 100;

    /// <summary>
    /// Maximum ELO ceiling (for display/clamping purposes).
    /// </summary>
    public const int EloCeiling = 3000;

    /// <summary>
    /// Calculate new ratings after a match.
    /// </summary>
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
    public static double GetExpectedScore(int playerElo, int opponentElo)
    {
        return 1.0 / (1.0 + Math.Pow(10, (opponentElo - playerElo) / 400.0));
    }

    /// <summary>
    /// Get the rank tier for a given ELO (purely ELO-based, never returns Fateforged).
    /// </summary>
    public static RankTier GetTier(int elo)
    {
        return elo switch
        {
            < 800 => RankTier.Unbound,
            < 1000 => RankTier.Apprentice,
            < 1200 => RankTier.Adept,
            < 1400 => RankTier.Mage,
            < 1600 => RankTier.Archmage,
            _ => RankTier.Sage
        };
    }

    /// <summary>
    /// Get the display tier, accounting for Fateforged (top 20 leaderboard).
    /// UI code should call this instead of GetTier() for display.
    /// </summary>
    public static RankTier GetDisplayTier(int elo, bool isTopTwenty)
    {
        return isTopTwenty ? RankTier.Fateforged : GetTier(elo);
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
            RankTier.Unbound => 0,
            RankTier.Apprentice => 800,
            RankTier.Adept => 1000,
            RankTier.Mage => 1200,
            RankTier.Archmage => 1400,
            RankTier.Sage => 1600,
            RankTier.Fateforged => 1600, // Same as Sage (leaderboard-based, not ELO-based)
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
            RankTier.Unbound => 799,
            RankTier.Apprentice => 999,
            RankTier.Adept => 1199,
            RankTier.Mage => 1399,
            RankTier.Archmage => 1599,
            RankTier.Sage => EloCeiling,
            RankTier.Fateforged => EloCeiling,
            _ => EloCeiling
        };
    }

    /// <summary>
    /// Format ELO with tier and division for display.
    /// Example: "Mage II (1250)"
    /// </summary>
    public static string FormatRating(int elo)
    {
        return FormatRating(elo, false);
    }

    /// <summary>
    /// Format ELO with tier and division for display, with Fateforged support.
    /// </summary>
    public static string FormatRating(int elo, bool isTopTwenty)
    {
        var tier = GetDisplayTier(elo, isTopTwenty);
        var division = GetDivision(elo);
        var divisionNumeral = division switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => ""
        };

        // Fateforged tier doesn't have divisions
        if (tier == RankTier.Fateforged)
            return $"Fateforged ({elo})";

        return $"{tier} {divisionNumeral} ({elo})";
    }
}

/// <summary>
/// Rank tiers from lowest to highest.
/// Fateforged is exclusive to the top 20 on the leaderboard.
/// </summary>
public enum RankTier
{
    Unbound = 0,
    Apprentice = 1,
    Adept = 2,
    Mage = 3,
    Archmage = 4,
    Sage = 5,
    Fateforged = 6
}
