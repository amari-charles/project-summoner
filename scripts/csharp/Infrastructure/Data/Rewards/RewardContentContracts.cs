using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fateforged.Cards;
using Fateforged.Data.Academy;
using Fateforged.Data.Cosmetics;
using Fateforged.Data.Emotes;
using Fateforged.Data.Events;
using Fateforged.Data.Items;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;

namespace Fateforged.Data.Rewards;

public sealed record RewardContentCatalog
{
    public ImmutableDictionary<UniversalRewardPoolId, RewardPoolDefinition> Pools { get; init; } =
        ImmutableDictionary<UniversalRewardPoolId, RewardPoolDefinition>.Empty;
}

public sealed record RewardContentLoadResult
{
    public bool IsReady { get; init; }
    public RewardContentCatalog Catalog { get; init; } = new();
    public ImmutableArray<string> Errors { get; init; } = [];
}

public interface IRewardContentLoader
{
    RewardContentLoadResult Load(string contentRoot);
}

public interface IRewardContentValidator
{
    ImmutableArray<string> Validate(
        RewardContentCatalog catalog,
        IEnumerable<RewardOfferDefinition> embeddedOffers
    );
}

public sealed class RewardContentLoader : IRewardContentLoader
{
    private readonly IRewardContentValidator _validator;

    public RewardContentLoader(IRewardContentValidator validator)
    {
        _validator = validator;
    }

    public RewardContentLoadResult Load(string contentRoot)
    {
        if (!Directory.Exists(contentRoot))
        {
            return new RewardContentLoadResult
            {
                IsReady = false,
                Errors = [$"Reward content directory '{contentRoot}' does not exist."],
            };
        }

        var pools = ImmutableDictionary.CreateBuilder<
            UniversalRewardPoolId,
            RewardPoolDefinition
        >();
        var errors = ImmutableArray.CreateBuilder<string>();

        foreach (var path in Directory.GetFiles(contentRoot, "*.json").Order())
        {
            try
            {
                var file = JsonSerializer.Deserialize<RewardPoolFile>(
                    File.ReadAllText(path),
                    RewardJson.Options
                );
                if (file == null)
                {
                    errors.Add($"{path}: reward content was empty.");
                    continue;
                }

                foreach (var pool in file.Pools)
                {
                    if (!pool.Id.HasValue)
                    {
                        errors.Add($"{path}: pool ID is required.");
                        continue;
                    }
                    if (!pools.TryAdd(pool.Id, pool))
                        errors.Add($"{path}: duplicate reward pool ID '{pool.Id}'.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"{path}: {exception.Message}");
            }
        }

        var catalog = new RewardContentCatalog { Pools = pools.ToImmutable() };
        errors.AddRange(_validator.Validate(catalog, []));
        return new RewardContentLoadResult
        {
            IsReady = errors.Count == 0,
            Catalog = catalog,
            Errors = errors.ToImmutable(),
        };
    }

    private sealed record RewardPoolFile
    {
        public ImmutableArray<RewardPoolDefinition> Pools { get; init; } = [];
    }
}

public sealed class RewardContentValidator : IRewardContentValidator
{
    private readonly IReadOnlySet<Type> _handledGrantTypes;

    public RewardContentValidator(IEnumerable<Type> handledGrantTypes)
    {
        _handledGrantTypes = handledGrantTypes.ToHashSet();
    }

    public ImmutableArray<string> Validate(
        RewardContentCatalog catalog,
        IEnumerable<RewardOfferDefinition> embeddedOffers
    )
    {
        var errors = ImmutableArray.CreateBuilder<string>();
        foreach (var pool in catalog.Pools.Values)
            ValidateOptions($"pool '{pool.Id}'", pool.Options, errors);
        foreach (var offer in embeddedOffers)
            ValidateOffer(offer, catalog, errors);
        return errors.ToImmutable();
    }

    private void ValidateOffer(
        RewardOfferDefinition offer,
        RewardContentCatalog catalog,
        ImmutableArray<string>.Builder errors
    )
    {
        var location = $"offer '{offer.Id}'";
        if (!offer.Id.HasValue)
            errors.Add("Reward offer ID is required.");
        if (offer.Selection.ShowCount <= 0 || offer.Selection.ChooseCount <= 0)
            errors.Add($"{location}: showCount and chooseCount must be positive.");
        if (offer.Selection.ChooseCount > offer.Selection.ShowCount)
            errors.Add($"{location}: chooseCount cannot exceed showCount.");

        switch (offer.OptionSource)
        {
            case AuthoredRewardOptionSourceDefinition authored:
                ValidateOptions(location, authored.Options, errors);
                if (authored.Options.Length < offer.Selection.ChooseCount)
                    errors.Add($"{location}: authored options cannot satisfy chooseCount.");
                break;
            case PoolRewardOptionSourceDefinition pool:
                if (!pool.PoolId.HasValue || !catalog.Pools.ContainsKey(pool.PoolId))
                    errors.Add($"{location}: unknown reward pool '{pool.PoolId}'.");
                break;
            default:
                errors.Add($"{location}: unsupported option source.");
                break;
        }
    }

    private void ValidateOptions(
        string location,
        ImmutableArray<RewardOptionDefinition> options,
        ImmutableArray<string>.Builder errors
    )
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            if (!option.Id.HasValue)
                errors.Add($"{location}: option ID is required.");
            else if (!ids.Add(option.Id.Value))
                errors.Add($"{location}: duplicate option ID '{option.Id}'.");
            if (option.Grants.IsDefaultOrEmpty)
                errors.Add($"{location} option '{option.Id}': at least one grant is required.");
            foreach (var grant in option.Grants)
                ValidateGrant($"{location} option '{option.Id}'", grant, errors);
        }
    }

