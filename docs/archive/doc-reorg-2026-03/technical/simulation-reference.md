# Architecture Diagram — Simulation Rewrite

> **Purpose**: Visual architecture reference with Mermaid diagrams showing the complete system design from scratch. Every class, method, enum, and data flow is documented here. This is the blueprint for implementation.
>
> **Status**: Finalized — 2026-02-26

---

## 1. System Architecture Overview

Four layers with strict data flow boundaries.

```mermaid
flowchart TB
    subgraph InputLayer["INPUT LAYER"]
        PlayerInput["Player Input\n(C#: InputCollector.cs)"]
        AIOpponent["AI Opponent\n(submits same commands)"]
    end

    subgraph SimLayer["SIMULATION LAYER — C# Pure, No Godot"]
        direction TB
        CommandQueue["Command Queue\n(PendingCommands)"]
        Tick["Simulation.Tick(fixedDelta)\n⚠️ ONLY sim-code writer to MatchState\n(+ SnapshotApplier on client)"]
        MatchState["MatchState\n(single source of truth)"]
        SimSystems["SimBehavior · SimDamage\nSimMovement · SimSteering\nSimTargeting · SimProjectile\nSimEffects"]
        Events["SimEvent list\n(output of each Tick)"]
    end

    subgraph MultiplayerLayer["MULTIPLAYER LAYER"]
        HostRunner["HostRunner\n• Drives Tick()\n• Broadcasts events\n• Periodic snapshots\n• Validates client commands"]
        ClientRunner["ClientRunner\n⚠️ Never calls Tick()\n• Receives events + snapshots\n• Sends commands to host"]
        Snapshots["StateSnapshot\n+ StateHash\n(drift correction)"]
    end

    subgraph PresentationLayer["PRESENTATION LAYER — READ-ONLY"]
        SimNode["SimulationNode (Bridge)\n• Converts SimEvents → Godot signals\n• Read-only API for GDScript\n• SubmitCommand() entry point"]
        UnitVisual["UnitVisual\n🔒 READ-ONLY\n• Reads position/HP/target\n• Plays animations on events"]
        SummonerVisual["SummonerVisual.cs\n🔒 READ-ONLY\n• Reads mana/HP\n• Shows cast bar from events\n• Submits PlayCardCommand"]
        HUD["HUD / UI\n🔒 READ-ONLY\n• Mana display\n• Kill count\n• Phase indicator"]
    end

    PlayerInput -->|"ICommand"| SimNode
    AIOpponent -->|"ICommand"| SimNode
    SimNode -->|"SubmitCommand()"| CommandQueue
    CommandQueue --> Tick
    Tick -->|"reads/writes"| MatchState
    Tick -->|"delegates to"| SimSystems
    SimSystems -->|"reads/writes"| MatchState
    Tick -->|"emits"| Events

    HostRunner -->|"calls"| Tick
    HostRunner -->|"serializes"| Events
    HostRunner -->|"builds"| Snapshots
    HostRunner <-->|"network"| ClientRunner
    ClientRunner -->|"applies"| Snapshots

    Events --> SimNode
    SimNode -->|"Godot signals"| UnitVisual
    SimNode -->|"Godot signals"| SummonerVisual
    SimNode -->|"Godot signals"| HUD
    SimNode -->|"read API"| UnitVisual
    SimNode -->|"read API"| SummonerVisual
```

---

## 2. Simulation Tick Pipeline

What `Simulation.Tick(fixedDelta)` does each frame, in order.

```mermaid
flowchart TD
    Start(["Tick(fixedDelta) called"])
    IncFrame["1. Increment FrameNumber\nAccumulate MatchTime += fixedDelta"]
    BranchPhase{"2. Branch on\nCurrentPhase"}

    subgraph Preparation["PREPARATION PHASE"]
        PrepTimer["Decrement PrepTimeRemaining -= fixedDelta"]
        PrepExpired{PrepTimeRemaining <= 0?}
        PrepCmds["Process Commands\n(summon cards only, reject spells)"]
        TransBattle["TransitionToBattle()\n• Activate all inactive units\n• Discard hand, draw fresh 4\n• Set Phase = Battle\n• Emit PhaseChangedEvent\n• Emit HandChangedEvent"]
        PrepEvents["Emit PrepTimerUpdatedEvent"]
    end

    subgraph BattlePhase["BATTLE PHASE"]
        ProcessCmds["2. ProcessCommands()\nDequeue → Validate → Apply/Reject"]
        PhaseCheck["3. Phase timers (no-op in Battle)"]
        TickCasting["4. TickCasting()\n• Decrement cast timers\n• On complete: spawn units or apply spell\n• Replacement draw for played card"]
        TickUnits["5. TickUnits() — per alive active unit:"]
        Cooldowns["  5a. TickCooldowns(fixedDelta)"]
        Targeting["  5b. TickTargeting()\n  AcquireTarget by priority"]
        Behavior["  5c. TickBehavior()\n  Movement style dispatch"]
        Movement["  5d. TickMovement(fixedDelta)\n  + SimSteering separation"]
        PendingDmg["  5e. TickPendingDamage()\n  Melee-timed hits + summoner damage\n  (ranged uses SimProjectile)"]
        TickProj["6. SimProjectile.TickAll()\n• Move projectiles\n• Check hits\n• Apply damage on hit"]
        TickEffects["7. SimEffects.TickBuffs()\n• Decrement durations\n• Apply periodic (DoT/HoT)\n• Remove expired buffs\n• Fire periodic triggers"]
        EvalWin["8. EvaluateWinConditions()\nIWinCondition.Evaluate(state)"]
        WinCheck{WinResult?}
        SetGameOver["Set Phase = GameOver\nSet WinnerTeam\nEmit GameOverEvent"]
    end

    ReturnEvents(["Return emitted events list"])

    Start --> IncFrame --> BranchPhase
    BranchPhase -->|"Preparation"| PrepCmds
    PrepCmds --> PrepTimer
    PrepTimer --> PrepExpired
    PrepExpired -->|"Yes"| TransBattle --> ReturnEvents
    PrepExpired -->|"No"| PrepEvents --> ReturnEvents

    BranchPhase -->|"Battle"| ProcessCmds
    ProcessCmds --> PhaseCheck
    PhaseCheck --> TickCasting
    TickCasting --> TickUnits
    TickUnits --> Cooldowns --> Targeting --> Behavior --> Movement --> PendingDmg
    PendingDmg --> TickProj --> TickEffects --> EvalWin
    EvalWin --> WinCheck
    WinCheck -->|"Winner found"| SetGameOver --> ReturnEvents
    WinCheck -->|"No winner"| ReturnEvents

    BranchPhase -->|"GameOver"| ReturnEvents
```

---

## 3. Class Diagrams

### 3.1 Enums

```mermaid
classDiagram
    class DamageType {
        <<enumeration>>
        Physical
        Magic
    }

    class TriggerType {
        <<enumeration>>
        OnAttack
        OnHit
        OnKill
        OnDeath
        OnDamaged
        HpThreshold
        Periodic
        OnSpawn
        LeaderDeath
    }

    class EffectType {
        <<enumeration>>
        DirectDamage
        Heal
        StatModifier
        DamageOverTime
        HealOverTime
        Shield
        Stun
        Slow
        AoE
        ChargeBonusDamage
    }

    class MovementStyle {
        <<enumeration>>
        MoveToward
        Kite
        FollowLeader
    }

    class TargetingPriority {
        <<enumeration>>
        NearestEnemy
        SummonerPriority
        LeaderTarget
    }

    class RetreatCondition {
        <<enumeration>>
        None
        HpThreshold
    }

    class SpellTargetingMode {
        <<enumeration>>
        Position
        Unit
    }

    class GamePhase {
        <<enumeration>>
        Preparation
        Battle
        Overtime
        GameOver
    }

    class UnitLifecycle {
        <<enumeration>>
        Spawning
        Active
        Dying
        Dead
    }

    class ProjectileMovementType {
        <<enumeration>>
        Straight
        Arc
        Homing
        Ballistic
        WeavingHoming
    }

    class CardType {
        <<enumeration>>
        Summon
        Spell
    }

    class Element {
        <<enumeration>>
        Fire
        Wind
        Earth
        Water
        Lightning
        Life
        Death
        Shadow
    }

    class EffectTargetMode {
        <<enumeration>>
        Self
        Target
        AlliesInRadius
        EnemiesInRadius
        AllInRadius
    }

    class FormationType {
        <<enumeration>>
        Grid
        Line
        Ring
        VShape
        Single
    }
```

