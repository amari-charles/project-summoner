using System;
using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.Meta.Cards;
using Fateforged.Meta.Deck;
using Fateforged.Meta.Summoner;
using Godot;

namespace Fateforged.Session;

public static class BattleSideResolver
{
    private const float DefaultMana = 100f;
    private const float DefaultCastSpeed = 1.0f;

    public static ResolvedBattleSide Resolve(
        Node caller,
        BattleSessionConfig config,
        int localTeam,
        float sceneDefaultMaxHp,
        int maxHandSize,
        Godot.Collections.Array<Resource> sceneFallbackDeck
    )
    {
        var definition = config.GetSide(localTeam);
        var resolved = new ResolvedBattleSide
        {
            Team = localTeam,
            MaxHandSize = maxHandSize,
            Controller = definition.Controller,
            Summoner = CreateDefaultSummoner(sceneDefaultMaxHp),
        };

        ResolveSummoner(caller, config, definition.Summoner, localTeam, resolved);
        ResolveDeck(caller, definition.Deck, resolved, sceneFallbackDeck);
        DrawOpeningHandSummonsOnly(resolved.Deck, maxHandSize);

        return resolved;
    }

    public static ResolvedBattleSide ClientPlaceholder(
        int localTeam,
        float sceneDefaultMaxHp,
        int maxHandSize
    ) =>
        new()
        {
            Team = localTeam,
            MaxHandSize = maxHandSize,
            Summoner = CreateDefaultSummoner(sceneDefaultMaxHp),
            Deck = new ResolvedDeckLoadout { IsDeferred = true },
            Controller = new BattleControllerDefinition { Kind = BattleControllerKind.Network },
        };

    public static Resource? CreateCardFromCatalog(string catalogId)
    {
        var cardDef = CardCatalog.GetCard(catalogId);
        if (cardDef == null)
            return null;
        return Card.FromDefinition(cardDef);
    }

    private static ResolvedSummonerLoadout CreateDefaultSummoner(float sceneDefaultMaxHp) =>
        new()
        {
            Hp = sceneDefaultMaxHp,
            MaxHp = sceneDefaultMaxHp,
            Mana = DefaultMana,
            MaxMana = DefaultMana,
            CastSpeed = DefaultCastSpeed,
        };

    private static void ResolveSummoner(
        Node caller,
        BattleSessionConfig config,
        BattleSummonerDefinition definition,
        int localTeam,
        ResolvedBattleSide resolved
    )
    {
        switch (definition.Source)
        {
            case BattleSideSource.Profile:
                LoadSummonerFromProfile(caller, localTeam, resolved);
                break;
            case BattleSideSource.MultiplayerOpponent:
                if (config.RawConfig == null || !TryLoadOpponentSummonerStats(config.RawConfig, resolved))
                    GD.PushWarning(
                        "[BattleSideResolver] Opponent summoner stats unavailable, using scene defaults"
                    );
                break;
            case BattleSideSource.Authored:
                ApplyAuthoredSummoner(definition, resolved.Summoner);
                break;
            case BattleSideSource.ClientPlaceholder:
            case BattleSideSource.SceneDefault:
                break;
        }
    }

    private static void ApplyAuthoredSummoner(
        BattleSummonerDefinition definition,
        ResolvedSummonerLoadout summoner
    )
    {
        summoner.Id = definition.Id;
        summoner.DisplayName = definition.DisplayName;
        summoner.MaxHp = definition.MaxHp ?? definition.Hp ?? summoner.MaxHp;
        summoner.Hp = definition.Hp ?? summoner.MaxHp;
        summoner.MaxMana = definition.MaxMana ?? summoner.MaxMana;
        summoner.Mana = definition.Mana ?? summoner.MaxMana;
        summoner.CastSpeed = definition.CastSpeed ?? summoner.CastSpeed;
        summoner.DamageBonus = definition.DamageBonus ?? summoner.DamageBonus;
        summoner.DamageReduction = definition.DamageReduction ?? summoner.DamageReduction;
        summoner.SoulStrength = definition.SoulStrength ?? summoner.SoulStrength;
        summoner.ElementalDamageBonuses.Clear();
        foreach (var kvp in definition.ElementalDamageBonuses)
            summoner.ElementalDamageBonuses[kvp.Key] = kvp.Value;
    }

