using System;
using Fateforged.Session;
using Fateforged.Simulation;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Self-syncing visual shell for one unit.
/// Reads its own UnitData from IGameSession.GetState() each frame.
/// Exposes reaction methods called by EntityManager on discrete events.
///
/// Replaces Unit3D — game logic moves to Simulation subsystems.
/// </summary>
public partial class UnitVisual : Node3D
{
    private IGameSession? _session;
    private int _unitId;
    private bool _isAlive = true;
    private bool _loggedMissing;

    // Sub-components (already exist in codebase)
    // IVisualComponent  — scripts/csharp/Visual/IVisualComponent.cs
    // ShadowComponent   — scripts/csharp/Visual/ShadowComponent.cs
    // SpawnRevealComponent — scripts/csharp/Units/Components/SpawnRevealComponent.cs
    // FloatingHPBar via HPBarService — scripts/csharp/Services/HPBarService.cs

    // --- Initialization (called by EntityManager at spawn) ---

    public void Initialize(IGameSession session, int unitId)
    {
        throw new NotImplementedException();
    }

    // --- Self-Sync (continuous, every frame) ---

    public override void _PhysicsProcess(double delta)
    {
        // Read UnitData from _session.GetState().Units[_unitId]
        // Sync: position, facing, HP bar, animation from BehaviorState
        throw new NotImplementedException();
    }

    // --- Event Reactions (called by EntityManager) ---

    public void PlayAttackAnimation()
    {
        throw new NotImplementedException();
    }

    public void FlashDamage(float damage, bool isCrit)
    {
        throw new NotImplementedException();
    }

    public void BeginDeath()
    {
        throw new NotImplementedException();
    }

    public void ShowBuffIcon(EffectType effectType)
    {
        throw new NotImplementedException();
    }

    public void ShowEvadeText()
    {
        throw new NotImplementedException();
    }
}
