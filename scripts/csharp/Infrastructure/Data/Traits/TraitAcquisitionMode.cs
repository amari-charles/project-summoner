namespace Fateforged.Data.Traits;

/// <summary>
/// Controls how a trait can be acquired.
/// </summary>
public enum TraitAcquisitionMode
{
    /// <summary>
    /// Trait can be rolled and spent from the level-up offer pool.
    /// </summary>
    LevelUpOffer,

    /// <summary>
    /// Trait is granted externally (events, story rewards, scripted rewards) and is not offerable.
    /// </summary>
    GrantedOnly
}

/// <summary>
/// Extension methods for TraitAcquisitionMode.
/// </summary>
public static class TraitAcquisitionModeExtensions
{
    /// <summary>Convert enum to lowercase string value for serialization/interop.</summary>
    public static string ToStringValue(this TraitAcquisitionMode mode) => mode switch
    {
        TraitAcquisitionMode.LevelUpOffer => "level_up_offer",
        TraitAcquisitionMode.GrantedOnly => "granted_only",
        _ => "level_up_offer"
    };

    /// <summary>Parse string to TraitAcquisitionMode.</summary>
    public static TraitAcquisitionMode Parse(string value) => value.ToLowerInvariant() switch
    {
        "level_up_offer" => TraitAcquisitionMode.LevelUpOffer,
        "granted_only" => TraitAcquisitionMode.GrantedOnly,
        _ => TraitAcquisitionMode.LevelUpOffer
    };
}
