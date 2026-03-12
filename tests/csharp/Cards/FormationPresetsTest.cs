namespace Fateforged.Tests.Cards;

using Fateforged.Cards.Formations;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for FormationPresets and formation strategies.
/// </summary>
[TestSuite]
public class FormationPresetsTest
{
    [TestCase]
    public void StandardGrid_ReturnsCenteredOffset_ForSingleUnit()
    {
        var offset = FormationPresets.StandardGrid.GetOffset(0, 1);

        AssertThat(offset).IsEqual(Vector3.Zero);
    }

    [TestCase]
    public void StandardGrid_ReturnsSymmetricOffsets_ForTwoUnits()
    {
        var offset1 = FormationPresets.StandardGrid.GetOffset(0, 2);
        var offset2 = FormationPresets.StandardGrid.GetOffset(1, 2);

        // Two units should be symmetric around center
        AssertThat(offset1.X).IsEqual(-offset2.X);
    }

    [TestCase]
    public void TightSwarmGrid_HasSmallerSpacing_ThanStandardGrid()
    {
        // Tight swarm uses 1.5f spacing vs standard 2.0f
        var tightOffset = FormationPresets.TightSwarmGrid.GetOffset(0, 4);
        var standardOffset = FormationPresets.StandardGrid.GetOffset(0, 4);

        // With same index and total, tight should be closer to center
        AssertThat(Mathf.Abs(tightOffset.X)).IsLess(Mathf.Abs(standardOffset.X));
    }

    [TestCase]
    public void FireAntSwarm_Uses5Columns()
    {
        // Fire ant swarm is 4 rows x 5 columns for 20 units
        // First row should have 5 units, so indices 0-4 should have same X (row depth)
        var offset0 = FormationPresets.FireAntSwarm.GetOffset(0, 20);
        var offset4 = FormationPresets.FireAntSwarm.GetOffset(4, 20);

        // Both are in row 0, so X offset should be the same
        AssertThat(offset0.X).IsEqual(offset4.X);
    }

    [TestCase]
    public void CloudSwarm_CreatesGroupedFormation()
    {
        // Cloud swarm is 3 groups of 2 (6 units total)
        var offset0 = FormationPresets.CloudSwarm.GetOffset(0, 6);
        var offset1 = FormationPresets.CloudSwarm.GetOffset(1, 6);

        // Units 0 and 1 should be in the same group (close together)
        var withinGroupDistance = offset0.DistanceTo(offset1);

        // Units 0 and 2 should be in different groups (farther apart)
        var offset2 = FormationPresets.CloudSwarm.GetOffset(2, 6);
        var betweenGroupDistance = offset0.DistanceTo(offset2);

        AssertThat(withinGroupDistance).IsLess(betweenGroupDistance);
    }

    [TestCase]
    public void StandardLine_AlignUnitsHorizontally()
    {
        var offset0 = FormationPresets.StandardLine.GetOffset(0, 5);
        var offset2 = FormationPresets.StandardLine.GetOffset(2, 5);
        var offset4 = FormationPresets.StandardLine.GetOffset(4, 5);

        // All units should be at same X (line spreads along Z axis)
        AssertThat(offset0.X).IsEqual(0f);
        AssertThat(offset2.X).IsEqual(0f);
        AssertThat(offset4.X).IsEqual(0f);

        // Z values should be different (spread along Z)
        AssertThat(offset0.Z).IsNotEqual(offset2.Z);
    }

    [TestCase]
    public void StandardRing_CreatesCircularFormation()
    {
        // All units should be equidistant from center
        var offset0 = FormationPresets.StandardRing.GetOffset(0, 6);
        var offset1 = FormationPresets.StandardRing.GetOffset(1, 6);
        var offset2 = FormationPresets.StandardRing.GetOffset(2, 6);

        var dist0 = offset0.Length();
        var dist1 = offset1.Length();
        var dist2 = offset2.Length();

        // All distances from center should be equal (the radius)
        AssertThat(dist0).IsEqualApprox(dist1, 0.01f);
        AssertThat(dist1).IsEqualApprox(dist2, 0.01f);
    }
}
