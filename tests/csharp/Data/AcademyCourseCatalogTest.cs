namespace Fateforged.Tests.Data;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Events;
using Fateforged.Data.Rewards;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AcademyCourseCatalogTest
{
    [TestCase]
    public void Semester1_IncludesRequiredMagic101()
    {
        var semester1 = AcademyCourseCatalog.ForSemester(1, 1);
        var magic101 = semester1.FirstOrDefault(course =>
            course.Id == CourseIds.IntroductionToMagic101
        );

        AssertThat(magic101).IsNotNull();
        AssertThat(magic101!.IsRequired).IsTrue();
        AssertThat(magic101.Activities).HasSize(4);
        AssertThat(
                magic101.Activities.All(activity =>
                    activity.ExecutionKind == AcademyActivityExecutionKind.Battle
                )
            )
            .IsTrue();
        AssertThat(
                magic101.Activities.Any(activity => activity.Role == AcademyActivityRole.Assessment)
            )
            .IsTrue();
        var courseRewards = CardGrants(magic101.RewardOffers);
        AssertThat(courseRewards).HasSize(1);
        AssertThat(courseRewards[0].CardId).IsEqual(CardIds.MagicBolt);

        var activityRewards = CardGrants(
            magic101.Activities.SelectMany(activity => activity.RewardOffers)
        );
        AssertThat(activityRewards).HasSize(1);
        AssertThat(activityRewards.Select(reward => reward.CardId))
            .Contains(CardIds.NeutralStarterUnit);
        AssertThat(activityRewards.Select(reward => reward.CardId)).NotContains(CardIds.MagicBolt);
    }

    [TestCase]
    public void Semester1_HasFoundationChoiceBetweenSummonAndSpell()
    {
        var foundationChoices = AcademyCourseCatalog
            .ForSemester(1, 1)
            .Where(course => course.ChoiceGroupId == "year_1_semester_1_foundation")
            .ToArray();

        AssertThat(foundationChoices).HasSize(2);
        AssertThat(foundationChoices.Select(course => course.Id))
            .Contains(CourseIds.SummoningBasics);
        AssertThat(foundationChoices.Select(course => course.Id))
            .Contains(CourseIds.PracticalSpellcraft);

        var practicalSpellcraft = foundationChoices.First(course =>
            course.Id == CourseIds.PracticalSpellcraft
        );
        var spellcraftRewards = CardGrants(practicalSpellcraft.RewardOffers);
        AssertThat(spellcraftRewards).HasSize(1);
        AssertThat(spellcraftRewards[0].CardId).IsEqual(CardIds.ManaBolt);
        AssertThat(
                foundationChoices
                    .SelectMany(course => CardGrants(course.RewardOffers))
                    .Any(reward => reward.CardId == CardIds.Charge)
            )
            .IsFalse();
    }

    [TestCase]
    public void Semester1_ElementIntrosGrantSummonAndSpell()
    {
        var elementCourses = AcademyCourseCatalog
            .ForSemester(1, 1)
            .Where(course => course.ChoiceGroupId == "year_1_semester_1_element")
            .ToArray();

        AssertThat(elementCourses).HasSize(4);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToFire);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToWater);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToEarth);
        AssertThat(elementCourses.Select(course => course.Id)).Contains(CourseIds.IntroToAir);

        foreach (var course in elementCourses)
        {
            var rewards = CardGrants(course.RewardOffers);
            AssertThat(rewards).HasSize(2);
            AssertThat(rewards.All(reward => reward.CardId.HasValue)).IsTrue();
        }
    }

    [TestCase]
    public void Semester2_IncludesAcceptedFirstPassCourses()
    {
        var semester2Ids = AcademyCourseCatalog
            .ForSemester(1, 2)
            .Select(course => course.Id)
            .ToArray();

        AssertThat(semester2Ids).Contains(CourseIds.FoundationsOfMagicII);
        AssertThat(semester2Ids).Contains(CourseIds.IntroductionToEmpowerment);
        AssertThat(semester2Ids).Contains(CourseIds.IntroductionToManaChanneling);
        AssertThat(semester2Ids).Contains(CourseIds.FirePracticumI);
        AssertThat(semester2Ids).Contains(CourseIds.WaterPracticumI);
        AssertThat(semester2Ids).Contains(CourseIds.EarthPracticumI);
        AssertThat(semester2Ids).Contains(CourseIds.AirPracticumI);
    }

    [TestCase]
    public void Magic101Activities_UseLoanerDecksAndStepUpPressure()
    {
        var magic101 = AcademyCourseCatalog
            .ForSemester(1, 1)
            .First(course => course.Id == CourseIds.IntroductionToMagic101);
        var summonPractice = magic101.Activities[0];
        var basicDuel = magic101.Activities[1];
        var spellPractice = magic101.Activities[2];
        var assessment = magic101.Activities.First(activity =>
            activity.Role == AcademyActivityRole.Assessment
        );

        AssertThat(summonPractice.Id).IsEqual("magic_101_summon_practice");
        AssertThat(summonPractice.BattleConfig).IsNotNull();
        AssertThat(summonPractice.BattleConfig!.Biome).IsEqual(BiomeIds.IslandWater);
        AssertThat(summonPractice.Loadout.SuppliedCards.Select(entry => entry.CardId))
            .Contains(CardIds.NeutralStarterUnit);
        AssertThat(summonPractice.BattleConfig.EnemyDeck).IsEmpty();
        AssertThat(summonPractice.BattleConfig.AiType).IsEqual("none");
        AssertThat(summonPractice.BattleConfig.EncounterAi).IsNotNull();
        AssertThat(summonPractice.BattleConfig.EncounterAi!.Preset).IsEqual("scripted_encounter");
        AssertThat(
                summonPractice
                    .BattleConfig.EncounterAi.Rules.SelectMany(rule => rule.Actions)
                    .Select(action => action.CardId)
            )
            .Contains(CardIds.TrainingTarget);
        AssertThat(summonPractice.RewardOffers).IsEmpty();

        AssertThat(basicDuel.Id).IsEqual("magic_101_basic_duel");
        AssertThat(basicDuel.BattleConfig).IsNotNull();
        AssertThat(basicDuel.BattleConfig!.EnemyDeck.Select(entry => entry.CardId))
            .Contains(CardIds.WeakEnemyUnit);
        AssertThat(CardGrants(basicDuel.RewardOffers).Select(reward => reward.CardId))
            .Contains(CardIds.NeutralStarterUnit);

        AssertThat(spellPractice.Id).IsEqual("magic_101_spell_practice");
        AssertThat(spellPractice.BattleConfig).IsNotNull();
        AssertThat(spellPractice.Loadout.SuppliedCards.Select(entry => entry.CardId))
            .Contains(CardIds.MagicBolt);
        AssertThat(spellPractice.BattleConfig!.EnemyDeck).IsEmpty();
        AssertThat(spellPractice.BattleConfig.EncounterAi).IsNotNull();
        AssertThat(
                spellPractice
                    .BattleConfig.EncounterAi!.Rules.SelectMany(rule => rule.Actions)
                    .Select(action => action.CardId)
            )
            .Contains(CardIds.WeakEnemyUnit);
        AssertThat(CardGrants(spellPractice.RewardOffers)).IsEmpty();

        AssertThat(assessment.BattleConfig).IsNotNull();
        AssertThat(assessment.BattleConfig!.AiType).IsEqual("simple");
        AssertThat(assessment.BattleConfig.AiDifficulty).IsEqual(0);
        AssertThat(assessment.BattleConfig.EnemyHp).IsEqual(50f);
        AssertThat(assessment.BattleConfig.EnemyDeck.Select(entry => entry.CardId))
            .Contains(CardIds.WeakEnemyUnit);
        AssertThat(
                assessment.BattleConfig.EnemyDeck.Any(entry =>
                    entry.CardId == CardIds.TrainingTarget
                )
            )
            .IsFalse();
        AssertThat(assessment.Loadout.SuppliedCards.Select(entry => entry.CardId))
            .Contains(CardIds.NeutralStarterUnit);
        AssertThat(assessment.Loadout.SuppliedCards.Select(entry => entry.CardId))
            .Contains(CardIds.MagicBolt);
    }

    [TestCase]
    public void BattleActivities_AlwaysHaveAValidAuthoredBattleSpecification()
    {
        var battleActivities = AcademyCourseCatalog
            .All.SelectMany(course => course.Activities)
            .Where(activity => activity.ExecutionKind == AcademyActivityExecutionKind.Battle)
            .ToArray();

        AssertThat(battleActivities).IsNotEmpty();
        AssertThat(battleActivities.All(activity => activity.BattleConfig != null)).IsTrue();
        AssertThat(battleActivities.All(activity => BiomeIds.IsValid(activity.BattleConfig!.Biome)))
            .IsTrue();
    }

    [TestCase]
    public void PracticalSpellcraftPractice_AuthorsSpellPreparationLimitations()
    {
        var practicalSpellcraft = AcademyCourseCatalog
            .ForSemester(1, 1)
            .First(course => course.Id == CourseIds.PracticalSpellcraft);
        var practice = practicalSpellcraft.Activities.First(activity =>
            activity.Id == "practical_spellcraft_practice"
        );

        AssertThat(practice.Loadout.Rules.HasRules).IsTrue();
        AssertThat(practice.Loadout.Rules.MinSummons).IsEqual(1);
        AssertThat(practice.Loadout.Rules.MinSpells).IsEqual(1);
        AssertThat(practice.Loadout.Rules.MaxDeckSize).IsEqual(12);
        AssertThat(practice.Loadout.Rules.RequiredOwnedCards).IsEmpty();
        AssertThat(practice.Loadout.SuppliedCards.Select(entry => entry.CardId))
            .Contains(CardIds.MagicBolt);
    }

    private static CardRewardGrantDefinition[] CardGrants(
        IEnumerable<RewardOfferDefinition> offers
    ) =>
        offers
            .SelectMany(offer => ((AuthoredRewardOptionSourceDefinition)offer.OptionSource).Options)
            .SelectMany(option => option.Grants)
            .OfType<CardRewardGrantDefinition>()
            .ToArray();

    [TestCase]
    public void Semester2Activities_StepUpWithoutUsingNormalAi()
    {
        var foundations2 = AcademyCourseCatalog
            .ForSemester(1, 2)
            .First(course => course.Id == CourseIds.FoundationsOfMagicII);
        var assessment = foundations2.Activities.First(activity =>
            activity.Role == AcademyActivityRole.Assessment
        );

        AssertThat(assessment.BattleConfig).IsNotNull();
        AssertThat(assessment.BattleConfig!.AiType).IsEqual("simple");
        AssertThat(assessment.BattleConfig.AiDifficulty).IsLessEqual(1);
        AssertThat(assessment.BattleConfig.EnemyDeck).HasSize(2);
        AssertThat(assessment.BattleConfig.AiPlayIntervalMin).IsGreaterEqual(6.0f);
    }
}
