using Godot;
using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards;

/// <summary>
/// Abstract base class for all cards.
/// Cards are playable items that have effects on the battlefield.
/// Note: Not marked [GlobalClass] to avoid conflict with GDScript Card class.
/// </summary>
public abstract partial class Card : Resource
{
    // =========================================================================
    // CONFIGURATION
    // =========================================================================

    /// <summary>
    /// Card configuration data.
    /// </summary>
    [Export]
    public CardConfig? Config { get; set; }

    // =========================================================================
    // INSTANCE STATE
    // =========================================================================

    /// <summary>
    /// Unique instance ID (for progression system tracking).
    /// </summary>
    public string InstanceId { get; set; } = "";

    // =========================================================================
    // PROPERTY ACCESSORS (convenience, delegate to config)
    // =========================================================================

    public string CatalogId => Config?.CatalogId ?? "";
    public string CardName => Config?.CardName ?? "Unknown Card";
    public CardType Type => Config?.CardType ?? CardType.Summon;
    public string Description => Config?.Description ?? "";
    public int ManaCost => Config?.ManaCost ?? 1;
    public float Cooldown => Config?.Cooldown ?? 2.0f;
    public Texture2D? CardIcon => Config?.CardIcon;

    // =========================================================================
    // GDSCRIPT-COMPATIBLE API (snake_case accessors)
    // =========================================================================

    // These properties allow GDScript to access using snake_case naming
    public string catalog_id => CatalogId;
    public string card_name => CardName;
    public int card_type => (int)Type;
    public string description => Description;
    public int mana_cost => ManaCost;
    public float cooldown => Cooldown;
    public Texture2D card_icon => CardIcon;
    public string instance_id
    {
        get => InstanceId;
        set => InstanceId = value;
    }

    // =========================================================================
    // API
    // =========================================================================

    /// <summary>
    /// Check if this card can be played with current mana.
    /// </summary>
    public bool CanPlay(int currentMana)
    {
        return currentMana >= ManaCost;
    }

    // GDScript-compatible wrapper
    public bool can_play(int currentMana) => CanPlay(currentMana);

    /// <summary>
    /// Execute the card effect at the given 3D position.
    /// This is the main entry point called by Summoner and other controllers.
    /// </summary>
    /// <param name="position">World position where the card is played.</param>
    /// <param name="team">Team of the caster.</param>
    /// <param name="battlefield">Reference to the battlefield node.</param>
    /// <param name="modifierSystem">Optional modifier system reference.</param>
    /// <param name="spawnDuration">Duration for spawn reveal animation (summon cards only).</param>
    public abstract void Play3D(
        Vector3 position,
        Team team,
        Node battlefield,
        Node modifierSystem = null,
        float spawnDuration = 0.0f
    );

    // GDScript-compatible wrapper (snake_case, int team)
    public void play_3d(
        Vector3 position,
        int team,
        Node battlefield,
        Node modifierSystem = null,
        float spawnDuration = 0.0f)
    {
        Play3D(position, (Team)team, battlefield, modifierSystem, spawnDuration);
    }
}
