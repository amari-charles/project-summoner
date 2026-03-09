namespace Fateforged.Simulation.Data;

public readonly record struct SimCardCatalogId(string Value)
{
    public static SimCardCatalogId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(SimCardCatalogId id) => id.Value;
    public static implicit operator SimCardCatalogId(string value) => new(value ?? "");
}

public readonly record struct SimCardInstanceId(string Value)
{
    public static SimCardInstanceId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(SimCardInstanceId id) => id.Value;
    public static implicit operator SimCardInstanceId(string value) => new(value ?? "");
}

public readonly record struct SimUnitCatalogId(string Value)
{
    public static SimUnitCatalogId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(SimUnitCatalogId id) => id.Value;
    public static implicit operator SimUnitCatalogId(string value) => new(value ?? "");
}

public readonly record struct SimProjectileCatalogId(string Value)
{
    public static SimProjectileCatalogId Empty => new("");
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
    public override string ToString() => Value;
    public static implicit operator string(SimProjectileCatalogId id) => id.Value;
    public static implicit operator SimProjectileCatalogId(string value) => new(value ?? "");
}