### 3.2 Data Model

```mermaid
classDiagram
    class MatchState {
        +int FrameNumber
        +double MatchTime
        +GamePhase Phase
        +float PrepTimeRemaining
        +SummonerData[2] Summoners
        +Dictionary~int, UnitData~ Units
        +Dictionary~int, ProjectileData~ Projectiles
        +Queue~ICommand~ PendingCommands
        +DeterministicRng Rng
        +IWinCondition WinCondition
        +int? WinnerTeam
        +NextUnitId() int
        +NextProjectileId() int
        +GetAliveActiveUnits() List~UnitData~
        +GetAliveActiveUnitsForTeam(team) List~UnitData~
        +GetUnitsInRadius(pos, radius) List~UnitData~
        +GetSummonerForTeam(team) SummonerData
        +GetEnemySummoner(team) SummonerData
    }

    class UnitData {
        +int NetworkId
        +int Team
        +string CatalogId
        +UnitLifecycle Lifecycle
        --Core Stats--
        +float CurrentHp
        +float MaxHp
        +float AttackDamage
        +DamageType AttackType
        +float AttackRange
        +float AttackSpeed
        +float MoveSpeed
        +float CritChance
        +float CritMultiplier
        +int MovementLayer
        +Element Element
        +float PhysicalDefense
        +float MagicDefense
        +float Evasion
        --Behavior Profile--
        +MovementStyle MovementStyle
        +TargetingPriority TargetingPriority
        +RetreatCondition RetreatCondition
        +float KiteRange
        +float RetreatHpThreshold
        --Group--
        +int? GroupId
        +int? LeaderId
        --Targeting State--
        +int? TargetNetworkId
        +float AttackCooldownRemaining
        --Combat State--
        +Vector3 Position
        +Vector3 Velocity
        +float Rotation
        --Steering State--
        +bool IsBlocked
        +int BlockedFrames
        --Effects--
        +List~ActiveBuff~ ActiveBuffs
        +List~TriggerConfig~ Triggers
        --Charge Tracking--
        +float DistanceTraveled
        +bool ChargeConsumed
    }

    class SummonerData {
        +int Team
        +Vector3 Position
        +float CurrentHp
        +float MaxHp
        +float CurrentMana
        +float MaxMana
        +float CastSpeed
        +float DamageBonus
        +float DamageReduction
        +Element Element
        --Casting State--
        +bool IsCasting
        +float CastTimeRemaining
        +int? CastingCardIndex
        --Deck State--
        +List~string~ Hand
        +List~string~ Deck
        +List~string~ DiscardPile
    }

    class ProjectileData {
        +int ProjectileId
        +int OwnerTeam
        +int AttackerNetworkId
        +int? TargetNetworkId
        --Damage Info--
        +float BaseDamage
        +DamageType DamageType
        +Element Element
        --Movement--
        +ProjectileMovementType MovementType
        +Vector3 Position
        +Vector3 Velocity
        +float Speed
        --Arc Params--
        +float ArcHeight
        +Vector3 ArcStartPos
        +Vector3 ArcTargetPos
        +float ArcProgress
        --Ballistic Params--
        +float Gravity
        +Vector3 LaunchVelocity
        --Weaving Params--
        +float VeerAngle
        +float CorrectionRate
        +float VeerTimer
        --Hit Detection--
        +float HitRadius
        +int PierceCount
        +int PierceRemaining
        +float AoeRadius
        --Lifecycle--
        +float Lifetime
        +float ElapsedTime
        +List~int~ AlreadyHitIds
    }

    class ActiveBuff {
        +int BuffId
        +int SourceUnitId
        +EffectType EffectType
        +string StatTarget
        +float Modifier
        +float DurationRemaining
        +float PeriodicInterval
        +float PeriodicTimer
        +float DotDamagePerTick
        +DamageType DotDamageType
        +float HotHealPerTick
        +float ShieldHp
        +int AppliedAtFrame
    }

    class TriggerConfig {
        +TriggerType TriggerType
        +EffectType EffectType
        +DamageType DamageType
        +float Amount
        +float Radius
        +float Duration
        +float Cooldown
        +float CooldownRemaining
        +float HpThreshold
        +bool HasFired
        +EffectTargetMode TargetMode
    }

    MatchState "1" *-- "2" SummonerData
    MatchState "1" *-- "*" UnitData
    MatchState "1" *-- "*" ProjectileData
    UnitData "1" *-- "*" ActiveBuff
    UnitData "1" *-- "*" TriggerConfig
```

### 3.3 Simulation Systems

```mermaid
classDiagram
    class Simulation {
        -MatchState State
        -ICardDefinitionProvider CardProvider
        +Tick(fixedDelta: float) List~SimEvent~
        -TickPreparation(fixedDelta: float)
        -TickBattle(fixedDelta: float)
        -TickUnits(fixedDelta: float)
        -TickCasting(fixedDelta: float)
        -ProcessCommands()
        -EvaluateWinConditions() WinResult?
        -TransitionToBattle()
        -HandleCastCompletion(summoner: SummonerData)
        -SpawnUnitsFromCard(card: SummonCardDefinition, pos: Vector3, team: int)
        -ApplySpellFromCard(card: SpellCardDefinition, targets: TargetInfo)
        -ReplacementDraw(summoner: SummonerData, slotIndex: int)
        -RecycleDeck(summoner: SummonerData)
        -EmitEvent(event: SimEvent)
    }

    class SimBehavior {
        <<static>>
        +TickCooldowns(unit: UnitData, fixedDelta: float)$
        +TickTargeting(unit: UnitData, state: MatchState)$
        +TickBehavior(unit: UnitData, state: MatchState, fixedDelta: float)$
        +TickPendingDamage(unit: UnitData, state: MatchState, rng: DeterministicRng, events: List) $
        +ApplyMeleeDamageToUnit(attacker: UnitData, target: UnitData, state: MatchState, rng: DeterministicRng, events: List)$
        +ApplyDamageToSummoner(unit: UnitData, summoner: SummonerData, state: MatchState, rng: DeterministicRng, events: List)$
        +FireTriggers(unit: UnitData, triggerType: TriggerType, context: TriggerContext, state: MatchState, events: List)$
    }

    class SimDamage {
        <<static>>
        +Calculate(baseDamage: float, damageType: DamageType, attacker: UnitData, target: UnitData, attackerSummoner: SummonerData, targetSummoner: SummonerData, rng: DeterministicRng) DamageResult$
        +CheckEvasion(evasion: float, rng: DeterministicRng) bool$
        +ApplyCrit(damage: float, critChance: float, critMult: float, rng: DeterministicRng) CritResult$
        +ApplyElementalMatchup(damage: float, attackerElement: Element, defenderElement: Element) float$
        +ApplySummonerBonus(damage: float, summoner: SummonerData) float$
        +ApplyDefense(damage: float, damageType: DamageType, target: UnitData) float$
        +ApplyShieldAbsorption(damage: float, target: UnitData) float$
        +FloorDamage(damage: float, minimum: float) float$
    }

    class SimMovement {
        <<static>>
        +Tick(unit: UnitData, state: MatchState, fixedDelta: float)$
        +CalculateForward(unit: UnitData, fixedDelta: float) Vector3$
        +CalculateTowardPosition(from: Vector3, target: Vector3, speed: float, fixedDelta: float) Vector3$
        +CalculateKite(unit: UnitData, targetPos: Vector3, fixedDelta: float) Vector3$
        +CalculateFollowLeader(unit: UnitData, leaderPos: Vector3, fixedDelta: float) Vector3$
        +CalculateStrafePosition(unit: UnitData, targetPos: Vector3) Vector3$
    }

    class SimSteering {
        <<static>>
        +CalculateSeparation(unit: UnitData, nearby: List~UnitData~) Vector3$
        +UpdateBlockedState(unit: UnitData, desiredPos: Vector3, actualPos: Vector3)$
        +CalculateFlankForce(unit: UnitData, targetPos: Vector3, nearby: List~UnitData~) Vector3$
        +CorrectOverlaps(units: List~UnitData~)$
    }

    class SimTargeting {
        <<static>>
        +AcquireTarget(unit: UnitData, state: MatchState) int?$
        +AcquireTargetForPriority(unit: UnitData, priority: TargetingPriority, state: MatchState) int?$
        +FindNearestEnemy(unit: UnitData, state: MatchState) int?$
        +FindSummonerPriority(unit: UnitData, state: MatchState) int?$
        +FindLeaderTarget(unit: UnitData, state: MatchState) int?$
        +CanAttack(attacker: UnitData, target: UnitData) bool$
        +PassesLayerFilter(attacker: UnitData, target: UnitData) bool$
        +ScoreTarget(unit: UnitData, candidate: UnitData) float$
    }

    class SimProjectile {
        <<static>>
        +Spawn(def: ProjectileDefinition, attacker: UnitData, target: int?, targetPos: Vector3, state: MatchState) ProjectileData$
        +TickAll(state: MatchState, fixedDelta: float, events: List)$
        +TickStraight(proj: ProjectileData, fixedDelta: float)$
        +TickArc(proj: ProjectileData, fixedDelta: float)$
        +TickHoming(proj: ProjectileData, state: MatchState, fixedDelta: float)$
        +TickBallistic(proj: ProjectileData, fixedDelta: float)$
        +TickWeavingHoming(proj: ProjectileData, state: MatchState, fixedDelta: float)$
        +CheckHits(proj: ProjectileData, state: MatchState) List~int~$
        +ApplyHit(proj: ProjectileData, targetId: int, state: MatchState, events: List)$
        +ApplyAoE(proj: ProjectileData, hitPos: Vector3, state: MatchState, events: List)$
    }

    class SimEffects {
        <<static>>
        +ApplyEffect(effectType: EffectType, source: UnitData, targets: List~UnitData~, params: EffectParams, state: MatchState, events: List)$
        +TickBuffs(state: MatchState, fixedDelta: float, events: List)$
        +ResolveSpellTargets(mode: SpellTargetingMode, targetPos: Vector3?, targetUnitId: int?, radius: float, state: MatchState) List~UnitData~$
        +ApplyDirectDamage(source: UnitData, target: UnitData, amount: float, damageType: DamageType, state: MatchState, events: List)$
        +ApplyHeal(target: UnitData, amount: float, events: List)$
        +ApplyStatModifier(target: UnitData, stat: string, modifier: float, duration: float, sourceId: int, frame: int, events: List)$
        +ApplyShield(target: UnitData, amount: float, duration: float, sourceId: int, frame: int, events: List)$
        +ApplyStun(target: UnitData, duration: float, sourceId: int, frame: int, events: List)$
        +ApplySlow(target: UnitData, modifier: float, duration: float, sourceId: int, frame: int, events: List)$
        +ApplyDoT(target: UnitData, dps: float, damageType: DamageType, duration: float, interval: float, sourceId: int, frame: int, events: List)$
        +ApplyHoT(target: UnitData, hps: float, duration: float, interval: float, sourceId: int, frame: int, events: List)$
    }

    class DeterministicRng {
        -uint[] DomainStates
        +Next(domain: int) uint
        +NextFloat(domain: int) float
        +Range(min: int, max: int, domain: int) int
        +RangeFloat(min: float, max: float, domain: int) float
    }

    Simulation --> SimBehavior : delegates
    Simulation --> SimDamage : delegates
    Simulation --> SimMovement : delegates
    Simulation --> SimSteering : delegates
    Simulation --> SimTargeting : delegates
    Simulation --> SimProjectile : delegates
    Simulation --> SimEffects : delegates
    Simulation --> DeterministicRng : uses
```

