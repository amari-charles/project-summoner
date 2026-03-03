namespace Fateforged.Data.Cosmetics;

public record CosmeticDefinition
{
    public required string Id { get; init; }
    public required CosmeticType Type { get; init; }
    public required string NameKey { get; init; }
    public required string DescriptionKey { get; init; }
    public string PreviewPath { get; init; } = "";
    public string ResourcePath { get; init; } = "";
    public string SummonerId { get; init; } = "";
    public int Price { get; init; }
    public string Rarity { get; init; } = "common";
}
