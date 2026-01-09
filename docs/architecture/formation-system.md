# Formation System Architecture

## How Cards Get Formations

```mermaid
flowchart LR
    Card["CardDefinition<br/>(C# CardCatalog)"] -->|"Formation property"| Strategy["IFormationStrategy"]
    Strategy -->|"GetOffset(i, total)"| Units["Unit Positions"]
```

## Formation Composition

```mermaid
classDiagram
    class CardDefinition {
        +string Id
        +IFormationStrategy Formation
        +int SpawnCount
    }

    class FormationPresets {
        +StandardGrid : GridFormation
        +TightSwarmGrid : GridFormation
        +CloudSwarm : GroupedLineFormation
        +StandardLine : LineFormation
        +StandardRing : RingFormation
    }

    class IFormationStrategy {
        <<interface>>
        +GetOffset(index, total) Vector3
    }

    class GridFormation
    class LineFormation
    class GroupedLineFormation
    class RingFormation

    CardDefinition --> IFormationStrategy : references
    FormationPresets ..> GridFormation : creates
    FormationPresets ..> LineFormation : creates
    FormationPresets ..> GroupedLineFormation : creates
    FormationPresets ..> RingFormation : creates
    IFormationStrategy <|.. GridFormation
    IFormationStrategy <|.. LineFormation
    IFormationStrategy <|.. GroupedLineFormation
    IFormationStrategy <|.. RingFormation
```

## The Flow

1. `CardCatalog` (C#) holds all `CardDefinition` objects
2. Each `CardDefinition` has a `Formation` property referencing a preset from `FormationPresets`
3. `CardFactory.execute_summon()` gets formation directly: `card.Formation`
4. `formation.GetOffset(i, total)` calculates each unit's position

## Key Files

| File | Purpose |
|------|---------|
| `scripts/csharp/Cards/CardCatalog.cs` | All card definitions with formation references |
| `scripts/csharp/Cards/CardDefinition.cs` | Card data class with Formation property |
| `scripts/csharp/Cards/Formations/FormationPresets.cs` | Named formation instances |
| `scripts/csharp/Cards/Formations/IFormationStrategy.cs` | Formation interface |
| `scripts/csharp/Cards/Formations/GridFormation.cs` | Default 2-row staggered grid |
| `scripts/csharp/Cards/Formations/GroupedLineFormation.cs` | Horizontal grouped line |
| `scripts/csharp/Cards/Formations/LineFormation.cs` | Simple horizontal line |
| `scripts/csharp/Cards/Formations/RingFormation.cs` | Circular formation |
| `scripts/csharp/Cards/CardFactory.cs` | Spawns units using card.Formation |

## Adding a New Formation

1. Create strategy class implementing `IFormationStrategy` in `Formations/`
2. Add preset instance to `FormationPresets.cs`
3. Reference the preset in card definitions: `Formation = FormationPresets.YourNewFormation`

## Adding a New Card

Define in `CardCatalog.cs`:
```csharp
["your_card"] = new CardDefinition
{
    Id = "your_card",
    Name = "Your Card",
    // ...
    SpawnCount = 6,
    Formation = FormationPresets.CloudSwarm,  // Direct reference
    // ...
}
```

No strings, no config parsing - just a direct reference to a preset formation.
