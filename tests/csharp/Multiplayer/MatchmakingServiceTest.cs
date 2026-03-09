namespace Fateforged.Tests.Multiplayer;

using GdUnit4;
using System.Collections.Generic;
using Fateforged.Multiplayer.Matchmaking;
using Fateforged.Multiplayer.Ranking;
using static GdUnit4.Assertions;

[TestSuite]
public class MatchmakingServiceTest
{
    [TestCase]
    public void ResolveQueueRating_UsesServiceRatingWhenValid()
    {
        int rating = MatchmakingService.ResolveQueueRating(1425);
        AssertThat(rating).IsEqual(1425);
    }

    [TestCase]
    public void ResolveQueueRating_FallsBackToStartingEloForInvalid()
    {
        AssertThat(MatchmakingService.ResolveQueueRating(null)).IsEqual(EloCalculator.StartingElo);
        AssertThat(MatchmakingService.ResolveQueueRating(0)).IsEqual(EloCalculator.StartingElo);
        AssertThat(MatchmakingService.ResolveQueueRating(-10)).IsEqual(EloCalculator.StartingElo);
    }

    [TestCase]
    public void ResolveOpponentInfo_UsesParticipantMetadata()
    {
        var participants = new List<MatchmakingService.MatchParticipantMetadata>
        {
            new MatchmakingService.MatchParticipantMetadata { UserId = "local", Username = "Local", SummonerId = "terra", Rating = 1300 },
            new MatchmakingService.MatchParticipantMetadata { UserId = "opponent", Username = "Rival", SummonerId = "ignis", Rating = 1488 },
        };

        var info = MatchmakingService.ResolveOpponentInfo("local", new[] { "local", "opponent" }, participants);

        AssertThat(info.UserId).IsEqual("opponent");
        AssertThat(info.Username).IsEqual("Rival");
        AssertThat(info.SummonerId).IsEqual("ignis");
        AssertThat(info.Rating).IsEqual(1488);
    }

    [TestCase]
    public void ResolveOpponentInfo_FallsBackWhenParticipantDataMissing()
    {
        var participants = new List<MatchmakingService.MatchParticipantMetadata>
        {
            new MatchmakingService.MatchParticipantMetadata { UserId = "local", Username = "Local", SummonerId = "terra", Rating = 1300 },
        };

        var info = MatchmakingService.ResolveOpponentInfo("local", new[] { "local", "opponent" }, participants);

        AssertThat(info.UserId).IsEqual("opponent");
        AssertThat(info.Username).IsEqual("Opponent");
        AssertThat(info.SummonerId).IsEqual("ignis");
        AssertThat(info.Rating).IsEqual(EloCalculator.StartingElo);
    }
}
