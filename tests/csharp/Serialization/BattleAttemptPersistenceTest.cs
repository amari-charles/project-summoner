namespace Fateforged.Tests.Serialization;

using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Rewards;
using Fateforged.Domain.Progression;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class BattleAttemptPersistenceTest
{
    [TestCase]
    public void BPA_C08_D01_D03_AttemptAndCompletionShapesRoundTrip()
    {
        var progress = new CampaignProgress
        {
            ActiveBattleAttempt = new BattleAttempt
            {
                AttemptId = new BattleAttemptId("attempt"),
                SummonerId = new SummonerId("summoner_cole"),
                CampaignId = new CampaignId("campaign"),
                BattleId = new BattleId("battle"),
                DeckCardInstanceIds = [new CardInstanceId("card-instance")],
                CardXpReward = 12,
                SummonerXpReward = 34,
                FirstClearRewardSnapshots =
                [
                    new ResolvedRewardOfferSnapshot
                    {
                        ClaimId = new RewardClaimId("first-clear-claim"),
                        OfferId = new RewardOfferId("first-clear-offer"),
                        Source = new RewardSourceContext
                        {
                            SourceType = "campaign_battle_first_clear",
                            SourceId = "campaign/battle",
                        },
                        SummonerId = new SummonerId("summoner_cole"),
                        SelectionMode = RewardSelectionMode.PlayerChoice,
                        ChooseCount = 1,
                        Options =
                        [
                            new RewardOptionDefinition
                            {
                                Id = new RewardOptionId("card-option"),
                                Grants =
                                [
                                    new CardRewardGrantDefinition
                                    {
                                        Target = new RewardOwnershipTarget(
                                            RewardOwnershipScope.Summoner,
                                            "summoner_cole"
                                        ),
                                        CardId = CardIds.Puff,
                                    },
                                ],
                            },
                        ],
                    },
                ],
                StartedAtUnixSeconds = 100,
            },
            BattleAttemptCompletions = new Dictionary<string, BattleAttemptCompletion>
            {
                ["old-attempt"] = new BattleAttemptCompletion
                {
                    AttemptId = new BattleAttemptId("old-attempt"),
                    Outcome = BattleTerminalOutcome.Victory,
                    CompletedAtUnixSeconds = 200,
                    ClaimIds = [new RewardClaimId("xp-claim")],
                    PendingClaimIds = [new RewardClaimId("choice-claim")],
                },
            },
        };

        var restored = DtoConverters.FromCampaignDict(DtoConverters.ToDict(progress));

        AssertThat(restored).IsNotNull();
        AssertThat(restored!.ActiveBattleAttempt).IsNotNull();
        AssertThat(restored.ActiveBattleAttempt!.AttemptId.Value).IsEqual("attempt");
        AssertThat(restored.ActiveBattleAttempt.DeckCardInstanceIds).HasSize(1);
        AssertThat(restored.ActiveBattleAttempt.FirstClearRewardSnapshots).HasSize(1);
        AssertThat(restored.ActiveBattleAttempt.FirstClearRewardSnapshots[0].Options[0].Id.Value)
            .IsEqual("card-option");
        AssertThat(restored.BattleAttemptCompletions.ContainsKey("old-attempt")).IsTrue();
        AssertThat(restored.BattleAttemptCompletions["old-attempt"].ClaimIds[0].Value)
            .IsEqual("xp-claim");
    }
}
