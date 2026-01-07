using System.Collections.Generic;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Interface for modifier providers.
/// Providers supply modifiers from various sources (card upgrades, traits, buffs, etc.).
/// </summary>
public interface IModifierProvider
{
    /// <summary>
    /// Unique identifier for this provider.
    /// Used for registration and debugging.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Get all modifiers from this provider.
    /// The ModifierService will filter these based on context.
    /// </summary>
    List<StatModifier> GetModifiers();
}
