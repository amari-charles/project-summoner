using System;
using Fateforged.Session;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Registered visual shell for one summoner.
/// Same self-sync model as UnitVisual — reads its own SummonerData from
/// IGameSession.GetState() each frame — but registered at battle init rather
/// than dynamically spawned, since summoners are always present.
///
/// Replaces visual code in summoner.gd — deck/mana/casting logic moves to Simulation.
/// </summary>
public partial class SummonerVisual : Node3D
{
    private IGameSession? _session;
    private int _teamIndex;
    private bool _isAlive = true;

    // Sub-components
    private Sprite3D? _sprite;
    // FloatingHPBar via HPBarService (width 1.5, offset Y 2.5, always visible)
    // HurtboxComponent (capsule radius 2.0, height 6.25)

    // --- Initialization (called by EntityManager at battle init, NOT spawned dynamically) ---

    public void Initialize(IGameSession session, int teamIndex)
    {
        throw new NotImplementedException();
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        // Read SummonerData from _session.GetState().Summoners[_teamIndex]
        // Sync: HP bar, alive status (position is fixed)
        throw new NotImplementedException();
    }

    // --- Event Reactions (called by EntityManager) ---

    public void FlashDamage()
    {
        throw new NotImplementedException();
    }

    public void BeginDeath()
    {
        throw new NotImplementedException();
    }
}