    private void ValidateGrant(
        string location,
        RewardGrantDefinition grant,
        ImmutableArray<string>.Builder errors
    )
    {
        if (!_handledGrantTypes.Contains(grant.GetType()))
            errors.Add($"{location}: no handler is registered for {grant.GetType().Name}.");

        if (
            grant.Target.Scope != RewardOwnershipScope.Account
            && string.IsNullOrWhiteSpace(grant.Target.TargetId)
        )
        {
            errors.Add($"{location}: {grant.Target.Scope} target ID is required.");
        }
        if (!SupportsTarget(grant))
            errors.Add($"{location}: {grant.GetType().Name} does not support {grant.Target.Scope} ownership.");

        switch (grant)
        {
            case CardRewardGrantDefinition card
                when !card.CardId.HasValue || !CardCatalog.HasCard(card.CardId):
                errors.Add($"{location}: unknown card '{card.CardId}'.");
                break;
            case CardRewardGrantDefinition card when card.Count <= 0:
                errors.Add($"{location}: card count must be positive.");
                break;
            case ResourceRewardGrantDefinition resource
                when string.IsNullOrWhiteSpace(resource.ResourceId) || resource.Amount <= 0:
                errors.Add($"{location}: resource ID and positive amount are required.");
                break;
            case ItemRewardGrantDefinition item
                when !item.ItemId.HasValue || !ItemCatalog.HasItem(item.ItemId):
                errors.Add($"{location}: unknown item '{item.ItemId}'.");
                break;
            case ItemRewardGrantDefinition item when item.Count <= 0:
                errors.Add($"{location}: item count must be positive.");
                break;
            case SummonerUnlockRewardGrantDefinition summoner
                when !summoner.SummonerId.HasValue
                    || !SummonerCatalog.HasSummoner(summoner.SummonerId):
                errors.Add($"{location}: unknown summoner '{summoner.SummonerId}'.");
                break;
            case SummonerTraitRewardGrantDefinition trait
                when !trait.TraitId.HasValue || !TraitCatalog.HasTrait(trait.TraitId):
                errors.Add($"{location}: unknown summoner trait '{trait.TraitId}'.");
                break;
            case SummonerTraitRewardGrantDefinition trait when trait.Amount != 1:
                errors.Add($"{location}: summoner trait rewards must grant exactly one trait.");
                break;
            case CardTraitRewardGrantDefinition trait when !trait.TraitId.HasValue:
                errors.Add($"{location}: card trait ID is required.");
                break;
            case CardTraitRewardGrantDefinition trait when trait.Amount != 1:
                errors.Add($"{location}: card trait rewards must grant exactly one trait.");
                break;
            case CosmeticRewardGrantDefinition cosmetic
                when string.IsNullOrWhiteSpace(cosmetic.CosmeticId)
                    || !CosmeticsCatalog.HasCosmetic(cosmetic.CosmeticId):
                errors.Add($"{location}: unknown cosmetic '{cosmetic.CosmeticId}'.");
                break;
            case EmoteRewardGrantDefinition emote
                when string.IsNullOrWhiteSpace(emote.EmoteId)
                    || !EmotesCatalog.HasEmote(emote.EmoteId):
                errors.Add($"{location}: unknown emote '{emote.EmoteId}'.");
                break;
            case SummonerExperienceRewardGrantDefinition xp when xp.Amount <= 0:
                errors.Add($"{location}: summoner XP must be positive.");
                break;
            case CardExperienceRewardGrantDefinition xp when xp.Amount <= 0:
                errors.Add($"{location}: card XP must be positive.");
                break;
            case AcademyProgressFlagRewardGrantDefinition flag
                when string.IsNullOrWhiteSpace(flag.FlagId):
                errors.Add($"{location}: Academy flag ID is required.");
                break;
        }
    }

