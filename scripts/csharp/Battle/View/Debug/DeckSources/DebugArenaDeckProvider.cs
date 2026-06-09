using Fateforged.Cards;
using Fateforged.Session;
using Godot;

namespace Fateforged.View.Debug.DeckSources;

/// <summary>
/// Debug Arena deck resolution adapter.
/// </summary>
public sealed class DebugArenaDeckProvider : IDebugArenaDeckProvider
{
    private const string DefaultDebugDeckPath = "res://data/debug/debug_deck.json";
    private const string KeyPlayerSide = "player_side";
    private const string KeyEnemySide = "enemy_side";
    private const string SourceTagCuratedFallback = "curated_fallback";
    private static readonly CardId[] CuratedFallbackCardIds =
    [
        CardIds.FireWisp,
        CardIds.WaterWisp,
        CardIds.WindWisp,
        CardIds.EarthWisp,
        CardIds.Puff,
        CardIds.WaterFrog,
    ];

    private readonly string _debugDeckPath;

    public DebugArenaDeckProvider(string? debugDeckPath = null)
    {
        _debugDeckPath = string.IsNullOrWhiteSpace(debugDeckPath)
            ? DefaultDebugDeckPath
            : debugDeckPath;
    }

    public DebugArenaDeckResolution Resolve(DebugArenaDeckResolveRequest request)
    {
        return request.SourceMode switch
        {
            DebugArenaDeckSourceMode.OverrideThenContextThenFileThenFallback =>
                ResolveOverrideContextFileFallback(request),
            DebugArenaDeckSourceMode.ContextThenFileThenFallback =>
                ResolveContextFileFallback(request),
            _ => ResolveFileBacked(),
        };
    }

    private DebugArenaDeckResolution ResolveFileBacked()
    {
        if (TryLoadDeckFromFile(out var fileDeck))
        {
            return new DebugArenaDeckResolution(
                fileDeck,
                (Godot.Collections.Array)fileDeck.Duplicate(true),
                "debug_file"
            );
        }

        var fallbackDeck = BuildCuratedFallbackDeck();
        return new DebugArenaDeckResolution(
            fallbackDeck,
            (Godot.Collections.Array)fallbackDeck.Duplicate(true),
            SourceTagCuratedFallback
        );
    }

    private DebugArenaDeckResolution ResolveContextFileFallback(DebugArenaDeckResolveRequest request)
    {
        if (TryResolveDecksFromConfig(request.ContextConfig, "context_config", out var resolution))
            return resolution;

        return ResolveFileBacked();
    }

    private DebugArenaDeckResolution ResolveOverrideContextFileFallback(
        DebugArenaDeckResolveRequest request
    )
    {
        if (TryResolveDecksFromConfig(request.OverrideConfig, "override_config", out var resolution))
            return resolution;

        return ResolveContextFileFallback(request);
    }

    private static bool TryResolveDecksFromConfig(
        Godot.Collections.Dictionary config,
        string sourceTag,
        out DebugArenaDeckResolution resolution
    )
    {
        resolution = default;
        if (config.Count == 0)
            return false;

        bool hasPlayer = TryGetNonEmptyDeck(config, KeyPlayerSide, out var playerDeck);
        bool hasEnemy = TryGetNonEmptyDeck(config, KeyEnemySide, out var enemyDeck);
        if (!hasPlayer && !hasEnemy)
            return false;

        var resolvedPlayer = hasPlayer
            ? playerDeck
            : (Godot.Collections.Array)enemyDeck.Duplicate(true);
        var resolvedEnemy = hasEnemy
            ? enemyDeck
            : (Godot.Collections.Array)playerDeck.Duplicate(true);

        resolution = new DebugArenaDeckResolution(resolvedPlayer, resolvedEnemy, sourceTag);
        return true;
    }

    private static bool TryGetNonEmptyDeck(
        Godot.Collections.Dictionary config,
        string key,
        out Godot.Collections.Array deck
    )
    {
        deck = new Godot.Collections.Array();
        if (!config.ContainsKey(key))
            return false;

        var value = config[key];
        if (value.VariantType != Variant.Type.Dictionary)
            return false;

        var side = value.AsGodotDictionary();
        var deckVar = side.GetValueOrDefault("deck", default);
        if (deckVar.VariantType != Variant.Type.Dictionary)
            return false;

        var deckDict = deckVar.AsGodotDictionary();
        var cardsVar = deckDict.GetValueOrDefault("cards", default);
        if (cardsVar.VariantType != Variant.Type.Array)
            return false;

        var parsed = cardsVar.AsGodotArray();
        if (parsed.Count == 0)
            return false;

        deck = (Godot.Collections.Array)parsed.Duplicate(true);
        return true;
    }

    private bool TryLoadDeckFromFile(out Godot.Collections.Array deck)
    {
        deck = new Godot.Collections.Array();
        if (!FileAccess.FileExists(_debugDeckPath))
        {
            GD.PushWarning(
                $"[DebugArenaDeckProvider] Debug deck not found at {_debugDeckPath}; falling back"
            );
            return false;
        }

        using var file = FileAccess.Open(_debugDeckPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning(
                $"[DebugArenaDeckProvider] Failed opening debug deck at {_debugDeckPath}; falling back"
            );
            return false;
        }

        var parsed = Json.ParseString(file.GetAsText());
        if (parsed.VariantType != Variant.Type.Array)
        {
            GD.PushWarning(
                $"[DebugArenaDeckProvider] Invalid debug deck JSON at {_debugDeckPath}; falling back"
            );
            return false;
        }

        var parsedDeck = parsed.AsGodotArray();
        if (parsedDeck.Count == 0)
        {
            GD.PushWarning(
                $"[DebugArenaDeckProvider] Empty debug deck JSON at {_debugDeckPath}; falling back"
            );
            return false;
        }

        deck = parsedDeck;
        return true;
    }

    private static Godot.Collections.Array BuildCuratedFallbackDeck()
    {
        var entries = new Godot.Collections.Array();
        foreach (var cardId in CuratedFallbackCardIds)
        {
            var card = CardCatalog.GetCard(cardId);
            if (card == null || card.Type != CardType.Summon)
                continue;

            entries.Add(
                new Godot.Collections.Dictionary { { "catalog_id", (string)cardId }, { "count", 1 } }
            );
        }

        if (entries.Count == 0)
        {
            GD.PushWarning(
                "[DebugArenaDeckProvider] Curated fallback deck is empty; attempting first summon fallback."
            );
            foreach (var cardDef in CardCatalog.GetAllCardsAsDict())
            {
                if (
                    !cardDef.TryGetValue("card_type", out var cardTypeVar)
                    || cardTypeVar.AsInt32() != (int)CardType.Summon
                )
                    continue;

                string catalogId = cardDef.TryGetValue("catalog_id", out var catalogIdVar)
                    ? catalogIdVar.AsString()
                    : "";
                if (string.IsNullOrEmpty(catalogId))
                    continue;

                entries.Add(
                    new Godot.Collections.Dictionary { { "catalog_id", catalogId }, { "count", 1 } }
                );
                break;
            }
        }

        return entries;
    }
}
