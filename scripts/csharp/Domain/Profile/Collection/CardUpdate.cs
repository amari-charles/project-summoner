using System.Collections.Generic;

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

    /// <summary>Replace the card's upgrades list.</summary>
    public List<string>? Upgrades { get; set; }
}
