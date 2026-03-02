using Godot;

namespace ProjectSummoner.Combat;

/// <summary>
/// Data class for combat events.
/// Used for effects that trigger on combat actions.
/// Passed to listeners via signals.
/// </summary>
[GlobalClass]
public partial class CombatEvent : RefCounted
{
    public enum EventType
    {
        DamageDealt,      // Attacker dealt damage to target
        DamageTaken,      // Target took damage from attacker
        UnitKilled,       // Attacker killed target
        UnitDied,         // Target died
        AttackStarted,    // Unit began attack animation
        AttackCompleted,  // Unit finished attack
        SpellCast,        // Spell was cast
        UnitHealed,       // Unit was healed
        UnitSpawned       // Unit was spawned
    }

    /// <summary>Type of event</summary>
    public EventType Type { get; set; }

    /// <summary>Who triggered the event (attacker, caster, healer)</summary>
    public Node3D? Source { get; set; }

    /// <summary>Who was affected (defender, victim, heal target)</summary>
    public Node3D? Target { get; set; }

    /// <summary>Numeric value (damage amount, heal amount, etc.)</summary>
    public float Value { get; set; }

    /// <summary>Type of damage/effect</summary>
    public string DamageType { get; set; } = DamageTypes.Physical;

    /// <summary>Additional data (is_crit, buff_id, etc.)</summary>
    public Godot.Collections.Dictionary Metadata { get; set; } = new();

    /// <summary>
    /// Default constructor for Godot serialization.
    /// </summary>
    public CombatEvent() { }

    /// <summary>
    /// Create a new combat event.
    /// </summary>
    public CombatEvent(
        EventType type,
        Node3D? source = null,
        Node3D? target = null,
        float value = 0f,
        string damageType = DamageTypes.Physical,
        Godot.Collections.Dictionary? metadata = null)
    {
        Type = type;
        Source = source;
        Target = target;
        Value = value;
        DamageType = damageType;
        Metadata = metadata ?? new Godot.Collections.Dictionary();
    }

    /// <summary>
    /// Check if this event is a critical hit.
    /// </summary>
    public bool IsCritical()
    {
        return Metadata.ContainsKey("is_crit") && Metadata["is_crit"].AsBool();
    }

    /// <summary>
    /// Get source team (if available).
    /// </summary>
    public int GetSourceTeam()
    {
        if (Source == null)
            return -1;

        var teamVar = Source.Get("Team");
        if (teamVar.VariantType == Variant.Type.Nil)
            teamVar = Source.Get("team");

        return teamVar.VariantType != Variant.Type.Nil ? teamVar.AsInt32() : -1;
    }

    /// <summary>
    /// Get target team (if available).
    /// </summary>
    public int GetTargetTeam()
    {
        if (Target == null)
            return -1;

        var teamVar = Target.Get("Team");
        if (teamVar.VariantType == Variant.Type.Nil)
            teamVar = Target.Get("team");

        return teamVar.VariantType != Variant.Type.Nil ? teamVar.AsInt32() : -1;
    }
}
