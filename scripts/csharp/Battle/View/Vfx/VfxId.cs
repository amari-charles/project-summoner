namespace Fateforged.Vfx;

/// <summary>
/// Strongly-typed identifier for VFX effects.
/// Prevents typos and enables IDE autocomplete via VfxIds static class.
/// Mirrors VFXIDs.gd for C#/GDScript interop.
/// </summary>
public readonly record struct VfxId(string Value)
{
    /// <summary>Returns the underlying string value.</summary>
    public override string ToString() => Value;

    /// <summary>Implicit conversion to string for interop with existing systems.</summary>
    public static implicit operator string(VfxId id) => id.Value;

    /// <summary>Check if this ID has a value (not empty).</summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>Empty/unset VFX ID.</summary>
    public static readonly VfxId None = new("");
}

/// <summary>
/// All known VFX IDs. Use these instead of raw strings.
/// Mirrors VFXIDs.gd constants for consistency.
/// Example: VfxIds.FireballExplosion instead of "fireball_explosion"
/// </summary>
public static class VfxIds
{
    // =========================================================================
    // FIREBALL EFFECTS
    // =========================================================================

    /// <summary>Fireball explosion on impact.</summary>
    public static readonly VfxId FireballExplosion = new("fireball_explosion");

    /// <summary>Fireball trail while projectile is in flight.</summary>
    public static readonly VfxId FireballTrail = new("fireball_trail");

    /// <summary>Fireball spell cast effect (AOE indicator + impact).</summary>
    public static readonly VfxId FireballSpell = new("fireball_spell");

    /// <summary>Water cleanse spell visual.</summary>
    public static readonly VfxId CleanseSpell = new("cleanse_spell");

    /// <summary>Water jet spell visual.</summary>
    public static readonly VfxId WaterJetSpell = new("water_jet_spell");

    /// <summary>Rain field spell visual.</summary>
    public static readonly VfxId RainFieldSpell = new("rain_field_spell");

    /// <summary>Reusable exact-radius area field spell visual.</summary>
    public static readonly VfxId SpellAreaField = new("spell_area_field");

    /// <summary>Reusable exact-radius area burst spell visual.</summary>
    public static readonly VfxId SpellAreaBurst = new("spell_area_burst");

    /// <summary>Reusable delayed/repeated area pulse spell visual.</summary>
    public static readonly VfxId SpellAreaPulse = new("spell_area_pulse");

    /// <summary>Reusable line spell visual.</summary>
    public static readonly VfxId SpellLine = new("spell_line");

    /// <summary>Reusable single-target spell visual.</summary>
    public static readonly VfxId SpellSingleTarget = new("spell_single_target");

    // =========================================================================
    // LIGHTNING EFFECTS
    // =========================================================================

    /// <summary>Lightning strike bolt from attacker to target.</summary>
    public static readonly VfxId LightningStrike = new("lightning_strike");

    // =========================================================================
    // SPELL COMMAND EFFECTS
    // =========================================================================

    /// <summary>Effect when a spell fails to cast (fizzle).</summary>
    public static readonly VfxId SpellFizzle = new("spell_fizzle");

    /// <summary>Rally command circle marker.</summary>
    public static readonly VfxId RallyCircle = new("rally_circle");

    /// <summary>Guard command position marker.</summary>
    public static readonly VfxId GuardMarker = new("guard_marker");

    /// <summary>Charge command target marker.</summary>
    public static readonly VfxId ChargeMarker = new("charge_marker");

    // =========================================================================
    // COMBAT EFFECTS
    // =========================================================================

    /// <summary>Execute critical hit effect.</summary>
    public static readonly VfxId ExecuteCrit = new("execute_crit");

    /// <summary>Default explosion effect.</summary>
    public static readonly VfxId ExplosionDefault = new("explosion_default");
}
