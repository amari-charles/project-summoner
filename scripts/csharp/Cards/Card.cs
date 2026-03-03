using Fateforged.Simulation;
using Godot;

namespace ProjectSummoner.Cards;

/// <summary>
/// Runtime card resource — unified replacement for GDScript card.gd + card_config.gd.
/// Created by CardCatalog.CreateCard() from a CardDefinition.
///
/// GDScript consumers access properties via PascalCase:
///   card.CatalogId, card.ManaCost, card.CanPlay(mana), etc.
///
/// For the CardType enum in GDScript, use UnitConstants.CardType.SUMMON / .SPELL
/// (C# nested enums aren't accessible from GDScript).
/// </summary>
[GlobalClass]
public partial class Card : Resource
{
    // =========================================================================
    // IDENTITY (from CardDefinition)
    // =========================================================================

    [Export] public string CatalogId { get; set; } = "";
    [Export] public string CardName { get; set; } = "Unknown Card";

    /// <summary>
    /// Card type as int for GDScript interop.
    /// 0 = Summon, 1 = Spell (matches CardType enum).
    /// </summary>
    [Export] public int Type { get; set; } = (int)CardType.Summon;

    [Export] public string Description { get; set; } = "";

    // =========================================================================
    // GAMEPLAY
    // =========================================================================

    [Export] public int ManaCost { get; set; } = 1;
    [Export] public float Cooldown { get; set; } = 2.0f;
    [Export] public float SummonTime { get; set; } = 1.0f;
    [Export] public int SpawnCount { get; set; } = 1;

    // =========================================================================
    // SPELL
    // =========================================================================

    [Export] public float SpellDamage { get; set; }
    [Export] public float SpellRadius { get; set; }
    [Export] public float SpellDuration { get; set; }
    [Export] public string ProjectileId { get; set; } = "";

    // =========================================================================
    // VISUAL
    // =========================================================================

    [Export] public Texture2D? CardIcon { get; set; }

    // =========================================================================
    // RUNTIME STATE (not from catalog — set per-instance)
    // =========================================================================

    /// <summary>Unique instance ID for progression tracking (from PlayerCardService).</summary>
    public string InstanceId { get; set; } = "";

    /// <summary>
    /// Stat overrides applied during spawning (set by EventSequencer / debug tools).
    /// Keys: "max_hp", "move_speed", "attack_damage", etc.
    /// </summary>
    public Godot.Collections.Dictionary CustomStatOverrides { get; set; } = new();

    // =========================================================================
    // METHODS
    // =========================================================================

    /// <summary>Check if this card can be played with the given mana.</summary>
    public bool CanPlay(int currentMana) => currentMana >= ManaCost;

    /// <summary>
    /// Check if this spell card needs click-targeting (Rally/Guard commands).
    /// Delegates to CardFactory.
    /// </summary>
    public bool NeedsClickTargeting()
    {
        if (Type != (int)CardType.Spell)
            return false;

        if (CardFactory.Instance == null)
            return false;

        return CardFactory.Instance.needs_click_targeting(CatalogId);
    }

    /// <summary>
    /// Get formation offset for a unit in this card's spawn group.
    /// Delegates to CardFactory for formation calculation.
    /// </summary>
    public Vector3 GetFormationOffset(int unitIndex)
    {
        if (SpawnCount <= 1)
            return Vector3.Zero;

        if (CardFactory.Instance == null)
        {
            GD.PushError("Card: CardFactory not available for formation offset.");
            return Vector3.Zero;
        }

        return CardFactory.Instance.get_formation_offset_by_id(CatalogId, unitIndex, SpawnCount);
    }

    /// <summary>
    /// Spawn units at the given position via SpawnUnitCommand.
    /// No mana, no casting — direct simulation spawn for debug/event/NPC paths.
    /// For normal gameplay card plays, use summoner.play_card_3d() which routes
    /// through PlayCardCommand instead.
    /// </summary>
    public void SpawnAt(Vector3 position, int team)
    {
        var sim = SimulationNode.Current;
        if (sim == null)
        {
            GD.PushError($"Card: SimulationNode not found. Cannot spawn '{CatalogId}'.");
            return;
        }

        var overrides = CustomStatOverrides.Count > 0 ? CustomStatOverrides : null;
        sim.QueueSpawnUnit(CatalogId, team, position, true, overrides);
    }

    // =========================================================================
    // FACTORY
    // =========================================================================

    /// <summary>
    /// Create a Card resource from a CardDefinition (catalog lookup).
    /// </summary>
    public static Card FromDefinition(CardDefinition def)
    {
        var card = new Card
        {
            CatalogId = def.Id,
            CardName = def.Name,
            Type = (int)def.Type,
            Description = def.Description,
            ManaCost = def.ManaCost,
            Cooldown = def.Cooldown,
            SummonTime = def.Summon?.SummonTime ?? def.SummonTime,
            SpawnCount = def.Summon?.TotalUnitCount ?? def.SpawnCount,
            SpellDamage = def.SpellDamage,
            SpellRadius = def.SpellRadius,
            SpellDuration = def.SpellDuration,
            ProjectileId = def.ProjectileId,
        };

        // Load card icon texture if path is set
        if (!string.IsNullOrEmpty(def.CardIconPath))
        {
            card.CardIcon = GD.Load<Texture2D>(def.CardIconPath);
        }

        return card;
    }
}
