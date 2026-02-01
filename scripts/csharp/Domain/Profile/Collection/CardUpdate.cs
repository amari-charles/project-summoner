using System.Collections.Generic;
using ProjectSummoner.Cards;

namespace ProjectSummoner.Domain.Profile.Collection;

/// <summary>
/// Typed DTO for updating card instance fields.
/// Null values indicate "do not update this field".
/// </summary>
public class CardUpdate
{
    /// <summary>Update the card's XP.</summary>
    public int? Xp { get; set; }

    /// <summary>Update the card's level.</summary>
    public int? Level { get; set; }

    /// <summary>Replace the card's traits list.</summary>
    public List<CardTraitId>? Traits { get; set; }
}
