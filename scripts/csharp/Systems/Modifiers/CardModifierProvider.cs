using System.Collections.Generic;
using ProjectSummoner.Services.Cards;
using ProjectSummoner.Stats;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Provides card trait modifiers to ModifierService.
/// Each card instance can have traits that modify its stats.
/// This provider wraps those traits in ModifierSystem format with
/// instance-scoped filtering (only applies to the specific card).
///
/// Replaces GDScript card_modifier_provider.gd
/// </summary>
public class CardModifierProvider : IModifierProvider
{
    private readonly string _cardInstanceId;

    public string ProviderId => $"card_{_cardInstanceId}";

    public CardModifierProvider(string cardInstanceId)
    {
        _cardInstanceId = cardInstanceId;
    }

    public List<StatModifier> GetModifiers()
    {
        var modifiers = new List<StatModifier>();

        if (string.IsNullOrEmpty(_cardInstanceId))
            return modifiers;

        // Get combined stat modifiers from CardService
        var cardService = CardService.Instance;
        if (cardService == null)
            return modifiers;

        var statMods = cardService.GetTraitStatModifiersTyped(_cardInstanceId);
        if (statMods.Count == 0)
            return modifiers;

        // Create a modifier with instance scope
        var modifier = new StatModifier
        {
            Source = "card_traits",
            CardInstanceId = _cardInstanceId
        };

        // Convert stat mods to StatMults (parse string keys to StatKey)
        foreach (var kvp in statMods)
        {
            var statKey = StatKeyExtensions.FromString(kvp.Key);
            if (statKey.HasValue)
            {
                modifier.StatMults[statKey.Value] = kvp.Value;
            }
        }

        modifiers.Add(modifier);
        return modifiers;
    }
}
