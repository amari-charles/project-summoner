using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ProjectSummoner.Services.Campaign.Handlers;

/// <summary>
/// Handles campaign and battle catalog queries.
/// </summary>
public class CampaignCatalogHandler
{
    private readonly CampaignDataStore _store;
    private readonly CampaignProgressHandler _progress;

    public CampaignCatalogHandler(CampaignDataStore store, CampaignProgressHandler progress)
    {
        _store = store;
        _progress = progress;
    }

    // =========================================================================
    // DATA LOADING
    // =========================================================================

    /// <summary>Load campaign data from GDScript.</summary>
    public void LoadCampaignsFromGDScript(Godot.Collections.Array<Godot.Collections.Dictionary> campaigns)
    {
        _store.Campaigns.Clear();
        _store.Battles.Clear();

        foreach (var campaign in campaigns)
        {
            var campaignId = campaign.GetValueOrDefault("campaign_id", "").AsString();
            if (string.IsNullOrEmpty(campaignId))
                continue;

            _store.Campaigns[campaignId] = campaign;

            // Load battles from campaign
            var battlesArray = campaign.GetValueOrDefault("battles", new Godot.Collections.Array());
            if (battlesArray.Obj is Godot.Collections.Array battles)
            {
                foreach (var battleVariant in battles)
                {
                    if (battleVariant.Obj is Godot.Collections.Dictionary battle)
                    {
                        var battleId = battle.GetValueOrDefault("id", "").AsString();
                        if (!string.IsNullOrEmpty(battleId))
                        {
                            _store.Battles[battleId] = battle;
                        }
                    }
                }
            }
        }

        GD.Print($"CampaignCatalogHandler: Loaded {_store.Campaigns.Count} campaigns with {_store.Battles.Count} total battles");
    }

    // =========================================================================
    // CAMPAIGN QUERIES
    // =========================================================================

    /// <summary>Get all campaigns with unlock status.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllCampaigns()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var kvp in _store.Campaigns)
        {
            var campaign = new Godot.Collections.Dictionary();
            foreach (var key in kvp.Value.Keys)
            {
                campaign[key] = kvp.Value[key];
            }
            campaign["is_unlocked"] = IsCampaignUnlocked(kvp.Key);
            result.Add(campaign);
        }

        // Sort by sort_order
        var sorted = result.OrderBy(c => c.GetValueOrDefault("sort_order", 999).AsInt32()).ToList();
        result.Clear();
        foreach (var c in sorted)
        {
            result.Add(c);
        }

        return result;
    }

    /// <summary>Get a specific campaign's metadata.</summary>
    public Godot.Collections.Dictionary GetCampaign(string campaignId)
    {
        if (_store.Campaigns.TryGetValue(campaignId, out var campaign))
            return campaign;
        return new Godot.Collections.Dictionary();
    }

    /// <summary>Check if a campaign is unlocked.</summary>
    public bool IsCampaignUnlocked(string campaignId)
    {
        if (!_store.Campaigns.TryGetValue(campaignId, out var campaign))
            return false;

        var requirements = campaign.GetValueOrDefault("unlock_requirements", new Godot.Collections.Array());
        if (requirements.Obj is not Godot.Collections.Array reqArray || reqArray.Count == 0)
            return true;

        foreach (var req in reqArray)
        {
            var reqStr = req.AsString();
            if (!_progress.IsCampaignComplete(reqStr))
                return false;
        }

        return true;
    }

    // =========================================================================
    // BATTLE QUERIES
    // =========================================================================

    /// <summary>Get all battles for the current campaign.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetAllBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        if (!_store.Campaigns.TryGetValue(_store.CurrentCampaignId, out var campaign))
            return result;

        var battlesVariant = campaign.GetValueOrDefault("battles", new Godot.Collections.Array());
        if (battlesVariant.Obj is Godot.Collections.Array battles)
        {
            foreach (var battleVariant in battles)
            {
                if (battleVariant.Obj is Godot.Collections.Dictionary battle)
                {
                    result.Add(battle);
                }
            }
        }

        return result;
    }

    /// <summary>Get a specific battle by ID.</summary>
    public Godot.Collections.Dictionary GetBattle(string battleId)
    {
        if (_store.Battles.TryGetValue(battleId, out var battle))
            return battle;
        return new Godot.Collections.Dictionary();
    }

    /// <summary>Get all completed battles.</summary>
    public Godot.Collections.Array<Godot.Collections.Dictionary> GetCompletedBattles()
    {
        var result = new Godot.Collections.Array<Godot.Collections.Dictionary>();

        foreach (var battleId in _store.CompletedBattles)
        {
            if (_store.Battles.TryGetValue(battleId, out var battle))
            {
                result.Add(battle);
            }
        }

        return result;
    }
}
