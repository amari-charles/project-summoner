using Godot;

namespace Fateforged.Data.Emotes;

/// <summary>
/// Bridge node for GDScript to access C# EmotesCatalog.
/// Registered as autoload "EmotesCatalog" in project.godot.
/// </summary>
public partial class EmotesCatalogBridge : Node
{
    public static EmotesCatalogBridge? Instance { get; private set; }

    /// <summary>GDScript-compatible constant for max equipped emotes.
    /// Exposed as property because C# const fields are not visible to GDScript via Godot interop.</summary>
    public int MAX_EQUIPPED_EMOTES => EmotesCatalog.MaxEquippedEmotes;

    private Node? _loc;

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(CacheDependencies));
        GD.Print($"EmotesCatalogBridge: Initialized with {EmotesCatalog.Count} emotes");
    }

    private void CacheDependencies()
    {
        _loc = GetNodeOrNull<Node>("/root/Loc");
    }

    /// <summary>Starter emote IDs as a Godot Array for GDScript.</summary>
    public Godot.Collections.Array<string> StarterEmotes
    {
        get
        {
            var arr = new Godot.Collections.Array<string>();
            foreach (var id in EmotesCatalog.StarterEmoteIds)
                arr.Add(id);
            return arr;
        }
    }

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    public Godot.Collections.Dictionary GetEmote(string emoteId)
    {
        var dict = EmotesCatalog.GetEmoteAsDict(emoteId);
        if (dict.Count == 0)
        {
            GD.PushWarning($"EmotesCatalog.GetEmote: Emote not found: {emoteId}");
        }
        else
        {
            dict["display_name"] = Localize((string)dict["display_name"]);
            dict["description"] = Localize((string)dict["description"]);
        }
        return dict;
    }

    public bool HasEmote(string emoteId)
    {
        return EmotesCatalog.HasEmote(emoteId);
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllEmotes()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var emote in EmotesCatalog.GetAllEmotes())
        {
            result.Add(LocalizedDict(EmotesCatalog.ToDictionary(emote)));
        }
        return result;
    }

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetEmotesByCategory(string category)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var emote in EmotesCatalog.GetEmotesByCategory(category))
        {
            result.Add(LocalizedDict(EmotesCatalog.ToDictionary(emote)));
        }
        return result;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetStarterEmotes()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var emote in EmotesCatalog.GetStarterEmotes())
        {
            result.Add(LocalizedDict(EmotesCatalog.ToDictionary(emote)));
        }
        return result;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetPurchasableEmotes()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var emote in EmotesCatalog.GetPurchasableEmotes())
        {
            result.Add(LocalizedDict(EmotesCatalog.ToDictionary(emote)));
        }
        return result;
    }

    // =========================================================================
    // DISPLAY HELPERS
    // =========================================================================

    public string GetEmoteName(string emoteId)
    {
        var emote = EmotesCatalog.GetEmote(emoteId);
        if (emote == null) return "";
        return Localize(emote.NameKey);
    }

    public int GetEmotePrice(string emoteId)
    {
        var emote = EmotesCatalog.GetEmote(emoteId);
        return emote?.Price ?? 0;
    }

    public bool IsStarterEmote(string emoteId)
    {
        return EmotesCatalog.IsStarterEmote(emoteId);
    }

    // =========================================================================
    // LOCALIZATION HELPERS
    // =========================================================================

    private string Localize(string key)
    {
        if (_loc != null && _loc.HasMethod("t"))
        {
            return (string)_loc.Call("t", key);
        }
        return key;
    }

    private Godot.Collections.Dictionary LocalizedDict(Godot.Collections.Dictionary dict)
    {
        if (dict.Count > 0)
        {
            dict["display_name"] = Localize((string)dict["display_name"]);
            dict["description"] = Localize((string)dict["description"]);
        }
        return dict;
    }
}
