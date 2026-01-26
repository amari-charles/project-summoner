using System.Collections.Generic;
using Godot;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Handles tutorial-related queries.
/// </summary>
public class TutorialHandler
{
    private readonly CampaignDataStore _store;
    private readonly CampaignCatalogHandler _catalog;
    private readonly CampaignProgressHandler _progress;

    public TutorialHandler(
        CampaignDataStore store,
        CampaignCatalogHandler catalog,
        CampaignProgressHandler progress)
    {
        _store = store;
        _catalog = catalog;
        _progress = progress;
    }

    /// <summary>Check if a specific battle is a tutorial battle.</summary>
    public bool IsBattleTutorial(string battleId)
    {
        if (!_store.Battles.TryGetValue(battleId, out Godot.Collections.Dictionary? battle) || battle == null)
            return false;

        return battle.GetValueOrDefault("is_tutorial", false).AsBool();
    }

    /// <summary>Check if all tutorial battles have been completed.</summary>
    public bool IsTutorialComplete()
    {
        foreach (Godot.Collections.Dictionary battle in _catalog.GetAllBattles())
        {
            if (battle.GetValueOrDefault("is_tutorial", false).AsBool())
            {
                var battleId = battle.GetValueOrDefault("id", "").AsString();
                if (!_progress.IsBattleCompleted(battleId))
                    return false;
            }
        }

        return true;
    }

    /// <summary>Get list of all tutorial battle IDs.</summary>
    public Godot.Collections.Array<string> GetTutorialBattles()
    {
        var result = new Godot.Collections.Array<string>();

        foreach (Godot.Collections.Dictionary battle in _catalog.GetAllBattles())
        {
            if (battle.GetValueOrDefault("is_tutorial", false).AsBool())
            {
                var battleId = battle.GetValueOrDefault("id", "").AsString();
                result.Add(battleId);
            }
        }

        return result;
    }
}
