using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Fateforged.Data.Emotes;

/// <summary>
/// Central registry of all emote definitions.
/// Provides type-safe emote lookup and query methods.
/// GDScript can call this via the EmotesCatalogBridge autoload.
/// </summary>
public static class EmotesCatalog
{
    public const int MaxEquippedEmotes = 4;

    public static readonly string[] StarterEmoteIds = ["emote_hello", "emote_good_game"];

    private static readonly Dictionary<string, EmoteDefinition> _emotes = new()
    {
        // Starter emotes (free)
        ["emote_hello"] = new EmoteDefinition
        {
            Id = "emote_hello",
            NameKey = "emote.emote_hello.name",
            DescriptionKey = "emote.emote_hello.description",
            Price = 0,
            Rarity = "common",
            Category = "neutral"
        },
        ["emote_good_game"] = new EmoteDefinition
        {
            Id = "emote_good_game",
            NameKey = "emote.emote_good_game.name",
            DescriptionKey = "emote.emote_good_game.description",
            Price = 0,
            Rarity = "common",
            Category = "celebration"
        },

        // Reaction emotes (purchasable)
        ["emote_laugh"] = new EmoteDefinition
        {
            Id = "emote_laugh",
            NameKey = "emote.emote_laugh.name",
            DescriptionKey = "emote.emote_laugh.description",
            Price = 150,
            Rarity = "common",
            Category = "reaction"
        },
        ["emote_shocked"] = new EmoteDefinition
        {
            Id = "emote_shocked",
            NameKey = "emote.emote_shocked.name",
            DescriptionKey = "emote.emote_shocked.description",
            Price = 150,
            Rarity = "common",
            Category = "reaction"
        },
        ["emote_thinking"] = new EmoteDefinition
        {
            Id = "emote_thinking",
            NameKey = "emote.emote_thinking.name",
            DescriptionKey = "emote.emote_thinking.description",
            Price = 200,
            Rarity = "rare",
            Category = "neutral"
        },

        // Taunt emotes (purchasable)
        ["emote_taunt"] = new EmoteDefinition
        {
            Id = "emote_taunt",
            NameKey = "emote.emote_taunt.name",
            DescriptionKey = "emote.emote_taunt.description",
            Price = 250,
            Rarity = "rare",
            Category = "taunt"
        },
        ["emote_confident"] = new EmoteDefinition
        {
            Id = "emote_confident",
            NameKey = "emote.emote_confident.name",
            DescriptionKey = "emote.emote_confident.description",
            Price = 300,
            Rarity = "epic",
            Category = "taunt"
        },

        // Celebration emotes (purchasable)
        ["emote_victory"] = new EmoteDefinition
        {
            Id = "emote_victory",
            NameKey = "emote.emote_victory.name",
            DescriptionKey = "emote.emote_victory.description",
            Price = 350,
            Rarity = "epic",
            Category = "celebration"
        }
    };

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    public static EmoteDefinition? GetEmote(string id)
    {
        return _emotes.GetValueOrDefault(id);
    }

    public static bool HasEmote(string id)
    {
        return _emotes.ContainsKey(id);
    }

    public static EmoteDefinition[] GetAllEmotes()
    {
        return [.. _emotes.Values];
    }

    public static int Count => _emotes.Count;

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    public static EmoteDefinition[] GetEmotesByCategory(string category)
    {
        return _emotes.Values.Where(e => e.Category == category).ToArray();
    }

    public static EmoteDefinition[] GetStarterEmotes()
    {
        return StarterEmoteIds
            .Where(id => _emotes.ContainsKey(id))
            .Select(id => _emotes[id])
            .ToArray();
    }

    public static EmoteDefinition[] GetPurchasableEmotes()
    {
        return _emotes.Values.Where(e => e.Price > 0).ToArray();
    }

    public static bool IsStarterEmote(string id)
    {
        return StarterEmoteIds.Contains(id);
    }

    // =========================================================================
    // GODOT DICTIONARY CONVERSION (for GDScript interop)
    // =========================================================================

    public static Godot.Collections.Dictionary ToDictionary(EmoteDefinition emote)
    {
        return new Godot.Collections.Dictionary
        {
            ["id"] = emote.Id,
            ["display_name"] = emote.NameKey,
            ["description"] = emote.DescriptionKey,
            ["icon_path"] = emote.IconPath,
            ["animation_path"] = emote.AnimationPath,
            ["sound_path"] = emote.SoundPath,
            ["price"] = emote.Price,
            ["rarity"] = emote.Rarity,
            ["category"] = emote.Category
        };
    }

    public static Godot.Collections.Dictionary GetEmoteAsDict(string id)
    {
        var emote = GetEmote(id);
        return emote != null ? ToDictionary(emote) : new Godot.Collections.Dictionary();
    }
}
