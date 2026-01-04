using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectSummoner.Cards.Effects.Filters;
using ProjectSummoner.Cards.Effects.Targeting;
using ProjectSummoner.Systems;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards.Effects.Core;

/// <summary>
/// Abstract base class for spell effects.
/// Provides common functionality for targeting, affinity filtering, timing, and event hooks.
/// </summary>
public abstract class SpellEffect : ISpellEffect
{
    // =========================================================================
    // TARGETING
    // =========================================================================

    /// <summary>
    /// Strategy for selecting targets (circle, cone, line, etc.).
    /// If null, the effect doesn't target units (e.g., pure VFX).
    /// </summary>
    public ITargetingStrategy? Targeting { get; set; }

    /// <summary>
    /// Which units to affect based on team relationship.
    /// Defaults to enemies.
    /// </summary>
    public Affinity Affinity { get; set; } = Affinity.Enemies;

    /// <summary>
    /// Additional filter to refine target selection (e.g., only targets below 50% HP).
    /// Applied after affinity filtering.
    /// </summary>
    public ITargetFilter? Filter { get; set; }

    // =========================================================================
    // TIMING
    // =========================================================================

    /// <summary>
    /// Delay before this effect executes (seconds).
    /// </summary>
    public float Delay { get; set; } = 0f;

    /// <summary>
    /// Duration of the effect (for over-time effects).
    /// </summary>
    public float Duration { get; set; } = 0f;

    // =========================================================================
    // EVENT HOOKS
    // =========================================================================

    /// <summary>
    /// Effect to execute when this effect completes.
    /// Useful for chaining effects or cleanup.
    /// </summary>
    public ISpellEffect? OnComplete { get; set; }

    // =========================================================================
    // ABSTRACT METHODS
    // =========================================================================

    /// <summary>
    /// Execute the spell effect. Implemented by concrete effect classes.
    /// </summary>
    public abstract void Execute(SpellContext context);

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Get all valid targets using targeting strategy, affinity, and filters.
    /// </summary>
    protected IEnumerable<Node3D> GetTargets(SpellContext context)
    {
        if (Targeting == null)
            return Enumerable.Empty<Node3D>();

        // Get raw targets from strategy
        var targets = Targeting.SelectTargets(context);

        // Filter by affinity
        targets = FilterByAffinity(targets, context.Team, Affinity);

        // Apply additional filter if present
        if (Filter != null)
            targets = Filter.Filter(targets, context);

        return targets;
    }

    /// <summary>
    /// Filter targets by team affinity relative to caster.
    /// </summary>
    private static IEnumerable<Node3D> FilterByAffinity(
        IEnumerable<Node3D> targets,
        Team casterTeam,
        Affinity affinity)
    {
        return affinity switch
        {
            Affinity.Enemies => targets.Where(t => GetTeam(t) != casterTeam),
            Affinity.Allies => targets.Where(t => GetTeam(t) == casterTeam),
            Affinity.Both => targets,
            _ => targets
        };
    }

    /// <summary>
    /// Get team from a unit (handles both C# Unit3D and GDScript units).
    /// </summary>
    protected static Team GetTeam(Node3D unit)
    {
        if (unit is Unit3D unit3D)
        {
            return (Team)unit3D.Team;
        }

        // GDScript fallback
        var teamVar = unit.Get("Team");
        if (teamVar.VariantType == Variant.Type.Nil)
            teamVar = unit.Get("team");

        if (teamVar.VariantType != Variant.Type.Nil)
        {
            return (Team)teamVar.AsInt32();
        }

        return Team.Player; // Default fallback
    }

    /// <summary>
    /// Execute the OnComplete hook if present.
    /// </summary>
    protected void ExecuteOnComplete(SpellContext context)
    {
        OnComplete?.Execute(context);
    }

    /// <summary>
    /// Check if a unit is alive.
    /// Handles both C# IDamageable and GDScript units.
    /// </summary>
    protected static bool IsAlive(Node3D target)
    {
        if (target is Capabilities.IDamageable damageable)
        {
            return damageable.IsAlive;
        }

        // GDScript fallback
        var aliveVar = target.Get("IsAlive");
        if (aliveVar.VariantType == Variant.Type.Nil)
            aliveVar = target.Get("is_alive");

        if (aliveVar.VariantType != Variant.Type.Nil)
        {
            return aliveVar.AsBool();
        }

        return true; // Assume alive if no property
    }
}
