using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using ProjectSummoner.Units;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

namespace ProjectSummoner.Tests.Simulation;

/// <summary>
/// Shared factory methods for simulation tests.
/// Creates MatchState, UnitData, SimCardData instances with sane defaults.
/// </summary>
public static class SimTestHelper
{
    /// <summary>
    /// Create a MatchState in Battle phase with two alive summoners and seeded RNG.
    /// </summary>
    public static MatchState CreateBattleState(uint seed = 42)
    {
        var state = new MatchState
        {
            Phase = GamePhase.Battle,
            FrameNumber = 0,
            MatchTime = 0f,
            Rng = new DeterministicRng(seed)
        };

        state.Summoners[0].CurrentHp = 100f;
        state.Summoners[0].MaxHp = 100f;
        state.Summoners[0].IsAlive = true;
        state.Summoners[0].Mana = 10f;
        state.Summoners[0].MaxMana = 10f;
        state.Summoners[0].Position = new SimVector3(-20f, 0f, 0f);

        state.Summoners[1].CurrentHp = 100f;
        state.Summoners[1].MaxHp = 100f;
        state.Summoners[1].IsAlive = true;
        state.Summoners[1].Mana = 10f;
        state.Summoners[1].MaxMana = 10f;
        state.Summoners[1].Position = new SimVector3(20f, 0f, 0f);

        return state;
    }

    /// <summary>
    /// Create a MatchState in Preparation phase.
    /// </summary>
    public static MatchState CreatePrepState(uint seed = 42, float prepTime = 10f)
    {
        var state = CreateBattleState(seed);
        state.Phase = GamePhase.Preparation;
        state.PrepTimeRemaining = prepTime;
        return state;
    }

    /// <summary>
    /// Create a melee UnitData and register it in state.
    /// </summary>
    public static UnitData CreateMeleeUnit(
        MatchState state, int team,
        float x = 0f, float z = 0f,
        float hp = 100f, float damage = 10f,
        float attackSpeed = 1f, float attackRange = 2f,
        float moveSpeed = 3f, float aggroRadius = 20f)
    {
        int unitId = state.NextUnitId();
        var unit = new UnitData
        {
            UnitId = unitId,
            Team = (Team)team,
            CurrentHp = hp,
            MaxHp = hp,
            IsAlive = true,
            Position = new SimVector3(x, 0f, z),
            AttackDamage = damage,
            AttackSpeed = attackSpeed,
            MoveSpeed = moveSpeed,
            AttackRange = attackRange,
            AggroRadius = aggroRadius,
            UnitType = UnitType.Melee,
            MovementLayer = MovementLayer.Ground,
            ActivationState = ActivationState.Active,
            IsFacingRight = team == 0,
            TargetLayerFilter = TargetLayer.Both
        };
        state.Units[unitId] = unit;
        return unit;
    }

    /// <summary>
    /// Create a ranged UnitData with projectile delay and register it in state.
    /// </summary>
    public static UnitData CreateRangedUnit(
        MatchState state, int team,
        float x = 0f, float z = 0f,
        float hp = 80f, float damage = 15f,
        float attackSpeed = 1f, float attackRange = 8f,
        float moveSpeed = 2.5f, float aggroRadius = 20f,
        float projectileDelay = 0.5f)
    {
        int unitId = state.NextUnitId();
        var unit = new UnitData
        {
            UnitId = unitId,
            Team = (Team)team,
            CurrentHp = hp,
            MaxHp = hp,
            IsAlive = true,
            Position = new SimVector3(x, 0f, z),
            AttackDamage = damage,
            AttackSpeed = attackSpeed,
            MoveSpeed = moveSpeed,
            AttackRange = attackRange,
            AggroRadius = aggroRadius,
            UnitType = UnitType.Ranged,
            MovementLayer = MovementLayer.Ground,
            ProjectileDelay = projectileDelay,
            ActivationState = ActivationState.Active,
            IsFacingRight = team == 0,
            TargetLayerFilter = TargetLayer.Both
        };
        state.Units[unitId] = unit;
        return unit;
    }

    /// <summary>
    /// Create a flying (air) UnitData and register it in state.
    /// </summary>
    public static UnitData CreateFlyingUnit(
        MatchState state, int team,
        float x = 0f, float z = 0f,
        float hp = 60f, float damage = 12f,
        float altitude = 3f, float attackRange = 6f)
    {
        int unitId = state.NextUnitId();
        var unit = new UnitData
        {
            UnitId = unitId,
            Team = (Team)team,
            CurrentHp = hp,
            MaxHp = hp,
            IsAlive = true,
            Position = new SimVector3(x, altitude, z),
            AttackDamage = damage,
            AttackSpeed = 1f,
            MoveSpeed = 3f,
            AttackRange = attackRange,
            AggroRadius = 20f,
            UnitType = UnitType.Melee,
            MovementLayer = MovementLayer.Air,
            FlightAltitude = altitude,
            ActivationState = ActivationState.Active,
            IsFacingRight = team == 0,
            TargetLayerFilter = TargetLayer.Both
        };
        state.Units[unitId] = unit;
        return unit;
    }

    /// <summary>
    /// Create a SimCardData for a summon card.
    /// </summary>
    public static SimCardData CreateSummonCard(
        string catalogId = "test_card",
        int manaCost = 3,
        float summonTime = 1.0f,
        float unitHp = 100f,
        float unitDamage = 10f,
        int unitCount = 1)
    {
        return new SimCardData
        {
            CatalogId = catalogId,
            ManaCost = manaCost,
            SummonTime = summonTime,
            IsSpell = false,
            UnitTemplates = new List<SimUnitTemplate>
            {
                new()
                {
                    Count = unitCount,
                    MaxHp = unitHp,
                    AttackDamage = unitDamage,
                    AttackSpeed = 1f,
                    MoveSpeed = 3f,
                    AttackRange = 2f,
                    AggroRadius = 20f,
                    UnitType = UnitType.Melee,
                    MovementLayer = MovementLayer.Ground
                }
            }
        };
    }

    /// <summary>
    /// Create a SimCardData for a spell card.
    /// </summary>
    public static SimCardData CreateSpellCard(
        string catalogId = "test_spell",
        int manaCost = 4,
        float damage = 50f,
        float radius = 5f)
    {
        return new SimCardData
        {
            CatalogId = catalogId,
            ManaCost = manaCost,
            SummonTime = 0.5f,
            IsSpell = true,
            SpellTargetingMode = SpellTargetingMode.Position,
            SpellRadius = radius,
            SpellEffects = new List<SimSpellEffect>
            {
                new()
                {
                    EffectType = EffectType.Damage,
                    Value = damage,
                    DamageType = DamageType.Magic,
                    Affinity = SpellAffinity.Enemies
                }
            }
        };
    }

    /// <summary>
    /// Count events of a specific type in an event list.
    /// </summary>
    public static int CountEvents<T>(List<SimEvent> events) where T : SimEvent
    {
        return events.Count(e => e is T);
    }

    /// <summary>
    /// Find the first event of a specific type in an event list.
    /// </summary>
    public static T? FindEvent<T>(List<SimEvent> events) where T : SimEvent
    {
        return events.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Find all events of a specific type in an event list.
    /// </summary>
    public static List<T> FindEvents<T>(List<SimEvent> events) where T : SimEvent
    {
        return events.OfType<T>().ToList();
    }
}
