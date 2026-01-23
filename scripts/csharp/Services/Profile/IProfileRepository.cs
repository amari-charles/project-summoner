using System;
using System.Collections.Generic;
using Godot;
using ProjectSummoner.Data.Items;
using ProjectSummoner.Data.Profile;

namespace ProjectSummoner.Services.Profile;

/// <summary>
/// Profile repository interface for C# code.
/// Provides typed access to profile data operations.
/// </summary>
public interface IProfileRepository
{
    // =========================================================================
    // EVENTS
    // =========================================================================

    event Action<string>? ProfileLoaded;
    event Action<string>? ProfileSaved;
    event Action<string>? SaveFailed;
    event Action? DataChanged;

    // =========================================================================
    // PROFILE OPERATIONS
    // =========================================================================

    bool LoadProfile(string profileId);
    void SaveProfile(bool immediate = false);
    string GetCurrentProfileId();
    void ResetProfile();
    ProfileData? GetProfileSnapshot();

    // =========================================================================
    // RESOURCE OPERATIONS
    // =========================================================================

    ResourceData GetResources();
    void UpdateResources(Dictionary<ResourceType, int> delta);

    // =========================================================================
    // SUMMONER OPERATIONS
    // =========================================================================

    string[] GetUnlockedSummoners();
    bool IsSummonerUnlocked(string summonerId);
    bool UnlockSummoner(string summonerId);
    bool SetStartingSummoner(string summonerId, bool chosenRandom);

    SummonerInstanceData? GetSummonerInstance(string summonerId);
    SummonerInstanceData[] GetAllSummonerInstances();
    bool SaveSummonerInstance(SummonerInstanceData instance);

    // =========================================================================
    // CARD COLLECTION OPERATIONS
    // =========================================================================

    string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards);
    string[] GrantCards(IEnumerable<(string catalogId, string rarity, ContentBinding binding, string? boundTo)> cards);
    bool RemoveCard(string cardInstanceId);
    CardInstanceData[] ListCards();
    int GetCardCount(string catalogId);
    CardInstanceData? GetCard(string cardInstanceId);
    bool UpdateCard(string cardInstanceId, Dictionary<string, object> updates);

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    string UpsertDeck(DeckData deck);
    bool DeleteDeck(string deckId);
    DeckData[] ListDecks();
    DeckData? GetDeck(string deckId);

    // =========================================================================
    // CAMPAIGN OPERATIONS
    // =========================================================================

    CampaignProgressData GetCampaignProgress(string summonerId);
    void UpdateCampaignProgress(string summonerId, CampaignProgressData progress);
    CampaignProgressData GetSharedCampaignProgress();
    void UpdateSharedCampaignProgress(CampaignProgressData progress);

    // =========================================================================
    // COSMETIC OPERATIONS
    // =========================================================================

    string[] GetOwnedCosmetics();
    bool IsCosmeticOwned(string cosmeticId);
    bool GrantCosmetic(string cosmeticId);
    Dictionary<string, string> GetEquippedCosmetics();
    bool EquipCosmetic(string slot, string cosmeticId);
    Dictionary<string, string> GetSummonerSkins();
    bool SetSummonerSkin(string summonerId, string skinId);

    // =========================================================================
    // EMOTE OPERATIONS
    // =========================================================================

    string[] GetOwnedEmotes();
    bool IsEmoteOwned(string emoteId);
    bool GrantEmote(string emoteId);
    string[] GetEquippedEmotes();
    bool EquipEmote(int slot, string emoteId);

    // =========================================================================
    // SHOP OPERATIONS
    // =========================================================================

    int GetPurchaseCount(string purchaseKey);
    bool IncrementPurchaseCount(string purchaseKey);
    (int epoch, string lastRefreshAt) GetShopRefreshState(string shopId);
    bool IncrementShopRefreshEpoch(string shopId);

    // =========================================================================
    // SETTINGS OPERATIONS
    // =========================================================================

    SettingsData GetSettings();
    void UpdateSettings(SettingsData settings);

    // =========================================================================
    // ITEM OPERATIONS
    // =========================================================================

    List<ItemInstanceData> ListItems();
    void SaveItems(List<ItemInstanceData> items);
}
