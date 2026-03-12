namespace Fateforged.Tests.Simulation.Abilities;

using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Tests.Simulation;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class AbilitySpellHealTest
{
    [TestCase]
    public void HealingField_HealsAlliesInRadius_AndDoesNotAffectEnemies()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var cardDef = CardDefinitions.HealingField;
        var simCard = SimCardData.FromCardDefinition(cardDef);
        state.CardDataMap[(string)cardDef.Id] = simCard;
        state.Summoners[0].Hand.Add((string)cardDef.Id);
        state.Summoners[0].Mana = 10f;

        var ally = SimTestHelper.CreateMeleeUnit(state, 0, x: 0f, z: 0f, hp: 100f);
        ally.CurrentHp = 25f;
        var enemy = SimTestHelper.CreateMeleeUnit(state, 1, x: 0f, z: 0f, hp: 100f);
        enemy.CurrentHp = 25f;

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, new SimVector3(0f, 0f, 0f)));
        sim.Tick(Simulation.FixedDeltaSeconds);

        AssertThat(ally.CurrentHp).IsGreater(25f);
        AssertThat(enemy.CurrentHp).IsEqual(25f);
    }
}