### 3.4 Commands

```mermaid
classDiagram
    class ICommand {
        <<interface>>
    }

    class PlayCardCommand {
        +int Team
        +int HandIndex
        +Vector3 SpawnPosition
        +Vector3? TargetPosition
        +int? TargetUnitId
        +int NetworkId
        +int Sequence
        +long IssuedFrame
        +long ExecuteFrame
    }

    class ForfeitCommand {
        +int Team
    }

    ICommand <|.. PlayCardCommand
    ICommand <|.. ForfeitCommand
```

### 3.5 Events (SimEvent Hierarchy)

```mermaid
classDiagram
    class SimEvent {
        <<abstract>>
    }

    class PhaseChangedEvent {
        +GamePhase OldPhase
        +GamePhase NewPhase
    }

    class PrepTimerUpdatedEvent {
        +float TimeRemaining
    }

    class SummonerHpChangedEvent {
        +int Team
        +float NewHp
        +float Damage
    }

    class SummonerManaChangedEvent {
        +int Team
        +float NewMana
    }

    class SummonerDamagedEvent {
        +int Team
        +int AttackerNetworkId
        +float Damage
        +DamageType DamageType
    }

    class CastingStartedEvent {
        +int Team
        +float Duration
        +string CardCatalogId
    }

    class CastingCompletedEvent {
        +int Team
        +string CardCatalogId
        +List~int~ SpawnedUnitIds
    }

    class CardDrawnEvent {
        +int Team
        +int SlotIndex
        +string CardCatalogId
    }

    class HandChangedEvent {
        +int Team
        +List~string~ NewHand
    }

    class DeckRecycledEvent {
        +int Team
    }

    class CommandRejectedEvent {
        +int Team
        +string Reason
    }

    class UnitActivationChangedEvent {
        +int NetworkId
        +UnitLifecycle NewLifecycle
    }

    class AbilityTriggeredEvent {
        +int SourceUnitId
        +string AbilityType
        +Vector3 Position
        +float Radius
    }

    class UnitRegisteredEvent {
        +int NetworkId
        +int Team
        +string CatalogId
        +Vector3 Position
        +float MaxHp
    }

    class UnitRemovedEvent {
        +int NetworkId
    }

    class UnitAttackedEvent {
        +int AttackerNetworkId
        +int TargetNetworkId
    }

    class UnitDamagedEvent {
        +int TargetNetworkId
        +int AttackerNetworkId
        +float Damage
        +DamageType DamageType
        +bool IsCrit
    }

    class UnitDiedSimEvent {
        +int NetworkId
        +int KillerNetworkId
    }

    class AttackEvadedEvent {
        +int AttackerNetworkId
        +int TargetNetworkId
    }

    class BuffAppliedEvent {
        +int UnitNetworkId
        +int BuffId
        +EffectType EffectType
        +string StatTarget
        +float Modifier
        +float Duration
    }

    class BuffExpiredEvent {
        +int UnitNetworkId
        +int BuffId
    }

    class ShieldAbsorbedEvent {
        +int UnitNetworkId
        +int BuffId
        +float AbsorbedAmount
        +float RemainingShieldHp
    }

    class TriggerFiredEvent {
        +int UnitNetworkId
        +TriggerType TriggerType
        +EffectType EffectType
    }

    class ProjectileSpawnedEvent {
        +int ProjectileId
        +int AttackerNetworkId
        +int? TargetNetworkId
        +Vector3 SpawnPosition
        +ProjectileMovementType MovementType
    }

    class ProjectileHitSimEvent {
        +int ProjectileId
        +int? TargetNetworkId
        +Vector3 HitPosition
    }

    class GameOverEvent {
        +int WinnerTeam
        +string Reason
    }

    SimEvent <|-- PhaseChangedEvent
    SimEvent <|-- PrepTimerUpdatedEvent
    SimEvent <|-- SummonerHpChangedEvent
    SimEvent <|-- SummonerManaChangedEvent
    SimEvent <|-- SummonerDamagedEvent
    SimEvent <|-- CastingStartedEvent
    SimEvent <|-- CastingCompletedEvent
    SimEvent <|-- CardDrawnEvent
    SimEvent <|-- HandChangedEvent
    SimEvent <|-- DeckRecycledEvent
    SimEvent <|-- CommandRejectedEvent
    SimEvent <|-- UnitActivationChangedEvent
    SimEvent <|-- AbilityTriggeredEvent
    SimEvent <|-- UnitRegisteredEvent
    SimEvent <|-- UnitRemovedEvent
    SimEvent <|-- UnitAttackedEvent
    SimEvent <|-- UnitDamagedEvent
    SimEvent <|-- UnitDiedSimEvent
    SimEvent <|-- AttackEvadedEvent
    SimEvent <|-- BuffAppliedEvent
    SimEvent <|-- BuffExpiredEvent
    SimEvent <|-- ShieldAbsorbedEvent
    SimEvent <|-- TriggerFiredEvent
    SimEvent <|-- ProjectileSpawnedEvent
    SimEvent <|-- ProjectileHitSimEvent
    SimEvent <|-- GameOverEvent
```

