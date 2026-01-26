using System;
using System.Collections.Generic;
using ProjectSummoner.Domain.Profile;
using ProjectSummoner.Domain.Profile.Account;
using ProjectSummoner.Domain.Profile.Campaign;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Domain.Profile.Decks;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Inventory;
using ProjectSummoner.Domain.Profile.Shop;
using ProjectSummoner.Domain.Profile.Summoners;

namespace ProjectSummoner.Infrastructure.Persistence;

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

    Resources GetResources();
    void UpdateResources(Dictionary<ResourceType, int> delta);

    // =========================================================================
    // SUMMONER OPERATIONS
    // =========================================================================

    string[] GetUnlockedSummoners();
    bool IsSummonerUnlocked(string summonerId);
    bool UnlockSummoner(string summonerId);
    bool SetStartingSummoner(string summonerId, bool chosenRandom);

    SummonerInstance? GetSummonerInstance(string summonerId);
    SummonerInstance[] GetAllSummonerInstances();
    bool SaveSummonerInstance(SummonerInstance instance);

    // =========================================================================
    // CARD COLLECTION OPERATIONS
    // =========================================================================

    string[] GrantCards(IEnumerable<(string catalogId, string rarity)> cards);
    string[] GrantCards(IEnumerable<(string catalogId, string rarity, ContentBinding binding, string? boundTo)> cards);
    bool RemoveCard(string cardInstanceId);
    CardInstance[] ListCards();
    int GetCardCount(string catalogId);
    CardInstance? GetCard(string cardInstanceId);
    bool UpdateCard(string cardInstanceId, Dictionary<string, object> updates);

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    string UpsertDeck(Deck deck);
    bool DeleteDeck(string deckId);
    Deck[] ListDecks();
    Deck? GetDeck(string deckId);

    // =========================================================================
    // CAMPAIGN OPERATIONS
    // =========================================================================

    CampaignProgress GetCampaignProgress(string summonerId);
    void UpdateCampaignProgress(string summonerId, CampaignProgress progress);
    CampaignProgress GetSharedCampaignProgress();
    void UpdateSharedCampaignProgress(CampaignProgress progress);

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
    ShopRefreshState GetShopRefreshState(string shopId);
    bool IncrementShopRefreshEpoch(string shopId);

    // =========================================================================
    // SETTINGS OPERATIONS
    // =========================================================================

    Settings GetSettings();
    void UpdateSettings(Settings settings);

    // =========================================================================
    // ITEM OPERATIONS
    // =========================================================================

    List<ItemInstance> ListItems();
    void SaveItems(List<ItemInstance> items);
}
