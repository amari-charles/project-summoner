namespace Fateforged.Data.Academy;

/// <summary>
/// Strongly-typed identifier for an Academy professor who stewards quest chains.
/// </summary>
public readonly record struct ProfessorId(string Value)
{
    public override string ToString() => Value;

    public static implicit operator string(ProfessorId id) => id.Value;

    public static ProfessorId FromString(string id) => new(id);

    public bool HasValue => !string.IsNullOrEmpty(Value);

    public static readonly ProfessorId None = new("");
}

public static class ProfessorIds
{
    public static readonly ProfessorId GeneralMagic = new("general_magic");
    public static readonly ProfessorId Fire = new("fire");
    public static readonly ProfessorId Water = new("water");
    public static readonly ProfessorId Earth = new("earth");
    public static readonly ProfessorId Wind = new("wind");
}
