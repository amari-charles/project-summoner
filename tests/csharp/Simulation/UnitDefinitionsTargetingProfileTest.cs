namespace Fateforged.Tests.Simulation;

using Fateforged.Constants;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class UnitDefinitionsTargetingProfileTest
{
    [TestCase]
    public void BuildSimTemplate_Puff_UsesFlyingConeStrafeProfile()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.Puff, count: 1);

        AssertThat(template.FallbackMovement).IsEqual(FallbackMovement.Strafe);
        AssertThat(template.HasConeConstraint).IsTrue();
        AssertThat(template.ConeHalfAngle).IsEqual(30f);
        AssertThat(template.TargetPolicyId).IsEqual(TargetPolicyId.PreferAttackableAndStick);
        AssertThat(template.MovementIntentStrategy).IsEqual(MovementIntentStrategy.Context);
    }

    [TestCase]
    public void BuildSimTemplate_Duckling_UsesRangedStrafeProfile()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.Duckling, count: 1);

        AssertThat(template.FallbackMovement).IsEqual(FallbackMovement.Strafe);
        AssertThat(template.HasConeConstraint).IsFalse();
        AssertThat(template.TargetPolicyId).IsEqual(TargetPolicyId.PreferAttackableAndStick);
        AssertThat(template.MovementIntentStrategy).IsEqual(MovementIntentStrategy.Context);
    }

    [TestCase]
    public void BuildSimTemplate_Rock_UsesPassiveProfile()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.Rock, count: 1);

        AssertThat(template.FallbackMovement).IsEqual(FallbackMovement.Idle);
        AssertThat(template.TargetPolicyId).IsEqual(TargetPolicyId.Legacy);
        AssertThat(template.MovementIntentStrategy).IsEqual(MovementIntentStrategy.Direct);
    }

    [TestCase]
    public void BuildSimTemplate_FireWisp_UsesInferredMeleeProfile()
    {
        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.FireWisp, count: 1);

        AssertThat(template.FallbackMovement).IsEqual(FallbackMovement.MoveToward);
        AssertThat(template.TargetLayerFilter).IsEqual(TargetLayer.GroundOnly);
        AssertThat(template.HealthScorerWeight).IsEqual(10f);
        AssertThat(template.TargetPolicyId).IsEqual(TargetPolicyId.PreferAttackableAndStick);
        AssertThat(template.MovementIntentStrategy).IsEqual(MovementIntentStrategy.Context);
    }

    [TestCase]
    public void BuildSimTemplate_DamageProfileFields_MapFromDefinition()
    {
        var def = UnitDefinitions.Get(UnitIds.FireWisp);
        AssertThat(def).IsNotNull();

        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.FireWisp, count: 1);

        AssertThat(template.PhysicalDamageRatio).IsEqual(def!.DamageProfile.PhysicalRatio);
        AssertThat(template.ElementalDamageRatio).IsEqual(def.DamageProfile.ElementalRatio);

        var expectedAttackType =
            def.DamageProfile.ElementalRatio > 0f && def.DamageProfile.PhysicalRatio <= 0f
                ? DamageType.Magic
                : DamageType.Physical;
        AssertThat(template.AttackType).IsEqual(expectedAttackType);
    }

    [TestCase]
    public void BuildSimTemplate_AttackVectorDefaults_MapFromDefinition()
    {
        var def = UnitDefinitions.Get(UnitIds.FireWisp);
        AssertThat(def).IsNotNull();

        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.FireWisp, count: 1);

        AssertThat(template.Attack.Preset).IsEqual(def!.Attack.Preset);
        AssertThat(template.Attack.Selection.Mode).IsEqual(AttackSelectionMode.Single);
        AssertThat(template.Attack.Area.Shape).IsEqual(AttackAreaShape.Sphere);
        AssertThat(template.Attack.DeliveryMode).IsEqual(AttackDeliveryMode.Instant);
        AssertThat(template.Attack.Propagation.Mode).IsEqual(AttackPropagationMode.None);
        AssertThat(template.Attack.Selection.TargetLimit).IsEqual(1);
    }

    [TestCase]
    public void BuildSimTemplate_AttackVectorTimingAndRules_MapFromDefinition()
    {
        var def = UnitDefinitions.Get(UnitIds.Puff);
        AssertThat(def).IsNotNull();

        SimUnitTemplate template = UnitDefinitions.BuildSimTemplate(UnitIds.Puff, count: 1);

        AssertThat(template.Attack.Timing.WindupSeconds).IsEqual(def!.Attack.Timing.WindupSeconds);
        AssertThat(template.Attack.Timing.ActiveSeconds).IsEqual(def.Attack.Timing.ActiveSeconds);
        AssertThat(template.Attack.Timing.RecoverySeconds).IsEqual(def.Attack.Timing.RecoverySeconds);
        AssertThat(template.Attack.Timing.TickIntervalSeconds).IsEqual(def.Attack.Timing.TickIntervalSeconds);
        AssertThat(template.Attack.Rules.TriggerMode).IsEqual(def.Attack.Rules.TriggerMode);
    }

    [TestCase(AttackPreset.AreaCleave, 3)]
    [TestCase(AttackPreset.LinePierce, 3)]
    [TestCase(AttackPreset.Chain, 3)]
    public void BuildAttackVectorState_UnsetTargetLimit_UsesPresetDefault(AttackPreset preset, int expectedLimit)
    {
        var config = new AttackVectorConfig
        {
            Preset = preset,
            Selection = new AttackSelectionConfig()
        };

        var state = AttackVectorStateBuilder.Build(config);

        AssertThat(state.Selection.TargetLimit).IsEqual(expectedLimit);
    }

    [TestCase(AttackPreset.AreaCleave)]
    [TestCase(AttackPreset.LinePierce)]
    [TestCase(AttackPreset.Chain)]
    public void BuildAttackVectorState_ExplicitTargetLimitOne_IsPreserved(AttackPreset preset)
    {
        var config = new AttackVectorConfig
        {
            Preset = preset,
            Selection = new AttackSelectionConfig { TargetLimit = 1 }
        };

        var state = AttackVectorStateBuilder.Build(config);

        AssertThat(state.Selection.TargetLimit).IsEqual(1);
    }

    [TestCase(AttackPreset.AreaCleave)]
    [TestCase(AttackPreset.LinePierce)]
    [TestCase(AttackPreset.Chain)]
    public void BuildAttackVectorState_ExplicitTargetLimitZero_IsPreserved(AttackPreset preset)
    {
        var config = new AttackVectorConfig
        {
            Preset = preset,
            Selection = new AttackSelectionConfig { TargetLimit = 0 }
        };

        var state = AttackVectorStateBuilder.Build(config);

        AssertThat(state.Selection.TargetLimit).IsEqual(0);
    }
}
