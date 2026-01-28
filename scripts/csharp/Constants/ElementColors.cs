using Godot;
using ProjectSummoner.Cards;

namespace ProjectSummoner.Constants;

/// <summary>
/// Element-to-color mapping for visual tinting.
/// Mirrors ELEMENT_COLORS from scripts/core/element_types.gd
/// </summary>
public static class ElementColors
{
    /// <summary>
    /// Get the color for an element. Used for unit tinting.
    /// Colors match element_types.gd ELEMENT_COLORS.
    /// </summary>
    public static Color GetColor(Element element)
    {
        return element switch
        {
            Element.Fire => new Color(0.9f, 0.3f, 0.2f),      // Orange-red
            Element.Water => new Color(0.2f, 0.5f, 0.9f),     // Blue
            Element.Wind => new Color(0.5f, 0.9f, 0.5f),      // Light green
            Element.Earth => new Color(0.6f, 0.4f, 0.2f),     // Brown
            Element.Lightning => new Color(0.9f, 0.9f, 0.3f), // Yellow
            Element.Shadow => new Color(0.4f, 0.2f, 0.5f),    // Purple
            Element.Life => new Color(0.3f, 0.9f, 0.5f),      // Green
            Element.Death => new Color(0.3f, 0.2f, 0.3f),     // Dark gray
            Element.Neutral => new Color(0.5f, 0.5f, 0.5f),   // Gray
            _ => new Color(1.0f, 1.0f, 1.0f)                  // White (no tint)
        };
    }

    /// <summary>
    /// Get a tint color suitable for placeholder sprites.
    /// Uses full saturation but preserves some of the original sprite's detail.
    /// </summary>
    public static Color GetTintColor(Element element, float opacity = 0.7f)
    {
        var baseColor = GetColor(element);
        return new Color(baseColor.R, baseColor.G, baseColor.B, opacity);
    }
}
