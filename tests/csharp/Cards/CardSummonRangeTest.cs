namespace Fateforged.Tests.Cards;

using Fateforged.Cards;
using Fateforged.Simulation.Data;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class CardSummonRangeTest
{
    [TestCase]
    public void CatalogRuntimeAndSimulationPreserveCardSpecificSummonRange()
    {
        var shortRangeDefinition = CardDefinitions.Pebbloom;
        var longRangeDefinition = CardDefinitions.WindDiver;

        AssertThat(shortRangeDefinition.SummonRange).IsLess(longRangeDefinition.SummonRange);
        AssertThat(Card.FromDefinition(shortRangeDefinition).SummonRange)
            .IsEqual(shortRangeDefinition.SummonRange);
        AssertThat(SimCardData.FromCardDefinition(longRangeDefinition).SummonRange)
            .IsEqual(longRangeDefinition.SummonRange);
    }
}