### 3.6 Card & Projectile Definitions (Read-Only Data)

```mermaid
classDiagram
    class ICardDefinitionProvider {
        <<interface>>
        +GetCardDefinition(catalogId: string) CardDefinition
    }

    class CardDefinition {
        <<abstract>>
        +string CatalogId
        +CardType CardType
        +float ManaCost
        +float CastTime
    }

    class SummonCardDefinition {
        +List~UnitTemplate~ UnitTemplates
        +FormationType FormationType
        +Dictionary~string, float~ FormationParams
    }

    class SpellCardDefinition {
        +SpellTargetingMode TargetingMode
        +List~SpellEffect~ Effects
        +float Radius
    }

    class SpellEffect {
        +EffectType EffectType
        +DamageType DamageType
        +float Amount
        +float Duration
        +float Radius
        +EffectTargetMode TargetMode
    }

    class UnitTemplate {
        +string CatalogId
        +float MaxHp
        +float AttackDamage
        +DamageType AttackType
        +float AttackRange
        +float AttackSpeed
        +float MoveSpeed
        +float CritChance
        +float CritMultiplier
        +int MovementLayer
        +Element Element
        +float PhysicalDefense
        +float MagicDefense
        +float Evasion
        +BehaviorProfile Behavior
        +List~TriggerConfig~ Triggers
    }

    class BehaviorProfile {
        +MovementStyle MovementStyle
        +TargetingPriority TargetingPriority
        +RetreatCondition RetreatCondition
        +float KiteRange
        +float RetreatHpThreshold
    }

    class ProjectileDefinition {
        +ProjectileMovementType MovementType
        +float Speed
        +float HitRadius
        +int PierceCount
        +float AoeRadius
        +float Lifetime
        --Arc--
        +float ArcHeight
        --Ballistic--
        +float Gravity
        --Weaving--
        +float VeerAngle
        +float CorrectionRate
    }

    CardDefinition <|-- SummonCardDefinition
    CardDefinition <|-- SpellCardDefinition
    SpellCardDefinition *-- SpellEffect
    SummonCardDefinition *-- UnitTemplate
    UnitTemplate *-- BehaviorProfile
    ICardDefinitionProvider --> CardDefinition : provides
```

### 3.7 Bridge Layer

```mermaid
classDiagram
    class SimulationNode {
        <<Godot Node>>
        -MatchState State
        -Simulation Sim
        -ICardDefinitionProvider CardProvider
        --Read-Only API--
        +GetPhase() GamePhase
        +GetUnitData(networkId: int) UnitData
        +GetAllUnits() Dictionary~int, UnitData~
        +GetPlayerHp(team: int) float
        +GetPlayerMana(team: int) float
        +GetPlayerHand(team: int) List~string~
        +GetSummonerData(team: int) SummonerData
        +GetFrameNumber() int
        +GetMatchTime() double
        +GetPrepTimeRemaining() float
        --Command Submission--
        +SubmitCommand(cmd: ICommand)
        --Signal Emission--
        -EmitEvents(events: List~SimEvent~)
        --Registration (Pre-Battle)--
        +RegisterSummoner(team: int, data: SummonerData)
        +RegisterUnit(data: UnitData)
        +SetWinCondition(condition: IWinCondition)
        --Snapshot--
        +ApplySnapshot(snapshot: StateSnapshot)
        --Coordinate Transform--
        +SimToWorld(simPos: Vector3) Vector3
        +WorldToSim(worldPos: Vector3) Vector3
        --Godot Signals--
        unit_activation_changed(networkId, newLifecycle)
        ability_triggered(sourceUnitId, abilityType, position, radius)
        unit_registered(networkId, team, catalogId, position)
        unit_removed(networkId)
        unit_attacked(attackerNetworkId, targetNetworkId)
        unit_damaged(targetNetworkId, attackerNetworkId, damage, damageType, isCrit)
        unit_died(networkId, killerNetworkId)
        attack_evaded(attackerNetworkId, targetNetworkId)
        summoner_hp_changed(team, newHp, damage)
        summoner_mana_changed(team, newMana)
        summoner_damaged(team, attackerNetworkId, damage, damageType)
        casting_started(team, duration, cardCatalogId)
        casting_completed(team, cardCatalogId, spawnedUnitIds)
        card_drawn(team, slotIndex, cardCatalogId)
        hand_changed(team, newHand)
        deck_recycled(team)
        command_rejected(team, reason)
        buff_applied(unitNetworkId, buffId, effectType, statTarget, modifier, duration)
        buff_expired(unitNetworkId, buffId)
        shield_absorbed(unitNetworkId, buffId, absorbedAmount, remainingShieldHp)
        trigger_fired(unitNetworkId, triggerType, effectType)
        projectile_spawned(projectileId, attackerNetworkId, targetNetworkId, spawnPos, movementType)
        projectile_hit(projectileId, targetNetworkId, hitPosition)
        phase_changed(oldPhase, newPhase)
        prep_timer_updated(timeRemaining)
        game_over(winnerTeam, reason)
    }

    SimulationNode --> Simulation : owns
    SimulationNode --> MatchState : owns
```

### 3.8 Win Conditions

```mermaid
classDiagram
    class IWinCondition {
        <<interface>>
        +Evaluate(state: MatchState) WinResult?
    }

    class WinResult {
        +int WinnerTeam
        +string Reason
    }

    class DestroyBaseCondition {
        +Evaluate(state: MatchState) WinResult?
    }
    note for DestroyBaseCondition "Enemy summoner HP <= 0"

    class SurviveCondition {
        +float TargetTime
        +int SurvivingTeam
        +Evaluate(state: MatchState) WinResult?
    }
    note for SurviveCondition "Summoner alive after TargetTime seconds"

    class FirstBloodCondition {
        +Evaluate(state: MatchState) WinResult?
    }
    note for FirstBloodCondition "First team to kill any enemy unit"

    class KillCountCondition {
        +int RequiredKills
        +Evaluate(state: MatchState) WinResult?
    }
    note for KillCountCondition "First team to reach RequiredKills"

    class TimedDestroyCondition {
        +float TimeLimit
        +int AttackingTeam
        +Evaluate(state: MatchState) WinResult?
    }
    note for TimedDestroyCondition "Destroy base within TimeLimit or lose"

    IWinCondition <|.. DestroyBaseCondition
    IWinCondition <|.. SurviveCondition
    IWinCondition <|.. FirstBloodCondition
    IWinCondition <|.. KillCountCondition
    IWinCondition <|.. TimedDestroyCondition
    IWinCondition --> WinResult : returns
```

### 3.9 Multiplayer

```mermaid
classDiagram
    class HostRunner {
        -SimulationNode SimNode
        -int SnapshotInterval
        -int FramesSinceSnapshot
        +ProcessFrame(delta: float)
        +HandleClientCommand(serialized: byte[])
        -RemapCommandCoordinates(cmd: ICommand)
        -ValidateClientCommand(cmd: ICommand) bool
        -EnqueueCommand(cmd: ICommand)
        -BroadcastEvents(events: List~SimEvent~)
        -BroadcastSnapshot()
        -ShouldSendSnapshot() bool
    }

    class ClientRunner {
        -SimulationNode SimNode
        -float PingTimer
        +ProcessFrame(delta: float)
        +HandleEvent(serialized: byte[])
        +HandleSnapshot(serialized: byte[])
        +SendCommand(cmd: ICommand)
        -RemapCommandCoordinates(cmd: ICommand)
        -ApplyEventToPresentation(event: SimEvent)
        -ApplySnapshotCorrection(snapshot: StateSnapshot)
        -ComputeLocalStateHash() uint
        -ReportHash(hash: uint, frame: int)
    }

    class StateSnapshotBuilder {
        +Build(state: MatchState) StateSnapshot$
        +ComputeStateHash(state: MatchState) uint$
    }

    class StateSnapshot {
        +int Frame
        +double MatchTime
        +GamePhase Phase
        +float PrepTimeRemaining
        +SummonerData[] Summoners
        +UnitData[] Units
        +ProjectileData[] Projectiles
        +uint StateHash
    }

    class MessageSerializer {
        +SerializeEvent(event: SimEvent) byte[]$
        +DeserializeEvent(data: byte[]) SimEvent$
        +SerializeCommand(cmd: ICommand) byte[]$
        +DeserializeCommand(data: byte[]) ICommand$
        +SerializeSnapshot(snap: StateSnapshot) byte[]$
        +DeserializeSnapshot(data: byte[]) StateSnapshot$
    }

    HostRunner --> SimulationNode : drives Tick
    HostRunner --> StateSnapshotBuilder : builds snapshots
    HostRunner --> MessageSerializer : serializes
    ClientRunner --> SimulationNode : reads only
    ClientRunner --> MessageSerializer : deserializes
    HostRunner <..> ClientRunner : network transport
```

