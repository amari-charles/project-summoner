using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Cosmetics;
using Fateforged.Data.Emotes;
using Fateforged.Data.Items;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile;
using Fateforged.Domain.Profile.Campaign;
using Fateforged.Domain.Profile.Collection;
using Fateforged.Domain.Profile.Enums;
using Fateforged.Domain.Profile.Inventory;
using Fateforged.Domain.Profile.Summoners;

namespace Fateforged.Meta.Rewards;

public sealed class RewardGrantHandlerRegistry
{
    private readonly IReadOnlyDictionary<Type, IRewardGrantHandler> _handlers;

    public RewardGrantHandlerRegistry(IEnumerable<IRewardGrantHandler> handlers)
    {
        var registered = new Dictionary<Type, IRewardGrantHandler>();
        foreach (var handler in handlers)
        {
            if (!registered.TryAdd(handler.GrantType, handler))
                throw new InvalidOperationException(
                    $"A reward handler is already registered for {handler.GrantType.Name}."
                );
        }
        _handlers = registered;
    }

    public IReadOnlySet<Type> HandledGrantTypes => _handlers.Keys.ToHashSet();

    public RewardGrantPreparation Prepare(RewardGrantDefinition grant, RewardGrantContext context)
    {
        if (!_handlers.TryGetValue(grant.GetType(), out var handler))
        {
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = [$"No reward handler is registered for {grant.GetType().Name}."],
            };
        }

        return handler.Prepare(grant, context);
    }

    public static RewardGrantHandlerRegistry CreateDefault() =>
        new([
            new CardRewardGrantHandler(),
            new ResourceRewardGrantHandler(),
            new ItemRewardGrantHandler(),
            new SummonerUnlockRewardGrantHandler(),
            new CosmeticRewardGrantHandler(),
            new EmoteRewardGrantHandler(),
            new SummonerExperienceRewardGrantHandler(),
            new CardExperienceRewardGrantHandler(),
            new SummonerTraitRewardGrantHandler(),
            new CardTraitRewardGrantHandler(),
            new AcademyProgressFlagRewardGrantHandler(),
        ]);
}

internal sealed class ProfileRewardMutation : IRewardGrantMutation
{
    private readonly Func<ProfileData, (bool success, string error)> _apply;

    public ProfileRewardMutation(Func<ProfileData, (bool success, string error)> apply)
    {
        _apply = apply;
    }

    public bool TryApply(ProfileData profile, out string error)
    {
        var result = _apply(profile);
        error = result.error;
        return result.success;
    }
}

public sealed class CardRewardGrantHandler : RewardGrantHandler<CardRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        CardRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!grant.CardId.HasValue || !CardCatalog.HasCard(grant.CardId) || grant.Count <= 0)
            return Invalid($"Invalid card reward '{grant.CardId}' x{grant.Count}.");
        if (
            grant.Target.Scope
            is not RewardOwnershipScope.Account
                and not RewardOwnershipScope.Summoner
        )
            return Invalid("Card rewards must target an account or summoner.");
        if (
            grant.Target.Scope == RewardOwnershipScope.Summoner
            && string.IsNullOrWhiteSpace(grant.Target.TargetId)
        )
            return Invalid("Summoner-bound card rewards require a summoner target.");

        return Valid(profile =>
        {
            var summonerId =
                grant.Target.Scope == RewardOwnershipScope.Summoner
                    ? new SummonerId(grant.Target.TargetId)
                    : (SummonerId?)null;
            var createdIds = new List<CardInstanceId>();
            for (var i = 0; i < grant.Count; i++)
            {
                var instanceId = new CardInstanceId(Guid.NewGuid().ToString());
                profile.Collection.Add(
                    new CardInstance
                    {
                        Id = instanceId,
                        CatalogId = grant.CardId,
                        ProfileId = profile.ProfileId,
                        Rarity = grant.Rarity,
                        Binding = summonerId.HasValue
                            ? ContentBinding.SummonerBound
                            : ContentBinding.AccountWide,
                        BoundToSummonerId = summonerId,
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    }
                );
                createdIds.Add(instanceId);
            }

            if (grant.Placement == CardRewardPlacement.SelectedDeckIfAvailable)
            {
                var selectedDeck = profile.Decks.FirstOrDefault(deck =>
                    deck.Id.Value == profile.Meta.SelectedDeck
                    && (!summonerId.HasValue || deck.SummonerId == summonerId.Value)
                );
                if (selectedDeck != null)
                {
                    selectedDeck.CardInstanceIds.AddRange(createdIds);
                    selectedDeck.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            }
            return Success();
        });
    }

    private static RewardGrantPreparation Valid(
        Func<ProfileData, (bool success, string error)> apply
    ) => new() { IsValid = true, Mutation = new ProfileRewardMutation(apply) };

    private static RewardGrantPreparation Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };

    private static (bool, string) Success() => (true, "");
}

