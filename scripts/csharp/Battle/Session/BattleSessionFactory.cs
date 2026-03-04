using System;
using System.Collections.Generic;
using Godot;
using Fateforged.Data.Summoners;
using Fateforged.Domain.Profile.Summoners;
using Fateforged.Infrastructure.Persistence;
using Fateforged.View;

namespace Fateforged.Session;

/// <summary>
/// Result of loading a summoner's battle data (deck cards + summoner stats).
/// Built by BattleSessionFactory, consumed by BattleScene for simulation registration.
/// </summary>
public class SummonerLoadResult
{
    public Godot.Collections.Array<Resource> Deck { get; set; } = new();
    public Godot.Collections.Array<Resource> Hand { get; set; } = new();
    public float Hp { get; set; }
    public float MaxHp { get; set; }
    public float Mana { get; set; } = 100f;
    public float MaxMana { get; set; } = 100f;
    public float CastSpeed { get; set; } = 1.0f;
    public bool IsDeferred { get; set; }

    /// <summary>True if deck was loaded from player profile (cards have instance IDs for XP).</summary>
    public bool LoadedFromProfile { get; set; }

    /// <summary>Summoner computed stats for BattleContext caching (player team only).</summary>
    public Godot.Collections.Dictionary? SummonerStats { get; set; }
}

/// <summary>
/// Extracts deck loading and summoner stat querying from BattleScene.
/// Queries autoloads (Decks, CardService, ProfileRepo, SummonerSelection, SummonerCatalog)
/// and produces typed results for simulation registration.
/// </summary>
public static class BattleSessionFactory
{
    /// <summary>
    /// Load deck and summoner data for a summoner entering battle.
    /// </summary>
    /// <param name="caller">Node used for autoload lookups via GetNodeOrNull.</param>
    /// <param name="config">Typed battle config.</param>
    /// <param name="localTeam">0 = player, 1 = enemy.</param>
    /// <param name="deckLoadStrategy">SummonerVisual's DeckLoadStrategy export.</param>
    /// <param name="defaultMaxHp">SummonerVisual's MaxHpExport.</param>
    /// <param name="maxHandSize">SummonerVisual's MaxHandSize.</param>
    /// <param name="staticDeck">SummonerVisual's StartingDeck export.</param>
    public static SummonerLoadResult LoadSummonerData(
        Node caller,
        BattleSessionConfig config,
        int localTeam,
        DeckLoadStrategy deckLoadStrategy,
        float defaultMaxHp,
        int maxHandSize,
        Godot.Collections.Array<Resource> staticDeck)
    {
        var result = new SummonerLoadResult
        {
            Hp = defaultMaxHp,
            MaxHp = defaultMaxHp,
        };

        var strategy = ResolveStrategy(deckLoadStrategy, localTeam, config);

        switch (strategy)
        {
            case DeckLoadStrategy.Static:
                GD.Print("[BattleSessionFactory] Using STATIC starting deck");
                result.Deck = new Godot.Collections.Array<Resource>(staticDeck);
                break;

            case DeckLoadStrategy.BattleContext:
                GD.Print("[BattleSessionFactory] Loading enemy deck from BattleContext...");
                LoadDeckFromBattleContext(config.RawConfig, result);
                if (result.Deck.Count == 0)
                {
                    GD.PushWarning("[BattleSessionFactory] Failed to load from BattleContext, using STATIC deck");
                    result.Deck = new Godot.Collections.Array<Resource>(staticDeck);
                }
                break;

            case DeckLoadStrategy.Profile:
                GD.Print("[BattleSessionFactory] Loading deck from player profile...");
                LoadDeckFromProfile(caller, config, localTeam, result);
                if (result.Deck.Count == 0)
                {
                    GD.PushWarning("[BattleSessionFactory] Failed to load from profile, using STATIC deck");
                    result.Deck = new Godot.Collections.Array<Resource>(staticDeck);
                }
                else
                {
                    result.LoadedFromProfile = true;
                }
                break;

            case DeckLoadStrategy.Deferred:
                GD.Print("[BattleSessionFactory] Using DEFERRED strategy - deck will be set manually later");
                result.IsDeferred = true;
                break;
        }

        // Split hand from deck
        int drawCount = Math.Min(maxHandSize, result.Deck.Count);
        for (int i = 0; i < drawCount; i++)
        {
            result.Hand.Add(result.Deck[0]);
            result.Deck.RemoveAt(0);
        }

        int totalCards = result.Deck.Count + result.Hand.Count;
        if (totalCards > 0)
            GD.Print($"[BattleSessionFactory] Loaded {totalCards} cards (strategy={strategy})");

        return result;
    }

