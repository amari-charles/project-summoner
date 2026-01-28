using System;

namespace ProjectSummoner.Cards;

/// <summary>
/// Visual traits affecting rendering and animation.
/// </summary>
[Flags]
public enum VisualTrait
{
    None = 0,

    /// <summary>Unit floats above ground (visual only, not flying).</summary>
    Floating = 1 << 0,

    /// <summary>Unit uses shared wisp visual rig and needs element-based tinting.</summary>
    UsesWispVisuals = 1 << 1,

    /// <summary>Unit glows or emits light.</summary>
    Glowing = 1 << 2,

    /// <summary>Unit has transparency/ethereal effect.</summary>
    Ethereal = 1 << 3
}