public sealed class ResourceRewardGrantHandler : RewardGrantHandler<ResourceRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        ResourceRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (grant.Amount <= 0)
            return Invalid("Resource reward amount must be positive.");

        return grant.Target.Scope switch
        {
            RewardOwnershipScope.Account => PrepareAccount(grant),
            RewardOwnershipScope.SummonerCampaign => PrepareCampaign(grant),
            _ => Invalid("Resources must target an account or summoner campaign."),
        };
    }

    private static RewardGrantPreparation PrepareAccount(ResourceRewardGrantDefinition grant)
    {
        if (
            !Enum.TryParse<ResourceType>(grant.ResourceId, true, out var resourceType)
            || !Enum.IsDefined(resourceType)
        )
            return Invalid($"Unknown account resource '{grant.ResourceId}'.");

        return Valid(profile =>
        {
            switch (resourceType)
            {
                case ResourceType.Gold:
                    profile.Resources.Gold += grant.Amount;
                    break;
                case ResourceType.Gems:
                    profile.Resources.Gems += grant.Amount;
                    break;
                case ResourceType.Essence:
                    profile.Resources.Essence += grant.Amount;
                    break;
                case ResourceType.Fragments:
                    profile.Resources.Fragments += grant.Amount;
                    break;
            }
            return (true, "");
        });
    }

    private static RewardGrantPreparation PrepareCampaign(ResourceRewardGrantDefinition grant)
    {
        if (!grant.ResourceId.Equals("gold", StringComparison.OrdinalIgnoreCase))
            return Invalid($"Unknown campaign resource '{grant.ResourceId}'.");
        if (string.IsNullOrWhiteSpace(grant.Target.TargetId))
            return Invalid("Campaign resource reward requires a summoner target.");

        return Valid(profile =>
        {
            var progress = GetOrCreateCampaign(profile, grant.Target.TargetId);
            progress.Gold += grant.Amount;
            return (true, "");
        });
    }

    private static RewardGrantPreparation Valid(
        Func<ProfileData, (bool success, string error)> apply
    ) => new() { IsValid = true, Mutation = new ProfileRewardMutation(apply) };

    private static RewardGrantPreparation Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };

    internal static CampaignProgress GetOrCreateCampaign(ProfileData profile, string summonerId)
    {
        if (!profile.CampaignProgressMap.TryGetValue(summonerId, out var progress))
        {
            progress = new CampaignProgress();
            profile.CampaignProgressMap[summonerId] = progress;
        }
        return progress;
    }
}

public sealed class ItemRewardGrantHandler : RewardGrantHandler<ItemRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        ItemRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!grant.ItemId.HasValue || !ItemCatalog.HasItem(grant.ItemId) || grant.Count <= 0)
            return Invalid($"Invalid item reward '{grant.ItemId}' x{grant.Count}.");
        var definition = ItemCatalog.GetItem(grant.ItemId)!;
        if (
            grant.Target.Scope
            is not RewardOwnershipScope.Account
                and not RewardOwnershipScope.Summoner
        )
            return Invalid("Items must target an account or summoner.");
        if (
            definition.Binding == ItemBinding.SummonerBound
            && (
                grant.Target.Scope != RewardOwnershipScope.Summoner
                || string.IsNullOrWhiteSpace(grant.Target.TargetId)
            )
        )
            return Invalid("Normal item rewards require an explicit summoner target.");
        if (
            definition.Binding == ItemBinding.AccountWide
            && (!definition.IsEventExclusive || grant.Target.Scope != RewardOwnershipScope.Account)
        )
            return Invalid(
                "Account-wide item rewards must be explicitly authored event-exclusive rewards."
            );

        return Valid(profile =>
        {
            SummonerId? boundTo =
                grant.Target.Scope == RewardOwnershipScope.Summoner
                    ? new SummonerId(grant.Target.TargetId)
                    : null;
            if (
                boundTo.HasValue
                && profile.SummonerInstances.All(instance => instance.SummonerId != boundTo.Value)
            )
                return (false, $"Target summoner '{grant.Target.TargetId}' does not exist.");
            for (var i = 0; i < grant.Count; i++)
            {
                profile.Items.Add(
                    new ItemInstance
                    {
                        Id = new ItemId(Guid.NewGuid().ToString()),
                        CatalogId = grant.ItemId,
                        BoundToSummonerId = boundTo,
                    }
                );
            }
            return (true, "");
        });
    }

    private static RewardGrantPreparation Valid(
        Func<ProfileData, (bool success, string error)> apply
    ) => new() { IsValid = true, Mutation = new ProfileRewardMutation(apply) };

    private static RewardGrantPreparation Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };
}

