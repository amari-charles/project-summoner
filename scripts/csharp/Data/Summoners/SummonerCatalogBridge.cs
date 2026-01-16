using Godot;

namespace ProjectSummoner.Data.Summoners;

/// <summary>
/// Bridge node for GDScript to access C# SummonerCatalog.
/// Registered as autoload "SummonerCatalogCS" in project.godot.
/// Wraps static SummonerCatalog methods as instance methods for GDScript compatibility.
/// Note: GDScript can call these PascalCase methods directly - Godot 4 auto-converts snake_case calls.
/// </summary>
public partial class SummonerCatalogBridge : Node
{
    public static SummonerCatalogBridge? Instance { get; private set; }

    private Node? _loc;

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(CacheDependencies));
        GD.Print($"SummonerCatalogBridge: Initialized with {SummonerCatalog.Count} summoners");
    }

    private void CacheDependencies()
    {
        _loc = GetNodeOrNull<Node>("/root/Loc");
    }

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a summoner as dictionary by ID. Returns empty dict if not found.</summary>
    public Godot.Collections.Dictionary GetSummoner(string summonerId)
    {
        return SummonerCatalog.GetSummonerAsDict(summonerId);
    }

    /// <summary>Check if a summoner exists in the catalog.</summary>
    public bool HasSummoner(string summonerId)
    {
        return SummonerCatalog.HasSummoner(summonerId);
    }

    /// <summary>Get all summoner IDs.</summary>
    public string[] GetAllSummonerIds()
    {
        return SummonerCatalog.GetAllSummonerIds();
    }

    /// <summary>Get summoner count.</summary>
    public int GetSummonerCount()
    {
        return SummonerCatalog.Count;
    }

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    /// <summary>Get all summoners as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> ListAllSummoners()
    {
        return SummonerCatalog.GetAllSummonersAsDict();
    }

    /// <summary>Get starting summoners as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetStartingSummoners()
    {
        return SummonerCatalog.GetStartingSummonersAsDict();
    }

    /// <summary>Get random pool summoners as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetRandomPoolSummoners()
    {
        return SummonerCatalog.GetRandomPoolSummonersAsDict();
    }

    /// <summary>Get purchasable summoners as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetPurchasableSummoners()
    {
        return SummonerCatalog.GetPurchasableSummonersAsDict();
    }

    // =========================================================================
    // DISPLAY HELPERS
    // =========================================================================

    /// <summary>Get localized summoner name.</summary>
    public string GetSummonerName(string summonerId)
    {
        var summoner = SummonerCatalog.GetSummoner(summonerId);
        if (summoner == null) return "";

        if (_loc != null && _loc.HasMethod("t"))
        {
            return (string)_loc.Call("t", summoner.NameKey);
        }
        return summoner.NameKey;
    }

    /// <summary>Get summoner element as integer (for GDScript ElementRegistry compatibility).</summary>
    public int GetSummonerElementId(string summonerId)
    {
        var summoner = SummonerCatalog.GetSummoner(summonerId);
        if (summoner == null) return 0; // Neutral

        return SummonerCatalog.ElementToGdElementId(summoner.ElementalAffinity);
    }

    /// <summary>Get innate trait IDs for a summoner.</summary>
    public string[] GetInnateTraitIds(string summonerId)
    {
        var summoner = SummonerCatalog.GetSummoner(summonerId);
        return summoner?.InnateTraitIds ?? [];
    }

    // =========================================================================
    // GDSCRIPT SNAKE_CASE ALIASES
    // =========================================================================

    public int get_summoner_count() => GetSummonerCount();
    public Godot.Collections.Dictionary get_summoner(string id) => GetSummoner(id);
    public bool has_summoner(string id) => HasSummoner(id);
    public string[] get_all_summoner_ids() => GetAllSummonerIds();
    public Godot.Collections.Array<Godot.Collections.Dictionary> list_all_summoners() => ListAllSummoners();
    public Godot.Collections.Array<Godot.Collections.Dictionary> get_starting_summoners() => GetStartingSummoners();
    public Godot.Collections.Array<Godot.Collections.Dictionary> get_random_pool_summoners() => GetRandomPoolSummoners();
    public Godot.Collections.Array<Godot.Collections.Dictionary> get_purchasable_summoners() => GetPurchasableSummoners();
    public string get_summoner_name(string id) => GetSummonerName(id);
    public int get_summoner_element_id(string id) => GetSummonerElementId(id);
    public string[] get_innate_trait_ids(string id) => GetInnateTraitIds(id);
}
