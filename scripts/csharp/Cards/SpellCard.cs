using Godot;
using ProjectSummoner.Cards.Configs;
using ProjectSummoner.Cards.Effects.Core;
using ProjectSummoner.Units;

namespace ProjectSummoner.Cards;

/// <summary>
/// Card that casts a spell effect when played.
/// The spell behavior is determined by the attached ISpellEffect.
/// </summary>
[GlobalClass]
public partial class SpellCard : Card
{
    // =========================================================================
    // SPELL-SPECIFIC
    // =========================================================================

    /// <summary>
    /// The spell effect to execute when this card is played.
    /// </summary>
    public ISpellEffect? Effect { get; set; }

    /// <summary>
    /// Get the spell-specific config (typed convenience accessor).
    /// </summary>
    public SpellCardConfig SpellConfig => Config as SpellCardConfig;

    // =========================================================================
    // GDSCRIPT-COMPATIBLE ACCESSORS
    // =========================================================================

    // Expose spell config properties for GDScript compatibility
    public float spell_damage => SpellConfig?.SpellDamage ?? 0f;
    public float spell_radius => SpellConfig?.SpellRadius ?? 0f;
    public float spell_duration => SpellConfig?.SpellDuration ?? 0f;
    public string spell_vfx => SpellConfig?.SpellVFX ?? "";
    public string projectile_id => SpellConfig?.ProjectileId ?? "";

    // =========================================================================
    // EXECUTION
    // =========================================================================

    public override void Play3D(
        Vector3 position,
        Team team,
        Node battlefield,
        Node modifierSystem = null,
        float spawnDuration = 0.0f)
    {
        if (Effect == null)
        {
            GD.PrintErr($"[SpellCard] Card '{CardName}' has no effect assigned!");
            return;
        }

        // Build spell context
        var context = new SpellContext
        {
            Position = position,
            Team = team,
            Battlefield = battlefield,
            ModifierSystem = modifierSystem,
            CardInstanceId = InstanceId,
            SceneTree = battlefield?.GetTree()
        };

        // Execute the spell effect
        Effect.Execute(context);
    }
}
