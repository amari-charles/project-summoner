using Godot;

namespace Fateforged.Data.Cosmetics;

/// <summary>
/// Bridge node for GDScript to access C# CosmeticsCatalog.
/// Registered as autoload "CosmeticsCatalog" in project.godot.
/// </summary>
public partial class CosmeticsCatalogBridge : Node
{
    public static CosmeticsCatalogBridge? Instance { get; private set; }

    /// <summary>GDScript-compatible CosmeticType constants (match GDScript enum ordinals).
    /// Exposed as properties because C# const fields are not visible to GDScript via Godot interop.</summary>
    public int CARD_BACK => 0;
    public int UI_THEME => 1;
    public int SUMMONER_SKIN => 2;

    private Node? _loc;

    public override void _Ready()
    {
        Instance = this;
        CallDeferred(nameof(CacheDependencies));
        GD.Print($"CosmeticsCatalogBridge: Initialized with {CosmeticsCatalog.Count} cosmetics");
    }

    private void CacheDependencies()
    {
        _loc = GetNodeOrNull<Node>("/root/Loc");
    }

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    public Godot.Collections.Dictionary GetCosmetic(string cosmeticId)
    {
        var dict = CosmeticsCatalog.GetCosmeticAsDict(cosmeticId);
        if (dict.Count == 0)
        {
            GD.PushWarning($"CosmeticsCatalog.GetCosmetic: Cosmetic not found: {cosmeticId}");
        }
        else
        {
            dict["display_name"] = Localize((string)dict["display_name"]);
            dict["description"] = Localize((string)dict["description"]);
        }
        return dict;
    }

    public bool HasCosmetic(string cosmeticId)
    {
        return CosmeticsCatalog.HasCosmetic(cosmeticId);
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCosmetics()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var cosmetic in CosmeticsCatalog.GetAllCosmetics())
        {
            result.Add(LocalizedDict(CosmeticsCatalog.ToDictionary(cosmetic)));
        }
        return result;
    }

    // =========================================================================
    // QUERY METHODS
    // =========================================================================

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCosmeticsByType(int type)
    {
        var cosmeticType = (CosmeticType)type;
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var cosmetic in CosmeticsCatalog.GetCosmeticsByType(cosmeticType))
        {
            result.Add(LocalizedDict(CosmeticsCatalog.ToDictionary(cosmetic)));
        }
        return result;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCardBacks()
    {
        return GetCosmeticsByType(CARD_BACK);
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetUiThemes()
    {
        return GetCosmeticsByType(UI_THEME);
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetSummonerSkins(string summonerId)
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var cosmetic in CosmeticsCatalog.GetSummonerSkins(summonerId))
        {
            result.Add(LocalizedDict(CosmeticsCatalog.ToDictionary(cosmetic)));
        }
        return result;
    }

    public Godot.Collections.Array<Godot.Collections.Dictionary> GetPurchasableCosmetics()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var cosmetic in CosmeticsCatalog.GetPurchasableCosmetics())
        {
            result.Add(LocalizedDict(CosmeticsCatalog.ToDictionary(cosmetic)));
        }
        return result;
    }

    // =========================================================================
    // DISPLAY HELPERS
    // =========================================================================

    public string GetCosmeticName(string cosmeticId)
    {
        var cosmetic = CosmeticsCatalog.GetCosmetic(cosmeticId);
        if (cosmetic == null)
            return "";
        return Localize(cosmetic.NameKey);
    }

    public int GetCosmeticPrice(string cosmeticId)
    {
        var cosmetic = CosmeticsCatalog.GetCosmetic(cosmeticId);
        return cosmetic?.Price ?? 0;
    }

    public static string TypeToString(int type)
    {
        return CosmeticsCatalog.TypeToString((CosmeticType)type);
    }

    public static int StringToType(string typeStr)
    {
        return (int)CosmeticsCatalog.StringToType(typeStr);
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
