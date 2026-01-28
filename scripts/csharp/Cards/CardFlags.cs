using System;

namespace ProjectSummoner.Cards;

/// <summary>
/// Miscellaneous card flags for special handling.
/// </summary>
[Flags]
public enum CardFlags
{
    None = 0,

    /// <summary>Development/testing only - excluded from normal pools.</summary>
    DevOnly = 1 << 0,

    /// <summary>Dummy unit for testing - no AI or attacks.</summary>
    Dummy = 1 << 1
}
