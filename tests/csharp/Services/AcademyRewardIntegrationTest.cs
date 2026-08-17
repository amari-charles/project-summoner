namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Data.Academy;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Campaign.Handlers;
using Fateforged.Meta.Rewards;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class AcademyRewardIntegrationTest
{
    private readonly List<Node> _nodes = [];

    [AfterTest]
    public void Cleanup()
    {
        foreach (var node in _nodes)
            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        _nodes.Clear();
    }

    [TestCase]
    public void URS_C01_C03_C04_C22_C23_AutomaticAndMultipleChoicesBlockUntilClaimed()
    {
        var repo = CreateRepo("academy_universal_combinations");
        var course = new AcademyCourseDefinition
        {
            Id = new CourseId("reward_combinations"),
            IsRequired = true,
            Activities =
            [
                new AcademyCourseActivity
                {
                    Id = "reward_lesson",
                    RewardOffers =
                    [
                        Automatic("automatic_gold", AccountGold("auto", 10)),
                        Choice("choice_one", CampaignFlag("one")),
                        Choice("choice_two", CampaignFlag("two")),
                    ],
                },
                new AcademyCourseActivity { Id = "no_reward_lesson" },
            ],
            RewardOffers =
            [
                Automatic("course_reward", CampaignFlag("course_complete")),
                Choice("course_choice", CampaignFlag("course_choice_complete")),
            ],
        };
        var runtime = UniversalRewardRuntime.Create(repo);
        var handler = new AcademyProgressHandler(repo, () => SummonerIds.Cole, runtime, [course]);
        var goldBefore = repo.GetResources().Gold;

        handler.GetProgress();
        AssertThat(handler.EnrollCourse((string)course.Id)).IsTrue();
        AssertThat(handler.CompleteActivity((string)course.Id, "reward_lesson")).IsTrue();
        AssertThat(repo.GetResources().Gold).IsEqual(goldBefore + 10);
        AssertThat(repo.GetRewardState().PendingSelections).HasSize(2);
        AssertThat(handler.CompleteActivity((string)course.Id, "no_reward_lesson")).IsFalse();

        var pending = repo.GetRewardState().PendingSelections.Keys.ToArray();
        AssertThat(
                handler.ClaimReward(pending[0], [OptionId(repo, pending[0])])["success"].AsBool()
            )
            .IsTrue();
        AssertThat(handler.CompleteActivity((string)course.Id, "no_reward_lesson")).IsFalse();
        AssertThat(
                handler.ClaimReward(pending[1], [OptionId(repo, pending[1])])["success"].AsBool()
            )
            .IsTrue();

        AssertThat(handler.CompleteActivity((string)course.Id, "no_reward_lesson")).IsTrue();
        var progress = repo.GetCampaignProgress(SummonerIds.Cole);
        AssertThat(progress.Academy.CompletedCourses).NotContains(course.Id);
        AssertThat(repo.GetRewardState().PendingSelections).HasSize(1);

        var courseClaimId = repo.GetRewardState().PendingSelections.Keys.Single();
        AssertThat(
                handler
                    .ClaimReward(courseClaimId, [OptionId(repo, courseClaimId)])["success"]
                    .AsBool()
            )
            .IsTrue();

        progress = repo.GetCampaignProgress(SummonerIds.Cole);
        AssertThat(progress.Academy.CompletedCourses).Contains(course.Id);
        AssertThat(progress.Academy.RewardFlags["one"]).IsEqual(1);
        AssertThat(progress.Academy.RewardFlags["two"]).IsEqual(1);
        AssertThat(progress.Academy.RewardFlags["course_complete"]).IsEqual(1);
        AssertThat(progress.Academy.RewardFlags["course_choice_complete"]).IsEqual(1);
        AssertThat(repo.GetResources().Gold).IsEqual(goldBefore + 10);
    }

    [TestCase]
    public void URS_C05_C06_CategoryWaitsUntilEarnedAndExactPreviewNeverRerolls()
    {
        var repo = CreateRepo("academy_preview_boundaries");
        var poolId = new UniversalRewardPoolId("preview_pool");
        var categoryCourse = PoolCourse(
            "category_course",
            poolId,
            RewardPreviewPolicy.CategoryUntilEarned
        );
        var exactCourse = PoolCourse("exact_course", poolId, RewardPreviewPolicy.Exact);
        var firstRuntime = UniversalRewardRuntime.Create(repo, Catalog(poolId, "a", "b", "c"));
        var handler = new AcademyProgressHandler(
            repo,
            () => SummonerIds.Cole,
            firstRuntime,
            [categoryCourse, exactCourse]
        );

        var categoryPreview = FindOffer(handler.GetCourse((string)categoryCourse.Id), "pool_offer");
        AssertThat(categoryPreview["options"].AsGodotArray()).IsEmpty();
        AssertThat(repo.GetRewardState().ResolvedOffers).IsEmpty();

        handler.GetProgress();
        AssertThat(handler.EnrollCourse((string)categoryCourse.Id)).IsTrue();
        AssertThat(handler.CompleteActivity((string)categoryCourse.Id, "lesson")).IsTrue();
        AssertThat(repo.GetRewardState().ResolvedOffers).HasSize(1);
        AssertThat(repo.GetRewardState().PendingSelections).HasSize(1);

        var exactPreview = FindOffer(handler.GetCourse((string)exactCourse.Id), "pool_offer");
        var originalIds = PreviewOptionIds(exactPreview);
        var secondHandler = new AcademyProgressHandler(
            repo,
            () => SummonerIds.Cole,
            UniversalRewardRuntime.Create(repo, Catalog(poolId, "x", "y", "z")),
            [categoryCourse, exactCourse]
        );
        var afterContentChange = FindOffer(
            secondHandler.GetCourse((string)exactCourse.Id),
            "pool_offer"
        );

        AssertThat(originalIds).IsEqual(PreviewOptionIds(afterContentChange));
        AssertThat(originalIds).NotContains("x");
        AssertThat(repo.GetRewardState().RewardSeedBySummoner).HasSize(1);
    }

    private ProfileRepository CreateRepo(string profileId)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var repo = new ProfileRepository { Name = $"RewardRepo_{Guid.NewGuid():N}" };
        tree.Root.AddChild(repo);
        _nodes.Add(repo);
        repo.LoadProfile(profileId);
        repo.ResetProfile();
        return repo;
    }

    private static AcademyCourseDefinition PoolCourse(
        string id,
        UniversalRewardPoolId poolId,
        RewardPreviewPolicy previewPolicy
    ) =>
        new()
        {
            Id = new CourseId(id),
            IsRequired = true,
            Activities =
            [
                new AcademyCourseActivity
                {
                    Id = "lesson",
                    RewardOffers =
                    [
                        new RewardOfferDefinition
                        {
                            Id = new RewardOfferId("pool_offer"),
                            PreviewPolicy = previewPolicy,
                            Selection = new RewardSelectionRule
                            {
                                Mode = RewardSelectionMode.PlayerChoice,
                                ShowCount = 2,
                                ChooseCount = 1,
                            },
                            OptionSource = new PoolRewardOptionSourceDefinition
                            {
                                PoolId = poolId,
                                PreviewCategoryKey = "reward.category.test",
                            },
                        },
                    ],
                },
            ],
        };

    private static RewardContentCatalog Catalog(
        UniversalRewardPoolId poolId,
        params string[] optionIds
    ) =>
        new()
        {
            Pools = ImmutableDictionary<
                UniversalRewardPoolId,
                Fateforged.Data.Rewards.RewardPoolDefinition
            >.Empty.Add(
                poolId,
                new Fateforged.Data.Rewards.RewardPoolDefinition
                {
                    Id = poolId,
                    Options =
                    [
                        .. optionIds.Select(id => new RewardOptionDefinition
                        {
                            Id = new RewardOptionId(id),
                            Grants = [CampaignFlag(id)],
                        }),
                    ],
                }
            ),
        };

    private static RewardOfferDefinition Automatic(string id, RewardGrantDefinition grant) =>
        new()
        {
            Id = new RewardOfferId(id),
            Selection = new RewardSelectionRule(),
            OptionSource = new AuthoredRewardOptionSourceDefinition
            {
                Options =
                [
                    new RewardOptionDefinition
                    {
                        Id = new RewardOptionId($"{id}_option"),
                        Grants = [grant],
                    },
                ],
            },
        };

    private static RewardOfferDefinition Choice(string id, RewardGrantDefinition grant) =>
        Automatic(id, grant) with
        {
            Selection = new RewardSelectionRule
            {
                Mode = RewardSelectionMode.PlayerChoice,
                ShowCount = 1,
                ChooseCount = 1,
            },
        };

    private static ResourceRewardGrantDefinition AccountGold(string id, int amount) =>
        new()
        {
            ResourceId = "gold",
            Amount = amount,
            Target = new RewardOwnershipTarget(RewardOwnershipScope.Account),
        };

    private static AcademyProgressFlagRewardGrantDefinition CampaignFlag(string id) =>
        new()
        {
            FlagId = id,
            Target = new RewardOwnershipTarget(
                RewardOwnershipScope.SummonerCampaign,
                (string)SummonerIds.Cole
            ),
        };

    private static string OptionId(ProfileRepository repo, string claimId) =>
        repo.GetRewardState().ResolvedOffers[claimId].Options[0].Id.Value;

    private static Godot.Collections.Dictionary FindOffer(
        Godot.Collections.Dictionary course,
        string offerId
    ) =>
        course["reward_previews"]
            .AsGodotArray()
            .Select(value => value.AsGodotDictionary())
            .First(offer => offer["offer_id"].AsString() == offerId);

    private static string PreviewOptionIds(Godot.Collections.Dictionary offer) =>
        string.Join(
            ",",
            offer["options"]
                .AsGodotArray()
                .Select(value => value.AsGodotDictionary()["option_id"].AsString())
        );
}
