using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Summons;

/// <summary>
/// Tracks a single summon operation and its spawned units.
/// Allows cards to reference and interact with units they spawned.
/// </summary>
public partial class UnitSummon : RefCounted
{
    /// <summary>Card catalog ID that created this summon.</summary>
    public string CatalogId { get; }

    /// <summary>Player card instance ID (empty for enemy cards).</summary>
    public string InstanceId { get; }

    /// <summary>Team that owns these units.</summary>
    public int Team { get; }

    /// <summary>Timestamp when summon was created.</summary>
    public ulong CreatedAtMsec { get; }

    private readonly List<Unit3D> _units = new();
    private readonly List<Unit3D> _deadUnits = new();

    // =========================================================================
    // SIGNALS
    // =========================================================================

    /// <summary>Emitted when a unit is added to this summon.</summary>
    [Signal]
    public delegate void UnitAddedEventHandler(Unit3D unit);

    /// <summary>Emitted when a unit from this summon dies.</summary>
    [Signal]
    public delegate void UnitDiedEventHandler(Unit3D unit);

    /// <summary>Emitted when all units from this summon have died.</summary>
    [Signal]
    public delegate void AllUnitsDiedEventHandler();

    // =========================================================================
    // CONSTRUCTOR
    // =========================================================================

    public UnitSummon(string catalogId, string instanceId, int team)
    {
        CatalogId = catalogId;
        InstanceId = instanceId;
        Team = team;
        CreatedAtMsec = Time.GetTicksMsec();
    }

    // =========================================================================
    // UNIT MANAGEMENT
    // =========================================================================

    /// <summary>
    /// Adds a unit to this summon's tracking.
    /// Connects to the unit's death signal.
    /// </summary>
    public void AddUnit(Unit3D unit)
    {
        if (unit == null || _units.Contains(unit))
            return;

        _units.Add(unit);

        // Connect to unit death signal
        unit.UnitDied += OnUnitDiedSignal;

        EmitSignal(SignalName.UnitAdded, unit);
    }

    private void OnUnitDiedSignal(Unit3D unit)
    {
        OnUnitDied(unit);
    }

    /// <summary>
    /// Removes a unit from tracking (without triggering death events).
    /// Disconnects the death signal to prevent orphaned callbacks.
    /// </summary>
    public void RemoveUnit(Unit3D unit)
    {
        if (_units.Remove(unit))
        {
            unit.UnitDied -= OnUnitDiedSignal;
        }
    }

    private void OnUnitDied(Unit3D unit)
    {
        if (_units.Remove(unit))
        {
            _deadUnits.Add(unit);
            EmitSignal(SignalName.UnitDied, unit);

            if (_units.Count == 0)
            {
                EmitSignal(SignalName.AllUnitsDied);
            }
        }
    }

    // =========================================================================
    // QUERIES
    // =========================================================================

    /// <summary>Gets all currently alive units from this summon.</summary>
    public IReadOnlyList<Unit3D> GetAliveUnits() => _units.AsReadOnly();

    /// <summary>Gets all dead units from this summon.</summary>
    public IReadOnlyList<Unit3D> GetDeadUnits() => _deadUnits.AsReadOnly();

    /// <summary>Gets all units ever spawned (alive + dead).</summary>
    public IReadOnlyList<Unit3D> GetAllUnits() => _units.Concat(_deadUnits).ToList().AsReadOnly();

    /// <summary>Number of currently alive units.</summary>
    public int AliveCount => _units.Count;

    /// <summary>Number of dead units.</summary>
    public int DeadCount => _deadUnits.Count;

    /// <summary>Total units spawned (alive + dead).</summary>
    public int TotalCount => _units.Count + _deadUnits.Count;

    /// <summary>Whether all units have died.</summary>
    public bool AllDead => _units.Count == 0 && _deadUnits.Count > 0;

    /// <summary>Whether any units are still alive.</summary>
    public bool HasAliveUnits => _units.Count > 0;

    // =========================================================================
    // ACTIONS
    // =========================================================================

    /// <summary>
    /// Kills all alive units from this summon.
    /// </summary>
    public void KillAll()
    {
        // Copy list since killing modifies it
        var unitsToKill = _units.ToList();
        foreach (var unit in unitsToKill)
        {
            if (GodotObject.IsInstanceValid(unit) && unit.IsAlive)
            {
                unit.TakeDamage(float.MaxValue);
            }
        }
    }

    /// <summary>
    /// Gets units as a Godot Array for GDScript interop.
    /// </summary>
    public Godot.Collections.Array<Unit3D> GetAliveUnitsArray()
    {
        var array = new Godot.Collections.Array<Unit3D>();
        foreach (var unit in _units)
        {
            if (GodotObject.IsInstanceValid(unit))
                array.Add(unit);
        }
        return array;
    }

    public override string ToString() =>
        $"UnitSummon({CatalogId}, alive={AliveCount}, dead={DeadCount})";
}
