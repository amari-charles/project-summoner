namespace ProjectSummoner.Tests.Projectiles.Paths;

using GdUnit4;
using Godot;
using ProjectSummoner.Projectiles.Paths;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for projectile path implementations.
/// </summary>
[TestSuite]
public class PathTests
{
    private static readonly Vector3 Start = new(0, 0, 0);
    private static readonly Vector3 End = new(10, 0, 10);

    // =========================================================================
    // StraightPath Tests
    // =========================================================================

    [TestCase]
    public void StraightPath_GetPosition_AtStart_ReturnsStart()
    {
        var path = new StraightPath(Start, End);
        var position = path.GetPosition(0f);
        AssertThat(position).IsEqual(Start);
    }

    [TestCase]
    public void StraightPath_GetPosition_AtEnd_ReturnsEnd()
    {
        var path = new StraightPath(Start, End);
        var position = path.GetPosition(1f);
        AssertThat(position).IsEqual(End);
    }

    [TestCase]
    public void StraightPath_GetPosition_AtMidpoint_ReturnsMidpoint()
    {
        var path = new StraightPath(Start, End);
        var position = path.GetPosition(0.5f);
        var expected = new Vector3(5, 0, 5);
        AssertThat(position).IsEqual(expected);
    }

    [TestCase]
    public void StraightPath_GetLength_ReturnsCorrectDistance()
    {
        var path = new StraightPath(Start, End);
        var expected = Start.DistanceTo(End);
        AssertThat(path.GetLength()).IsEqual(expected);
    }

    [TestCase]
    public void StraightPath_UpdateTarget_UpdatesEndpoint()
    {
        var path = new StraightPath(Start, End);
        var newEnd = new Vector3(20, 0, 20);
        path.UpdateTarget(newEnd);

        var position = path.GetPosition(1f);
        AssertThat(position).IsEqual(newEnd);
    }

    [TestCase]
    public void StraightPath_GetDirection_ReturnsNormalizedDirection()
    {
        var path = new StraightPath(Start, End);
        var direction = path.GetDirection(0.5f);
        var expected = (End - Start).Normalized();
        AssertThat(direction.IsEqualApprox(expected)).IsTrue();
    }

    // =========================================================================
    // ArcPath Tests
    // =========================================================================

    [TestCase]
    public void ArcPath_GetPosition_AtStart_ReturnsStart()
    {
        var path = new ArcPath(Start, End, arcHeight: 3f);
        var position = path.GetPosition(0f);
        AssertThat(position).IsEqual(Start);
    }

    [TestCase]
    public void ArcPath_GetPosition_AtEnd_ReturnsEnd()
    {
        var path = new ArcPath(Start, End, arcHeight: 3f);
        var position = path.GetPosition(1f);
        AssertThat(position).IsEqual(End);
    }

    [TestCase]
    public void ArcPath_GetPosition_AtMidpoint_IsElevated()
    {
        float arcHeight = 3f;
        var path = new ArcPath(Start, End, arcHeight);
        var position = path.GetPosition(0.5f);

        // Midpoint should be elevated (Y > 0)
        AssertThat(position.Y).IsGreater(0f);
    }

    [TestCase]
    public void ArcPath_GetPosition_AtMidpoint_XZMatchesLinearInterpolation()
    {
        var path = new ArcPath(Start, End, arcHeight: 3f);
        var position = path.GetPosition(0.5f);

        // X and Z should be approximately at the midpoint
        AssertThat(Mathf.Abs(position.X - 5f)).IsLess(0.5f);
        AssertThat(Mathf.Abs(position.Z - 5f)).IsLess(0.5f);
    }

    [TestCase]
    public void ArcPath_UpdateTarget_RecalculatesPath()
    {
        var path = new ArcPath(Start, End, arcHeight: 3f);
        var newEnd = new Vector3(20, 0, 20);
        path.UpdateTarget(newEnd);

        var position = path.GetPosition(1f);
        AssertThat(position).IsEqual(newEnd);
    }

    [TestCase]
    public void ArcPath_GetLength_ReturnsPositiveValue()
    {
        var path = new ArcPath(Start, End, arcHeight: 3f);
        AssertThat(path.GetLength()).IsGreater(0f);
    }

    [TestCase]
    public void ArcPath_GetLength_IsGreaterThanStraightLine()
    {
        var path = new ArcPath(Start, End, arcHeight: 3f);
        float straightDistance = Start.DistanceTo(End);
        AssertThat(path.GetLength()).IsGreater(straightDistance);
    }

    [TestCase]
    public void ArcPath_ShortDistance_ScalesArcHeight()
    {
        var shortEnd = new Vector3(1, 0, 1);
        var path = new ArcPath(Start, shortEnd, arcHeight: 10f);
        var position = path.GetPosition(0.5f);

        // Arc height should be scaled down for short distances
        // Full arc distance is 5.0, so 1.41 distance should scale to ~28% of arc height
        AssertThat(position.Y).IsLess(10f);
    }

    // =========================================================================
    // BallisticPath Tests
    // =========================================================================

    [TestCase]
    public void BallisticPath_GetPosition_AtStart_ReturnsStart()
    {
        var path = new BallisticPath(Start, End, speed: 10f);
        var position = path.GetPosition(0f);
        AssertThat(position).IsEqual(Start);
    }

    [TestCase]
    public void BallisticPath_GetPosition_AtEnd_ReturnsApproximatelyEnd()
    {
        var path = new BallisticPath(Start, End, speed: 10f);
        var position = path.GetPosition(1f);

        // Should be very close to end (may have small floating point differences)
        AssertThat(position.DistanceTo(End)).IsLess(0.1f);
    }

    [TestCase]
    public void BallisticPath_GetPosition_AtMidpoint_HasParabolicArc()
    {
        var path = new BallisticPath(Start, End, speed: 10f);
        var position = path.GetPosition(0.5f);

        // Midpoint of parabolic trajectory should be elevated
        AssertThat(position.Y).IsGreater(0f);
    }

    [TestCase]
    public void BallisticPath_GetLength_ReturnsPositiveValue()
    {
        var path = new BallisticPath(Start, End, speed: 10f);
        AssertThat(path.GetLength()).IsGreater(0f);
    }

    [TestCase]
    public void BallisticPath_GetDirection_ReturnsNormalizedVector()
    {
        var path = new BallisticPath(Start, End, speed: 10f);
        var direction = path.GetDirection(0.5f);
        AssertThat(Mathf.Abs(direction.Length() - 1f)).IsLess(0.01f);
    }

    [TestCase]
    public void BallisticPath_HigherStart_HasDownwardTrajectory()
    {
        var highStart = new Vector3(0, 10, 0);
        var lowEnd = new Vector3(10, 0, 10);
        var path = new BallisticPath(highStart, lowEnd, speed: 10f);

        // At end, direction should be pointing downward
        var direction = path.GetDirection(1f);
        AssertThat(direction.Y).IsLess(0f);
    }
}