---

## 4. Damage Pipeline

Full calculation flow from attack initiation to HP deduction.

```mermaid
flowchart TD
    Start(["Attack Initiated\n(melee hit / projectile hit / spell damage)"])
    BaseDmg["Base Damage\n(from unit ATK or spell amount)"]
    DmgType["Determine DamageType\n(Physical or Magic)"]
    Evasion{"Evasion Check\n(seeded RNG vs target.Evasion)"}
    Evaded(["EVADED\nEmit AttackEvadedEvent\nDamage = 0"])
    Crit{"Critical Hit Check\n(seeded RNG vs attacker.CritChance)"}
    CritYes["Apply CritMultiplier\ndamage *= attacker.CritMultiplier"]
    CritNo["No crit"]
    Elemental["Elemental Matchup\n• Advantage: damage *= 1.25\n• Disadvantage: damage *= 0.8\n• Neutral: unchanged"]
    SummonerBonus["Summoner Damage Bonus\ndamage *= (1 + summoner.DamageBonus)"]
    Defense["Defense Reduction\n(pluggable formula)\n• Physical → target.PhysicalDefense\n• Magic → target.MagicDefense"]
    Shield{"Target has Shields?"}
    ShieldAbsorb["Shield Absorption\n(oldest shield first)\nLoop shields:\n  absorbed = min(shield.ShieldHp, remaining)\n  shield.ShieldHp -= absorbed\n  remaining -= absorbed\n  if shield depleted → remove buff\n  Emit ShieldAbsorbedEvent"]
    Floor["Floor Damage\ndamage = max(damage, 1)"]
    DeductHP["Deduct HP\ntarget.CurrentHp -= damage"]
    EmitDmg["Emit UnitDamagedEvent\n(damage, type, isCrit)"]
    DeathCheck{"target.CurrentHp <= 0?"}
    Death["Set Lifecycle = Dying\nEmit UnitDiedSimEvent"]
    DeathTriggers["Fire OnDeath triggers on target\nFire OnKill triggers on attacker\nFire LeaderDeath if target was leader"]
    Alive(["Target survives"])

    Start --> BaseDmg --> DmgType --> Evasion
    Evasion -->|"Evaded"| Evaded
    Evasion -->|"Not evaded"| Crit
    Crit -->|"Crit!"| CritYes --> Elemental
    Crit -->|"No crit"| CritNo --> Elemental
    Elemental --> SummonerBonus --> Defense --> Shield
    Shield -->|"Yes"| ShieldAbsorb --> Floor
    Shield -->|"No"| Floor
    Floor --> DeductHP --> EmitDmg --> DeathCheck
    DeathCheck -->|"Yes"| Death --> DeathTriggers
    DeathCheck -->|"No"| Alive
```

---

## 5. Multiplayer Data Flow

Host-client interactions for commands, events, and snapshots.

```mermaid
sequenceDiagram
    participant P as Player (Client)
    participant CR as ClientRunner
    participant Net as Network
    participant HR as HostRunner
    participant Sim as Simulation.Tick()
    participant SS as StateSnapshotBuilder

    Note over P, SS: Command Flow (Client → Host → Sim)
    P->>CR: PlayCardCommand (local coords)
    CR->>CR: Remap coords (local → canonical)
    CR->>Net: Serialize & send command
    Net->>HR: Receive command
    HR->>HR: Remap coords, validate
    HR->>Sim: Enqueue command

    Note over P, SS: Tick Processing (Host Only)
    HR->>Sim: Tick(fixedDelta)
    Sim->>Sim: Process commands, run systems
    Sim-->>HR: Return List<SimEvent>

    Note over P, SS: Event Broadcast (Host → Client)
    HR->>Net: Serialize events
    Net->>CR: Receive events
    CR->>CR: Apply to presentation
    CR->>P: Godot signals → UI updates

    Note over P, SS: Periodic Snapshot (Drift Correction)
    HR->>SS: Build(MatchState)
    SS-->>HR: StateSnapshot + StateHash
    HR->>Net: Serialize snapshot
    Net->>CR: Receive snapshot
    CR->>CR: ApplySnapshotCorrection()

    Note over P, SS: Desync Detection
    CR->>CR: ComputeLocalStateHash()
    CR->>Net: ReportHash(hash, frame)
    Net->>HR: Receive hash report
    HR->>HR: Compare with local hash
    alt Hash mismatch
        HR->>SS: Build full snapshot
        HR->>Net: Send corrective snapshot
        Net->>CR: Apply full state correction
    end
```

---

## 6. Card Play Flow

Full path from player input to visual result.

```mermaid
sequenceDiagram
    participant Player as Player
    participant SG as SummonerVisual.cs
    participant SN as SimulationNode
    participant CQ as Command Queue
    participant Tick as Simulation.Tick()
    participant SE as SimEffects
    participant Pres as Presentation (UnitVisual / HUD)

    Note over Player, Pres: 1. Input Phase
    Player->>SG: Drag card to battlefield
    SG->>SG: Local sanity check\n(hand index valid, mana plausible)
    SG->>SN: SubmitCommand(PlayCardCommand)
    SN->>CQ: Enqueue command

    Note over Player, Pres: 2. Validation Phase (inside Tick)
    Tick->>CQ: Dequeue PlayCardCommand
    Tick->>Tick: Validate:\n• Mana >= cost?\n• HandIndex in bounds?\n• Phase allows card type?\n• Spawn position in valid zone?
    alt Invalid
        Tick-->>SN: Emit CommandRejectedEvent
        SN-->>SG: command_rejected signal
        SG-->>Player: Show rejection feedback
    end

    Note over Player, Pres: 3. Casting Phase
    Tick->>Tick: Deduct mana immediately\nStart cast timer\neffective_time = base / cast_speed
    Tick-->>SN: Emit SummonerManaChangedEvent
    Tick-->>SN: Emit CastingStartedEvent
    SN-->>SG: casting_started signal
    SG-->>Player: Show casting bar

    Note over Player, Pres: 4a. Cast Complete — Summon Card
    Tick->>Tick: CastTimeRemaining <= 0
    Tick->>Tick: Spawn units in formation\nAssign group IDs + leader\nSet Lifecycle = Spawning/Active
    Tick->>Tick: Move card to discard\nReplacement draw
    Tick-->>SN: Emit CastingCompletedEvent
    Tick-->>SN: Emit UnitRegisteredEvent (per unit)
    Tick-->>SN: Emit CardDrawnEvent
    SN-->>Pres: unit_registered signal → spawn UnitVisual
    SN-->>SG: hand_changed signal → update UI

    Note over Player, Pres: 4b. Cast Complete — Spell Card
    Tick->>SE: ResolveSpellTargets(mode, pos/unitId, radius)
    SE-->>Tick: Target list
    Tick->>SE: ApplyEffect(effectType, source, targets, params)
    SE->>SE: Apply damage/heal/buff/debuff
    Tick->>Tick: Move card to discard\nReplacement draw
    Tick-->>SN: Emit CastingCompletedEvent
    Tick-->>SN: Emit effect-specific events
    SN-->>Pres: Signals → VFX, health bars, buffs
```

---

## 7. Unit Behavior State Machine

### 7.1 Lifecycle States

```mermaid
stateDiagram-v2
    [*] --> Spawning: Unit created by\ncard play
    Spawning --> Active: Spawn animation done\n(or immediately at battle start\nfor prep units)
    Active --> Dying: HP <= 0\n(UnitDiedSimEvent)
    Dying --> Dead: Death animation\ncomplete
    Dead --> [*]: Removed from\nMatchState
```

### 7.2 Behavior Loop (within Active state)

