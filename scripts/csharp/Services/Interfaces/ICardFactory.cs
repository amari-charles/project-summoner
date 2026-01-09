using Godot;

namespace ProjectSummoner.Services.Interfaces;

/// <summary>
/// Interface for the CardFactory service.
/// Enables dependency injection and testing.
/// </summary>
public interface ICardFactory
{
    // =========================================================================
    // SPELL API
    // =========================================================================

    /// <summary>
    /// Check if a spell effect exists for the given catalog ID.
    /// </summary>
    bool has_effect(string catalogId);

    /// <summary>
    /// Create a SpellCard with the appropriate effect attached.
    /// </summary>
    Resource? create_spell_card(string catalogId, Godot.Collections.Dictionary cardDef);

    /// <summary>
    /// Execute a spell effect at the given position.
    /// </summary>
    void execute_spell(
        string catalogId,
        Vector3 position,
        int team,
        Node battlefield,
        Node? modifierSystem = null,
        string instanceId = "");

    // =========================================================================
    // SUMMON API
    // =========================================================================

    /// <summary>
    /// Check if summon execution is supported for the given catalog ID.
    /// </summary>
    bool has_summon(string catalogId);

    /// <summary>
    /// Execute a summon at the given position.
    /// </summary>
    void execute_summon(
        string catalogId,
        Vector3 position,
        int team,
        Node battlefield,
        Godot.Collections.Dictionary cardDef,
        Godot.Collections.Dictionary effectiveStats,
        Godot.Collections.Dictionary customOverrides,
        Node? modifierSystem = null,
        string instanceId = "",
        float spawnDuration = 0.0f);

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
    /// </summary>
    Godot.Collections.Array<Vector3> get_safe_spawn_positions(
        string catalogId,
        Vector3 centerPosition,
        Node? battlefield,
        float collisionRadius);
}
