namespace Fateforged.Simulation;

/// <summary>
/// Strongly-typed team identifier replacing bare int throughout the sim layer.
/// Migration happens incrementally — callers switch from int to Team as they're touched.
/// </summary>
public readonly record struct Team(int Value)
{
    public static readonly Team Player1 = new(0);
    public static readonly Team Player2 = new(1);

    public Team Enemy => new(Value == 0 ? 1 : 0);
}
