using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.AI;
using Fateforged.Simulation.Data;
using Godot;

namespace Fateforged.Session;

public enum BattleSideSource
{
    SceneDefault,
    Profile,
    Authored,
    MultiplayerOpponent,
    ClientPlaceholder,
}

public enum BattleDeckSource
{
    None,
    Profile,
    Authored,
}

public enum BattleControllerKind
{
    None,
    Player,
    TrainerAi,
    EncounterAi,
    Network,
}

public sealed class BattleSideDefinition
{
    public int Team { get; set; }
    public BattleSideSource Source { get; set; } = BattleSideSource.Authored;
    public BattleSummonerDefinition Summoner { get; set; } = new();
    public BattleDeckDefinition Deck { get; set; } = new();
    public BattleControllerDefinition Controller { get; set; } = new();

    public static BattleSideDefinition ProfilePlayer(int team = 0) =>
        new()
        {
            Team = team,
            Source = BattleSideSource.Profile,
            Summoner = new BattleSummonerDefinition { Source = BattleSideSource.Profile },
            Deck = new BattleDeckDefinition { Source = BattleDeckSource.Profile },
            Controller = new BattleControllerDefinition { Kind = BattleControllerKind.Player },
        };

    public static BattleSideDefinition AuthoredEnemy(int team = 1) =>
        new()
        {
            Team = team,
            Source = BattleSideSource.Authored,
            Summoner = new BattleSummonerDefinition { Source = BattleSideSource.Authored },
            Deck = new BattleDeckDefinition { Source = BattleDeckSource.Authored },
            Controller = new BattleControllerDefinition { Kind = BattleControllerKind.TrainerAi },
        };
}

public sealed class BattleSummonerDefinition
{
    public BattleSideSource Source { get; set; } = BattleSideSource.Authored;
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float? Hp { get; set; }
    public float? MaxHp { get; set; }
    public float? Mana { get; set; }
    public float? MaxMana { get; set; }
    public float? CastSpeed { get; set; }
    public float? DamageBonus { get; set; }
    public float? DamageReduction { get; set; }
    public float? SoulStrength { get; set; }
    public Dictionary<Element, float> ElementalDamageBonuses { get; } = new();
}

public sealed class BattleDeckDefinition
{
    public BattleDeckSource Source { get; set; } = BattleDeckSource.Authored;
    public bool Deferred { get; set; }
    public List<BattleDeckEntryDefinition> Cards { get; set; } = [];
}

public sealed class BattleDeckEntryDefinition
{
    public string CatalogId { get; set; } = "";
    public int Count { get; set; } = 1;
}

public sealed class BattleControllerDefinition
{
    public BattleControllerKind Kind { get; set; } = BattleControllerKind.TrainerAi;
    public AiType AiType { get; set; } = AiType.Heuristic;
    public AiPersonality AiPersonality { get; set; } = AiPersonality.Balanced;
    public int AiDifficulty { get; set; } = 3;
    public float AiIntervalMin { get; set; } = 3.0f;
    public float AiIntervalMax { get; set; } = 6.0f;
    public Godot.Collections.Array? AiScript { get; set; }
    public EncounterAiConfig? EncounterAi { get; set; }
}

public sealed class ResolvedBattleSide
{
    public int Team { get; set; }
    public ResolvedSummonerLoadout Summoner { get; set; } = new();
    public ResolvedDeckLoadout Deck { get; set; } = new();
    public BattleControllerDefinition Controller { get; set; } = new();
    public int MaxHandSize { get; set; } = 4;
    public Godot.Collections.Dictionary? SummonerStats { get; set; }

    public string[] DeckCatalogIds() => Deck.CatalogIds(includeHand: false);

    public string[] HandCatalogIds() => Deck.HandCatalogIds();

    public SimCardRuntimeRef[] DeckRefs() => Deck.RuntimeRefs(includeHand: false);

    public SimCardRuntimeRef[] HandRefs() => Deck.HandRuntimeRefs();

    public Godot.Collections.Array<Resource> AllCardsForRewards()
    {
        var allCards = new Godot.Collections.Array<Resource>(Deck.HandCards);
        foreach (var card in Deck.DeckCards)
            allCards.Add(card);
        return allCards;
    }
}

public sealed class ResolvedSummonerLoadout
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float Hp { get; set; }
    public float MaxHp { get; set; }
    public float Mana { get; set; } = 100f;
    public float MaxMana { get; set; } = 100f;
    public float CastSpeed { get; set; } = 1.0f;
    public float DamageBonus { get; set; }
    public float DamageReduction { get; set; }
    public float SoulStrength { get; set; }
    public Dictionary<Element, float> ElementalDamageBonuses { get; } = new();
}

public sealed class ResolvedDeckLoadout
{
    public Godot.Collections.Array<Resource> DeckCards { get; set; } = new();
    public Godot.Collections.Array<Resource> HandCards { get; set; } = new();
    public bool IsDeferred { get; set; }
    public bool LoadedFromProfile { get; set; }

    public int TotalCards => DeckCards.Count + HandCards.Count;

    public string[] CatalogIds(bool includeHand)
    {
        var source = includeHand ? AllCards() : DeckCards;
        var ids = new string[source.Count];
        for (int i = 0; i < source.Count; i++)
            ids[i] = GetCatalogId(source[i]);
        return ids;
    }

    public string[] HandCatalogIds()
    {
        var ids = new string[HandCards.Count];
        for (int i = 0; i < HandCards.Count; i++)
            ids[i] = GetCatalogId(HandCards[i]);
        return ids;
    }

    public SimCardRuntimeRef[] RuntimeRefs(bool includeHand)
    {
        var source = includeHand ? AllCards() : DeckCards;
        var refs = new SimCardRuntimeRef[source.Count];
        for (int i = 0; i < source.Count; i++)
            refs[i] = GetRuntimeRef(source[i]);
        return refs;
    }

    public SimCardRuntimeRef[] HandRuntimeRefs()
    {
        var refs = new SimCardRuntimeRef[HandCards.Count];
        for (int i = 0; i < HandCards.Count; i++)
            refs[i] = GetRuntimeRef(HandCards[i]);
        return refs;
    }

    private Godot.Collections.Array<Resource> AllCards()
    {
        var cards = new Godot.Collections.Array<Resource>(HandCards);
        foreach (var card in DeckCards)
            cards.Add(card);
        return cards;
    }

    private static string GetCatalogId(Resource card)
    {
        if (card is GodotObject go)
            return go.Get("CatalogId").AsString();
        return "";
    }

    private static SimCardRuntimeRef GetRuntimeRef(Resource card)
    {
        if (card is not GodotObject go)
            return new SimCardRuntimeRef();

        return new SimCardRuntimeRef
        {
            CatalogId = go.Get("CatalogId").AsString(),
            InstanceId = go.Get("InstanceId").AsString(),
        };
    }
}