    /// <summary>Create a Card resource from a catalog ID. Returns null if not found.</summary>
    public static Resource? CreateCardFromCatalog(string catalogId)
    {
        var cardDef = Fateforged.Cards.CardCatalog.GetCard(catalogId);
        if (cardDef == null) return null;
        return Fateforged.Cards.Card.FromDefinition(cardDef);
    }

    // =========================================================================
    // STRATEGY RESOLUTION
    // =========================================================================

    private static DeckLoadStrategy ResolveStrategy(DeckLoadStrategy strategy, int localTeam, BattleSessionConfig config)
    {
        // Auto-correct strategy based on team
        if (localTeam == 0 && strategy == DeckLoadStrategy.BattleContext)
            strategy = DeckLoadStrategy.Profile;
        else if (localTeam == 1 && strategy == DeckLoadStrategy.Profile)
            strategy = DeckLoadStrategy.BattleContext;

        // Auto-detect event_sequence battles with empty enemy deck
        if (localTeam == 1 && strategy == DeckLoadStrategy.BattleContext && config.HasEventSequence)
        {
            if (config.RawConfig.ContainsKey("enemy_deck"))
            {
                var enemyDeckVar = config.RawConfig["enemy_deck"];
                if (enemyDeckVar.VariantType == Variant.Type.Array)
                {
                    var arr = enemyDeckVar.AsGodotArray();
                    if (arr.Count == 0)
                    {
                        GD.Print("[BattleSessionFactory] Battle uses event_sequence with empty enemy_deck - switching to DEFERRED");
                        strategy = DeckLoadStrategy.Deferred;
                    }
                }
            }
        }

        return strategy;
    }

    // =========================================================================
    // DECK LOADING — BATTLE CONTEXT
    // =========================================================================

    private static void LoadDeckFromBattleContext(Godot.Collections.Dictionary config, SummonerLoadResult result)
    {
        if (!config.ContainsKey("enemy_deck")) return;

        var enemyDeckVar = config["enemy_deck"];
        if (enemyDeckVar.VariantType != Variant.Type.Array) return;

        LoadDeckEntries(enemyDeckVar.AsGodotArray(), result);
    }

    // =========================================================================
    // DECK LOADING — PROFILE
    // =========================================================================

    private static void LoadDeckFromProfile(
        Node caller, BattleSessionConfig config, int localTeam, SummonerLoadResult result)
    {
        var rawConfig = config.RawConfig;

        // Check for dev test deck override
        if (rawConfig.ContainsKey("dev_player_deck"))
        {
            GD.Print("[BattleSessionFactory] Loading DEV TEST deck...");
            var devDeckConfig = rawConfig["dev_player_deck"];
            if (devDeckConfig.VariantType == Variant.Type.Array)
                LoadDeckEntries(devDeckConfig.AsGodotArray(), result);
        }
        else
        {
            LoadDeckFromProfileServices(caller, result);
        }

        // Load summoner stats from profile (always, even if deck fell back to static)
        LoadSummonerFromProfile(caller, localTeam, result);
    }

    private static void LoadDeckFromProfileServices(Node caller, SummonerLoadResult result)
    {
        var decksService = caller.GetNodeOrNull("/root/Decks");
        var cardService = caller.GetNodeOrNull("/root/CardService");
        if (decksService == null || cardService == null) return;

        string deckId = GetSelectedDeckId(caller, decksService);
        if (string.IsNullOrEmpty(deckId)) return;

        var deckData = decksService.Call("get_deck", deckId);
        if (deckData.VariantType != Variant.Type.Dictionary) return;

        var deckDict = deckData.AsGodotDictionary();
        var instanceIdsVar = deckDict.GetValueOrDefault("card_instance_ids", new Godot.Collections.Array());
        if (instanceIdsVar.VariantType != Variant.Type.Array) return;

        var ids = instanceIdsVar.AsGodotArray();
        foreach (var instanceId in ids)
        {
            var cardData = cardService.Call("GetCardDict", instanceId.ToString());
            if (cardData.VariantType != Variant.Type.Dictionary) continue;

            var cardDict = cardData.AsGodotDictionary();
            string catalogId = cardDict.GetValueOrDefault("catalog_id", "").ToString();
            if (!string.IsNullOrEmpty(catalogId))
            {
                var card = CreateCardFromCatalog(catalogId);
                if (card != null) result.Deck.Add(card);
            }
        }
    }

