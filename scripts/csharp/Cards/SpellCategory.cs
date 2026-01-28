namespace ProjectSummoner.Cards;

/// <summary>
/// Primary category for spell cards.
/// Determines the spell's fundamental behavior and targeting.
/// </summary>
public enum SpellCategory
{
    /// <summary>Not categorized (legacy or generic).</summary>
    None,

    /// <summary>Direct damage spell.</summary>
    Damage,

    /// <summary>Tactical command spell (Rally, Guard, Charge).</summary>
    Command,

    /// <summary>Healing or buff spell.</summary>
    Support,

    /// <summary>Debuff or crowd control spell.</summary>
    Control,

    /// <summary>Utility spell (vision, teleport, etc.).</summary>
    Utility
}
