namespace Fateforged.Tests.Simulation;

using System;
using Fateforged.Simulation;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
public class SummonPlacementRulesTest
{
    [TestCase]
    public void ClampToCardRange_OutsideRadius_PreservesAimDirectionAtBoundary()
    {
        var center = new SimVector3(2f, 0f, -3f);
        var requested = new SimVector3(12f, 4f, 7f);

        var resolved = SummonPlacementRules.ClampToCardRange(center, requested, 5f);

        float expectedOffset = 5f / MathF.Sqrt(2f);
        AssertThat(resolved.X).IsEqualApprox(center.X + expectedOffset, 0.001f);
        AssertThat(resolved.Y).IsEqual(4f);
        AssertThat(resolved.Z).IsEqualApprox(center.Z + expectedOffset, 0.001f);
        float distance = MathF.Sqrt(
            MathF.Pow(resolved.X - center.X, 2f) + MathF.Pow(resolved.Z - center.Z, 2f)
        );
        AssertThat(distance).IsEqualApprox(5f, 0.001f);
    }

    [TestCase]
    public void ClampToCardRange_InsideRadius_RemainsAtRequestedPoint()
    {
        var requested = new SimVector3(3f, 2f, 4f);

        var resolved = SummonPlacementRules.ClampToCardRange(
            SimVector3.Zero,
            requested,
            6f
        );

        AssertThat(resolved).IsEqual(requested);
    }
}
