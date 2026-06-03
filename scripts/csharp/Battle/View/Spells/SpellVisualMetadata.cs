using System.Linq;
using Fateforged.Cards;
using Fateforged.Simulation.Enums;

namespace Fateforged.View.Spells;

/// <summary>
/// View-facing spell shape metadata derived from gameplay card data.
/// Keeps preview and cast VFX aligned with simulation area resolution.
/// </summary>
public sealed record SpellVisualMetadata(
    string Shape,
    float Radius,
    float LineWidth,
    string Element
)
{
    public const string Circle = "circle";
    public const string Square = "square";
    public const string Line = "line";
    public const string Cone = "cone";
    public const string SingleTarget = "single_target";

    public static SpellVisualMetadata FromCardDefinition(CardDefinition card)
    {
        string shape = ResolveShape(card);
        float radius = ResolveRadius(card);
        string element = card.ElementalAffinity.ToString().ToLowerInvariant();
        return new SpellVisualMetadata(shape, radius, SpellAreaLineWidth.FullWidth, element);
    }

    private static string ResolveShape(CardDefinition card)
    {
        if (card.SpellTargeting == SpellTargeting.SingleTarget)
            return SingleTarget;

        if (card.SpellEffects.Any(e => e.AreaShape == SpellAreaShape.Line))
            return Line;
        if (card.SpellEffects.Any(e => e.AreaShape == SpellAreaShape.Cone))
            return Cone;
        if (card.SpellEffects.Any(e => e.AreaShape == SpellAreaShape.Square))
            return Square;

        return Circle;
    }

    private static float ResolveRadius(CardDefinition card)
    {
        var shapedEffect = card.SpellEffects.FirstOrDefault(e =>
            e.AreaShape is SpellAreaShape.Line or SpellAreaShape.Cone or SpellAreaShape.Square
        );
        if (shapedEffect != null && shapedEffect.RadiusOverride > 0f)
            return shapedEffect.RadiusOverride;

        var radiusEffect = card.SpellEffects.FirstOrDefault(e => e.RadiusOverride > 0f);
        if (radiusEffect != null)
            return radiusEffect.RadiusOverride;

        return card.SpellRadius;
    }
}

public static class SpellAreaLineWidth
{
    // Must match SpellAreaResolver's line half-width of 1.25.
    public const float FullWidth = 2.5f;
}
