using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Stats;

namespace Fateforged.Simulation.Commands;

/// <summary>
/// Direct unit spawn — places units on the battlefield without going through
/// the card-game economy (no mana, no casting, no hand/discard management).
///
/// Models scenarios where units enter the battlefield outside of a summoner
/// playing a card:
///   - Tutorial sequences placing training dummies or scripted enemies
///   - Campaign events spawning reinforcements or boss adds mid-battle
///   - Debug arena drag-and-drop placement
///   - Environmental spawns (traps, hazards, summoning circles)
///
/// For normal gameplay where a player or AI spends mana from their hand,
/// use PlayCardCommand instead.
///
/// Both commands share SpawnUnitsFromCard() for UnitData creation.
/// </summary>
public class SpawnUnitCommand : ICommand
{
    /// <summary>Card catalog ID to look up unit templates from.</summary>
    public SimCardCatalogId CatalogId { get; }

    /// <summary>Team index (0 = player, 1 = enemy).</summary>
    public int Team { get; }

    /// <summary>World position where units should appear.</summary>
    public SimVector3 SpawnPosition { get; }

    /// <summary>
    /// Optional stat overrides applied after template creation.
    /// </summary>
    public Dictionary<StatKey, float>? StatOverrides { get; set; }

    /// <summary>
    /// If true, units activate immediately (no spawn timer).
    /// If false, units use the card's summon time as spawn timer.
    /// </summary>
    public bool ActivateImmediately { get; set; }

    public long ExecuteFrame { get; set; }

    public SpawnUnitCommand(SimCardCatalogId catalogId, int team, SimVector3 spawnPosition)
    {
        CatalogId = catalogId;
        Team = team;
        SpawnPosition = spawnPosition;
    }
}