    private static bool SupportsTarget(RewardGrantDefinition grant) =>
        grant switch
        {
            CardRewardGrantDefinition => grant.Target.Scope
                is RewardOwnershipScope.Account or RewardOwnershipScope.Summoner,
            ResourceRewardGrantDefinition => grant.Target.Scope
                is RewardOwnershipScope.Account or RewardOwnershipScope.SummonerCampaign,
            ItemRewardGrantDefinition => grant.Target.Scope
                is RewardOwnershipScope.Account or RewardOwnershipScope.Summoner,
            SummonerUnlockRewardGrantDefinition
            or CosmeticRewardGrantDefinition
            or EmoteRewardGrantDefinition => grant.Target.Scope == RewardOwnershipScope.Account,
            SummonerExperienceRewardGrantDefinition
            or SummonerTraitRewardGrantDefinition => grant.Target.Scope
                == RewardOwnershipScope.Summoner,
            CardExperienceRewardGrantDefinition or CardTraitRewardGrantDefinition =>
                grant.Target.Scope == RewardOwnershipScope.CardInstance,
            AcademyProgressFlagRewardGrantDefinition => grant.Target.Scope
                == RewardOwnershipScope.SummonerCampaign,
            _ => false,
        };
}

internal static class RewardJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        options.Converters.Add(new RewardOfferIdConverter());
        options.Converters.Add(new RewardOptionIdConverter());
        options.Converters.Add(new RewardPoolIdConverter());
        options.Converters.Add(new CardIdConverter());
        options.Converters.Add(new ItemIdConverter());
        options.Converters.Add(new SummonerIdConverter());
        options.Converters.Add(new TraitIdConverter());
        options.Converters.Add(new CardTraitIdConverter());
        options.Converters.Add(new CourseIdConverter());
        options.Converters.Add(new BiomeIdConverter());
        return options;
    }

    private abstract class StringIdConverter<T> : JsonConverter<T>
    {
        protected abstract T Create(string value);
        protected abstract string GetValue(T value);

        public override T Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) => Create(reader.GetString() ?? "");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(GetValue(value));
    }

    private sealed class RewardOfferIdConverter : StringIdConverter<RewardOfferId>
    {
        protected override RewardOfferId Create(string value) => new(value);
        protected override string GetValue(RewardOfferId value) => value.Value;
    }

    private sealed class RewardOptionIdConverter : StringIdConverter<RewardOptionId>
    {
        protected override RewardOptionId Create(string value) => new(value);
        protected override string GetValue(RewardOptionId value) => value.Value;
    }

    private sealed class RewardPoolIdConverter : StringIdConverter<UniversalRewardPoolId>
    {
        protected override UniversalRewardPoolId Create(string value) => new(value);
        protected override string GetValue(UniversalRewardPoolId value) => value.Value;
    }

    private sealed class CardIdConverter : StringIdConverter<CardId>
    {
        protected override CardId Create(string value) => new(value);
        protected override string GetValue(CardId value) => value.Value;
    }

    private sealed class CourseIdConverter : StringIdConverter<CourseId>
    {
        protected override CourseId Create(string value) => new(value);
        protected override string GetValue(CourseId value) => value.Value;
    }

    private sealed class BiomeIdConverter : StringIdConverter<BiomeId>
    {
        protected override BiomeId Create(string value) => new(value);
        protected override string GetValue(BiomeId value) => value.Value;
    }

    private sealed class ItemIdConverter : StringIdConverter<ItemId>
    {
        protected override ItemId Create(string value) => new(value);
        protected override string GetValue(ItemId value) => value.Value;
    }

    private sealed class SummonerIdConverter : StringIdConverter<SummonerId>
    {
        protected override SummonerId Create(string value) => new(value);
        protected override string GetValue(SummonerId value) => value.Value;
    }

    private sealed class TraitIdConverter : StringIdConverter<TraitId>
    {
        protected override TraitId Create(string value) => new(value);
        protected override string GetValue(TraitId value) => value.Value;
    }

    private sealed class CardTraitIdConverter : StringIdConverter<CardTraitId>
    {
        protected override CardTraitId Create(string value) => new(value);
        protected override string GetValue(CardTraitId value) => value.Value;
    }
}
