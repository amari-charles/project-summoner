using System;
using System.Collections.Generic;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;

namespace Fateforged.Session;

/// <summary>
/// Single interface that Input and View talk to.
/// Three session types implement it: LocalSession, HostSession, ClientSession.
/// </summary>
public interface IGameSession
{
    /// <summary>
    /// Read current game state. Shells poll this every frame for continuous data
    /// (positions, HP, animation state).
    /// </summary>
    MatchState GetState();

    /// <summary>
    /// Discrete events produced by the simulation this tick.
    /// EntityManager and BattleHUD subscribe for one-shot reactions (VFX, sound, animations).
    /// </summary>
    event Action<IReadOnlyList<SimEvent>> SimEventsEmitted;

    /// <summary>
    /// Input layer sends player commands (play card, cast spell, redirect unit).
    /// Session validates via CommandRouter before feeding to simulation.
    /// </summary>
    void SubmitCommand(ICommand command);
}
