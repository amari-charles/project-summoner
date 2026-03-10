namespace Fateforged.Tests.Traits;

using System.Collections.Generic;
using Fateforged.Simulation.Data;
using Fateforged.Stats;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class UnifiedTraitRuntimeStateTest
{
    [TestCase]
    public void ApplySpawnModifiers_AppliesCardInstanceMultipliers()
    {
        var runtime = MatchTraitRuntimeState.Empty();
        runtime.SetCardInstanceStatMultipliers(
            new TraitRuntimeCardInstanceId("card_instance_test"),
            new Dictionary<StatKey, float>
            {
                [StatKey.AttackDamage] = 1.25f,
                [StatKey.MaxHp] = 1.10f,
                [StatKey.SoulStrength] = 1.50f
            });

        var unit = new UnitData
        {
            AttackDamage = 40f,
            MaxHp = 100f,
            CurrentHp = 100f,
            SoulStrength = 4f
        };

        runtime.ApplySpawnModifiers(unit, new TraitRuntimeSpawnContext
        {
            CardInstanceId = new TraitRuntimeCardInstanceId("card_instance_test")
        });

        AssertThat(unit.AttackDamage).IsEqual(50f);
        AssertThat(unit.MaxHp).IsEqual(110f);
        AssertThat(unit.CurrentHp).IsEqual(110f);
        AssertThat(unit.SoulStrength).IsEqual(6f);
    }
}
