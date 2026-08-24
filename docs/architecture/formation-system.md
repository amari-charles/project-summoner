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
3. `Card.FromDefinition()` creates the runtime card and
   `Card.GetFormationOffset()` reads the definition's formation
4. `InputCollector` uses those offsets to build the summon positions submitted
   to simulation

## Key Files

| File | Purpose |
|------|---------|
| `scripts/csharp/Infrastructure/Data/Cards/CardCatalog.cs` | Card catalog facade |
| `scripts/csharp/Infrastructure/Data/Cards/CardDefinition.cs` | Card data class with Formation property |
| `scripts/csharp/Infrastructure/Data/Cards/Formations/FormationPresets.cs` | Named formation instances |
| `scripts/csharp/Infrastructure/Data/Cards/Formations/IFormationStrategy.cs` | Formation interface |
| `scripts/csharp/Infrastructure/Data/Cards/Formations/GridFormation.cs` | Default 2-row staggered grid |
| `scripts/csharp/Infrastructure/Data/Cards/Formations/GroupedLineFormation.cs` | Horizontal grouped line |
| `scripts/csharp/Infrastructure/Data/Cards/Formations/LineFormation.cs` | Simple horizontal line |
| `scripts/csharp/Infrastructure/Data/Cards/Formations/RingFormation.cs` | Circular formation |

## Adding a New Formation

1. Create strategy class implementing `IFormationStrategy` in `Formations/`
2. Add preset instance to `FormationPresets.cs`
3. Reference the preset in card definitions: `Formation = FormationPresets.YourNewFormation`

## Adding a New Card

Define in `CardDefinitions.cs`:
```csharp
public static readonly CardDefinition YourCard = new()
{
    Id = new CardId("your_card"),
    Name = "Your Card",
    // ...
    SpawnCount = 6,
    Formation = FormationPresets.CloudSwarm,  // Direct reference
    // ...
}
```

Register it in the definition lookup. Formation selection remains a direct,
type-safe reference to a preset.
