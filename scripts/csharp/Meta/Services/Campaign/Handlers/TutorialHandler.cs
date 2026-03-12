using Fateforged.Data.Events;
using Godot;

namespace Fateforged.Meta.Campaign.Handlers;

/// <summary>
/// Handles tutorial-related queries.
/// Typed API — facades handle string conversion.
/// </summary>
public class TutorialHandler
{
    private readonly CampaignDataStore _store;
    private readonly CampaignCatalogHandler _catalog;
    private readonly CampaignProgressHandler _progress;

    public TutorialHandler(
        CampaignDataStore store,
        CampaignCatalogHandler catalog,
        CampaignProgressHandler progress
    )
    {
        _store = store;
        _catalog = catalog;
        _progress = progress;
    }

    /// <summary>Check if a specific battle is a tutorial battle.</summary>
    public bool IsBattleTutorial(EventId eventId)
    {
        if (!_store.Events.TryGetValue(eventId, out var evt))
            return false;

        return evt is BattleEventDefinition battleEvt && battleEvt.IsTutorial;
    }

    /// <summary>Check if all tutorial battles have been completed.</summary>
    public bool IsTutorialComplete()
    {
        foreach (var (eventId, evt) in _store.Events)
        {
            if (evt is BattleEventDefinition battleEvt && battleEvt.IsTutorial)
            {
                if (!_progress.IsBattleCompleted(new BattleId(eventId.Value)))
                    return false;
            }
        }

        return true;
    }

    /// <summary>Get list of all tutorial battle IDs.</summary>
    public Godot.Collections.Array<string> GetTutorialBattles()
    {
        var result = new Godot.Collections.Array<string>();

        foreach (var (eventId, evt) in _store.Events)
        {
            if (evt is BattleEventDefinition battleEvt && battleEvt.IsTutorial)
            {
                result.Add(eventId.Value);
            }
        }

        return result;
    }
}
