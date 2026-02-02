using System;
using System.Collections.Generic;
using ProjectSummoner.Cards;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Domain.Profile;
using ProjectSummoner.Domain.Profile.Account;
using ProjectSummoner.Domain.Profile.Campaign;
using ProjectSummoner.Domain.Profile.Collection;
using ProjectSummoner.Domain.Profile.Decks;
using ProjectSummoner.Domain.Profile.Enums;
using ProjectSummoner.Domain.Profile.Inventory;
using ProjectSummoner.Domain.Profile.Shop;
using ProjectSummoner.Domain.Profile.Summoners;
using ProjectSummoner.Services.Deck;
using ProjectSummoner.Services.Shop;

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

    bool LoadProfile(ProfileId profileId);
    void SaveProfile(bool immediate = false);
    ProfileId GetCurrentProfileId();
    void ResetProfile();
    /// <summary>
    /// Get partial profile metadata (Version, ProfileId, UpdatedAt, Resources, UnlockedSummoners, Meta).
    /// NOTE: This is READ-ONLY metadata. To update fields, use specific update methods like UpdateProfileMeta().
    /// </summary>
    ProfileData? GetProfileMetadata();

    // =========================================================================
    // RESOURCE OPERATIONS
    // =========================================================================

    Resources GetResources();
    void UpdateResources(Dictionary<ResourceType, int> delta);

    // =========================================================================
    // SUMMONER OPERATIONS
    // =========================================================================

    SummonerId[] GetUnlockedSummoners();
    bool IsSummonerUnlocked(SummonerId summonerId);
    bool UnlockSummoner(SummonerId summonerId);
    bool SetStartingSummoner(SummonerId summonerId, bool chosenRandom);

    SummonerInstance? GetSummonerInstance(SummonerId summonerId);
    SummonerInstance[] GetAllSummonerInstances();
    bool SaveSummonerInstance(SummonerInstance instance);

    // =========================================================================
    // CARD COLLECTION OPERATIONS
    // =========================================================================

    CardInstanceId[] GrantCards(IEnumerable<(CardId catalogId, string rarity)> cards);
    CardInstanceId[] GrantCards(IEnumerable<(CardId catalogId, string rarity, ContentBinding binding, SummonerId? boundTo)> cards);
    bool RemoveCard(CardInstanceId cardInstanceId);
    CardInstance[] ListCards();
    int GetCardCount(CardId catalogId);
    CardInstance? GetCard(CardInstanceId cardInstanceId);
    bool UpdateCard(CardInstanceId cardInstanceId, CardUpdate updates);

    // =========================================================================
    // DECK OPERATIONS
    // =========================================================================

    DeckId UpsertDeck(Deck deck);
    bool DeleteDeck(DeckId deckId);
    Deck[] ListDecks();
    Deck? GetDeck(DeckId deckId);

    // =========================================================================
    // CAMPAIGN OPERATIONS
    // =========================================================================

    CampaignProgress GetCampaignProgress(SummonerId summonerId);
    void UpdateCampaignProgress(SummonerId summonerId, CampaignProgress progress);
    CampaignProgress GetSharedCampaignProgress();
    void UpdateSharedCampaignProgress(CampaignProgress progress);

    // =========================================================================
    // COSMETIC OPERATIONS
    // =========================================================================

    CosmeticId[] GetOwnedCosmetics();
    bool IsCosmeticOwned(CosmeticId cosmeticId);
    bool GrantCosmetic(CosmeticId cosmeticId);
    Dictionary<string, CosmeticId> GetEquippedCosmetics();
    bool EquipCosmetic(string slot, CosmeticId cosmeticId);
    Dictionary<SummonerId, SkinId> GetSummonerSkins();
    bool SetSummonerSkin(SummonerId summonerId, SkinId skinId);

    // =========================================================================
    // EMOTE OPERATIONS
    // =========================================================================

    EmoteId[] GetOwnedEmotes();
    bool IsEmoteOwned(EmoteId emoteId);
    bool GrantEmote(EmoteId emoteId);
    EmoteId[] GetEquippedEmotes();
    bool EquipEmote(int slot, EmoteId emoteId);

    // =========================================================================
    // SHOP OPERATIONS
    // =========================================================================

    int GetPurchaseCount(string purchaseKey);
    bool IncrementPurchaseCount(string purchaseKey);
    ShopRefreshState GetShopRefreshState(ShopId shopId);
    bool IncrementShopRefreshEpoch(ShopId shopId);

    // =========================================================================
    // META OPERATIONS
    // =========================================================================

    /// <summary>Update profile meta fields (merges with existing).</summary>
    void UpdateProfileMeta(MetaUpdate updates);

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
