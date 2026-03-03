namespace Fateforged.Simulation.Enums;

/// <summary>
/// Phase of a match. Mirrors GDScript BattlePhase enum values
/// in game_controller_3d.gd (PREPARATION=0, BATTLE=1) with
/// the addition of GameOver=2.
/// </summary>
public enum GamePhase
{
    Preparation = 0,
    Battle = 1,
    GameOver = 2
}
