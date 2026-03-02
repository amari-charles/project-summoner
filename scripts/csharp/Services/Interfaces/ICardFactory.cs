using Godot;

namespace ProjectSummoner.Services.Interfaces;

/// <summary>
/// Interface for the CardFactory service.
/// Provides formation queries and spawn position calculations.
/// Spell/summon execution is now handled by the simulation layer.
/// </summary>
public interface ICardFactory
{
    // =========================================================================
    // SUMMON API
    // =========================================================================

    /// <summary>
    /// Check if summon execution is supported for the given catalog ID.
    /// </summary>
    bool has_summon(string catalogId);

    /// <summary>
    /// Check if a spell needs click-targeting (command spells like Rally/Guard/Charge).
    /// </summary>
    bool needs_click_targeting(string catalogId);

    // =========================================================================
    // FORMATION API
    // =========================================================================

    /// <summary>
    /// Get formation offset for a unit by catalog ID.
    /// </summary>
    Vector3 get_formation_offset_by_id(string catalogId, int unitIndex, int totalUnits);

    /// <summary>
    /// Calculate safe spawn positions for all units in a formation.
    /// Single source of truth - used by both preview and actual spawn.
    /// Respects team spawn boundaries.
    /// </summary>
    Godot.Collections.Array<Vector3> get_safe_spawn_positions(
        string catalogId,
        Vector3 centerPosition,
        Node? battlefield,
        float collisionRadius,
        int team = 0);
}
