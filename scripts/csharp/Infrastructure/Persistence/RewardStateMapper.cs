using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Data.Items;
using Fateforged.Data.Rewards;
using Fateforged.Data.Summoners;
using Fateforged.Data.Traits;
using Fateforged.Domain.Profile.Rewards;
using Godot;
using GdArray = Godot.Collections.Array;
using GdDict = Godot.Collections.Dictionary;

namespace Fateforged.Infrastructure.Persistence;

internal static class RewardStateMapper
{
    public static GdDict ToDictionary(RewardProfileState state)
    {
        var seeds = new GdDict();
        foreach (var (summonerId, seed) in state.AcademySeedBySummoner)
            seeds[summonerId] = seed.ToString();

        var resolved = new GdDict();
        foreach (var (claimId, snapshot) in state.ResolvedOffers)
            resolved[claimId] = ToDictionary(snapshot);

        var pending = new GdDict();
        foreach (var (claimId, selection) in state.PendingSelections)
            pending[claimId] = ToDictionary(selection);

        var receipts = new GdDict();
        foreach (var (claimId, receipt) in state.ClaimReceipts)
            receipts[claimId] = ToDictionary(receipt);

        return new GdDict
        {
            ["academy_seed_by_summoner"] = seeds,
            ["resolved_offers"] = resolved,
            ["pending_selections"] = pending,
            ["claim_receipts"] = receipts,
        };
    }

    public static RewardProfileState FromDictionary(GdDict? dict)
    {
        var state = new RewardProfileState();
        if (dict == null)
            return state;

        if (TryGetDictionary(dict, "academy_seed_by_summoner", out var seeds))
        {
            foreach (var key in seeds.Keys)
            {
                if (ulong.TryParse(seeds[key].AsString(), out var seed))
                    state.AcademySeedBySummoner[key.AsString()] = seed;
            }
        }

        if (TryGetDictionary(dict, "resolved_offers", out var resolved))
        {
            foreach (var key in resolved.Keys)
            {
                if (resolved[key].VariantType != Variant.Type.Dictionary)
                    continue;
                var snapshot = FromSnapshotDictionary(resolved[key].AsGodotDictionary());
                if (snapshot != null)
                    state.ResolvedOffers[key.AsString()] = snapshot;
            }
        }

        if (TryGetDictionary(dict, "pending_selections", out var pending))
        {
            foreach (var key in pending.Keys)
            {
                if (pending[key].VariantType != Variant.Type.Dictionary)
                    continue;
                var selection = FromPendingDictionary(pending[key].AsGodotDictionary());
                if (selection != null)
                    state.PendingSelections[key.AsString()] = selection;
            }
        }

        if (TryGetDictionary(dict, "claim_receipts", out var receipts))
        {
            foreach (var key in receipts.Keys)
            {
                if (receipts[key].VariantType != Variant.Type.Dictionary)
                    continue;
                var receipt = FromReceiptDictionary(receipts[key].AsGodotDictionary());
                if (receipt != null)
                    state.ClaimReceipts[key.AsString()] = receipt;
            }
        }

        return state;
    }

    private static GdDict ToDictionary(ResolvedRewardOfferSnapshot snapshot) =>
        new()
        {
            ["claim_id"] = snapshot.ClaimId.Value,
            ["offer_id"] = snapshot.OfferId.Value,
            ["source"] = ToDictionary(snapshot.Source),
            ["summoner_id"] = (string)snapshot.SummonerId,
            ["resolution_version"] = snapshot.ResolutionVersion,
            ["selection_mode"] = snapshot.SelectionMode.ToString(),
            ["choose_count"] = snapshot.ChooseCount,
            ["options"] = ToOptionArray(snapshot.Options),
        };

