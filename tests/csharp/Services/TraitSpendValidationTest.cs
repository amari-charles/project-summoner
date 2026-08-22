namespace Fateforged.Tests.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Summoner;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class TraitSpendValidationTest
{
    private readonly List<Node> _createdNodes = [];

    [AfterTest]
    public void Cleanup()
    {
        for (int i = _createdNodes.Count - 1; i >= 0; i--)
        {
            var node = _createdNodes[i];
            if (!GodotObject.IsInstanceValid(node))
                continue;

            node.GetParent()?.RemoveChild(node);
            node.Free();
        }

        _createdNodes.Clear();
    }

    [TestCase]
    public void CardService_GetCardTraitDict_ResolvesUnifiedTrait()
    {
        var repo = CreateRepo("trait_spend_validation_trait_dict");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var dict = cardService.GetCardTraitDict(TraitIds.Power);

        AssertThat(dict.Count).IsGreater(0);
        AssertThat(dict["id"].AsString()).IsEqual((string)TraitIds.Power);
        AssertThat(dict["name"].AsString()).IsNotEmpty();
        AssertThat(dict["description"].AsString()).IsNotEmpty();
        AssertThat(dict["summary_short"].AsString()).IsNotEmpty();
    }

    [TestCase]
    public void CardService_SpendCardTraitPoint_RejectsUnknownAndIneligibleTraits()
    {
        var repo = CreateRepo("trait_spend_validation_card_spend");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(instanceId)).IsFalse();
        AssertThat(
                repo.UpdateCard(CardInstanceId.FromString(instanceId), new CardUpdate { Level = 2 })
            )
            .IsTrue();

        cardService.GrantCardTraitPoints(instanceId, 2, "test");
        AssertThat(cardService.GetCardUnspentTraitPoints(instanceId)).IsEqual(2);

        AssertThat(cardService.SpendCardTraitPoint(instanceId, "not_a_real_trait")).IsFalse();
        AssertThat(cardService.GetCardUnspentTraitPoints(instanceId)).IsEqual(2);

        AssertThat(cardService.SpendCardTraitPoint(instanceId, TraitIds.ColeSoulStrengthI))
            .IsFalse();
        AssertThat(cardService.GetCardUnspentTraitPoints(instanceId)).IsEqual(2);

        AssertThat(cardService.SpendCardTraitPoint(instanceId, TraitIds.FireWispTwinFlame))
            .IsTrue();
        AssertThat(cardService.GetCardUnspentTraitPoints(instanceId)).IsEqual(1);
    }

    [TestCase]
    public void CardService_RollCardTraitOffers_ProvidesEligibleOptionsForPendingLevelUp()
    {
        var repo = CreateRepo("trait_spend_validation_card_roll");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(instanceId)).IsFalse();
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(instanceId),
                    new CardUpdate
                    {
                        Level = 1,
                        Xp = 999,
                        UnspentTraitPoints = 0,
                    }
                )
            )
            .IsTrue();

        var offers = cardService.RollCardTraitOffers(instanceId, 3);
        AssertThat(offers.Count).IsGreater(0);

        var allowedTraitIds = new HashSet<string>
        {
            TraitIds.FireWispTwinFlame,
            TraitIds.FireWispCondensedFlame,
        };

        foreach (var offer in offers)
        {
            var traitId = offer["trait_id"].AsString();
            AssertThat(string.IsNullOrWhiteSpace(traitId)).IsFalse();
            AssertThat(allowedTraitIds.Contains(traitId)).IsTrue();
            AssertThat(offer["summary_short"].AsString()).IsNotEmpty();
        }
    }

    [TestCase]
    public void CardService_SpellCards_DoNotReceiveSummonTraitOffers()
    {
        var repo = CreateRepo("trait_spend_validation_spell_roll");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = cardService.GrantCard(CardIds.ManaBolt, "common");
        AssertThat(string.IsNullOrWhiteSpace(instanceId)).IsFalse();
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(instanceId),
                    new CardUpdate
                    {
                        Level = 2,
                        Xp = 999,
                        UnspentTraitPoints = 1,
                    }
                )
            )
            .IsTrue();

        var offers = cardService.RollCardTraitOffers(instanceId, 3);
        AssertThat(offers.Count).IsEqual(0);

        AssertThat(cardService.SpendCardTraitPoint(instanceId, TraitIds.Swiftness)).IsFalse();
        AssertThat(cardService.GetCardUnspentTraitPoints(instanceId)).IsEqual(1);
    }

    [TestCase]
    public void CardService_GetCardTraitDict_UsesCompactSummaryCopy()
    {
        var repo = CreateRepo("trait_spend_validation_compact_summary");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var dict = cardService.GetCardTraitDict(TraitIds.Power);
        AssertThat(dict.Count).IsGreater(0);
        AssertThat(dict["summary_short"].AsString()).IsEqual("+6% Attack Damage");
    }

    [TestCase]
    public void CardService_GetEffectiveStatsDict_AppliesTraitMultipliers()
    {
        var repo = CreateRepo("trait_spend_validation_effective_stats");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(instanceId)).IsFalse();
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(instanceId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        var baselineStats = cardService.GetEffectiveStatsDict(instanceId);
        AssertThat(baselineStats.ContainsKey("attack_damage")).IsTrue();
        var baseDamage = (float)baselineStats["attack_damage"].AsDouble();

        AssertThat(cardService.SpendCardTraitPoint(instanceId, TraitIds.FireWispCondensedFlame))
            .IsTrue();

        var effectiveStats = cardService.GetEffectiveStatsDict(instanceId);
        AssertThat(effectiveStats.ContainsKey("attack_damage")).IsTrue();

        var effectiveDamage = (float)effectiveStats["attack_damage"].AsDouble();
        var expectedDamage = baseDamage * 1.15f;

        AssertThat(Math.Abs(effectiveDamage - expectedDamage)).IsLess(0.01f);
    }

    [TestCase]
    public void CardService_GetTraitSpawnCountBonus_AppliesCoreSpawnCountAdd()
    {
        var repo = CreateRepo("trait_spend_validation_effective_stats_adds");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(instanceId)).IsFalse();
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(instanceId),
                    new CardUpdate { Level = 2, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        AssertThat(cardService.GetTraitSpawnCountBonus(instanceId)).IsEqual(0);

        AssertThat(cardService.SpendCardTraitPoint(instanceId, TraitIds.FireWispTwinFlame))
            .IsTrue();

        AssertThat(cardService.GetTraitSpawnCountBonus(instanceId)).IsEqual(1);
    }

    [TestCase]
    public void SummonerProgressionService_SpendTraitPoint_ValidatesCatalogAndEligibility()
    {
        var repo = CreateRepo("trait_spend_validation_summoner_spend");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 2;
        repo.SaveSummonerInstance(summoner);

        service.GrantTraitPoints(summonerId, 2, "test");
        AssertThat(service.GetUnspentTraitPoints(summonerId)).IsEqual(2);

        AssertThat(service.SpendTraitPoint(summonerId, "not_a_real_trait")).IsFalse();
        AssertThat(service.GetUnspentTraitPoints(summonerId)).IsEqual(2);

        AssertThat(service.SpendTraitPoint(summonerId, TraitIds.SeleneHealthI)).IsFalse();
        AssertThat(service.GetUnspentTraitPoints(summonerId)).IsEqual(2);

        AssertThat(service.SpendTraitPoint(summonerId, TraitIds.ColeSoulStrengthI)).IsTrue();
        AssertThat(service.GetUnspentTraitPoints(summonerId)).IsEqual(1);
    }

    [TestCase]
    public void CardService_SpendCardTraitPoint_RejectsMissingPrerequisites()
    {
        var repo = CreateRepo("trait_spend_validation_missing_prereq");
        var cardService = CreateNode<CardService>();
        cardService.InitForTesting(repo);

        var instanceId = cardService.GrantCard(CardIds.FireWisp, "common");
        AssertThat(string.IsNullOrWhiteSpace(instanceId)).IsFalse();
        AssertThat(
                repo.UpdateCard(
                    CardInstanceId.FromString(instanceId),
                    new CardUpdate { Level = 4, UnspentTraitPoints = 1 }
                )
            )
            .IsTrue();

        AssertThat(cardService.SpendCardTraitPoint(instanceId, TraitIds.FireWispDancingEmbers))
            .IsFalse();
        AssertThat(cardService.GetCardUnspentTraitPoints(instanceId)).IsEqual(1);

        var card = repo.GetCard(CardInstanceId.FromString(instanceId));
        AssertThat(card).IsNotNull();
        AssertThat(
                card!.Traits.Contains(CardTraitId.FromString(TraitIds.FireWispDancingEmbers))
            )
            .IsFalse();
    }

    [TestCase]
    public void SummonerProgressionService_RollTraitOffers_ReturnsEligibleTraits()
    {
        var repo = CreateRepo("trait_spend_validation_summoner_roll");
        var service = CreateNode<SummonerProgressionService>();
        service.InitForTesting(repo);

        var summonerId = EnsureUnlockedSummoner(repo, SummonerIds.Cole);
        var summoner = repo.GetSummonerInstance(summonerId);
        AssertThat(summoner).IsNotNull();
        summoner!.Level = 2;
        summoner.UnspentTraitPoints = 1;
        repo.SaveSummonerInstance(summoner);

        var offers = service.RollTraitOffers(summonerId, 3);
        AssertThat(offers.Count).IsGreater(0);

        var allowedTraitIds = new HashSet<string>
        {
            TraitIds.ColeSoulStrengthI,
            TraitIds.ColeCastSpeedI,
        };

        foreach (var offer in offers)
        {
            var traitId = offer["trait_id"].AsString();
            AssertThat(string.IsNullOrWhiteSpace(traitId)).IsFalse();
            var trait = TraitCatalog.GetTrait(traitId);
            AssertThat(trait).IsNotNull();
            AssertThat(allowedTraitIds.Contains(traitId)).IsTrue();
            AssertThat(trait!.Tags.Contains(TraitTags.Summoner)).IsTrue();
            AssertThat(trait.AcquisitionMode).IsEqual(TraitAcquisitionMode.LevelUpOffer);
        }
    }

    private ProfileRepository CreateRepo(string profileId)
    {
        var repo = CreateNode<ProfileRepository>();
        repo.LoadProfile(new ProfileId(profileId));
        repo.ResetProfile();
        return repo;
    }

    private T CreateNode<T>()
        where T : Node, new()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var root = tree.Root;

        var node = new T { Name = $"{typeof(T).Name}_TraitSpendValidation_{Guid.NewGuid():N}" };
        root.AddChild(node);
        _createdNodes.Add(node);
        return node;
    }

    private static SummonerId EnsureUnlockedSummoner(IProfileRepository repo, SummonerId candidate)
    {
        if (!repo.IsSummonerUnlocked(candidate))
            repo.UnlockSummoner(candidate);

        if (repo.GetSummonerInstance(candidate) == null)
        {
            repo.SaveSummonerInstance(new SummonerInstance { SummonerId = candidate });
        }

        return candidate;
    }
}