    private static void LoadSummonerFromProfile(
        Node caller,
        int localTeam,
        ResolvedBattleSide resolved
    )
    {
        var summonerSelection = caller.GetNodeOrNull<SummonerSelectionService>(
            "/root/SummonerSelection"
        );
        if (summonerSelection == null)
            return;

        string summonerId = summonerSelection.GetActiveSummonerId();
        if (string.IsNullOrEmpty(summonerId))
            return;

        var summonerInstance = ProfileRepository.Instance?.GetSummonerInstance(
            new SummonerId(summonerId)
        );
        if (summonerInstance == null)
        {
            if (!SummonerCatalog.HasSummoner(summonerId))
                return;
            summonerInstance = new SummonerInstance { SummonerId = new SummonerId(summonerId) };
        }

        var stats = summonerInstance.GetComputedStats();
        ApplyComputedStats(localTeam, resolved, stats);
        resolved.Summoner.Id = summonerId;
    }

    private static bool TryLoadOpponentSummonerStats(
        Godot.Collections.Dictionary rawConfig,
        ResolvedBattleSide resolved
    )
    {
        if (
            rawConfig.TryGetValue("opponent_summoner_data", out var opponentDataVar)
            && opponentDataVar.VariantType == Variant.Type.Dictionary
        )
        {
            var opponentData = opponentDataVar.AsGodotDictionary();
            if (opponentData.Count > 0)
            {
                var instance = DtoConverters.FromSummonerDict(opponentData);
                if (instance != null)
                {
                    ApplyComputedStats(localTeam: 1, resolved, instance.GetComputedStats());
                    resolved.Summoner.Id = (string)instance.SummonerId;
                    return true;
                }
            }
        }

        string opponentSummonerId = rawConfig
            .GetValueOrDefault("opponent_summoner_id", "")
            .ToString();
        if (
            !string.IsNullOrEmpty(opponentSummonerId)
            && SummonerCatalog.HasSummoner(opponentSummonerId)
        )
        {
            var fallbackInstance = new SummonerInstance
            {
                SummonerId = new SummonerId(opponentSummonerId),
            };
            ApplyComputedStats(localTeam: 1, resolved, fallbackInstance.GetComputedStats());
            resolved.Summoner.Id = opponentSummonerId;
            return true;
        }

        return false;
    }

    private static void ApplyComputedStats(
        int localTeam,
        ResolvedBattleSide resolved,
        Dictionary<string, float> stats
    )
    {
        if (stats.Count == 0)
            return;

        var summoner = resolved.Summoner;
        summoner.MaxMana = stats.GetValueOrDefault("max_mana", DefaultMana);
        summoner.Mana = summoner.MaxMana;
        summoner.CastSpeed = stats.GetValueOrDefault("cast_speed", DefaultCastSpeed);
        float health = stats.GetValueOrDefault("health", summoner.MaxHp);
        summoner.MaxHp = health;
        summoner.Hp = health;
        summoner.DamageBonus = stats.GetValueOrDefault("damage_bonus", 0f);
        summoner.DamageReduction = stats.GetValueOrDefault("damage_reduction", 0f);
        summoner.SoulStrength = stats.GetValueOrDefault("soul_strength", 0f);
        summoner.ElementalDamageBonuses.Clear();
        PopulateElementalDamageBonuses(summoner.ElementalDamageBonuses, stats);

        if (localTeam == 0)
        {
            var godotStats = new Godot.Collections.Dictionary();
            foreach (var kvp in stats)
                godotStats[kvp.Key] = kvp.Value;
            resolved.SummonerStats = godotStats;
        }
    }

    private static void PopulateElementalDamageBonuses(
        Dictionary<Element, float> destination,
        Dictionary<string, float> stats
    )
    {
        TrySetElementalBonus(destination, stats, "fire_damage_bonus", Element.Fire);
        TrySetElementalBonus(destination, stats, "water_damage_bonus", Element.Water);
        TrySetElementalBonus(destination, stats, "wind_damage_bonus", Element.Wind);
        TrySetElementalBonus(destination, stats, "earth_damage_bonus", Element.Earth);
        TrySetElementalBonus(destination, stats, "lightning_damage_bonus", Element.Lightning);
        TrySetElementalBonus(destination, stats, "life_damage_bonus", Element.Life);
        TrySetElementalBonus(destination, stats, "death_damage_bonus", Element.Death);
        TrySetElementalBonus(destination, stats, "shadow_damage_bonus", Element.Shadow);
    }