public sealed class SummonerUnlockRewardGrantHandler
    : RewardGrantHandler<SummonerUnlockRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        SummonerUnlockRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!grant.SummonerId.HasValue || !SummonerCatalog.HasSummoner(grant.SummonerId))
            return Invalid($"Unknown summoner '{grant.SummonerId}'.");
        if (grant.Target.Scope != RewardOwnershipScope.Account)
            return Invalid("Summoner unlocks must target the account.");

        return Valid(profile =>
        {
            if (!profile.UnlockedSummoners.Contains(grant.SummonerId))
                profile.UnlockedSummoners.Add(grant.SummonerId);
            if (profile.SummonerInstances.All(instance => instance.SummonerId != grant.SummonerId))
            {
                profile.SummonerInstances.Add(
                    new SummonerInstance { SummonerId = grant.SummonerId }
                );
            }
            return (true, "");
        });
    }

    private static RewardGrantPreparation Valid(
        Func<ProfileData, (bool success, string error)> apply
    ) => new() { IsValid = true, Mutation = new ProfileRewardMutation(apply) };

    private static RewardGrantPreparation Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };
}

public sealed class CosmeticRewardGrantHandler : RewardGrantHandler<CosmeticRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        CosmeticRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!CosmeticsCatalog.HasCosmetic(grant.CosmeticId))
            return Invalid($"Unknown cosmetic '{grant.CosmeticId}'.");
        if (grant.Target.Scope != RewardOwnershipScope.Account)
            return Invalid("Cosmetic unlocks must target the account.");
        return Valid(profile =>
        {
            var id = new CosmeticId(grant.CosmeticId);
            if (!profile.Cosmetics.Owned.Contains(id))
                profile.Cosmetics.Owned.Add(id);
            return (true, "");
        });
    }

    private static RewardGrantPreparation Valid(
        Func<ProfileData, (bool success, string error)> apply
    ) => new() { IsValid = true, Mutation = new ProfileRewardMutation(apply) };

    private static RewardGrantPreparation Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };
}

public sealed class EmoteRewardGrantHandler : RewardGrantHandler<EmoteRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        EmoteRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!EmotesCatalog.HasEmote(grant.EmoteId))
            return Invalid($"Unknown emote '{grant.EmoteId}'.");
        if (grant.Target.Scope != RewardOwnershipScope.Account)
            return Invalid("Emote unlocks must target the account.");
        return Valid(profile =>
        {
            var id = new EmoteId(grant.EmoteId);
            if (!profile.Emotes.Owned.Contains(id))
                profile.Emotes.Owned.Add(id);
            return (true, "");
        });
    }

    private static RewardGrantPreparation Valid(
        Func<ProfileData, (bool success, string error)> apply
    ) => new() { IsValid = true, Mutation = new ProfileRewardMutation(apply) };

    private static RewardGrantPreparation Invalid(string error) =>
        new() { IsValid = false, Errors = [error] };
}

public sealed class SummonerExperienceRewardGrantHandler
    : RewardGrantHandler<SummonerExperienceRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        SummonerExperienceRewardGrantDefinition grant,
        RewardGrantContext context
    ) =>
        PrepareTargetedSummonerMutation(
            grant.Target,
            grant.Amount,
            (instance, amount) => instance.Xp += amount,
            "summoner XP"
        );

    internal static RewardGrantPreparation PrepareTargetedSummonerMutation(
        RewardOwnershipTarget target,
        int amount,
        Action<SummonerInstance, int> apply,
        string label
    )
    {
        if (
            target.Scope != RewardOwnershipScope.Summoner
            || string.IsNullOrWhiteSpace(target.TargetId)
            || amount <= 0
        )
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = [$"{label} requires a summoner target and positive amount."],
            };

        return new RewardGrantPreparation
        {
            IsValid = true,
            Mutation = new ProfileRewardMutation(profile =>
            {
                var instance = profile.SummonerInstances.FirstOrDefault(candidate =>
                    candidate.SummonerId.Value == target.TargetId
                );
                if (instance == null)
                    return (false, $"Summoner '{target.TargetId}' was not found.");
                apply(instance, amount);
                return (true, "");
            }),
        };
    }
}

