using System;

namespace Fateforged.Cards;

/// <summary>
/// Visual traits affecting rendering and animation.
/// </summary>
[Flags]
public enum VisualTrait
{
    None = 0,

    /// <summary>Unit uses shared wisp visual rig and needs element-based tinting.</summary>
    UsesWispVisuals = 1 << 0,
}
