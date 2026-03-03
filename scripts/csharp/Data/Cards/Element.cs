namespace ProjectSummoner.Cards;

/// <summary>
/// Elemental affinities for cards, units, and summoners.
/// </summary>
public enum Element
{
    /// <summary>No elemental affinity.</summary>
    Neutral,

    // =========================================================================
    // CORE ELEMENTS
    // =========================================================================

    /// <summary>Core element - flames and heat.</summary>
    Fire,

    /// <summary>Core element - water and fluidity.</summary>
    Water,

    /// <summary>Core element - air and movement.</summary>
    Wind,

    /// <summary>Core element - stone and stability.</summary>
    Earth,

    // =========================================================================
    // OUTER ELEMENTS
    // =========================================================================

    /// <summary>Outer element - electricity and storms.</summary>
    Lightning,

    /// <summary>Outer element - darkness and stealth.</summary>
    Shadow,

    /// <summary>Outer element - toxins and decay.</summary>
    Poison,

    /// <summary>Outer element - healing and growth.</summary>
    Life,

    /// <summary>Outer element - undeath and entropy.</summary>
    Death,

    // =========================================================================
    // OCCULTIST
    // =========================================================================

    /// <summary>Forbidden magic.</summary>
    Occultist,

    // =========================================================================
    // ELEVATED ELEMENTS
    // =========================================================================

    /// <summary>Elevated Fire - divine flames.</summary>
    Holy,

    /// <summary>Elevated Water - frozen stillness.</summary>
    Ice,

    /// <summary>Elevated Earth - refined ore.</summary>
    Metal,

    /// <summary>Elevated Life - ethereal essence.</summary>
    Spirit
}