    private static ResolvedRewardOfferSnapshot? FromSnapshotDictionary(GdDict dict)
    {
        var claimId = GetString(dict, "claim_id");
        var offerId = GetString(dict, "offer_id");
        var summonerId = new SummonerId(GetString(dict, "summoner_id"));
        if (
            string.IsNullOrWhiteSpace(claimId)
            || string.IsNullOrWhiteSpace(offerId)
            || !summonerId.HasValue
            || !TryGetDictionary(dict, "source", out var sourceDict)
            || !TryFromSourceDictionary(sourceDict, out var source)
            || !Enum.TryParse<RewardSelectionMode>(
                GetString(dict, "selection_mode"),
                out var selectionMode
            )
            || GetInt(dict, "choose_count", 0) <= 0
            || !TryFromOptionArray(GetArray(dict, "options"), out var options)
        )
        {
            return null;
        }

        return new ResolvedRewardOfferSnapshot
        {
            ClaimId = new RewardClaimId(claimId),
            OfferId = new RewardOfferId(offerId),
            Source = source,
            SummonerId = summonerId,
            ResolutionVersion = GetInt(dict, "resolution_version", 1),
            SelectionMode = selectionMode,
            ChooseCount = GetInt(dict, "choose_count", 0),
            Options = options,
        };
    }

    private static GdDict ToDictionary(PendingRewardSelection selection) =>
        new()
        {
            ["claim_id"] = selection.ClaimId.Value,
            ["choose_count"] = selection.ChooseCount,
            ["selected_option_ids"] = ToStringArray(
                selection.SelectedOptionIds.Select(id => id.Value)
            ),
        };

    private static PendingRewardSelection? FromPendingDictionary(GdDict dict)
    {
        var claimId = GetString(dict, "claim_id");
        if (string.IsNullOrWhiteSpace(claimId))
            return null;

        return new PendingRewardSelection
        {
            ClaimId = new RewardClaimId(claimId),
            ChooseCount = GetInt(dict, "choose_count", 0),
            SelectedOptionIds = GetArray(dict, "selected_option_ids")
                .Select(item => new RewardOptionId(item.AsString()))
                .ToImmutableArray(),
        };
    }

    private static GdDict ToDictionary(RewardClaimReceipt receipt) =>
        new()
        {
            ["claim_id"] = receipt.ClaimId.Value,
            ["claimed_option_ids"] = ToStringArray(
                receipt.ClaimedOptionIds.Select(id => id.Value)
            ),
            ["applied_grants"] = ToGrantArray(receipt.AppliedGrants),
        };

    private static RewardClaimReceipt? FromReceiptDictionary(GdDict dict)
    {
        var claimId = GetString(dict, "claim_id");
        if (string.IsNullOrWhiteSpace(claimId))
            return null;

        return new RewardClaimReceipt
        {
            ClaimId = new RewardClaimId(claimId),
            ClaimedOptionIds = GetArray(dict, "claimed_option_ids")
                .Select(item => new RewardOptionId(item.AsString()))
                .ToImmutableArray(),
            AppliedGrants = FromGrantArray(GetArray(dict, "applied_grants")),
        };
    }

    private static GdDict ToDictionary(RewardSourceContext source) =>
        new()
        {
            ["source_type"] = source.SourceType,
            ["source_id"] = source.SourceId,
            ["occurrence_id"] = source.OccurrenceId,
        };

    private static bool TryFromSourceDictionary(
        GdDict dict,
        out RewardSourceContext source
    )
    {
        source = new RewardSourceContext
        {
            SourceType = GetString(dict, "source_type"),
            SourceId = GetString(dict, "source_id"),
            OccurrenceId = GetString(dict, "occurrence_id"),
        };
        return !string.IsNullOrWhiteSpace(source.SourceType)
            && !string.IsNullOrWhiteSpace(source.SourceId);
    }

    private static GdArray ToOptionArray(ImmutableArray<RewardOptionDefinition> options)
    {
        var result = new GdArray();
        foreach (var option in options)
        {
            result.Add(
                new GdDict
                {
                    ["id"] = option.Id.Value,
                    ["label_key"] = option.LabelKey,
                    ["description_key"] = option.DescriptionKey,
                    ["grants"] = ToGrantArray(option.Grants),
                }
            );
        }
        return result;
    }