```mermaid
stateDiagram-v2
    state Active {
        [*] --> NoTarget
        NoTarget --> Chasing: Target acquired\n(SimTargeting)
        NoTarget --> AdvanceToSummoner: No enemies alive

        Chasing --> NoTarget: Target died/invalid
        Chasing --> InRange: Distance <= AttackRange
        Chasing --> AdvanceToSummoner: No valid targets

        InRange --> Attacking: Cooldown ready
        InRange --> Chasing: Target moved\nout of range
        InRange --> NoTarget: Target died

        Attacking --> InRange: Attack executed\nCooldown started
        Attacking --> NoTarget: Target died\nduring attack

        AdvanceToSummoner --> Chasing: New enemy\nspawned/found
        AdvanceToSummoner --> DamageSummoner: Reached summoner\nposition

        DamageSummoner --> AdvanceToSummoner: Continue\nattacking summoner
        DamageSummoner --> Chasing: New enemy\ntarget found
    }

    state "Behavior Profile Modifiers" as BPM {
        state "MovementStyle" as MS {
            MoveToward: Walk directly to target
            Kite: Maintain KiteRange from target\nRetreat if too close
            FollowLeader: Stay near leader\nMove where leader moves
        }

        state "TargetingPriority" as TP {
            NearestEnemy: Target closest enemy
            SummonerPriority: Prefer enemy summoner
            LeaderTarget: Attack leader's target
        }

        state "RetreatCondition" as RC {
            NoRetreat: Default — no retreat
            HpThreshold: Flee below X% HP\nChanges movement behavior
        }
    }
```

---

## 8. Effect System Flow

Three trigger entry points converging on a shared effect pipeline.

```mermaid
flowchart TD
    subgraph EntryPoints["THREE TRIGGER ENTRY POINTS"]
        Combat["SimBehavior Combat Events\n• OnAttack (after attack)\n• OnHit (after damage dealt)\n• OnKill (after kill)\n• OnDeath (when HP <= 0)\n• OnDamaged (after taking damage)\n• HpThreshold (HP below %)"]
        Periodic["SimEffects.TickBuffs()\n• Periodic triggers\n  (every N seconds)\n• DoT tick\n• HoT tick"]
        SpellCast["Simulation.Tick()\nSpell Cast Completion\n• Card cast timer expires\n• Spell effects applied"]
    end

    subgraph Dispatch["TRIGGER DISPATCH"]
        MatchTrigger["Match TriggerType\non unit's TriggerConfig list"]
        CheckCooldown{"Trigger on cooldown?"}
        CooldownYes(["Skip — not ready"])
        CheckFired{"One-shot already fired?\n(HasFired for HpThreshold)"}
        FiredYes(["Skip — already used"])
        StartCooldown["Start trigger cooldown\nMark HasFired if one-shot"]
    end

    subgraph TargetRes["TARGET RESOLUTION"]
        ResolveMode{"EffectTargetMode?"}
        SelfTarget["Self → source unit"]
        TargetUnit["Target → current combat target"]
        AlliesRadius["AlliesInRadius\n→ GetAliveActiveUnitsForTeam\n  within radius of source"]
        EnemiesRadius["EnemiesInRadius\n→ GetAliveActiveUnitsForTeam\n  (enemy team) within radius"]
        AllRadius["AllInRadius\n→ GetAliveActiveUnits\n  within radius"]
        SpellResolve["SpellTargetingMode resolution\n• Position → units in radius of point\n• Unit → specific unit / AoE centered"]
    end

    subgraph Apply["EFFECT APPLICATION (SimEffects.ApplyEffect)"]
        DispatchEffect{"EffectType?"}
        DirectDmg["DirectDamage\n→ SimDamage.Calculate()\n→ Deduct HP\n→ Emit UnitDamagedEvent"]
        Heal["Heal\n→ Restore HP (cap at MaxHp)\n→ Emit event"]
        StatMod["StatModifier\n→ Add ActiveBuff\n→ Modify stat\n→ Emit BuffAppliedEvent"]
        Shield["Shield\n→ Add ActiveBuff with ShieldHp\n→ Emit BuffAppliedEvent"]
        Stun["Stun\n→ Add ActiveBuff (blocks attack + move)\n→ Emit BuffAppliedEvent"]
        Slow["Slow\n→ Add ActiveBuff (reduce MoveSpeed)\n→ Emit BuffAppliedEvent"]
        DoT["DamageOverTime\n→ Add ActiveBuff with periodic damage\n→ TickBuffs handles ticks\n→ Emit BuffAppliedEvent"]
        HoT["HealOverTime\n→ Add ActiveBuff with periodic heal\n→ TickBuffs handles ticks\n→ Emit BuffAppliedEvent"]
        ChargeDmg["ChargeBonusDamage\n→ Check DistanceTraveled\n→ Apply bonus to next attack"]
        AoE["AoE\n→ Resolve area targets\n→ Apply inner effect to each"]
    end

    Combat --> MatchTrigger
    Periodic --> MatchTrigger
    SpellCast --> SpellResolve

    MatchTrigger --> CheckCooldown
    CheckCooldown -->|"Yes"| CooldownYes
    CheckCooldown -->|"No"| CheckFired
    CheckFired -->|"Yes"| FiredYes
    CheckFired -->|"No"| StartCooldown

    StartCooldown --> ResolveMode
    ResolveMode -->|"Self"| SelfTarget
    ResolveMode -->|"Target"| TargetUnit
    ResolveMode -->|"AlliesInRadius"| AlliesRadius
    ResolveMode -->|"EnemiesInRadius"| EnemiesRadius
    ResolveMode -->|"AllInRadius"| AllRadius
    SpellResolve --> DispatchEffect

    SelfTarget --> DispatchEffect
    TargetUnit --> DispatchEffect
    AlliesRadius --> DispatchEffect
    EnemiesRadius --> DispatchEffect
    AllRadius --> DispatchEffect

    DispatchEffect -->|"DirectDamage"| DirectDmg
    DispatchEffect -->|"Heal"| Heal
    DispatchEffect -->|"StatModifier"| StatMod
    DispatchEffect -->|"Shield"| Shield
    DispatchEffect -->|"Stun"| Stun
    DispatchEffect -->|"Slow"| Slow
    DispatchEffect -->|"DamageOverTime"| DoT
    DispatchEffect -->|"HealOverTime"| HoT
    DispatchEffect -->|"ChargeBonusDamage"| ChargeDmg
    DispatchEffect -->|"AoE"| AoE
```

---

## 9. Requirements Verification Matrix

Every requirement mapped to responsible classes, with determinism and multiplayer safety verification.

### 9.1 Match Flow

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Deck locked at match start | Simulation (init), SummonerData | Yes — set once at initialization | Yes — part of MatchState, in snapshots |
| Preparation phase ~30s timer | Simulation.TickPreparation | Yes — fixedDelta-based countdown inside Tick | Yes — host-only Tick, timer in MatchState |
| Prep: summon cards only | Simulation.ProcessCommands | Yes — phase check is pure logic | Yes — validated by host Tick |
| Prep: units spawn inactive | Simulation.SpawnUnitsFromCard | Yes — Lifecycle set to Spawning | Yes — lifecycle in UnitData, in snapshots |
| Prep: fixed mana, no regen | SummonerData, Simulation | Yes — mana only decremented inside Tick | Yes — mana in MatchState |
| Battle start: activate all units | Simulation.TransitionToBattle | Yes — iterates all units, sets Active | Yes — host-only Tick emits PhaseChangedEvent |
| Battle start: hand refresh (draw 4) | Simulation.TransitionToBattle | Yes — seeded RNG for deck order | Yes — hand state in MatchState, HandChangedEvent broadcast |
| Battle start: mana carries over | Simulation.TransitionToBattle | Yes — no mana mutation on transition | Yes — mana unchanged in MatchState |
| Battle: all card types available | Simulation.ProcessCommands | Yes — phase check allows all types | Yes — validated by host Tick |
| Battle: no mana regen | SummonerData, Simulation | Yes — no regen logic exists | Yes — mana only modified by commands in Tick |
| Game over on win condition | Simulation.EvaluateWinConditions | Yes — predicate evaluation inside Tick | Yes — GameOverEvent broadcast |

