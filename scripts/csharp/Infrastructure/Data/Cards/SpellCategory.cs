namespace Fateforged.Cards;

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

    /// <summary>Healing spell effects.</summary>
    Heal,

    /// <summary>Tactical command spell (Rally, Guard, Charge).</summary>
    Command,
}
