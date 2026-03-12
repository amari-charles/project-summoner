namespace Fateforged.Cards;

/// <summary>
/// Types of cards in the game.
/// </summary>
public enum CardType
{
    /// <summary>
    /// Card that summons units onto the battlefield.
    /// </summary>
    Summon = 0,

    /// <summary>
    /// Card that casts a spell effect (damage, heal, command, etc.).
    /// </summary>
    Spell = 1,
}
