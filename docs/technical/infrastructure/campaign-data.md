# Campaign Data Architecture

## Overview

Campaign structure and event definitions are typed C# content. `CampaignCatalog` owns campaign graphs and `EventCatalog` owns battles and other events. GDScript receives normalized dictionaries through the campaign service; it does not author progression or reward rules.

## Key Files

| File | Responsibility |
|---|---|
| `scripts/csharp/Infrastructure/Data/Events/CampaignCatalog.cs` | Campaign metadata and graph membership |
| `scripts/csharp/Infrastructure/Data/Events/EventCatalog.cs` | Typed event and battle definitions |
| `scripts/csharp/Infrastructure/Data/Events/EventDefinition.cs` | Immutable content shapes |
| `scripts/csharp/Infrastructure/Data/Events/BattleRewardAuthoring.cs` | Universal battle-offer authoring helpers |
| `scripts/csharp/Meta/Services/Campaign/Handlers/CampaignCatalogHandler.cs` | Read-only presentation facade |

## Adding a Battle

1. Add a stable typed event ID.
2. Add a `BattleEventDefinition` to `EventCatalog` with side configuration, difficulty, tutorial/repeat rules, and any XP or first-clear offers.
3. Add the event to the appropriate campaign graph in `CampaignCatalog`.
4. Add localization keys and content-validation coverage.

```csharp
new BattleEventDefinition
{
    Id = EventIds.MyBattle,
    NameKey = "campaign.battle.my_battle.name",
    DescriptionKey = "campaign.battle.my_battle.description",
    Difficulty = 1,
    CardXpReward = 15,
    SummonerXpReward = 75,
    FirstClearRewardOffers = BattleRewardAuthoring.AutomaticCards(
        EventIds.MyBattle,
        50,
        new BattleRewardCard(CardIds.Charge, "common")
    ),
    PlayerSide = /* typed side definition */,
    EnemySide = /* typed side definition */,
};
```

`FirstClearRewardOffers` accepts universal offer definitions, so battles may use automatic grants, authored choices, pools, mixed grant bundles, or no offer. XP is separate because it is attempt-scoped and can be earned again on replay; first-clear offers cannot.

## Runtime Rules

- Campaign launch persists an authority-created battle attempt before scene navigation.
- The attempt freezes XP and resolved first-clear promises.
- Victory, defeat, and abandonment are reported to `IProgressionAuthority`.
- Campaign navigation reads completed state synchronized from the profile authority boundary.
- Old dictionary reward fields and old save shapes are unsupported; development saves may be discarded.