public sealed class CardExperienceRewardGrantHandler
    : RewardGrantHandler<CardExperienceRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        CardExperienceRewardGrantDefinition grant,
        RewardGrantContext context
    ) =>
        PrepareTargetedCardMutation(
            grant.Target,
            grant.Amount,
            (card, amount) => card.Xp += amount,
            "card XP"
        );

    internal static RewardGrantPreparation PrepareTargetedCardMutation(
        RewardOwnershipTarget target,
        int amount,
        Action<CardInstance, int> apply,
        string label
    )
    {
        if (
            target.Scope != RewardOwnershipScope.CardInstance
            || string.IsNullOrWhiteSpace(target.TargetId)
            || amount <= 0
        )
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = [$"{label} requires a card-instance target and positive amount."],
            };

        return new RewardGrantPreparation
        {
            IsValid = true,
            Mutation = new ProfileRewardMutation(profile =>
            {
                var card = profile.Collection.FirstOrDefault(candidate =>
                    candidate.Id.Value == target.TargetId
                );
                if (card == null)
                    return (false, $"Card instance '{target.TargetId}' was not found.");
                apply(card, amount);
                return (true, "");
            }),
        };
    }
}

public sealed class SummonerTraitRewardGrantHandler
    : RewardGrantHandler<SummonerTraitRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        SummonerTraitRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!grant.TraitId.HasValue || !TraitCatalog.HasTrait(grant.TraitId))
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = [$"Unknown summoner trait '{grant.TraitId}'."],
            };
        if (grant.Amount != 1)
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = ["Summoner trait rewards must grant exactly one trait."],
            };
        return SummonerExperienceRewardGrantHandler.PrepareTargetedSummonerMutation(
            grant.Target,
            grant.Amount,
            (instance, _) =>
            {
                if (!instance.AcquiredTraitIds.Contains(grant.TraitId))
                    instance.AcquiredTraitIds.Add(grant.TraitId);
            },
            "summoner trait"
        );
    }
}

public sealed class CardTraitRewardGrantHandler : RewardGrantHandler<CardTraitRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        CardTraitRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (!grant.TraitId.HasValue)
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = ["Card trait ID is required."],
            };
        if (grant.Amount != 1)
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = ["Card trait rewards must grant exactly one trait."],
            };
        return CardExperienceRewardGrantHandler.PrepareTargetedCardMutation(
            grant.Target,
            grant.Amount,
            (card, _) =>
            {
                if (!card.Traits.Contains(grant.TraitId))
                    card.Traits.Add(grant.TraitId);
            },
            "card trait"
        );
    }
}

public sealed class AcademyProgressFlagRewardGrantHandler
    : RewardGrantHandler<AcademyProgressFlagRewardGrantDefinition>
{
    public override RewardGrantPreparation Prepare(
        AcademyProgressFlagRewardGrantDefinition grant,
        RewardGrantContext context
    )
    {
        if (
            grant.Target.Scope != RewardOwnershipScope.SummonerCampaign
            || string.IsNullOrWhiteSpace(grant.Target.TargetId)
            || string.IsNullOrWhiteSpace(grant.FlagId)
            || grant.Amount <= 0
        )
        {
            return new RewardGrantPreparation
            {
                IsValid = false,
                Errors = ["Academy progress flag requires a summoner campaign target."],
            };
        }

        return new RewardGrantPreparation
        {
            IsValid = true,
            Mutation = new ProfileRewardMutation(profile =>
            {
                var progress = ResourceRewardGrantHandler.GetOrCreateCampaign(
                    profile,
                    grant.Target.TargetId
                );
                progress.Academy.RewardFlags[grant.FlagId] =
                    progress.Academy.RewardFlags.GetValueOrDefault(grant.FlagId) + grant.Amount;
                return (true, "");
            }),
        };
    }
}
