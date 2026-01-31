using System.Collections.Generic;
using ProjectSummoner.Services.Items;

namespace ProjectSummoner.Systems.Modifiers;

/// <summary>
/// Provides unit modifiers from equipped items.
/// Delegates to ItemService for StatModifier conversion.
/// </summary>
public class ItemModifierProvider : IModifierProvider
{
    private readonly string _summonerId;

    public string ProviderId => $"items_{_summonerId}";

    public ItemModifierProvider(string summonerId)
    {
        _summonerId = summonerId;
    }

    public List<StatModifier> GetModifiers()
    {
        var itemService = ItemService.Instance;
        if (itemService == null)
            return [];

        // ItemService handles the TraitModifier -> StatModifier conversion
        return itemService.GetEquippedItemStatModifiers(_summonerId);
    }
}
