namespace Fateforged.Tests.Simulation;

using Fateforged.Simulation;
using Fateforged.Simulation.Effects;
using Fateforged.Simulation.Enums;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SpellAreaShapeResolutionTest
{
    [TestCase]
    public void SpellAreaShape_Circle_ResolvesByRadius()
    {
        var center = new SimVector3(0f, 0f, 0f);
        var inside = new SimVector3(0.9f, 0f, 0f);
        var outside = new SimVector3(1.1f, 0f, 0f);

        AssertThat(SpellAreaResolver.IsWithinArea(SpellAreaShape.Circle, center, inside, 1f)).IsTrue();
        AssertThat(SpellAreaResolver.IsWithinArea(SpellAreaShape.Circle, center, outside, 1f)).IsFalse();
    }

    [TestCase]
    public void SpellAreaShape_Square_ResolvesByBounds()
    {
        var center = new SimVector3(0f, 0f, 0f);
        var insideSquare = new SimVector3(0.9f, 0f, 0.9f);
        var outsideSquare = new SimVector3(1.1f, 0f, 0f);

        AssertThat(SpellAreaResolver.IsWithinArea(SpellAreaShape.Square, center, insideSquare, 1f))
            .IsTrue();
        AssertThat(SpellAreaResolver.IsWithinArea(SpellAreaShape.Square, center, outsideSquare, 1f))
            .IsFalse();
    }
}