### 9.2 Card System

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Hand size = 4 cards | SummonerData, Simulation | Yes — fixed constant | Yes — hand in MatchState |
| Replacement draw on play | Simulation.ReplacementDraw | Yes — seeded RNG for deck | Yes — CardDrawnEvent broadcast |
| Hand refresh at battle start | Simulation.TransitionToBattle | Yes — seeded RNG | Yes — HandChangedEvent broadcast |
| Mana fixed pool (~100) | SummonerData | Yes — set at init | Yes — in MatchState |
| Cast time per card | Simulation.TickCasting | Yes — timer in SummonerData, decremented in Tick | Yes — CastingStartedEvent broadcast |
| Cast speed modifier | Simulation (effective_time = base / cast_speed) | Yes — pure math | Yes — CastSpeed in SummonerData |
| Mana deducted immediately | Simulation.ProcessCommands | Yes — deducted when command validated | Yes — SummonerManaChangedEvent broadcast |
| Cast lock (no card during cast) | Simulation.ProcessCommands | Yes — IsCasting check | Yes — casting state in MatchState |
| Deck recycle (shuffle discard) | Simulation.RecycleDeck | Yes — seeded RNG (DeckShuffle domain) | Yes — DeckRecycledEvent broadcast |
| Spawn formations | Simulation.SpawnUnitsFromCard | Yes — deterministic geometry from FormationType | Yes — unit positions in UnitData |
| Spell targeting: position-based | SimEffects.ResolveSpellTargets | Yes — radius check, pure math | Yes — resolved inside Tick |
| Spell targeting: unit-based | SimEffects.ResolveSpellTargets | Yes — unit lookup from MatchState | Yes — resolved inside Tick |

### 9.3 Summoner

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Static base (fixed position) | SummonerData | Yes — position set at init, never changes | Yes — position in MatchState |
| HP / Max HP | SummonerData, SimBehavior | Yes — only modified inside Tick | Yes — SummonerHpChangedEvent broadcast |
| Mana / Max Mana | SummonerData, Simulation | Yes — only modified inside Tick | Yes — SummonerManaChangedEvent broadcast |
| Cast Speed stat | SummonerData | Yes — read at cast time inside Tick | Yes — in MatchState |
| Damage Bonus % | SummonerData, SimDamage | Yes — applied in damage formula | Yes — in MatchState |
| Damage Reduction | SummonerData, SimDamage | Yes — applied in damage formula | Yes — in MatchState |
| Element affinity | SummonerData, SimDamage | Yes — used in elemental matchup | Yes — in MatchState |
| Summoner HP 0 = defeat | IWinCondition (DestroyBaseCondition) | Yes — evaluated inside Tick | Yes — GameOverEvent broadcast |
| Progression baked at init | SimulationNode.RegisterSummoner | Yes — one-time setup before Tick runs | Yes — set before first Tick |

### 9.4 Combat System

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Physical/Magic damage types | DamageType enum, SimDamage | Yes — enum comparison, pure logic | Yes — DamageType in UnitData + events |
| Physical Defense stat | UnitData, SimDamage.ApplyDefense | Yes — pluggable formula, pure math | Yes — in UnitData, in snapshots |
| Magic Defense stat | UnitData, SimDamage.ApplyDefense | Yes — pluggable formula, pure math | Yes — in UnitData, in snapshots |
| Evasion (% dodge) | UnitData, SimDamage.CheckEvasion | Yes — seeded RNG (CombatCrits domain) | Yes — AttackEvadedEvent broadcast |
| Shield (stackable, oldest first) | ActiveBuff, SimDamage.ApplyShieldAbsorption | Yes — ordered iteration, pure math | Yes — ActiveBuffs in UnitData, in snapshots |
| Armor (temporary reduction) | ActiveBuff, SimEffects | Yes — buff applied inside Tick | Yes — ActiveBuffs in snapshots |
| Damage pipeline order | SimDamage.Calculate | Yes — fixed order, pure functions | Yes — all inside Tick |
| Pluggable defense formula | SimDamage.ApplyDefense | Yes — pure function | Yes — deterministic, same on host |
| Elemental matchups | SimDamage.ApplyElementalMatchup | Yes — table lookup, pure function | Yes — deterministic |
| Critical hits (seeded RNG) | SimDamage.ApplyCrit, DeterministicRng | Yes — seeded RNG (CombatCrits domain) | Yes — deterministic, same seed |
| Summoner damage from units | SimBehavior.ApplyDamageToSummoner | Yes — distance check + damage inside Tick | Yes — SummonerDamagedEvent broadcast |
| Floor at minimum 1 | SimDamage.FloorDamage | Yes — pure math | Yes — inside Tick |

### 9.5 Unit System

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Melee / Ranged / Flying types | UnitData (MovementLayer, AttackRange) | Yes — stat-driven | Yes — in UnitData, in snapshots |
| All 13 unit stats | UnitData | Yes — stored in MatchState | Yes — in snapshots |
| Lifecycle: Spawning → Active → Dying → Dead | UnitData.Lifecycle, Simulation | Yes — transitions inside Tick | Yes — lifecycle in UnitData, events broadcast |
| Autonomous behavior (no player control) | SimBehavior | Yes — FSM inside Tick, pure logic | Yes — host-only Tick |
| Acquire target → move → attack → repeat | SimTargeting, SimMovement, SimBehavior | Yes — all inside Tick | Yes — host-only Tick |
| Advance toward summoner if no enemies | SimBehavior.TickBehavior | Yes — fallback to summoner position | Yes — inside Tick |
| MovementStyle: MoveToward | SimMovement.CalculateForward | Yes — pure math | Yes — position in UnitData |
| MovementStyle: Kite | SimMovement.CalculateKite | Yes — pure math with KiteRange | Yes — position in UnitData |
| MovementStyle: FollowLeader | SimMovement.CalculateFollowLeader | Yes — reads leader position from MatchState | Yes — position in UnitData |
| TargetingPriority: NearestEnemy | SimTargeting.FindNearestEnemy | Yes — distance comparison | Yes — inside Tick |
| TargetingPriority: SummonerPriority | SimTargeting.FindSummonerPriority | Yes — prefers summoner position | Yes — inside Tick |
| TargetingPriority: LeaderTarget | SimTargeting.FindLeaderTarget | Yes — reads leader's target from MatchState | Yes — inside Tick |
| RetreatCondition: HpThreshold | SimBehavior.TickBehavior | Yes — HP comparison | Yes — inside Tick |
| Group ID tracking | UnitData.GroupId | Yes — assigned at spawn | Yes — in UnitData, in snapshots |
| Leader-follower relationships | UnitData.LeaderId, SimTargeting | Yes — set at spawn, queried in Tick | Yes — in UnitData, in snapshots |
| Death triggers on relationships | SimBehavior.FireTriggers (LeaderDeath) | Yes — fires inside Tick | Yes — TriggerFiredEvent broadcast |

### 9.6 Effect System

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Data-driven (config, not code) | TriggerConfig, SpellEffect | Yes — configs are pure data | Yes — configs in UnitData/CardDefinition |
| Runs inside Tick | SimEffects, SimBehavior | Yes — all inside Tick | Yes — host-only Tick |
| Trigger: OnAttack | SimBehavior.FireTriggers | Yes — fires after attack in Tick | Yes — inside Tick |
| Trigger: OnHit | SimBehavior.FireTriggers | Yes — fires after damage dealt in Tick | Yes — inside Tick |
| Trigger: OnKill | SimBehavior.FireTriggers | Yes — fires after kill in Tick | Yes — inside Tick |
| Trigger: OnDeath | SimBehavior.FireTriggers | Yes — fires when HP <= 0 in Tick | Yes — inside Tick |
| Trigger: OnDamaged | SimBehavior.FireTriggers | Yes — fires after taking damage in Tick | Yes — inside Tick |
| Trigger: HpThreshold | SimBehavior.FireTriggers | Yes — HP comparison in Tick | Yes — inside Tick |
| Trigger: Periodic | SimEffects.TickBuffs | Yes — timer-based inside Tick | Yes — timer in TriggerConfig |
| Trigger: OnSpawn | SimBehavior.FireTriggers | Yes — fires at activation in Tick | Yes — inside Tick |
| Trigger: LeaderDeath | SimBehavior.FireTriggers | Yes — fires on leader death in Tick | Yes — inside Tick |
| Effect: DirectDamage | SimEffects.ApplyDirectDamage → SimDamage | Yes — damage pipeline | Yes — UnitDamagedEvent |
| Effect: Heal | SimEffects.ApplyHeal | Yes — HP clamped at max | Yes — event broadcast |
| Effect: StatModifier | SimEffects.ApplyStatModifier | Yes — buff added to ActiveBuffs | Yes — BuffAppliedEvent |
| Effect: DamageOverTime | SimEffects.ApplyDoT, TickBuffs | Yes — periodic inside Tick | Yes — buff in ActiveBuffs |
| Effect: HealOverTime | SimEffects.ApplyHoT, TickBuffs | Yes — periodic inside Tick | Yes — buff in ActiveBuffs |
| Effect: Shield | SimEffects.ApplyShield | Yes — ShieldHp tracked in buff | Yes — BuffAppliedEvent |
| Effect: Stun | SimEffects.ApplyStun | Yes — blocks attack + move via buff | Yes — buff in ActiveBuffs |
| Effect: Slow | SimEffects.ApplySlow | Yes — speed modifier via buff | Yes — buff in ActiveBuffs |
| Effect: ChargeBonusDamage | UnitData.DistanceTraveled, SimBehavior | Yes — distance tracked in Tick | Yes — in UnitData |
| Effect: AoE | SimEffects + radius check | Yes — geometry, pure math | Yes — inside Tick |
| Shared system (abilities + spells) | SimEffects.ApplyEffect (both paths) | Yes — same code path | Yes — same events |
| ChargeAbility migration | TriggerConfig (OnAttack), DistanceTraveled | Yes — distance + trigger inside Tick | Yes — in UnitData |
| AuraAbility migration | TriggerConfig (Periodic), radius check | Yes — periodic inside Tick | Yes — TriggerFiredEvent |
| DeathExplosionAbility migration | TriggerConfig (OnDeath), AoE damage | Yes — fires on death inside Tick | Yes — inside Tick |
| SlowOnHitAbility migration | TriggerConfig (OnHit), Slow effect | Yes — fires on hit inside Tick | Yes — BuffAppliedEvent |