    private static bool TryFromOptionArray(
        GdArray options,
        out ImmutableArray<RewardOptionDefinition> parsed
    )
    {
        var result = ImmutableArray.CreateBuilder<RewardOptionDefinition>();
        foreach (var value in options)
        {
            if (value.VariantType != Variant.Type.Dictionary)
            {
                parsed = [];
                return false;
            }
            var dict = value.AsGodotDictionary();
            var id = GetString(dict, "id");
            if (
                string.IsNullOrWhiteSpace(id)
                || !TryFromGrantArray(GetArray(dict, "grants"), out var grants)
            )
            {
                parsed = [];
                return false;
            }
            result.Add(
                new RewardOptionDefinition
                {
                    Id = new RewardOptionId(id),
                    LabelKey = GetString(dict, "label_key"),
                    DescriptionKey = GetString(dict, "description_key"),
                    Grants = grants,
                }
            );
        }
        parsed = result.ToImmutable();
        return parsed.Length > 0;
    }

    private static bool TryFromGrantArray(
        GdArray grants,
        out ImmutableArray<RewardGrantDefinition> parsed
    )
    {
        var result = ImmutableArray.CreateBuilder<RewardGrantDefinition>();
        foreach (var value in grants)
        {
            if (value.VariantType != Variant.Type.Dictionary)
            {
                parsed = [];
                return false;
            }
            var grant = FromGrantDictionary(value.AsGodotDictionary());
            if (grant == null)
            {
                parsed = [];
                return false;
            }
            result.Add(grant);
        }
        parsed = result.ToImmutable();
        return parsed.Length > 0;
    }

    private static GdArray ToGrantArray(ImmutableArray<RewardGrantDefinition> grants)
    {
        var result = new GdArray();
        foreach (var grant in grants)
            result.Add(ToDictionary(grant));
        return result;
    }

    private static ImmutableArray<RewardGrantDefinition> FromGrantArray(GdArray grants)
    {
        var result = ImmutableArray.CreateBuilder<RewardGrantDefinition>();
        foreach (var value in grants)
        {
            if (value.VariantType != Variant.Type.Dictionary)
                continue;
            var grant = FromGrantDictionary(value.AsGodotDictionary());
            if (grant != null)
                result.Add(grant);
        }
        return result.ToImmutable();
    }

    private static GdDict ToDictionary(RewardGrantDefinition grant)
    {
        var dict = new GdDict
        {
            ["target_scope"] = grant.Target.Scope.ToString(),
            ["target_id"] = grant.Target.TargetId,
        };

        switch (grant)
        {
            case CardRewardGrantDefinition card:
                dict["kind"] = "card";
                dict["card_id"] = (string)card.CardId;
                dict["rarity"] = card.Rarity;
                dict["count"] = card.Count;
                break;
            case ResourceRewardGrantDefinition resource:
                dict["kind"] = "resource";
                dict["resource_id"] = resource.ResourceId;
                dict["amount"] = resource.Amount;
                break;
            case ItemRewardGrantDefinition item:
                dict["kind"] = "item";
                dict["item_id"] = (string)item.ItemId;
                dict["count"] = item.Count;
                break;
            case SummonerUnlockRewardGrantDefinition summoner:
                dict["kind"] = "summoner_unlock";
                dict["summoner_id"] = (string)summoner.SummonerId;
                break;
            case CosmeticRewardGrantDefinition cosmetic:
                dict["kind"] = "cosmetic";
                dict["cosmetic_id"] = cosmetic.CosmeticId;
                break;
            case EmoteRewardGrantDefinition emote:
                dict["kind"] = "emote";
                dict["emote_id"] = emote.EmoteId;
                break;
            case SummonerExperienceRewardGrantDefinition summonerXp:
                dict["kind"] = "summoner_xp";
                dict["amount"] = summonerXp.Amount;
                break;
            case CardExperienceRewardGrantDefinition cardXp:
                dict["kind"] = "card_xp";
                dict["amount"] = cardXp.Amount;
                break;
            case SummonerTraitRewardGrantDefinition summonerTrait:
                dict["kind"] = "summoner_trait";
                dict["trait_id"] = (string)summonerTrait.TraitId;
                dict["amount"] = summonerTrait.Amount;
                break;
            case CardTraitRewardGrantDefinition cardTrait:
                dict["kind"] = "card_trait";
                dict["trait_id"] = (string)cardTrait.TraitId;
                dict["amount"] = cardTrait.Amount;
                break;
            case AcademyProgressFlagRewardGrantDefinition flag:
                dict["kind"] = "academy_progress_flag";
                dict["flag_id"] = flag.FlagId;
                dict["amount"] = flag.Amount;
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported persisted reward grant type {grant.GetType().Name}."
                );
        }

