namespace Fateforged.Tests.Simulation.Abilities;

using System.Collections.Generic;
using System.Linq;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Simulation.Enums;
using Fateforged.Tests.Simulation;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilityWaterSetTest
{
    [TestCase]
    public void CleanseSpell_CleansesDebuffs_AndHealsAlliesOnly()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var cardDef = CardDefinitions.Cleanse;
        state.CardDataMap[(string)cardDef.Id] = SimCardData.FromCardDefinition(cardDef);
        state.Summoners[0].Hand.Add((string)cardDef.Id);
        state.Summoners[0].Mana = 10f;

        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, hp: 100f);
        ally.CurrentHp = 50f;
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 11,
                EffectType = EffectType.Slow,
                Value = 0.3f,
                Duration = 3f,
            }
        );
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 12,
                EffectType = EffectType.Stun,
                Duration = 1f,
            }
        );
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 13,
                EffectType = EffectType.Damage,
                Value = 2f,
                Duration = 5f,
                TickInterval = 1f,
                TickTimer = 1f,
                StatusKind = StatusEffectKind.Poison,
            }
        );
        ally.ActiveBuffs.Add(
            new ActiveBuff
            {
                BuffId = 14,
                EffectType = EffectType.Shield,
                ShieldHp = 30f,
                Duration = -1f,
            }
        );
        ally.ForcedTargetUnitId = 99;
        ally.ForcedTargetTimer = 2f;

        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, z: 0f, hp: 100f);
        enemy.CurrentHp = 50f;

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f)));
        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(ally.CurrentHp).IsGreater(50f);
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsFalse();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Stun)).IsFalse();
        AssertThat(ally.ActiveBuffs.Any(b => b.StatusKind == StatusEffectKind.Poison)).IsFalse();
        AssertThat(ally.ActiveBuffs.Any(b => b.EffectType == EffectType.Shield)).IsTrue();
        AssertThat(ally.ForcedTargetUnitId.HasValue).IsFalse();
        AssertThat(ally.ForcedTargetTimer).IsEqual(0f);

        AssertThat(enemy.CurrentHp).IsEqual(50f);
    }

    [TestCase]
    public void WaterJet_DamagesAndKnocksBackTarget()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var cardDef = CardDefinitions.WaterJet;
        state.CardDataMap[(string)cardDef.Id] = SimCardData.FromCardDefinition(cardDef);
        state.Summoners[0].Hand.Add((string)cardDef.Id);
        state.Summoners[0].Mana = 10f;

        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 6f, z: 0f, hp: 120f);
        float hpBefore = enemy.CurrentHp;
        float xBefore = enemy.Position.X;

        var cmd = new PlayCardCommand(0, 0, new SimVector3(6f, 0f, 0f)) { TargetUnitId = enemy.UnitId };
        state.PendingCommandBuffer.Add(cmd);
        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(enemy.CurrentHp).IsLess(hpBefore);
        AssertThat(enemy.Position.X).IsGreater(xBefore);
    }

    [TestCase]
    public void RainField_AppliesImmediateSlow_AndRepeatingDamagePulses()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var cardDef = CardDefinitions.RainField;
        state.CardDataMap[(string)cardDef.Id] = SimCardData.FromCardDefinition(cardDef);
        state.Summoners[0].Hand.Add((string)cardDef.Id);
        state.Summoners[0].Mana = 10f;

        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, z: 0f, hp: 120f);
        float hpBefore = enemy.CurrentHp;

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f)));
        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(enemy.ActiveBuffs.Any(b => b.EffectType == EffectType.Slow)).IsTrue();
        AssertThat(state.DelayedEffects.Count).IsEqual(5);

        for (int i = 0; i < 240; i++)
            sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(enemy.CurrentHp).IsLess(hpBefore);
    }
}