    private static string GetSelectedDeckId(Node caller, Node decksService)
    {
        var profileRepo = caller.GetNodeOrNull("/root/ProfileRepo");
        if (profileRepo != null)
        {
            var profile = profileRepo.Call("get_active_profile");
            if (profile.VariantType == Variant.Type.Dictionary)
            {
                var profileDict = profile.AsGodotDictionary();
                var metaVar = profileDict.GetValueOrDefault("meta", new Godot.Collections.Dictionary());
                if (metaVar.VariantType == Variant.Type.Dictionary)
                {
                    var metaDict = metaVar.AsGodotDictionary();
                    var selectedDeck = metaDict.GetValueOrDefault("selected_deck", "");
                    if (selectedDeck.VariantType == Variant.Type.String)
                        return selectedDeck.ToString();
                }
            }
        }

        // Fallback to first available deck
        var deckList = decksService.Call("list_decks");
        if (deckList.VariantType == Variant.Type.Array)
        {
            var list = deckList.AsGodotArray();
            if (list.Count > 0)
            {
                var firstDeck = list[0].AsGodotDictionary();
                return firstDeck.GetValueOrDefault("id", "").ToString();
            }
        }

        return "";
    }

    // =========================================================================
    // SUMMONER STATS
    // =========================================================================

    private static void LoadSummonerFromProfile(Node caller, int localTeam, SummonerLoadResult result)
    {
        var summonerSelection = caller.GetNodeOrNull("/root/SummonerSelection");
        if (summonerSelection == null) return;

        string summonerId = summonerSelection.Call("GetActiveSummonerId").AsString();
        if (string.IsNullOrEmpty(summonerId)) return;

        GD.Print($"[BattleSessionFactory] Active summoner ID: '{summonerId}'");

        // Get summoner instance from profile, or create default if not found
        var summonerInstance = ProfileRepository.Instance?.GetSummonerInstance(new SummonerId(summonerId));
        if (summonerInstance == null)
        {
            // Create default instance for this summoner if no profile data exists
            if (!SummonerCatalog.HasSummoner(summonerId)) return;
            summonerInstance = new SummonerInstance { SummonerId = new SummonerId(summonerId) };
        }

        var stats = summonerInstance.GetComputedStats();
        if (stats.Count == 0) return;

        result.MaxMana = stats.GetValueOrDefault("max_mana", 100.0f);
        result.Mana = result.MaxMana;
        result.CastSpeed = stats.GetValueOrDefault("cast_speed", 1.0f);
        float health = stats.GetValueOrDefault("health", 300.0f);
        result.MaxHp = health;
        result.Hp = health;

        if (localTeam == 0)
        {
            var godotStats = new Godot.Collections.Dictionary();
            foreach (var kvp in stats)
                godotStats[kvp.Key] = kvp.Value;
            result.SummonerStats = godotStats;
        }

        GD.Print($"[BattleSessionFactory] Applied summoner bonuses — Max HP: {result.MaxHp:F0}, Max Mana: {result.MaxMana:F0}, Cast Speed: {result.CastSpeed:F2}");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static void LoadDeckEntries(Godot.Collections.Array entries, SummonerLoadResult result)
    {
        foreach (var entry in entries)
        {
            if (entry.VariantType != Variant.Type.Dictionary) continue;
            var entryDict = entry.AsGodotDictionary();
            string catalogId = entryDict.GetValueOrDefault("catalog_id", "").ToString();
            int count = (int)entryDict.GetValueOrDefault("count", 1);
            for (int i = 0; i < count; i++)
            {
                var card = CreateCardFromCatalog(catalogId);
                if (card != null) result.Deck.Add(card);
            }
        }
    }
}