        return dict;
    }

    private static RewardGrantDefinition? FromGrantDictionary(GdDict dict)
    {
        if (
            !Enum.TryParse<RewardOwnershipScope>(
                GetString(dict, "target_scope"),
                out var scope
            )
        )
            return null;

        var target = new RewardOwnershipTarget(
            scope,
            GetString(dict, "target_id")
        );
        var amount = GetInt(dict, "amount", 0);

        return GetString(dict, "kind") switch
        {
            "card" => new CardRewardGrantDefinition
            {
                Target = target,
                CardId = new CardId(GetString(dict, "card_id")),
                Rarity = GetString(dict, "rarity", "common"),
                Count = GetInt(dict, "count", 1),
            },
            "resource" => new ResourceRewardGrantDefinition
            {
                Target = target,
                ResourceId = GetString(dict, "resource_id"),
                Amount = amount,
            },
            "item" => new ItemRewardGrantDefinition
            {
                Target = target,
                ItemId = new ItemId(GetString(dict, "item_id")),
                Count = GetInt(dict, "count", 1),
            },
            "summoner_unlock" => new SummonerUnlockRewardGrantDefinition
            {
                Target = target,
                SummonerId = new SummonerId(GetString(dict, "summoner_id")),
            },
            "cosmetic" => new CosmeticRewardGrantDefinition
            {
                Target = target,
                CosmeticId = GetString(dict, "cosmetic_id"),
            },
            "emote" => new EmoteRewardGrantDefinition
            {
                Target = target,
                EmoteId = GetString(dict, "emote_id"),
            },
            "summoner_xp" => new SummonerExperienceRewardGrantDefinition
            {
                Target = target,
                Amount = amount,
            },
            "card_xp" => new CardExperienceRewardGrantDefinition
            {
                Target = target,
                Amount = amount,
            },
            "summoner_trait" => new SummonerTraitRewardGrantDefinition
            {
                Target = target,
                TraitId = new TraitId(GetString(dict, "trait_id")),
                Amount = amount,
            },
            "card_trait" => new CardTraitRewardGrantDefinition
            {
                Target = target,
                TraitId = new CardTraitId(GetString(dict, "trait_id")),
                Amount = amount,
            },
            "academy_progress_flag" => new AcademyProgressFlagRewardGrantDefinition
            {
                Target = target,
                FlagId = GetString(dict, "flag_id"),
                Amount = amount,
            },
            _ => null,
        };
    }

    private static bool TryGetDictionary(GdDict dict, string key, out GdDict value)
    {
        if (
            dict.TryGetValue(key, out var variant)
            && variant.VariantType == Variant.Type.Dictionary
        )
        {
            value = variant.AsGodotDictionary();
            return true;
        }

        value = [];
        return false;
    }

    private static string GetString(GdDict dict, string key, string fallback = "") =>
        dict.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil
            ? value.AsString()
            : fallback;

    private static int GetInt(GdDict dict, string key, int fallback) =>
        dict.TryGetValue(key, out var value) && value.VariantType != Variant.Type.Nil
            ? value.AsInt32()
            : fallback;

    private static GdArray GetArray(GdDict dict, string key) =>
        dict.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Array
            ? value.AsGodotArray()
            : [];

    private static GdArray ToStringArray(IEnumerable<string> values)
    {
        var result = new GdArray();
        foreach (var value in values)
            result.Add(value);
        return result;
    }
}
