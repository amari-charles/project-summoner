using Godot;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards.Effects.Core;

/// <summary>
/// Context passed to spell effects during execution.
/// Contains all information needed to execute a spell: position, team, battlefield access.
/// </summary>
public class SpellContext
{
    /// <summary>
    /// Position where the spell was cast (target location).
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Team of the caster (used for affinity filtering).
    /// </summary>
    public Team Team { get; set; }

    /// <summary>
    /// Reference to the battlefield node for scene access.
    /// </summary>
    public Node? Battlefield { get; set; }

    /// <summary>
    /// Reference to the modifier service for buff/debuff application.
    /// </summary>
    public Node? ModifierService { get; set; }

    /// <summary>
    /// Unique ID of the card instance that cast this spell.
    /// Used for tracking and effect management.
    /// </summary>
    public CardInstanceId CardInstanceId { get; set; } = Cards.CardInstanceId.None;

    /// <summary>
    /// Reference to the scene tree for timers and async operations.
    /// </summary>
    public SceneTree? SceneTree { get; set; }

    /// <summary>
    /// Optional reference to the caster unit (for caster-relative targeting like cones).
    /// </summary>
    public Node3D? Caster { get; set; }

    /// <summary>
    /// Get the caster's position if available, otherwise fall back to spell position.
    /// </summary>
    public Vector3 CasterPosition => Caster?.GlobalPosition ?? Position;

    /// <summary>
    /// Create a copy of this context with a new position.
    /// Useful for chain effects or position-shifted sub-effects.
    /// </summary>
    public SpellContext WithPosition(Vector3 newPosition)
    {
        return new SpellContext
        {
            Position = newPosition,
            Team = Team,
            Battlefield = Battlefield,
            ModifierService = ModifierService,
            CardInstanceId = CardInstanceId,
            SceneTree = SceneTree,
            Caster = Caster
        };
    }
}
