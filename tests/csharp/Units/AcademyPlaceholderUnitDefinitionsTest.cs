namespace Fateforged.Tests.Units;

using Fateforged.Constants;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AcademyPlaceholderUnitDefinitionsTest
{
    [TestCase]
    public void NeutralStarterUnit_IsPlainMeleeSummon()
    {
        var unit = UnitDefinitions.Get(UnitIds.NeutralStarterUnit);

        AssertThat(unit.DisplayName).IsEqual("Neutral Starter Unit");
        AssertThat(unit.UnitType).IsEqual(UnitType.Melee);
        AssertThat(unit.TargetingProfile).IsEqual(UnitTargetingProfile.MeleeGround);
        AssertThat(unit.Stats.MaxHp).IsEqual(70f);
        AssertThat(unit.Stats.AttackDamage).IsEqual(10f);
        AssertThat(unit.Stats.AttackSpeed).IsEqual(1.0f);
        AssertThat(unit.Stats.MoveSpeed).IsEqual(3.0f);
    }

    [TestCase]
    public void TrainingTarget_IsHarmlessAndPassive()
    {
        var unit = UnitDefinitions.Get(UnitIds.TrainingTarget);

        AssertThat(unit.DisplayName).IsEqual("Training Target");
        AssertThat(unit.TargetingProfile).IsEqual(UnitTargetingProfile.Passive);
        AssertThat(unit.Stats.MaxHp).IsEqual(60f);
        AssertThat(unit.Stats.AttackDamage).IsEqual(0f);
        AssertThat(unit.Stats.MoveSpeed).IsEqual(0f);
        AssertThat(unit.Stats.AggroRadius).IsEqual(0f);
    }

    [TestCase]
    public void WeakEnemyUnit_IsLowPressureThreat()
    {
        var unit = UnitDefinitions.Get(UnitIds.WeakEnemyUnit);

        AssertThat(unit.DisplayName).IsEqual("Weak Enemy Unit");
        AssertThat(unit.UnitType).IsEqual(UnitType.Melee);
        AssertThat(unit.TargetingProfile).IsEqual(UnitTargetingProfile.MeleeGround);
        AssertThat(unit.Stats.MaxHp).IsEqual(45f);
        AssertThat(unit.Stats.AttackDamage).IsEqual(7f);
        AssertThat(unit.Stats.AttackSpeed).IsEqual(1.0f);
        AssertThat(unit.Stats.MoveSpeed).IsEqual(3.2f);
    }
}