    private static void TrySetElementalBonus(
        Dictionary<Element, float> destination,
        Dictionary<string, float> stats,
        string key,
        Element element
    )
    {
        if (!stats.TryGetValue(key, out float bonus))
            return;
        if (Math.Abs(bonus) <= 0.0001f)
            return;
        destination[element] = bonus;
    }

    private static void ResolveDeck(
        Node caller,
        BattleDeckDefinition definition,
        ResolvedBattleSide resolved,
        Godot.Collections.Array<Resource> sceneFallbackDeck
    )
    {
        switch (definition.Source)
        {
            case BattleDeckSource.Profile:
                LoadDeckFromProfileServices(caller, resolved);
                resolved.Deck.LoadedFromProfile = resolved.Deck.TotalCards > 0;
                break;
            case BattleDeckSource.Authored:
                LoadDeckEntries(definition.Cards, resolved.Deck);
                resolved.Deck.IsDeferred = definition.Deferred;
                break;
            case BattleDeckSource.None:
                resolved.Deck.IsDeferred = true;
                break;
        }

        if (resolved.Deck.TotalCards == 0 && !resolved.Deck.IsDeferred && sceneFallbackDeck.Count > 0)
            resolved.Deck.DeckCards = new Godot.Collections.Array<Resource>(sceneFallbackDeck);
    }

    private static void LoadDeckFromProfileServices(Node caller, ResolvedBattleSide resolved)
    {
        var decksService = caller.GetNodeOrNull<DeckService>("/root/Decks");
        var cardService = caller.GetNodeOrNull<CardService>("/root/CardService");
        if (decksService == null || cardService == null)
            return;

        string deckId = GetSelectedDeckId(decksService);
        if (string.IsNullOrEmpty(deckId))
            return;

        var deck = decksService.GetDeck(deckId);
        if (deck == null)
            return;

        foreach (var instanceId in deck.CardInstanceIds)
        {
            var cardInstance = cardService.GetCard((string)instanceId);
            if (cardInstance == null)
                continue;

            var card = CreateCardFromCatalog((string)cardInstance.CatalogId);
            if (card is Card typedCard)
                typedCard.InstanceId = (string)cardInstance.Id;
            if (card != null)
                resolved.Deck.DeckCards.Add(card);
        }
    }

    private static string GetSelectedDeckId(DeckService decksService)
    {
        var profileData = ProfileRepository.Instance?.GetProfileMetadata();
        if (profileData != null)
        {
            var selectedDeck = profileData.Meta.SelectedDeck;
            if (!string.IsNullOrEmpty(selectedDeck))
                return selectedDeck;
        }

        var decks = decksService.ListDecks();
        if (decks.Length > 0)
            return (string)decks[0].Id;

        return "";
    }

    private static void LoadDeckEntries(
        IReadOnlyList<BattleDeckEntryDefinition> entries,
        ResolvedDeckLoadout deck
    )
    {
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                var card = CreateCardFromCatalog(entry.CatalogId);
                if (card != null)
                    deck.DeckCards.Add(card);
            }
        }
    }

    private static void DrawOpeningHandSummonsOnly(ResolvedDeckLoadout deck, int maxHandSize)
    {
        if (maxHandSize <= 0 || deck.DeckCards.Count == 0)
            return;

        int deckIndex = 0;
        while (deckIndex < deck.DeckCards.Count && deck.HandCards.Count < maxHandSize)
        {
            var cardResource = deck.DeckCards[deckIndex];
            if (!IsSummonCard(cardResource))
            {
                deckIndex++;
                continue;
            }

            deck.HandCards.Add(cardResource);
            deck.DeckCards.RemoveAt(deckIndex);
        }
    }

    private static bool IsSummonCard(Resource cardResource)
    {
        if (cardResource is Card typedCard)
            return typedCard.Type == (int)CardType.Summon;

        if (cardResource is not GodotObject cardObject)
            return false;

        var typeVar = cardObject.Get("Type");
        if (typeVar.VariantType == Variant.Type.Int)
            return typeVar.AsInt32() == (int)CardType.Summon;

        var catalogId = cardObject.Get("CatalogId").AsString();
        if (string.IsNullOrEmpty(catalogId))
            catalogId = cardObject.Get("catalog_id").AsString();
        if (string.IsNullOrEmpty(catalogId))
            return false;

        var def = CardCatalog.GetCard(catalogId);
        return def != null && def.Type == CardType.Summon;
    }
}
