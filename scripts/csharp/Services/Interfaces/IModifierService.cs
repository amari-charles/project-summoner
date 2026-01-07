using System.Collections.Generic;
using ProjectSummoner.Systems.Modifiers;

namespace ProjectSummoner.Services.Interfaces;

/// <summary>
/// Interface for the ModifierService.
/// Enables dependency injection and testing.
/// </summary>
public interface IModifierService
{
    // =========================================================================
    // PROVIDER REGISTRATION
    // =========================================================================

    /// <summary>
    /// Register a modifier provider.
    /// </summary>
    void RegisterProvider(IModifierProvider provider);

    /// <summary>
    /// Register a provider with a custom ID.
    /// </summary>
    void RegisterProvider(string providerId, IModifierProvider provider);

    /// <summary>
    /// Unregister a provider by ID.
    /// </summary>
    void UnregisterProvider(string providerId);

    /// <summary>
    /// Unregister a provider.
    /// </summary>
    void UnregisterProvider(IModifierProvider provider);

    /// <summary>
    /// Clear all providers.
    /// </summary>
    void ClearProviders();

    // =========================================================================
    // MODIFIER COLLECTION
    // =========================================================================

    /// <summary>
    /// Get all modifiers that apply to a target.
    /// </summary>
    List<StatModifier> GetModifiers(ModifierContext context);
}
