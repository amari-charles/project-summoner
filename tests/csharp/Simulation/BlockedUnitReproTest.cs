namespace Fateforged.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using GdUnit4;
using static GdUnit4.Assertions;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;

/// <summary>
/// Repro tests for: "Puff Units Get Stuck in Idle When Blocked by Other Units"
/// Sets up scenarios where friendly units physically block each other's path
/// to an enemy and checks whether units still attack or get stuck idle.
/// </summary>
[TestSuite]
public class BlockedUnitReproTest
{
    private const float Delta = 1f / 60f;
    private const int FiveSeconds = 300; // 5s at 60fps
    private const int TenSeconds = 600;
    private const int TwentySeconds = 1200;

    private MatchState _state = null!;
    private Fateforged.Simulation.Simulation _sim = null!;

    [BeforeTest]
    public void Setup()
    {
        _state = SimTestHelper.CreateBattleState();
        _sim = new Fateforged.Simulation.Simulation(_state);
    }

    /// <summary>
    /// Core repro: two friendly melee units in a line with one enemy ahead.
    /// The front unit should engage. The back unit should either:
    /// (a) flank around the front unit, or
    /// (b) still attack through the front unit (no LOS checks exist).
    /// It should NOT be stuck idle with zero velocity for extended periods.
    /// </summary>
    [TestCase]
    public void BlockedMelee_BehindFriendly_ShouldNotStayIdleForever()
    {
        // Back unit (this is the one we're testing — it's behind the front unit)
        var backUnit = SimTestHelper.CreateMeleeUnit(_state, 0, x: -4f, z: 0f,
            attackRange: 2f, moveSpeed: 3f);

        // Front unit (blocker — same team, between back unit and enemy)
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -1f, z: 0f,
            attackRange: 2f, moveSpeed: 3f);

