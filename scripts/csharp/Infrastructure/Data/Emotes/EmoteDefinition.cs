namespace Fateforged.Data.Emotes;

public record EmoteDefinition
{
    public required string Id { get; init; }
    public required string NameKey { get; init; }
    public required string DescriptionKey { get; init; }
    public string IconPath { get; init; } = "";
    public string AnimationPath { get; init; } = "";
    public string SoundPath { get; init; } = "";
    public int Price { get; init; }
    public string Rarity { get; init; } = "common";
    public string Category { get; init; } = "neutral";
}
