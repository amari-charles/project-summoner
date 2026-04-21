namespace Fateforged.Tests.Simulation;

using System;
using System.Collections.Generic;
using System.Globalization;
using Fateforged.Simulation;
using Fateforged.Simulation.Combat;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class MeleeClumpingStabilityTest
{
    private const float Delta = 1f / 60f;
    private const int TenSecondsFrames = 600;
    private const int CheckpointIntervalFrames = 120;
    private const float HighVelocityThreshold = 1.25f;
    private const float ScenarioEngageRectHalfWidth = 0.9f;
    private const float ScenarioNavigationRadius = 0.5f;

    [TestCase]
    public void CommitSlotFlow_ReducesChurn_InDenseClump()
    {
        var result = RunScenario(seed: 20260312u, useCommitSlotMode: true);
        float recoveryRatio =
            result.EngagedFrames > 0 ? result.RecoveryFrames / (float)result.EngagedFrames : 1f;
        int totalHits = result.AttackerOneAttackCount + result.AttackerTwoAttackCount;

        AssertThat(totalHits).IsGreater(0);
        AssertThat(result.EngagedFrames).IsGreater(300);
        AssertThat(recoveryRatio).IsLess(0.20f);
        AssertThat(result.TargetSwitchCount).IsEqual(0);
        AssertThat(result.BlockedTimeoutRetargetCount).IsLessEqual(1);
    }

    [TestCase]
    public void PebloomRepro_TwoMeleeAttackers_StableClumpAndContributesDamage()
    {
        var result = RunScenario(seed: 20260311u);
        float configuredBudget = MathF.Max(
            0.20f,
            MathF.Min(0.75f * ScenarioEngageRectHalfWidth, 1.10f)
        );
        float spreadUpperBound = configuredBudget + (ScenarioNavigationRadius * 1.5f);
        float recoveryRatio =
            result.EngagedFrames > 0 ? result.RecoveryFrames / (float)result.EngagedFrames : 1f;

        int totalHits = result.AttackerOneAttackCount + result.AttackerTwoAttackCount;
        AssertThat(totalHits).IsGreater(0);
        AssertThat(result.EngagedFrames).IsGreater(90);
        AssertThat(result.AverageLateralSpread).IsLessEqual(spreadUpperBound);
        AssertThat(recoveryRatio).IsLess(0.15f);
    }

    [TestCase]
    public void PebloomRepro_FixedSeedScenario_IsDeterministicAtCheckpoints()
    {
        var runOne = RunScenario(seed: 20260311u);
        var runTwo = RunScenario(seed: 20260311u);

        AssertThat(runOne.AttackerOneAttackCount).IsEqual(runTwo.AttackerOneAttackCount);
        AssertThat(runOne.AttackerTwoAttackCount).IsEqual(runTwo.AttackerTwoAttackCount);
        AssertThat(runOne.Checkpoints.Count).IsEqual(runTwo.Checkpoints.Count);

        for (int i = 0; i < runOne.Checkpoints.Count; i++)
            AssertThat(runOne.Checkpoints[i]).IsEqual(runTwo.Checkpoints[i]);
    }

    private static ScenarioResult RunScenario(uint seed, bool useCommitSlotMode = false)
    {
        var state = SimTestHelper.CreateBattleState(seed);
        var sim = new Fateforged.Simulation.Simulation(state);

        var target = SimTestHelper.CreateMeleeUnit(
            state,
            team: 1,
            x: 2.25f,
            z: 0f,
            hp: 2000f,
            damage: 0f,
            attackSpeed: 0f,
            attackRange: 1.5f,
            moveSpeed: 0f,
            aggroRadius: 1f
        );

        var attackerOne = SimTestHelper.CreateMeleeUnit(
            state,
            team: 0,
            x: -2.75f,
            z: -0.35f,
            hp: 220f,
            damage: 5f,
            attackSpeed: 1.15f,
            attackRange: 2.1f,
            moveSpeed: 2.9f,
            aggroRadius: 20f
        );
        var attackerTwo = SimTestHelper.CreateMeleeUnit(
            state,
            team: 0,
            x: -2.75f,
            z: 0.35f,
            hp: 220f,
            damage: 5f,
            attackSpeed: 1.15f,
            attackRange: 2.1f,
            moveSpeed: 2.9f,
            aggroRadius: 20f
        );

        ConfigureForwardRectMelee(attackerOne);
        ConfigureForwardRectMelee(attackerTwo);
        if (useCommitSlotMode)
        {
            attackerOne.Attack.Rules.MeleeEngagementModel = MeleeEngagementModel.SlotRing;
            attackerTwo.Attack.Rules.MeleeEngagementModel = MeleeEngagementModel.SlotRing;
            attackerOne.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
            attackerTwo.Engagement.LifecycleState = CombatLifecycleState.AcquireTarget;
            attackerOne.Engagement.LockedTargetUnitId = target.UnitId;
            attackerTwo.Engagement.LockedTargetUnitId = target.UnitId;
            attackerOne.Engagement.TargetUnitId = target.UnitId;
            attackerTwo.Engagement.TargetUnitId = target.UnitId;
        }

        int attackerOneAttackCount = 0;
        int attackerTwoAttackCount = 0;
        float spreadAccumulator = 0f;
        int engagedFrames = 0;
        int highVelocityFrames = 0;
        int recoveryFrames = 0;
        var checkpoints = new List<string>();

        for (int frame = 1; frame <= TenSecondsFrames; frame++)
        {
            var events = sim.Tick(Delta);

            foreach (var evt in events)
            {
                if (evt is not UnitAttackedEvent attackedEvent)
                    continue;

                if (attackedEvent.AttackerUnitId == attackerOne.UnitId)
                    attackerOneAttackCount++;
                else if (attackedEvent.AttackerUnitId == attackerTwo.UnitId)
                    attackerTwoAttackCount++;
            }

            bool bothWithinEngage =
                SimTargeting.IsWithinEngageDistance(attackerOne, target.Position)
                && SimTargeting.IsWithinEngageDistance(attackerTwo, target.Position);
            if (bothWithinEngage)
            {
                engagedFrames++;
                spreadAccumulator += MathF.Abs(attackerOne.Position.Z - attackerTwo.Position.Z);

                bool highVelocity =
                    attackerOne.Velocity.Length() > HighVelocityThreshold
                    || attackerTwo.Velocity.Length() > HighVelocityThreshold;
                if (highVelocity)
                    highVelocityFrames++;

                bool inRecovery =
                    attackerOne.NavigationYieldTimer > 0f
                    || attackerOne.NavigationEscapeTimer > 0f
                    || attackerTwo.NavigationYieldTimer > 0f
                    || attackerTwo.NavigationEscapeTimer > 0f;
                if (inRecovery)
                    recoveryFrames++;
            }

            if (frame % CheckpointIntervalFrames == 0)
            {
                checkpoints.Add(
                    FormatCheckpoint(
                        frame,
                        attackerOne,
                        attackerTwo,
                        target,
                        attackerOneAttackCount,
                        attackerTwoAttackCount
                    )
                );
            }
        }

        float averageSpread =
            engagedFrames > 0 ? spreadAccumulator / engagedFrames : float.MaxValue;
        return new ScenarioResult(
            attackerOneAttackCount,
            attackerTwoAttackCount,
            averageSpread,
            engagedFrames,
            highVelocityFrames,
            recoveryFrames,
            state.CombatTargetSwitchCount,
            state.CombatBlockedTimeoutRetargetCount,
            checkpoints
        );
    }

    private static void ConfigureForwardRectMelee(UnitData unit)
    {
        unit.EngageShape = EngageShape.ForwardRect;
        unit.EngageRectLength = unit.AttackRange * 0.9f;
        unit.EngageRectHalfWidth = ScenarioEngageRectHalfWidth;
        unit.EngageRectForwardOffset = 0f;
        unit.EngageCloseRadius = 0.45f;
    }

    private static string FormatCheckpoint(
        int frame,
        UnitData attackerOne,
        UnitData attackerTwo,
        UnitData target,
        int attackerOneAttackCount,
        int attackerTwoAttackCount
    )
    {
        string t1 = attackerOne.Engagement.TargetUnitId.HasValue
            ? attackerOne.Engagement.TargetUnitId.Value.ToString(CultureInfo.InvariantCulture)
            : "null";
        string t2 = attackerTwo.Engagement.TargetUnitId.HasValue
            ? attackerTwo.Engagement.TargetUnitId.Value.ToString(CultureInfo.InvariantCulture)
            : "null";

        return string.Format(
            CultureInfo.InvariantCulture,
            "f={0}|a1=({1:F3},{2:F3})|a2=({3:F3},{4:F3})|target=({5:F3},{6:F3})|t1={7}|t2={8}|hits=({9},{10})",
            frame,
            attackerOne.Position.X,
            attackerOne.Position.Z,
            attackerTwo.Position.X,
            attackerTwo.Position.Z,
            target.Position.X,
            target.Position.Z,
            t1,
            t2,
            attackerOneAttackCount,
            attackerTwoAttackCount
        );
    }

    private sealed record ScenarioResult(
        int AttackerOneAttackCount,
        int AttackerTwoAttackCount,
        float AverageLateralSpread,
        int EngagedFrames,
        int HighVelocityFrames,
        int RecoveryFrames,
        int TargetSwitchCount,
        int BlockedTimeoutRetargetCount,
        List<string> Checkpoints
    );
}
