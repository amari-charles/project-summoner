using Fateforged.Simulation;
using Fateforged.View.Spawning;
using Godot;

namespace Fateforged.Cards;

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

    [Export]
    public string CatalogId { get; set; } = "";

    [Export]
    public string CardName { get; set; } = "Unknown Card";

    /// <summary>
    /// Card type as int for GDScript interop.
    /// 0 = Summon, 1 = Spell (matches CardType enum).
    /// </summary>
    [Export]
    public int Type { get; set; } = (int)CardType.Summon;

    [Export]
    public string Description { get; set; } = "";

    // =========================================================================
    // GAMEPLAY
    // =========================================================================

    [Export]
    public int ManaCost { get; set; } = 1;

    [Export]
    public float Cooldown { get; set; } = 2.0f;

    [Export]
    public float SummonTime { get; set; } = 1.0f;

    [Export]
    public int SpawnCount { get; set; } = 1;

    // =========================================================================
    // SPELL
    // =========================================================================

    [Export]
    public float SpellDamage { get; set; }

    [Export]
    public float SpellRadius { get; set; }

    [Export]
    public float SpellDuration { get; set; }

    [Export]
    public string ProjectileId { get; set; } = "";

    [Export]
    public string CommandType { get; set; } = "";

    // =========================================================================
    // VISUAL
    // =========================================================================

    [Export]
    public Texture2D? CardIcon { get; set; }

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
    /// Get formation offset for a unit in this card's spawn group.
    /// </summary>
    public Vector3 GetFormationOffset(int unitIndex)
    {
        if (SpawnCount <= 1)
            return Vector3.Zero;

        var def = CardCatalog.GetCard(CatalogId);
        if (def == null)
        {
            GD.PushWarning($"Card: Card not found for formation offset: {CatalogId}");
            return Vector3.Zero;
        }

        return def.Formation.GetOffset(unitIndex, SpawnCount);
    }

    /// <summary>
    /// Calculate safe spawn positions for all units in this card's formation.
    /// Used for spawn preview and actual spawning.
    /// Respects team spawn boundaries and avoids overlapping existing units.
    /// </summary>
    public Godot.Collections.Array<Vector3> GetSafeSpawnPositions(
        Vector3 centerPosition,
        Node? battlefield,
        float collisionRadius,
        int team = 0
    )
    {
        var result = new Godot.Collections.Array<Vector3>();

        if (battlefield == null)
        {
            GD.PushWarning("Card: Battlefield is null for safe spawn calculation");
            return result;
        }

        var def = CardCatalog.GetCard(CatalogId);
        if (def == null)
        {
            GD.PushWarning($"Card: Card not found in catalog: {CatalogId}");
            return result;
        }

        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            def.Formation,
            centerPosition,
            SpawnCount,
            battlefield.GetTree(),
            collisionRadius > 0 ? collisionRadius : 0.5f,
            team
        );

        foreach (var pos in positions)
            result.Add(pos);

        return result;
    }

    /// <summary>
    /// Spawn units at the given position via SpawnUnitCommand.
    /// No mana, no casting — direct simulation spawn for debug/event/NPC paths.
    /// For normal gameplay card plays, use summoner.play_card_3d() which routes
    /// through PlayCardCommand instead.
    /// </summary>
    public void SpawnAt(Vector3 position, int team)
    {
        if (Type == (int)CardType.Spell)
        {
            GD.PushError($"Card.SpawnAt called with spell '{CatalogId}'. Use CastAt() instead.");
            return;
        }

        var sim = SimulationNode.Current;
        if (sim == null)
        {
            GD.PushError($"Card: SimulationNode not found. Cannot spawn '{CatalogId}'.");
            return;
        }

        var overrides = CustomStatOverrides.Count > 0 ? CustomStatOverrides : null;
        sim.QueueSpawnUnit(CatalogId, team, position, true, overrides);
    }

    /// <summary>
    /// Cast a spell card at the given position via QueueCastSpell.
    /// No mana, no hand/casting state changes — debug/event injection path.
    /// </summary>
    public void CastAt(Vector3 position, int team)
    {
        if (Type != (int)CardType.Spell)
        {
            GD.PushWarning($"Card.CastAt called with non-spell '{CatalogId}'.");
            return;
        }

        var sim = SimulationNode.Current;
        if (sim == null)
        {
            GD.PushError($"Card: SimulationNode not found. Cannot cast '{CatalogId}'.");
            return;
        }

        sim.QueueCastSpell(CatalogId, team, position);
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
            CommandType = def.CommandType?.ToString() ?? "",
        };

        // Load card icon texture if path is set
        if (!string.IsNullOrEmpty(def.CardIconPath))
        {
            card.CardIcon = GD.Load<Texture2D>(def.CardIconPath);
        }

        return card;
    }
}