### 9.7 Projectile System

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Movement: Straight | SimProjectile.TickStraight | Yes — linear math | Yes — position in ProjectileData |
| Movement: Arc | SimProjectile.TickArc | Yes — Bezier/parabolic, pure math | Yes — position in ProjectileData |
| Movement: Homing | SimProjectile.TickHoming | Yes — tracks target from MatchState | Yes — position in ProjectileData |
| Movement: Ballistic | SimProjectile.TickBallistic | Yes — gravity math | Yes — position in ProjectileData |
| Movement: WeavingHoming | SimProjectile.TickWeavingHoming | Yes — seeded RNG for veer direction | Yes — position in ProjectileData |
| Per-definition config | ProjectileDefinition (read-only data) | Yes — data-driven | Yes — same data on host |
| Speed | ProjectileData.Speed | Yes — constant per projectile | Yes — in ProjectileData |
| Hit radius | SimProjectile.CheckHits | Yes — distance check | Yes — inside Tick |
| Pierce count | ProjectileData.PierceRemaining | Yes — decremented inside Tick | Yes — in ProjectileData |
| AoE radius | SimProjectile.ApplyAoE | Yes — radius check | Yes — inside Tick |
| Lifetime | ProjectileData.Lifetime, ElapsedTime | Yes — timer inside Tick | Yes — in ProjectileData |
| Deterministic hit detection | SimProjectile.CheckHits | Yes — line-segment distance, inside Tick | Yes — host decides hits |

### 9.8 Win Conditions

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Configurable predicate system | IWinCondition interface | Yes — evaluated inside Tick | Yes — set at init, in MatchState |
| Destroy base (HP <= 0) | DestroyBaseCondition | Yes — HP comparison | Yes — GameOverEvent broadcast |
| Survive (alive after X seconds) | SurviveCondition | Yes — time comparison | Yes — GameOverEvent broadcast |
| First blood | FirstBloodCondition | Yes — kill count check | Yes — GameOverEvent broadcast |
| Kill count (N kills) | KillCountCondition | Yes — counter comparison | Yes — GameOverEvent broadcast |
| Timed destroy | TimedDestroyCondition | Yes — time + HP check | Yes — GameOverEvent broadcast |
| Overtime (placeholder) | Simulation | Deferred — placeholder acceptable | Deferred |

### 9.9 Multiplayer

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Host-authoritative | HostRunner, Simulation | Yes — single Tick on host | Yes — by design |
| Client never runs Tick | ClientRunner (no Tick call) | N/A — client has no sim | Yes — enforced by IsHost check |
| Command-based input | ICommand, PlayCardCommand, ForfeitCommand | Yes — commands processed in Tick order | Yes — validated by host |
| Event broadcast | HostRunner.BroadcastEvents | N/A — network layer | Yes — events serialized and sent |
| Periodic snapshots | HostRunner.BroadcastSnapshot, StateSnapshotBuilder | N/A — correction mechanism | Yes — full state sync |
| Desync detection (state hash) | StateSnapshotBuilder.ComputeStateHash | Yes — deterministic hash | Yes — hash comparison triggers correction |
| Coordinate remapping | HostRunner, ClientRunner (remap methods) | Yes — fixed transform | Yes — canonical coords in MatchState |
| Single-player = local host | HostRunner (default) | Yes — same code path | Yes — no network, same Tick |
| AI uses command queue | AI Opponent → SubmitCommand | Yes — same pipeline as player | Yes — same validation |

### 9.10 Randomness

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Seeded RNG | DeterministicRng (Xorshift32) | Yes — same seed = same sequence | Yes — host seed is authoritative |
| Domain: DeckShuffle | DeterministicRng, Simulation.RecycleDeck | Yes — per-domain state | Yes — deck state in snapshots |
| Domain: AiDecisions | DeterministicRng, AI | Yes — per-domain state | Yes — AI on host only |
| Domain: CombatCrits | DeterministicRng, SimDamage.ApplyCrit | Yes — per-domain state | Yes — crit result in events |
| Domain: SpawnPositions | DeterministicRng | Yes — per-domain state | Yes — positions in UnitData |
| Domain: ProjectileBehavior | DeterministicRng, SimProjectile | Yes — per-domain state | Yes — positions in ProjectileData |
| Non-deterministic presentation RNG | System.Random / GDScript randf() | N/A — visual only | Yes — no gameplay impact |

### 9.11 Battlefield

| Requirement | Responsible Classes | Deterministic? | Multiplayer-Safe? |
|---|---|---|---|
| Flat open rectangle | MatchState (boundaries) | Yes — fixed at init | Yes — same boundaries |
| Canonical coordinate system | SimulationNode (SimToWorld, WorldToSim) | Yes — fixed transform | Yes — all sim uses canonical coords |
| Perspective flip for client | ClientRunner, SimulationNode | Yes — deterministic transform | Yes — applied at presentation only |
| Spawn zone validation | Simulation.ProcessCommands | Yes — boundary check inside Tick | Yes — validated by host |

---

## Key Design Decisions

### 1. Fat Struct for ActiveBuff/TriggerConfig
One struct covers all effect types. Unused fields are zeroed. This is preferred over an interface hierarchy because:
- Serialization is trivial (no polymorphic dispatch)
- Snapshot hashing is straightforward (hash all fields)
- No allocations for different buff subtypes
- Simpler iteration in TickBuffs

### 2. PlayCardCommand Handles Both Summons and Spells
`TargetPosition` and `TargetUnitId` are nullable. Which is set depends on `SpellTargetingMode` (from the card definition). This is cleaner than separate command types because:
- Single command validation pipeline
- Single casting flow in Tick
- Type resolution happens at cast completion, not at command creation

### 3. IWinCondition Stored on MatchState
The win condition is part of the serializable state. It is:
- Included in snapshots
- Set at match initialization
- Immutable during gameplay
- Evaluated every Tick by calling `IWinCondition.Evaluate(state)`

### 4. ICardDefinitionProvider Interface
The simulation queries this interface to resolve `CatalogId → CardDefinition`. The bridge layer provides the implementation using the existing card catalog system. This keeps:
- Simulation free of Godot dependencies
- Card data loaded from existing catalog infrastructure
- Clean separation between data and logic

### 5. UnitLifecycle Enum Replaces ActivationState
A proper C# enum inside simulation with clear states (Spawning, Active, Dying, Dead) instead of integer flags. No GDScript dependency. Lifecycle transitions happen only inside Tick.

---

*Last updated: 2026-02-26*
*Status: Finalized*
*Source: Derived from requirements.md, architecture-decisions.md, implementation-plan.md, ai-implementation-guide.md, problem-analysis.md*
