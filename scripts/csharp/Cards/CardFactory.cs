using Godot;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Services.Interfaces;
using ProjectSummoner.Summons;

namespace ProjectSummoner.Cards;

/// <summary>
/// Factory autoload for card queries from GDScript.
/// Spell/summon execution is now handled by the simulation layer.
/// This bridges the GDScript CardCatalog with formation and spawn position calculations.
/// </summary>
public partial class CardFactory : Node, ICardFactory
{
    // =========================================================================
    // SINGLETON
    // =========================================================================

    public static CardFactory? Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GD.Print("CardFactory: Initialized");
    }

    // =========================================================================
    // QUERY API (GDScript-compatible snake_case methods)
    // =========================================================================

    /// <summary>
    /// Check if summon execution is supported for the given catalog ID.
    /// Currently all summons are supported (they use the GridFormation by default).
    /// </summary>
    public bool has_summon(string catalogId)
    {
        return true;
    }

    /// <summary>
    /// Check if a spell needs click-targeting (command spells like Rally/Guard/Charge).
    /// </summary>
    public bool needs_click_targeting(string catalogId)
    {
        var card = CardCatalog.GetCard(catalogId);
        return card?.CommandType != null;
    }

    /// <summary>
    /// Calculate safe spawn positions for all units in a formation.
    /// Single source of truth - used by both preview and actual spawn.
    /// Calculates all positions at once against the current state to ensure
    /// preview matches actual spawn positions.
    /// Respects team spawn boundaries.
    /// </summary>
    public Godot.Collections.Array<Vector3> get_safe_spawn_positions(
        string catalogId,
        Vector3 centerPosition,
        Node? battlefield,
        float collisionRadius,
        int team = 0)
    {
        var result = new Godot.Collections.Array<Vector3>();

        if (battlefield == null)
        {
            GD.PushWarning("[CardFactory] Battlefield is null for safe spawn calculation");
            return result;
        }

        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
        {
            GD.PushWarning($"[CardFactory] Card not found in catalog: {catalogId}");
            return result;
        }

        // Use SpawnPositionCalculator with team boundary enforcement
        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            card.Formation,
            centerPosition,
            card.SpawnCount,
            battlefield.GetTree(),
            collisionRadius > 0 ? collisionRadius : 0.5f,
            team);

        foreach (var pos in positions)
            result.Add(pos);

        return result;
    }

    // =========================================================================
    // FORMATION API (for GDScript preview)
    // =========================================================================

    /// <summary>
    /// Get formation offset for a unit by catalog ID.
    /// Called by GDScript Card class for spawn preview.
    /// </summary>
    public Vector3 get_formation_offset_by_id(string catalogId, int unitIndex, int totalUnits)
    {
        if (totalUnits <= 1)
            return Vector3.Zero;

        var card = CardCatalog.GetCard(catalogId);
        if (card == null)
        {
            GD.PushWarning($"[CardFactory] Card not found for formation offset: {catalogId}");
            return Vector3.Zero;
        }

        return card.Formation.GetOffset(unitIndex, totalUnits);
    }
}
