using Godot;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Cards.Effects.Core;

namespace ProjectSummoner.Cards.Effects.Conditions;

/// <summary>
/// Condition that checks if a target's HP is above or below a threshold.
/// Use for execute-style spells (e.g., "if HP < 30%, deal bonus damage").
/// </summary>
public class HPThresholdCondition : ISpellCondition
{
    /// <summary>
    /// HP threshold as a percentage (0.0 - 1.0).
    /// </summary>
    public float Threshold { get; set; } = 0.5f;

    /// <summary>
    /// If true, condition is met when HP is BELOW threshold.
    /// If false, condition is met when HP is ABOVE threshold.
    /// </summary>
    public bool Below { get; set; } = true;

    public bool Evaluate(SpellContext context, Node3D? target = null)
    {
        if (target == null) return false;

        float hpPercent = GetHPPercent(target);
        if (hpPercent < 0) return false; // Couldn't get HP

        return Below
            ? hpPercent < Threshold
            : hpPercent >= Threshold;
    }

    /// <summary>
    /// Get HP as a percentage (0.0 - 1.0) from a unit.
    /// </summary>
    private static float GetHPPercent(Node3D target)
    {
        // Try C# interface
        if (target is IDamageable damageable)
        {
            if (damageable.MaxHp <= 0) return -1;
            return damageable.CurrentHp / damageable.MaxHp;
        }

        // GDScript fallback
        float currentHp = 0;
        float maxHp = 0;

        var currentVar = target.Get("CurrentHp");
        if (currentVar.VariantType == Variant.Type.Nil)
            currentVar = target.Get("current_hp");
        if (currentVar.VariantType != Variant.Type.Nil)
            currentHp = currentVar.AsSingle();

        var maxVar = target.Get("MaxHp");
        if (maxVar.VariantType == Variant.Type.Nil)
            maxVar = target.Get("max_hp");
        if (maxVar.VariantType != Variant.Type.Nil)
            maxHp = maxVar.AsSingle();

        if (maxHp <= 0) return -1;
        return currentHp / maxHp;
    }
}
