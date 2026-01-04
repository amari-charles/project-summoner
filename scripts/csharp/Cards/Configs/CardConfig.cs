using Godot;

namespace ProjectSummoner.Cards.Configs;

/// <summary>
/// Base configuration for all card types.
/// Contains identity and gameplay properties shared by all cards.
/// Note: Not marked [GlobalClass] to avoid conflict with GDScript CardConfig class.
/// </summary>
public partial class CardConfig : Resource
{
    // =========================================================================
    // IDENTITY
    // =========================================================================

    /// <summary>
    /// Unique identifier in the card catalog.
    /// </summary>
    [Export]
    public string CatalogId { get; set; } = "";

    /// <summary>
    /// Display name of the card.
    /// </summary>
    [Export]
    public string CardName { get; set; } = "Unknown Card";

    /// <summary>
    /// Type of card (Summon or Spell).
    /// </summary>
    [Export]
    public CardType CardType { get; set; } = CardType.Summon;

    /// <summary>
    /// Card description for UI display.
    /// </summary>
    [Export]
    public string Description { get; set; } = "";

    // =========================================================================
    // GAMEPLAY
    // =========================================================================

    /// <summary>
    /// Mana cost to play this card.
    /// </summary>
    [Export]
    public int ManaCost { get; set; } = 1;

    /// <summary>
    /// Cooldown after playing (seconds before another card can be played).
    /// </summary>
    [Export]
    public float Cooldown { get; set; } = 2.0f;

    // =========================================================================
    // VISUAL
    // =========================================================================

    /// <summary>
    /// Card icon for UI display.
    /// </summary>
    [Export]
    public Texture2D? CardIcon { get; set; }
}
