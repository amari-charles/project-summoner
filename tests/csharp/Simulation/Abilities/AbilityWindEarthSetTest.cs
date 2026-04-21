namespace Fateforged.Tests.Simulation.Abilities;

using System.Linq;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityWindEarthSetTest
{
    [TestCase]
    public void TailWind_AppliesSquareAttackSpeedBuffAndDebuff()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var cardDef = CardDefinitions.TailWind;
        state.CardDataMap[(string)cardDef.Id] = SimCardData.FromCardDefinition(cardDef);
        state.Summoners[0].Hand.Add((string)cardDef.Id);
        state.Summoners[0].Mana = 10f;

        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 1f, z: 1f, hp: 100f);
        var enemySquareOnly = SimTestHelper.CreateMeleeUnit(state, 1, x: 5.5f, z: 5.5f, hp: 100f);
        var enemyOutside = SimTestHelper.CreateMeleeUnit(state, 1, x: 6.5f, z: 6.5f, hp: 100f);

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f)));
        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(
                ally.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.Value > 0f
                )
            )
            .IsTrue();
        AssertThat(
                enemySquareOnly.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.AttackSpeedModifier && b.Value < 0f
                )
            )
            .IsTrue();
        AssertThat(
                enemyOutside.ActiveBuffs.Any(b => b.EffectType == EffectType.AttackSpeedModifier)
            )
            .IsFalse();
    }

    [TestCase]
    public void Fortify_AppliesFlatDamageReductionWithoutHealing()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var cardDef = CardDefinitions.Fortify;
        state.CardDataMap[(string)cardDef.Id] = SimCardData.FromCardDefinition(cardDef);
        state.Summoners[0].Hand.Add((string)cardDef.Id);
        state.Summoners[0].Mana = 10f;

        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, hp: 100f);
        ally.CurrentHp = 50f;
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, z: 0f, hp: 100f);

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f)));
        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(ally.CurrentHp).IsEqual(50f);
        AssertThat(
                ally.ActiveBuffs.Any(b =>
                    b.EffectType == EffectType.FlatDamageReduction && b.Value == 4f
                )
            )
            .IsTrue();
        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.FlatDamageReduction)).IsFalse();
    }
}
