namespace Fateforged.Tests.Services;

using Fateforged.Meta.Deck;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class DeckSizeContractTest
{
    [TestCase]
    public void StandardDeckMaximum_IsTwelveCards()
    {
        AssertThat(DeckService.MaxDeckSize).IsEqual(12);
    }
}
