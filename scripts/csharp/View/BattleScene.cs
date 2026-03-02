using System;
using Fateforged.Session;
using Godot;

namespace Fateforged.View;

/// <summary>
/// Top-level facade for the battle visual layer.
/// Owns EntityManager, BattleHUD, BattleCamera, BattlefieldEnvironment.
/// Wires IGameSession to EntityManager and BattleHUD at init.
/// Camera and Environment are state-independent.
///
/// Replaces GameController3D as a thin typed facade — no game logic.
/// </summary>
public partial class BattleScene : Node3D
{
    private IGameSession? _session;

    // Child components — resolved from scene tree
    public EntityManager EntityManager { get; private set; } = null!;

    /// <summary>
    /// Wires IGameSession to EntityManager and BattleHUD.
    /// Camera and Environment are state-independent.
    /// </summary>
    public void Initialize(IGameSession session)
    {
        throw new NotImplementedException();
    }
}
