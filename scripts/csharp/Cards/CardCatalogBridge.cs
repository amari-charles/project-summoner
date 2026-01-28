using Godot;

namespace ProjectSummoner.Cards;

/// <summary>
/// Bridge node for GDScript to access C# CardCatalog.
/// Registered as autoload "CardCatalogCS" in project.godot.
/// Wraps static CardCatalog methods as instance methods for GDScript compatibility.
/// </summary>
public partial class CardCatalogBridge : Node
{
    public static CardCatalogBridge? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Print($"CardCatalogBridge: Initialized with {CardCatalog.Count} cards");
    }

    // =========================================================================
    // LOOKUP METHODS
    // =========================================================================

    /// <summary>Get a card as dictionary by ID. Returns empty dict if not found.</summary>
    public Godot.Collections.Dictionary GetCardAsDict(string id)
    {
        return CardCatalog.GetCardAsDict(id);
    }

    /// <summary>Check if a card exists in the catalog.</summary>
    public bool HasCard(string id)
    {
        return CardCatalog.HasCard(id);
    }

    /// <summary>Get all card IDs.</summary>
    public string[] GetAllCardIds()
    {
        return CardCatalog.GetAllCardIds();
    }

    /// <summary>Get card count.</summary>
    public int GetCardCount()
    {
        return CardCatalog.Count;
    }

    // =========================================================================
    // QUERY METHODS (return arrays of dictionaries)
    // =========================================================================

    /// <summary>Get all cards as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCardsAsDict()
    {
        return CardCatalog.GetAllCardsAsDict();
    }

    /// <summary>Get cards by rarity as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByRarityAsDict(string rarity)
    {
        return CardCatalog.GetCardsByRarityAsDict(rarity);
    }

    /// <summary>Get cards by type as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByTypeAsDict(int type)
    {
        return CardCatalog.GetCardsByTypeAsDict(type);
    }

    /// <summary>Get starter cards as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetStarterCardsAsDict()
    {
        return CardCatalog.GetStarterCardsAsDict();
    }

    /// <summary>Get cards by element as dictionaries.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCardsByElementAsDict(string element)
    {
        return CardCatalog.GetCardsByElementAsDict(element);
    }
}