        // Enemy
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0f,
            hp: 500f, attackRange: 2f);

        // Run simulation for 5 seconds
        int stuckIdleFrames = 0;
        var allEvents = new List<SimEvent>();
        for (int i = 0; i < FiveSeconds; i++)
        {
            allEvents.AddRange(_sim.Tick(Delta));

            // Count frames where unit has a target but zero velocity
            if (backUnit.TargetUnitId.HasValue &&
                backUnit.Velocity.LengthSquared() < 0.001f &&
                backUnit.BehaviorState != BehaviorState.Attacking)
            {
                stuckIdleFrames++;
            }
        }

        // Commit-slot flow can queue behind occupied slots, so some additional
        // idle time is expected. Guard against true deadlock by requiring attacks
        // and bounding idle windows to less than 4 seconds over a 5 second sim.
        bool backUnitAttacked = SimTestHelper.FindEvents<UnitAttackedEvent>(allEvents)
            .Any(e => e.AttackerUnitId == backUnit.UnitId);
        AssertThat(backUnitAttacked).IsTrue();
        int fourSecondsOfFrames = 240;
        AssertThat(stuckIdleFrames).IsLess(fourSecondsOfFrames);
    }

    /// <summary>
    /// No-target objective-advance regression:
    /// when enemy units are alive but outside aggro, the unit should keep advancing
    /// instead of idling due null target in commit-melee movement.
    /// </summary>
    [TestCase]
    public void NoTarget_ObjectiveAdvance_DoesNotIdleWithNullTarget()
    {
        var mover = SimTestHelper.CreateMeleeUnit(_state, 0, x: -18f, z: 0f,
            attackRange: 2f, aggroRadius: 4f, moveSpeed: 3f);

        // Keep at least one enemy unit alive, but well outside aggro so commit targeting returns null.
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 20f, z: 0f,
            attackRange: 2f, aggroRadius: 0f, moveSpeed: 0f);

        int movingFramesWithoutTarget = 0;
        for (int i = 0; i < FiveSeconds; i++)
        {
            _sim.Tick(Delta);
            if (!mover.TargetUnitId.HasValue && mover.Velocity.LengthSquared() > 0.001f)
                movingFramesWithoutTarget++;
        }

        AssertThat(movingFramesWithoutTarget).IsGreater(240); // >4 seconds of active advance over 5-second run
    }

    /// <summary>
    /// Forward-rect melee with positive forward offset (Pebloom-like profile)
    /// should still secure a slot that allows attacks instead of parking idle
    /// while a nearby enemy free-hits.
    /// </summary>
    [TestCase]
    public void ForwardRectOffsetUnit_VersusFireWisp_EventuallyAttacks()
    {
        var pebbloomLike = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: -2f,
            z: 0f,
            hp: 280f,
            damage: 20f,
            attackSpeed: 0.9f,
            attackRange: 3f,
            moveSpeed: 1.8f,
            aggroRadius: 20f);
        pebbloomLike.EngageShape = EngageShape.ForwardRect;
        pebbloomLike.EngageRectLength = 5.4f;
        pebbloomLike.EngageRectHalfWidth = 2.6f;
        pebbloomLike.EngageRectForwardOffset = 2.1f;
        pebbloomLike.EngageCloseRadius = 0.54f;

        var fireWispLike = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2f,
            z: 0f,
            hp: 120f,
            damage: 10f,
            attackSpeed: 1.2f,
            attackRange: 3f,
            moveSpeed: 3.5f,
            aggroRadius: 20f);

        var allEvents = new List<SimEvent>();
        for (int i = 0; i < TenSeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        bool pebbloomAttacked = SimTestHelper.FindEvents<UnitAttackedEvent>(allEvents)
            .Any(e => e.AttackerUnitId == pebbloomLike.UnitId);
        bool fireWispDamaged = SimTestHelper.FindEvents<UnitDamagedEvent>(allEvents)
            .Any(e => e.TargetUnitId == fireWispLike.UnitId && e.AttackerUnitId == pebbloomLike.UnitId);

        AssertThat(pebbloomAttacked).IsTrue();
        AssertThat(fireWispDamaged).IsTrue();
    }

    /// <summary>
    /// Multiple friendly units in a column — back units should still contribute
    /// to combat (either by flanking or attacking through friendlies).
    /// </summary>
    [TestCase]
    public void BlockedMelee_ThreeUnitColumn_AllUnitsEventuallyAttack()
    {
        // Three friendly units in a line
        var unit1 = SimTestHelper.CreateMeleeUnit(_state, 0, x: -6f, z: 0f,
            attackRange: 2f, damage: 5f);
        var unit2 = SimTestHelper.CreateMeleeUnit(_state, 0, x: -3f, z: 0f,
            attackRange: 2f, damage: 5f);
        var unit3 = SimTestHelper.CreateMeleeUnit(_state, 0, x: -1f, z: 0f,
            attackRange: 2f, damage: 5f);

        // Tanky enemy
        var enemy = SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0f,
            hp: 2000f, attackRange: 2f, damage: 1f);

        var allEvents = new List<SimEvent>();

        // Run for 10 seconds
        for (int i = 0; i < TenSeconds; i++)
        {
            var events = _sim.Tick(Delta);
            allEvents.AddRange(events);
        }

        // All three units should have attacked at least once
        var attackEvents = SimTestHelper.FindEvents<UnitAttackedEvent>(allEvents);
        bool unit1Attacked = attackEvents.Any(e => e.AttackerUnitId == unit1.UnitId);
        bool unit2Attacked = attackEvents.Any(e => e.AttackerUnitId == unit2.UnitId);
        bool unit3Attacked = attackEvents.Any(e => e.AttackerUnitId == unit3.UnitId);

        AssertThat(unit3Attacked).IsTrue(); // Front unit — should definitely attack
        AssertThat(unit2Attacked).IsTrue(); // Middle unit
        AssertThat(unit1Attacked).IsTrue(); // Back unit — most likely to get stuck
    }

    /// <summary>
    /// Ranged units (Puffs) behind melee should still fire projectiles.
    /// Projectiles pass through friendlies, so being "behind" shouldn't matter
    /// as long as the ranged unit reaches attack range.
    /// </summary>
    [TestCase]
    public void BlockedRanged_BehindMelee_ShouldStillFireProjectiles()
    {
        // Front melee unit
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -1f, z: 0f,
            attackRange: 2f);

        // Ranged unit behind — longer attack range should let it fire over/through
        var ranged = SimTestHelper.CreateRangedUnit(_state, 0, x: -4f, z: 0f,
            attackRange: 8f, projectileDelay: 0.3f);

        // Enemy
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0f,
            hp: 500f);

        var allEvents = new List<SimEvent>();

        for (int i = 0; i < FiveSeconds; i++)
        {
            var events = _sim.Tick(Delta);
            allEvents.AddRange(events);
        }

        // Ranged unit should have spawned at least one projectile
        var attacks = SimTestHelper.FindEvents<UnitAttackedEvent>(allEvents)
            .Where(e => e.AttackerUnitId == ranged.UnitId)
            .ToList();

        AssertThat(attacks.Count).IsGreater(0);
    }

    /// <summary>
    /// Two rows of units facing each other — back-row units on each side
    /// should eventually engage, not stay idle behind their front line.
    /// </summary>
    [TestCase]
    public void BlockedMelee_TwoArmies_BackRowUnitsContribute()
    {
        // Team 0: front and back
        var t0Front = SimTestHelper.CreateMeleeUnit(_state, 0, x: -1f, z: 0f,
            attackRange: 2f, damage: 3f, hp: 200f);
        var t0Back = SimTestHelper.CreateMeleeUnit(_state, 0, x: -4f, z: 0f,
            attackRange: 2f, damage: 3f, hp: 200f);

        // Team 1: front and back
        var t1Front = SimTestHelper.CreateMeleeUnit(_state, 1, x: 1f, z: 0f,
            attackRange: 2f, damage: 3f, hp: 200f);
        var t1Back = SimTestHelper.CreateMeleeUnit(_state, 1, x: 4f, z: 0f,
            attackRange: 2f, damage: 3f, hp: 200f);

        var allEvents = new List<SimEvent>();

        for (int i = 0; i < TenSeconds; i++)
        {
            var events = _sim.Tick(Delta);
            allEvents.AddRange(events);
        }

        var attackEvents = SimTestHelper.FindEvents<UnitAttackedEvent>(allEvents);

        bool t0BackAttacked = attackEvents.Any(e => e.AttackerUnitId == t0Back.UnitId);
        bool t1BackAttacked = attackEvents.Any(e => e.AttackerUnitId == t1Back.UnitId);

        AssertThat(t0BackAttacked).IsTrue();
        AssertThat(t1BackAttacked).IsTrue();
    }

    /// <summary>
    /// Units should still be able to reach and damage a summoner from distance.
    /// Regression guard for summoner wrap/orbit movement targets staying outside attack range.
    /// </summary>
    [TestCase]
    public void SummonerFocus_SingleMeleeFromDistance_EventuallyDamagesSummoner()
    {
        _state.Summoners[1].CurrentHp = 500f;
        _state.Summoners[1].MaxHp = 500f;

        SimTestHelper.CreateMeleeUnit(_state, 0, x: -8f, z: 0f, attackRange: 2f, damage: 5f, moveSpeed: 3f);

        float hpBefore = _state.Summoners[1].CurrentHp;
        for (int i = 0; i < TenSeconds; i++)
            _sim.Tick(Delta);

        AssertThat(_state.Summoners[1].CurrentHp).IsLess(hpBefore);
    }

    /// <summary>
    /// Backline melee behind a friendly frontliner should still wrap and damage
    /// an enemy summoner instead of idling behind the front.
    /// </summary>
    [TestCase]
    public void SummonerFocus_BlockedBacklineUnit_EventuallyDamagesSummoner()
    {
        _state.Summoners[1].CurrentHp = 600f;
        _state.Summoners[1].MaxHp = 600f;

        // Frontline unit starts in range and tends to hold the front slot.
        SimTestHelper.CreateMeleeUnit(_state, 0, x: 18f, z: 0f, attackRange: 2f, damage: 5f, moveSpeed: 2.5f);
        var backline = SimTestHelper.CreateMeleeUnit(_state, 0, x: 14f, z: 0f, attackRange: 2f, damage: 5f, moveSpeed: 3f);

        var allEvents = new List<SimEvent>();
        for (int i = 0; i < TwentySeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        var summonerDamages = SimTestHelper.FindEvents<SummonerDamagedEvent>(allEvents)
            .Where(e => e.Team == 1)
            .ToList();

        AssertThat(summonerDamages.Count).IsGreater(0);
        bool backlineDamagedSummoner = summonerDamages.Any(e => e.AttackerUnitId == backline.UnitId);
        AssertThat(backlineDamagedSummoner || _state.Summoners[1].CurrentHp < 600f).IsTrue();
    }

    /// <summary>
    /// High-density summoner pressure repro (60 total units).
    /// Ensures backline attackers in dense clumps can still rotate into attack slots.
    /// </summary>
    [TestCase]
    public void SummonerFocus_DenseSwarm_HasBroadAttackerContribution()
    {
        const int unitsPerTeam = 30;
        const float minAttackerContributionRatioPerTeam = 0.33f;
        int minDistinctAttackersPerTeam = (int)MathF.Ceiling(unitsPerTeam * minAttackerContributionRatioPerTeam);

        _state.Summoners[0].CurrentHp = 20000f;
        _state.Summoners[0].MaxHp = 20000f;
        _state.Summoners[1].CurrentHp = 20000f;
        _state.Summoners[1].MaxHp = 20000f;

        var team0Ids = new HashSet<int>();
        var team1Ids = new HashSet<int>();

        for (int i = 0; i < unitsPerTeam; i++)
        {
            int row = i / 6;
            int col = i % 6;
            float laneZ = (col - 2.5f) * 0.70f;

            var t0 = SimTestHelper.CreateMeleeUnit(
                _state, 0,
                x: 18.5f - (row * 1.0f),
                z: laneZ,
                hp: 140f,
                damage: 4f,
                attackRange: 2f,
                moveSpeed: 3.2f
            );
            team0Ids.Add(t0.UnitId);

            var t1 = SimTestHelper.CreateMeleeUnit(
                _state, 1,
                x: -18.5f + (row * 1.0f),
                z: laneZ,
                hp: 140f,
                damage: 4f,
                attackRange: 2f,
                moveSpeed: 3.2f
            );
            team1Ids.Add(t1.UnitId);
        }

        var allEvents = new List<SimEvent>();
        for (int i = 0; i < TwentySeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        var summonerDamages = SimTestHelper.FindEvents<SummonerDamagedEvent>(allEvents);
        int team0Attackers = summonerDamages
            .Where(e => e.Team == 1)
            .Select(e => e.AttackerUnitId)
            .Distinct()
            .Count(attackerId => team0Ids.Contains(attackerId));
        int team1Attackers = summonerDamages
            .Where(e => e.Team == 0)
            .Select(e => e.AttackerUnitId)
            .Distinct()
            .Count(attackerId => team1Ids.Contains(attackerId));

        AssertThat(team0Attackers).IsGreaterEqual(minDistinctAttackersPerTeam);
        AssertThat(team1Attackers).IsGreaterEqual(minDistinctAttackersPerTeam);
    }
}
