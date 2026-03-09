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
}
