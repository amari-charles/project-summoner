namespace Fateforged.Tests.Simulation;

using System.Collections.Generic;
using Fateforged.Cards;
using Fateforged.Simulation;
using Fateforged.Simulation.Commands;
using Fateforged.Simulation.Data;
using Fateforged.Units;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class PuffProjectileSpawnIntegrationTest
{
    private const float Delta = 1f / 60f;

    [TestCase]
    public void Tick_PuffCardSpawnedViaCatalog_PuffEmitsProjectileAgainstEnemyUnit()
    {
        var state = SimTestHelper.CreateBattleState();
        var sim = new Fateforged.Simulation.Simulation(state);

        var puffCard = CardCatalog.GetCard("puff");
        AssertThat(puffCard).IsNotNull();
        if (puffCard == null)
            return;

        var simCard = SimCardData.FromCardDefinition(puffCard);
        if (puffCard.Summon != null)
        {
            foreach (var entry in puffCard.Summon.Units)
                simCard.UnitTemplates.Add(UnitDefinitions.BuildSimTemplate(entry.UnitId, entry.Count, entry.Modifier));
        }
        else
        {
            simCard.UnitTemplates.Add(
                UnitDefinitions.BuildSimTemplate(puffCard.UnitId, puffCard.SpawnCount, puffCard.UnitModifier));
        }

        state.CardDataMap["puff"] = simCard;
        state.Summoners[0].Hand = new List<SimCardCatalogId> { "puff" };
        state.Summoners[0].Deck = new List<SimCardCatalogId> { "puff" };
        state.Summoners[0].Mana = 10f;

        var enemy = SimTestHelper.CreateMeleeUnit(state, team: 1, x: 8f, z: 0f, hp: 300f);
        enemy.AttackRange = 1.5f;
        enemy.MoveSpeed = 0f;

        state.PendingCommandBuffer.Add(new PlayCardCommand(0, 0, new SimVector3(-2f, 0f, 0f))
        {
            ExecuteFrame = 1
        });

        bool observedProjectile = false;
        for (int i = 0; i < 420; i++)
        {
            sim.Tick(Delta);
            if (state.Projectiles.Count > 0)
            {
                observedProjectile = true;
                break;
            }
        }

        AssertThat(observedProjectile).IsTrue();
    }
}
