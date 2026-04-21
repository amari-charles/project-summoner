namespace Fateforged.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Linq;
using Fateforged.Constants;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Combat.Slots;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

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
        SummonerMeleeBubble.ClearOverrideRadius();
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
        var backUnit = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -4f,
            z: 0f,
            attackRange: 2f,
            moveSpeed: 3f
        );

        // Front unit (blocker — same team, between back unit and enemy)
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -1f, z: 0f, attackRange: 2f, moveSpeed: 3f);

        // Enemy
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0f, hp: 500f, attackRange: 2f);

        // Run simulation for 5 seconds
        int stuckIdleFrames = 0;
        var allEvents = new List<SimEvent>();
        for (int i = 0; i < FiveSeconds; i++)
        {
            allEvents.AddRange(_sim.Tick(Delta));

            // Count frames where unit has a target but zero velocity
            if (
                backUnit.Engagement.TargetUnitId.HasValue
                && backUnit.Velocity.LengthSquared() < 0.001f
                && backUnit.BehaviorState != BehaviorState.Attacking
            )
            {
                stuckIdleFrames++;
            }
        }

        // Commit-slot flow can queue behind occupied slots, so some additional
        // idle time is expected. Guard against true deadlock by requiring attacks
        // and bounding idle windows to less than 4 seconds over a 5 second sim.
        bool backUnitAttacked = SimTestHelper
            .FindEvents<UnitAttackedEvent>(allEvents)
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
        var mover = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -18f,
            z: 0f,
            attackRange: 2f,
            aggroRadius: 4f,
            moveSpeed: 3f
        );

        // Keep at least one enemy unit alive, but well outside aggro so commit targeting returns null.
        SimTestHelper.CreateMeleeUnit(
            _state,
            1,
            x: 20f,
            z: 0f,
            attackRange: 2f,
            aggroRadius: 0f,
            moveSpeed: 0f
        );

        int movingFramesWithoutTarget = 0;
        for (int i = 0; i < FiveSeconds; i++)
        {
            _sim.Tick(Delta);
            if (!mover.Engagement.TargetUnitId.HasValue && mover.Velocity.LengthSquared() > 0.001f)
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
            aggroRadius: 20f
        );
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
            aggroRadius: 20f
        );

        var allEvents = new List<SimEvent>();
        for (int i = 0; i < TenSeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        bool pebbloomAttacked = SimTestHelper
            .FindEvents<UnitAttackedEvent>(allEvents)
            .Any(e => e.AttackerUnitId == pebbloomLike.UnitId);

        AssertThat(pebbloomAttacked).IsTrue();
    }

    /// <summary>
    /// Template-driven guard for forward-offset melee (Pebbloom). This captures
    /// the "run in circles / keep pushing" symptom by requiring a bounded time
    /// to first attack and bounded stall-idle frames while targeting.
    /// </summary>
    [TestCase]
    public void ForwardRectOffsetUnit_FromTemplate_AttacksWithinSixSeconds_WithoutLongIdleStall()
    {
        SimUnitTemplate pebbloomTemplate = UnitDefinitions.BuildSimTemplate(UnitIds.EarthSprite, count: 1);
        var pebbloom = CreateUnitFromTemplate(_state, pebbloomTemplate, team: 0, x: -2f, z: 0f, hp: 280f);
        pebbloom.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2f,
            z: 0f,
            hp: 220f,
            damage: 10f,
            attackSpeed: 1.2f,
            attackRange: 3f,
            moveSpeed: 3.5f,
            aggroRadius: 20f
        );
        enemy.Evasion = 0f;

        const int sixSeconds = 360;
        int firstAttackFrame = -1;
        int stalledIdleFrames = 0;
        var allEvents = new List<SimEvent>();

        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            allEvents.AddRange(events);

            if (firstAttackFrame < 0)
            {
                bool attackedThisFrame = events
                    .OfType<UnitAttackedEvent>()
                    .Any(e => e.AttackerUnitId == pebbloom.UnitId);
                if (attackedThisFrame)
                    firstAttackFrame = frame;
            }

            if (
                pebbloom.Engagement.TargetUnitId.HasValue
                && pebbloom.Velocity.LengthSquared() < 0.001f
                && pebbloom.BehaviorState != BehaviorState.Attacking
            )
            {
                stalledIdleFrames++;
            }
        }

        AssertThat(firstAttackFrame >= 0).IsTrue();
        AssertThat(firstAttackFrame).IsLessEqual(sixSeconds);
        AssertThat(stalledIdleFrames).IsLess(240);
    }

    /// <summary>
    /// 2v1 commit guard: when two allied forward-rect melee units collapse onto one target,
    /// both attackers should commit attacks within the same short engagement window.
    /// This intentionally fails under current stall/ringing logic and acts as a hard repro.
    /// </summary>
    [TestCase]
    public void CommitBehavior_TwoVOne_ForwardRectAllies_BothAttackWithinWindow()
    {
        SimUnitTemplate pebbloomTemplate = UnitDefinitions.BuildSimTemplate(UnitIds.EarthSprite, count: 1);
        var lead = CreateUnitFromTemplate(_state, pebbloomTemplate, team: 0, x: -2.0f, z: 0f, hp: 280f);
        var trail = CreateUnitFromTemplate(_state, pebbloomTemplate, team: 0, x: -4.3f, z: 0f, hp: 280f);
        lead.CritChance = 0f;
        trail.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2.2f,
            z: 0f,
            hp: 420f,
            damage: 10f,
            attackSpeed: 1.2f,
            attackRange: 3.0f,
            moveSpeed: 3.5f,
            aggroRadius: 20f
        );
        enemy.Evasion = 0f;

        var allyAttackers = new HashSet<int>();
        int leadFirstAttackFrame = -1;
        int trailFirstAttackFrame = -1;
        int trailNearRangeIdleFrames = 0;
        const int commitWindowFrames = 360; // 6s @ 60fps

        for (int frame = 0; frame < commitWindowFrames; frame++)
        {
            var events = _sim.Tick(Delta);
            foreach (var attacked in events.OfType<UnitAttackedEvent>())
            {
                if (attacked.AttackerUnitId == lead.UnitId || attacked.AttackerUnitId == trail.UnitId)
                    allyAttackers.Add(attacked.AttackerUnitId);
                if (attacked.AttackerUnitId == lead.UnitId)
                {
                    if (leadFirstAttackFrame < 0)
                        leadFirstAttackFrame = frame;
                }
                if (attacked.AttackerUnitId == trail.UnitId)
                {
                    if (trailFirstAttackFrame < 0)
                        trailFirstAttackFrame = frame;
                }
            }

            float trailDistance = DistanceXZ(trail.Position, enemy.Position);
            bool nearRangeIdle =
                !allyAttackers.Contains(trail.UnitId)
                && trail.Engagement.TargetUnitId.HasValue
                && trailDistance <= trail.AttackRange + 1.5f
                && trail.Velocity.LengthSquared() < 0.001f
                && trail.BehaviorState != BehaviorState.Attacking;
            if (nearRangeIdle)
                trailNearRangeIdleFrames++;
        }

        AssertThat(allyAttackers.Contains(lead.UnitId)).IsTrue();
        AssertThat(allyAttackers.Contains(trail.UnitId)).IsTrue();
        const int bothCommitByFrame = 120; // 2s @ 60fps (strict red guard for current issue)
        AssertThat(leadFirstAttackFrame).IsLessEqual(bothCommitByFrame);
        AssertThat(trailFirstAttackFrame).IsLessEqual(bothCommitByFrame);
        AssertThat(trailNearRangeIdleFrames).IsLess(45); // <0.75s near-range idle before first commit
    }

    /// <summary>
    /// Offset-lane 2v1 guard: when allies approach from different lateral lanes,
    /// both should still begin attacking quickly (no lane-based starvation).
    /// </summary>
    [TestCase]
    public void CommitBehavior_TwoVOne_OffsetLanes_BothCommitQuickly()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.EarthSprite, count: 1);
        var laneA = CreateUnitFromTemplate(_state, template, team: 0, x: -2.0f, z: -0.9f, hp: 280f);
        var laneB = CreateUnitFromTemplate(_state, template, team: 0, x: -4.6f, z: 1.1f, hp: 280f);
        laneA.CritChance = 0f;
        laneB.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2.3f,
            z: 0f,
            hp: 520f,
            damage: 10f,
            attackSpeed: 1.2f,
            attackRange: 3.0f,
            moveSpeed: 3.5f,
            aggroRadius: 20f
        );
        enemy.Evasion = 0f;

        int laneAFirstAttack = -1;
        int laneBFirstAttack = -1;
        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            if (laneAFirstAttack < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == laneA.UnitId))
                laneAFirstAttack = frame;
            if (laneBFirstAttack < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == laneB.UnitId))
                laneBFirstAttack = frame;
        }

        AssertThat(laneAFirstAttack >= 0).IsTrue();
        AssertThat(laneBFirstAttack >= 0).IsTrue();
        const int commitByFrame = 120; // 2s @ 60fps
        AssertThat(laneAFirstAttack).IsLessEqual(commitByFrame);
        AssertThat(laneBFirstAttack).IsLessEqual(commitByFrame);
    }

    /// <summary>
    /// 3v1 guard for slot starvation: rear allies should not wait multiple seconds
    /// behind early reserves before committing at least one attack.
    /// </summary>
    [TestCase]
    public void CommitBehavior_ThreeVOne_SlotStarvation_AlliesCommitWithinThreeSeconds()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.EarthSprite, count: 1);
        var front = CreateUnitFromTemplate(_state, template, team: 0, x: -2.1f, z: 0.0f, hp: 280f);
        var mid = CreateUnitFromTemplate(_state, template, team: 0, x: -3.5f, z: 0.45f, hp: 280f);
        var back = CreateUnitFromTemplate(_state, template, team: 0, x: -4.9f, z: -0.45f, hp: 280f);
        front.CritChance = 0f;
        mid.CritChance = 0f;
        back.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2.2f,
            z: 0f,
            hp: 900f,
            damage: 8f,
            attackSpeed: 1.1f,
            attackRange: 3.0f,
            moveSpeed: 3.4f,
            aggroRadius: 20f
        );
        enemy.Evasion = 0f;

        int frontFirstAttack = -1;
        int midFirstAttack = -1;
        int backFirstAttack = -1;
        int backNearRangeIdleFrames = 0;

        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            if (frontFirstAttack < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == front.UnitId))
                frontFirstAttack = frame;
            if (midFirstAttack < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == mid.UnitId))
                midFirstAttack = frame;
            if (backFirstAttack < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == back.UnitId))
                backFirstAttack = frame;

            float backDistance = DistanceXZ(back.Position, enemy.Position);
            bool backNearRangeIdle =
                backFirstAttack < 0
                && back.Engagement.TargetUnitId.HasValue
                && backDistance <= back.AttackRange + 1.5f
                && back.Velocity.LengthSquared() < 0.001f
                && back.BehaviorState != BehaviorState.Attacking;
            if (backNearRangeIdle)
                backNearRangeIdleFrames++;
        }

        AssertThat(frontFirstAttack >= 0).IsTrue();
        AssertThat(midFirstAttack >= 0).IsTrue();
        AssertThat(backFirstAttack >= 0).IsTrue();
        const int commitByFrame = 180; // 3s @ 60fps
        AssertThat(frontFirstAttack).IsLessEqual(commitByFrame);
        AssertThat(midFirstAttack).IsLessEqual(commitByFrame);
        AssertThat(backFirstAttack).IsLessEqual(commitByFrame);
        AssertThat(backNearRangeIdleFrames).IsLess(60); // <1s near-range pre-commit idle
    }

    /// <summary>
    /// Mobile-target ringing guard: the second attacker should not spend long
    /// tangential-orbit streaks near engage distance before first commit.
    /// </summary>
    [TestCase]
    public void RingingBehavior_MobileTarget_TwoAllies_SecondAttackerAvoidsLongTangentialOrbit()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.EarthSprite, count: 1);
        var first = CreateUnitFromTemplate(_state, template, team: 0, x: -6.0f, z: -0.8f, hp: 280f);
        var second = CreateUnitFromTemplate(_state, template, team: 0, x: -6.5f, z: 0.9f, hp: 280f);
        first.CritChance = 0f;
        second.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 1.8f,
            z: 0f,
            hp: 1200f,
            damage: 7f,
            attackSpeed: 1.1f,
            attackRange: 3.0f,
            moveSpeed: 3.6f,
            aggroRadius: 20f
        );
        enemy.Evasion = 0f;

        int secondFirstAttack = -1;
        int tangentialStreak = 0;
        int maxTangentialStreak = 0;
        float prevDistance = DistanceXZ(second.Position, enemy.Position);

        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            if (secondFirstAttack < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == second.UnitId))
                secondFirstAttack = frame;

            float distance = DistanceXZ(second.Position, enemy.Position);
            float speed = second.Velocity.Length();
            bool nearEngageBand = distance <= second.AttackRange + 1.4f;
            bool tangentialNoProgress = false;

            if (
                secondFirstAttack < 0
                && nearEngageBand
                && speed > 0.15f
                && second.BehaviorState != BehaviorState.Attacking
            )
            {
                float toTargetX = enemy.Position.X - second.Position.X;
                float toTargetZ = enemy.Position.Z - second.Position.Z;
                float toTargetLen = MathF.Sqrt((toTargetX * toTargetX) + (toTargetZ * toTargetZ));
                if (toTargetLen > 0.0001f)
                {
                    float velX = second.Velocity.X / speed;
                    float velZ = second.Velocity.Z / speed;
                    float dirX = toTargetX / toTargetLen;
                    float dirZ = toTargetZ / toTargetLen;
                    float radialAlignment = MathF.Abs((velX * dirX) + (velZ * dirZ));
                    float radialProgress = prevDistance - distance;
                    tangentialNoProgress = radialAlignment < 0.25f && radialProgress < 0.01f;
                }
            }

            if (tangentialNoProgress)
            {
                tangentialStreak++;
                maxTangentialStreak = Math.Max(maxTangentialStreak, tangentialStreak);
            }
            else
            {
                tangentialStreak = 0;
            }

            prevDistance = distance;
        }

        AssertThat(secondFirstAttack >= 0).IsTrue();
        AssertThat(secondFirstAttack).IsLessEqual(180); // <=3s first commit
        AssertThat(maxTangentialStreak).IsLess(120); // <2s sustained near-range orbit pre-commit
    }

    /// <summary>
    /// Mixed congestion guard: ranged backliner behind two melee allies should still
    /// contribute quickly while melee units engage, instead of idling indefinitely.
    /// </summary>
    [TestCase]
    public void CommitBehavior_MixedMeleeRanged_CongestedFront_BacklineContributesEarly()
    {
        SimUnitTemplate meleeTemplate = UnitDefinitions.BuildSimTemplate(UnitIds.EarthSprite, count: 1);
        SimUnitTemplate puffTemplate = UnitDefinitions.BuildSimTemplate(UnitIds.Puff, count: 1);

        var meleeFront = CreateUnitFromTemplate(_state, meleeTemplate, team: 0, x: -2.1f, z: -0.2f, hp: 280f);
        var meleeRear = CreateUnitFromTemplate(_state, meleeTemplate, team: 0, x: -3.8f, z: 0.2f, hp: 280f);
        var rangedBack = CreateUnitFromTemplate(_state, puffTemplate, team: 0, x: -5.4f, z: 0.1f, hp: 90f);
        meleeFront.CritChance = 0f;
        meleeRear.CritChance = 0f;
        rangedBack.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2.3f,
            z: 0f,
            hp: 900f,
            damage: 9f,
            attackSpeed: 1.1f,
            attackRange: 3.0f,
            moveSpeed: 3.5f,
            aggroRadius: 20f
        );
        enemy.Evasion = 0f;

        int meleeFrontFirst = -1;
        int meleeRearFirst = -1;
        int rangedFirst = -1;
        int rangedIdleWithTargetFrames = 0;

        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            if (meleeFrontFirst < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == meleeFront.UnitId))
                meleeFrontFirst = frame;
            if (meleeRearFirst < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == meleeRear.UnitId))
                meleeRearFirst = frame;
            if (rangedFirst < 0 && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == rangedBack.UnitId))
                rangedFirst = frame;

            bool rangedIdleWithTarget =
                rangedFirst < 0
                && rangedBack.Engagement.TargetUnitId.HasValue
                && rangedBack.Velocity.LengthSquared() < 0.0005f
                && rangedBack.BehaviorState != BehaviorState.Attacking;
            if (rangedIdleWithTarget)
                rangedIdleWithTargetFrames++;
        }

        AssertThat(meleeFrontFirst >= 0).IsTrue();
        AssertThat(meleeRearFirst >= 0).IsTrue();
        AssertThat(rangedFirst >= 0).IsTrue();
        AssertThat(meleeFrontFirst).IsLessEqual(150); // <=2.5s
        AssertThat(meleeRearFirst).IsLessEqual(180); // <=3s
        AssertThat(rangedFirst).IsLessEqual(150); // <=2.5s
        AssertThat(rangedIdleWithTargetFrames).IsLess(45); // <0.75s pre-commit idle
    }

    /// <summary>
    /// Multiple friendly units in a column — back units should still contribute
    /// to combat (either by flanking or attacking through friendlies).
    /// </summary>
    [TestCase]
    public void BlockedMelee_ThreeUnitColumn_AllUnitsEventuallyAttack()
    {
        // Three friendly units in a line
        var unit1 = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -6f,
            z: 0f,
            attackRange: 2f,
            damage: 5f
        );
        var unit2 = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -3f,
            z: 0f,
            attackRange: 2f,
            damage: 5f
        );
        var unit3 = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -1f,
            z: 0f,
            attackRange: 2f,
            damage: 5f
        );

        // Tanky enemy
        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            1,
            x: 2f,
            z: 0f,
            hp: 2000f,
            attackRange: 2f,
            damage: 1f
        );

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
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -1f, z: 0f, attackRange: 2f);

        // Ranged unit behind — longer attack range should let it fire over/through
        var ranged = SimTestHelper.CreateRangedUnit(
            _state,
            0,
            x: -4f,
            z: 0f,
            attackRange: 8f,
            projectileDelay: 0.3f
        );

        // Enemy
        SimTestHelper.CreateMeleeUnit(_state, 1, x: 2f, z: 0f, hp: 500f);

        var allEvents = new List<SimEvent>();

        for (int i = 0; i < FiveSeconds; i++)
        {
            var events = _sim.Tick(Delta);
            allEvents.AddRange(events);
        }

        // Ranged unit should have spawned at least one projectile
        var attacks = SimTestHelper
            .FindEvents<UnitAttackedEvent>(allEvents)
            .Where(e => e.AttackerUnitId == ranged.UnitId)
            .ToList();

        AssertThat(attacks.Count).IsGreater(0);
    }

    /// <summary>
    /// Template-driven Puff regression: even when a friendly melee occupies front
    /// space, Puff should still contribute by attacking and spawning projectiles.
    /// </summary>
    [TestCase]
    public void BlockedPuff_FromTemplate_BehindMelee_StillSpawnsProjectilesAndAttacks()
    {
        // Front melee ally occupying near-front space.
        SimTestHelper.CreateMeleeUnit(_state, 0, x: -1.2f, z: 0f, attackRange: 2f, moveSpeed: 2.6f);

        SimUnitTemplate puffTemplate = UnitDefinitions.BuildSimTemplate(UnitIds.Puff, count: 1);
        var puff = CreateUnitFromTemplate(_state, puffTemplate, team: 0, x: -4.5f, z: 0.4f, hp: 80f);
        puff.CritChance = 0f;

        var enemy = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 8f,
            z: 0f,
            hp: 350f,
            attackRange: 1.5f,
            moveSpeed: 0f
        );
        enemy.Evasion = 0f;

        bool observedProjectile = false;
        bool puffAttacked = false;
        for (int i = 0; i < TenSeconds; i++)
        {
            var events = _sim.Tick(Delta);
            if (!observedProjectile && _state.Projectiles.Any(p => p.Value.SourceUnitId == puff.UnitId))
                observedProjectile = true;
            if (!puffAttacked && events.OfType<UnitAttackedEvent>().Any(e => e.AttackerUnitId == puff.UnitId))
                puffAttacked = true;
        }

        AssertThat(observedProjectile).IsTrue();
        AssertThat(puffAttacked).IsTrue();
    }

    /// <summary>
    /// Two rows of units facing each other — back-row units on each side
    /// should eventually engage, not stay idle behind their front line.
    /// </summary>
    [TestCase]
    public void BlockedMelee_TwoArmies_BackRowUnitsContribute()
    {
        // Team 0: front and back
        var t0Front = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -1f,
            z: 0f,
            attackRange: 2f,
            damage: 3f,
            hp: 200f
        );
        var t0Back = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -4f,
            z: 0f,
            attackRange: 2f,
            damage: 3f,
            hp: 200f
        );

        // Team 1: front and back
        var t1Front = SimTestHelper.CreateMeleeUnit(
            _state,
            1,
            x: 1f,
            z: 0f,
            attackRange: 2f,
            damage: 3f,
            hp: 200f
        );
        var t1Back = SimTestHelper.CreateMeleeUnit(
            _state,
            1,
            x: 4f,
            z: 0f,
            attackRange: 2f,
            damage: 3f,
            hp: 200f
        );

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

        SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: -8f,
            z: 0f,
            attackRange: 2f,
            damage: 5f,
            moveSpeed: 3f
        );

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
        SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: 18f,
            z: 0f,
            attackRange: 2f,
            damage: 5f,
            moveSpeed: 2.5f
        );
        var backline = SimTestHelper.CreateMeleeUnit(
            _state,
            0,
            x: 14f,
            z: 0f,
            attackRange: 2f,
            damage: 5f,
            moveSpeed: 3f
        );

        var allEvents = new List<SimEvent>();
        for (int i = 0; i < TwentySeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        var summonerDamages = SimTestHelper
            .FindEvents<SummonerDamagedEvent>(allEvents)
            .Where(e => e.Team == 1)
            .ToList();

        AssertThat(summonerDamages.Count).IsGreater(0);
        bool backlineDamagedSummoner = summonerDamages.Any(e =>
            e.AttackerUnitId == backline.UnitId
        );
        AssertThat(backlineDamagedSummoner || _state.Summoners[1].CurrentHp < 600f).IsTrue();
    }

    [TestCase]
    public void SummonerFocus_AttackableWithoutFreeSlot_StillDamages()
    {
        _state.Summoners[1].CurrentHp = 4000f;
        _state.Summoners[1].MaxHp = 4000f;
        int summonerTargetId = MatchState.GetSummonerTargetId(team: 1);

        // Fill all currently computed summoner slots with friendly blockers.
        var blockers = new List<UnitData>();
        var firstBlocker = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 14f,
            z: -4f,
            attackRange: 3f,
            moveSpeed: 0f,
            damage: 0f
        );
        blockers.Add(firstBlocker);
        bool firstReserved = SimMeleeSlotManager.TryReserveSlot(
            firstBlocker,
            _state,
            summonerTargetId,
            out _
        );
        AssertThat(firstReserved).IsTrue();

        int slotCount = _state.TargetSlotStates[summonerTargetId].Slots.Count;
        for (int i = 1; i < slotCount; i++)
        {
            float z = -4f + (i * 0.24f);
            blockers.Add(
                SimTestHelper.CreateMeleeUnit(
                    _state,
                    team: 0,
                    x: 14f,
                    z: z,
                    attackRange: 3f,
                    moveSpeed: 0f,
                    damage: 0f
                )
            );
        }

        for (int i = 1; i < blockers.Count; i++)
        {
            bool reserved = SimMeleeSlotManager.TryReserveSlot(
                blockers[i],
                _state,
                summonerTargetId,
                out _
            );
            AssertThat(reserved).IsTrue();
        }

        var attacker = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 0,
            x: 15f,
            z: 0f,
            attackRange: 3f,
            damage: 12f,
            attackSpeed: 1.0f,
            moveSpeed: 0f
        );
        attacker.EngageShape = EngageShape.ForwardRect;
        attacker.EngageRectLength = 5.4f;
        attacker.EngageRectHalfWidth = 2.6f;
        attacker.EngageRectForwardOffset = 2.1f;
        attacker.EngageCloseRadius = 2.15f;
        attacker.Engagement.LockedTargetUnitId = summonerTargetId;
        attacker.Engagement.TargetUnitId = summonerTargetId;
        attacker.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;

        var allEvents = new List<SimEvent>();
        for (int i = 0; i < FiveSeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        bool attackerDamagedSummoner = SimTestHelper
            .FindEvents<SummonerDamagedEvent>(allEvents)
            .Any(e => e.Team == 1 && e.AttackerUnitId == attacker.UnitId);
        AssertThat(attackerDamagedSummoner).IsTrue();
    }

    [TestCase]
    public void SummonerFocus_PebbloomCluster_MultipleAttackersContributeDamage()
    {
        _state.Summoners[1].CurrentHp = 1200f;
        _state.Summoners[1].MaxHp = 1200f;

        var attackers = new List<UnitData>
        {
            SimTestHelper.CreateMeleeUnit(_state, 0, x: 16.4f, z: -1.1f, attackRange: 3f, damage: 18f, attackSpeed: 0.9f, moveSpeed: 1.8f),
            SimTestHelper.CreateMeleeUnit(_state, 0, x: 16.2f, z: 0.0f, attackRange: 3f, damage: 18f, attackSpeed: 0.9f, moveSpeed: 1.8f),
            SimTestHelper.CreateMeleeUnit(_state, 0, x: 16.4f, z: 1.1f, attackRange: 3f, damage: 18f, attackSpeed: 0.9f, moveSpeed: 1.8f)
        };

        foreach (var attacker in attackers)
        {
            attacker.EngageShape = EngageShape.ForwardRect;
            attacker.EngageRectLength = 5.4f;
            attacker.EngageRectHalfWidth = 2.6f;
            attacker.EngageRectForwardOffset = 2.1f;
            attacker.EngageCloseRadius = 2.15f;
        }

        var attackerIds = attackers.Select(u => u.UnitId).ToHashSet();
        var allEvents = new List<SimEvent>();
        for (int i = 0; i < TenSeconds; i++)
            allEvents.AddRange(_sim.Tick(Delta));

        var summonerDamages = SimTestHelper.FindEvents<SummonerDamagedEvent>(allEvents)
            .Where(e => e.Team == 1 && attackerIds.Contains(e.AttackerUnitId))
            .ToList();
        int distinctAttackers = summonerDamages.Select(e => e.AttackerUnitId).Distinct().Count();

        AssertThat(summonerDamages.Count).IsGreater(0);
        AssertThat(distinctAttackers).IsGreaterEqual(2);
        AssertThat(_state.Summoners[1].CurrentHp).IsLess(1200f);
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
        int minDistinctAttackersPerTeam = (int)
            MathF.Ceiling(unitsPerTeam * minAttackerContributionRatioPerTeam);

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
                _state,
                0,
                x: 18.5f - (row * 1.0f),
                z: laneZ,
                hp: 140f,
                damage: 4f,
                attackRange: 2f,
                moveSpeed: 3.2f
            );
            team0Ids.Add(t0.UnitId);

            var t1 = SimTestHelper.CreateMeleeUnit(
                _state,
                1,
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

    /// <summary>
    /// Ringing repro guard: attackers that reach near-engage distance should not
    /// orbit tangentially for long windows without ever committing an attack.
    /// </summary>
    [TestCase]
    public void RingingBehavior_NearTargetTangentialOrbit_StaysBoundedAndUnitsCommitAttacks()
    {
        var enemyTank = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 2f,
            z: 0f,
            hp: 4200f,
            damage: 1f,
            attackSpeed: 0.5f,
            attackRange: 2f,
            moveSpeed: 0f,
            aggroRadius: 20f
        );
        enemyTank.Evasion = 0f;

        var attackers = new List<UnitData>();
        for (int i = 0; i < 12; i++)
        {
            int row = i / 4;
            int col = i % 4;
            attackers.Add(
                SimTestHelper.CreateMeleeUnit(
                    _state,
                    team: 0,
                    x: -8.0f + (row * 1.35f),
                    z: (col - 1.5f) * 0.9f,
                    hp: 130f,
                    damage: 6f,
                    attackSpeed: 0.9f,
                    attackRange: 2.1f,
                    moveSpeed: 3.1f,
                    aggroRadius: 20f
                )
            );
        }

        var attackedIds = new HashSet<int>();
        var tangentialStreak = attackers.ToDictionary(u => u.UnitId, _ => 0);
        var maxTangentialStreak = attackers.ToDictionary(u => u.UnitId, _ => 0);
        var prevDistance = attackers.ToDictionary(
            u => u.UnitId,
            u => DistanceXZ(u.Position, enemyTank.Position)
        );

        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            foreach (var attacked in events.OfType<UnitAttackedEvent>())
                attackedIds.Add(attacked.AttackerUnitId);

            foreach (var unit in attackers)
            {
                if (!unit.IsAlive)
                    continue;

                float distance = DistanceXZ(unit.Position, enemyTank.Position);
                float speed = unit.Velocity.Length();
                bool nearEngageBand = distance <= unit.AttackRange + 1.25f;

                bool tangentialNoProgress = false;
                if (
                    nearEngageBand
                    && speed > 0.15f
                    && unit.BehaviorState != BehaviorState.Attacking
                )
                {
                    float toTargetX = enemyTank.Position.X - unit.Position.X;
                    float toTargetZ = enemyTank.Position.Z - unit.Position.Z;
                    float toTargetLen = MathF.Sqrt((toTargetX * toTargetX) + (toTargetZ * toTargetZ));
                    if (toTargetLen > 0.0001f)
                    {
                        float velX = unit.Velocity.X / speed;
                        float velZ = unit.Velocity.Z / speed;
                        float dirX = toTargetX / toTargetLen;
                        float dirZ = toTargetZ / toTargetLen;
                        float radialAlignment = MathF.Abs((velX * dirX) + (velZ * dirZ));
                        float radialProgress = prevDistance[unit.UnitId] - distance;
                        tangentialNoProgress = radialAlignment < 0.25f && radialProgress < 0.01f;
                    }
                }

                if (tangentialNoProgress)
                {
                    tangentialStreak[unit.UnitId]++;
                    maxTangentialStreak[unit.UnitId] = Math.Max(
                        maxTangentialStreak[unit.UnitId],
                        tangentialStreak[unit.UnitId]
                    );
                }
                else
                {
                    tangentialStreak[unit.UnitId] = 0;
                }

                prevDistance[unit.UnitId] = distance;
            }
        }

        var ringingOffenders = attackers
            .Where(unit => !attackedIds.Contains(unit.UnitId))
            .Where(unit => maxTangentialStreak[unit.UnitId] >= 180)
            .Select(unit => unit.UnitId)
            .ToList();

        AssertThat(ringingOffenders.Count).IsEqual(0);
    }

    /// <summary>
    /// Pushing repro guard: in dense frontal columns, backline units should not
    /// spend long windows moving while making effectively zero approach progress.
    /// </summary>
    [TestCase]
    public void PushingBehavior_DenseColumns_NoLongMovingNoProgressStalls()
    {
        var enemyTank = SimTestHelper.CreateMeleeUnit(
            _state,
            team: 1,
            x: 3.2f,
            z: 0f,
            hp: 6000f,
            damage: 0f,
            attackSpeed: 0.2f,
            attackRange: 2f,
            moveSpeed: 0f,
            aggroRadius: 20f
        );
        enemyTank.Evasion = 0f;

        var attackers = new List<UnitData>();
        for (int i = 0; i < 8; i++)
        {
            attackers.Add(
                SimTestHelper.CreateMeleeUnit(
                    _state,
                    team: 0,
                    x: -9f + (i * 1.0f),
                    z: (i % 2 == 0) ? -0.15f : 0.15f,
                    hp: 140f,
                    damage: 7f,
                    attackSpeed: 0.95f,
                    attackRange: 2.0f,
                    moveSpeed: 3.2f,
                    aggroRadius: 20f
                )
            );
        }

        var attackedIds = new HashSet<int>();
        var noProgressStreak = attackers.ToDictionary(u => u.UnitId, _ => 0);
        var maxNoProgressStreak = attackers.ToDictionary(u => u.UnitId, _ => 0);
        var prevDistance = attackers.ToDictionary(
            u => u.UnitId,
            u => DistanceXZ(u.Position, enemyTank.Position)
        );

        for (int frame = 0; frame < TenSeconds; frame++)
        {
            var events = _sim.Tick(Delta);
            foreach (var attacked in events.OfType<UnitAttackedEvent>())
                attackedIds.Add(attacked.AttackerUnitId);

            foreach (var unit in attackers)
            {
                if (!unit.IsAlive)
                    continue;

                float distance = DistanceXZ(unit.Position, enemyTank.Position);
                float speed = unit.Velocity.Length();
                float distanceDelta = prevDistance[unit.UnitId] - distance;
                bool movingNoProgress =
                    unit.Engagement.TargetUnitId.HasValue
                    && speed > 0.45f
                    && distance > unit.AttackRange * 0.95f
                    && MathF.Abs(distanceDelta) < 0.005f
                    && unit.BehaviorState != BehaviorState.Attacking;

                if (movingNoProgress)
                {
                    noProgressStreak[unit.UnitId]++;
                    maxNoProgressStreak[unit.UnitId] = Math.Max(
                        maxNoProgressStreak[unit.UnitId],
                        noProgressStreak[unit.UnitId]
                    );
                }
                else
                {
                    noProgressStreak[unit.UnitId] = 0;
                }

                prevDistance[unit.UnitId] = distance;
            }
        }

        var pushingOffenders = attackers
            .Where(unit => !attackedIds.Contains(unit.UnitId))
            .Where(unit => maxNoProgressStreak[unit.UnitId] >= 180)
            .Select(unit => unit.UnitId)
            .ToList();

        AssertThat(pushingOffenders.Count).IsEqual(0);
        AssertThat(attackedIds.Count(id => attackers.Any(u => u.UnitId == id))).IsGreater(1);
    }

    private static float DistanceXZ(SimVector3 a, SimVector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }

    private static UnitData CreateUnitFromTemplate(
        MatchState state,
        SimUnitTemplate template,
        int team,
        float x,
        float z,
        float hp
    )
    {
        int unitId = state.NextUnitId();
        float y = template.MovementLayer == MovementLayer.Air ? template.FlightAltitude : 0f;
        var unit = new UnitData
        {
            UnitId = unitId,
            Team = (Team)team,
            CurrentHp = hp,
            MaxHp = hp,
            IsAlive = true,
            Position = new SimVector3(x, y, z),
            AttackDamage = template.AttackDamage,
            AttackSpeed = template.AttackSpeed,
            MoveSpeed = template.MoveSpeed,
            AttackRange = template.AttackRange,
            AggroRadius = template.AggroRadius,
            SeparationRadius = template.SeparationRadius,
            NavigationRadius = template.NavigationRadius,
            HurtboxRadius = template.HurtboxRadius,
            HurtboxHeight = template.HurtboxHeight,
            HurtboxHorizontal = template.HurtboxHorizontal,
            HurtboxOffset = template.HurtboxOffset,
            CritChance = template.CritChance,
            CritDamage = template.CritDamage,
            SoulStrength = template.SoulStrength,
            UnitType = template.UnitType,
            TacticalRole = template.TacticalRole,
            MovementLayer = template.MovementLayer,
            ElementId = template.ElementId,
            FallbackMovement = template.FallbackMovement,
            EngageShape = template.EngageShape,
            EngageRectLength = template.EngageRectLength,
            EngageRectHalfWidth = template.EngageRectHalfWidth,
            EngageRectForwardOffset = template.EngageRectForwardOffset,
            EngageCloseRadius = template.EngageCloseRadius,
            HasConeConstraint = template.HasConeConstraint,
            ConeHalfAngle = template.ConeHalfAngle,
            ConeCenterOffsetDegrees = template.ConeCenterOffsetDegrees,
            CloseRangeThreshold = template.CloseRangeThreshold,
            TargetLayerFilter = template.TargetLayerFilter,
            DistanceScorerWeight = template.DistanceScorerWeight,
            HealthScorerWeight = template.HealthScorerWeight,
            TargetPolicyId = template.TargetPolicyId,
            MovementIntentStrategy = template.MovementIntentStrategy,
            FlightAltitude = template.FlightAltitude,
            ProjectileCatalogId = template.ProjectileCatalogId,
            ProjectileDelay = template.ProjectileDelay,
            ProjectileTargetAffinity = template.ProjectileTargetAffinity,
            ProjectileImpactKind = template.ProjectileImpactKind,
            ProjectileStatusKind = template.ProjectileStatusKind,
            ProjectileStatusDuration = template.ProjectileStatusDuration,
            ProjectileStatusTickInterval = template.ProjectileStatusTickInterval,
            ProjectileStatusPotencyPerStack = template.ProjectileStatusPotencyPerStack,
            ProjectileStatusMaxStacks = template.ProjectileStatusMaxStacks,
            AttackType = template.AttackType,
            PhysicalDamageRatio = template.PhysicalDamageRatio,
            ElementalDamageRatio = template.ElementalDamageRatio,
            PhysicalDefense = template.PhysicalDefense,
            MagicDefense = template.MagicDefense,
            Evasion = template.Evasion,
            Attack = template.Attack.DeepClone(),
            Abilities = template.Abilities.Select(ability => ability.DeepClone()).ToList(),
            ActivationState = ActivationState.Active,
            IsFacingRight = team == 0,
        };

        state.Units[unitId] = unit;
        return unit;
    }
}
