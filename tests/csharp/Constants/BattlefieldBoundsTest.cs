namespace ProjectSummoner.Tests.Constants;

using GdUnit4;
using Godot;
using ProjectSummoner.Constants;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for BattlefieldBounds.
/// </summary>
[TestSuite]
public class BattlefieldBoundsTest
{
    // =========================================================================
    // ClampToBounds Tests
    // =========================================================================

    [TestCase]
    public void ClampToBounds_InsideBounds_Unchanged()
    {
        var position = new Vector3(10, 5, 20);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped).IsEqual(position);
    }

    [TestCase]
    public void ClampToBounds_OutsideMinX_ClampsToMinX()
    {
        var position = new Vector3(-100, 5, 0);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped.X).IsEqual(BattlefieldBounds.MinX);
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(0); // Unchanged
    }

    [TestCase]
    public void ClampToBounds_OutsideMaxX_ClampsToMaxX()
    {
        var position = new Vector3(100, 5, 0);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped.X).IsEqual(BattlefieldBounds.MaxX);
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(0); // Unchanged
    }

    [TestCase]
    public void ClampToBounds_OutsideMinZ_ClampsToMinZ()
    {
        var position = new Vector3(0, 5, -100);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped.X).IsEqual(0); // Unchanged
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(BattlefieldBounds.MinZ);
    }

    [TestCase]
    public void ClampToBounds_OutsideMaxZ_ClampsToMaxZ()
    {
        var position = new Vector3(0, 5, 100);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped.X).IsEqual(0); // Unchanged
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(BattlefieldBounds.MaxZ);
    }

    [TestCase]
    public void ClampToBounds_PreservesY()
    {
        var position = new Vector3(-100, 999, 100);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped.Y).IsEqual(999);
    }

    [TestCase]
    public void ClampToBounds_AtExactBounds_Unchanged()
    {
        var position = new Vector3(BattlefieldBounds.MaxX, 0, BattlefieldBounds.MaxZ);

        var clamped = BattlefieldBounds.ClampToBounds(position);

        AssertThat(clamped).IsEqual(position);
    }

    // =========================================================================
    // IsInBounds Tests
    // =========================================================================

    [TestCase]
    public void IsInBounds_CenterPosition_ReturnsTrue()
    {
        var position = new Vector3(0, 0, 0);

        AssertThat(BattlefieldBounds.IsInBounds(position)).IsTrue();
    }

    [TestCase]
    public void IsInBounds_AtMaxBounds_ReturnsTrue()
    {
        var position = new Vector3(BattlefieldBounds.MaxX, 0, BattlefieldBounds.MaxZ);

        AssertThat(BattlefieldBounds.IsInBounds(position)).IsTrue();
    }

    [TestCase]
    public void IsInBounds_AtMinBounds_ReturnsTrue()
    {
        var position = new Vector3(BattlefieldBounds.MinX, 0, BattlefieldBounds.MinZ);

        AssertThat(BattlefieldBounds.IsInBounds(position)).IsTrue();
    }

    [TestCase]
    public void IsInBounds_OutsideX_ReturnsFalse()
    {
        var position = new Vector3(BattlefieldBounds.MaxX + 1, 0, 0);

        AssertThat(BattlefieldBounds.IsInBounds(position)).IsFalse();
    }

    [TestCase]
    public void IsInBounds_OutsideZ_ReturnsFalse()
    {
        var position = new Vector3(0, 0, BattlefieldBounds.MinZ - 1);

        AssertThat(BattlefieldBounds.IsInBounds(position)).IsFalse();
    }

    // =========================================================================
    // IsValidSpawnPositionForTeam Tests
    // =========================================================================

    [TestCase]
    public void IsValidSpawnPositionForTeam_Player_NegativeX_ReturnsTrue()
    {
        var position = new Vector3(-10, 0, 0);

        AssertThat(BattlefieldBounds.IsValidSpawnPositionForTeam(position, 0)).IsTrue();
    }

    [TestCase]
    public void IsValidSpawnPositionForTeam_Player_AtBoundary_ReturnsTrue()
    {
        var position = new Vector3(0, 0, 0);

        AssertThat(BattlefieldBounds.IsValidSpawnPositionForTeam(position, 0)).IsTrue();
    }

    [TestCase]
    public void IsValidSpawnPositionForTeam_Player_PositiveX_ReturnsFalse()
    {
        var position = new Vector3(10, 0, 0);

        AssertThat(BattlefieldBounds.IsValidSpawnPositionForTeam(position, 0)).IsFalse();
    }

    [TestCase]
    public void IsValidSpawnPositionForTeam_Enemy_PositiveX_ReturnsTrue()
    {
        var position = new Vector3(10, 0, 0);

        AssertThat(BattlefieldBounds.IsValidSpawnPositionForTeam(position, 1)).IsTrue();
    }

    [TestCase]
    public void IsValidSpawnPositionForTeam_Enemy_AtBoundary_ReturnsFalse()
    {
        var position = new Vector3(0, 0, 0);

        AssertThat(BattlefieldBounds.IsValidSpawnPositionForTeam(position, 1)).IsFalse();
    }

    [TestCase]
    public void IsValidSpawnPositionForTeam_Enemy_NegativeX_ReturnsFalse()
    {
        var position = new Vector3(-10, 0, 0);

        AssertThat(BattlefieldBounds.IsValidSpawnPositionForTeam(position, 1)).IsFalse();
    }

    // =========================================================================
    // ClampToValidSpawnZone Tests
    // =========================================================================

    [TestCase]
    public void ClampToValidSpawnZone_Player_ValidPosition_Unchanged()
    {
        var position = new Vector3(-20, 5, 10);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 0);

        AssertThat(clamped).IsEqual(position);
    }

    [TestCase]
    public void ClampToValidSpawnZone_Player_InvalidPosition_ClampsToBoundary()
    {
        var position = new Vector3(20, 5, 10);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 0);

        AssertThat(clamped.X).IsEqual(BattlefieldBounds.SpawnBoundaryX);
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(10); // Preserved
    }

    [TestCase]
    public void ClampToValidSpawnZone_Enemy_ValidPosition_Unchanged()
    {
        var position = new Vector3(20, 5, 10);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 1);

        AssertThat(clamped).IsEqual(position);
    }

    [TestCase]
    public void ClampToValidSpawnZone_Enemy_InvalidPosition_ClampsToBoundary()
    {
        var position = new Vector3(-20, 5, 10);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 1);

        AssertThat(clamped.X).IsEqual(BattlefieldBounds.SpawnBoundaryX + BattlefieldBounds.SpawnBoundaryEpsilon);
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(10); // Preserved
    }

    [TestCase]
    public void ClampToValidSpawnZone_Enemy_AtExactBoundary_SnapsToEpsilon()
    {
        var position = new Vector3(0, 0, 0);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 1);

        AssertThat(clamped.X).IsEqual(BattlefieldBounds.SpawnBoundaryEpsilon);
    }

    [TestCase]
    public void ClampToValidSpawnZone_Player_OutsideBounds_ClampsToOuterBounds()
    {
        // Position is on player side (X < 0) but outside battlefield bounds
        var position = new Vector3(-100, 5, 100);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 0);

        // Should be clamped to outer bounds
        AssertThat(clamped.X).IsEqual(BattlefieldBounds.MinX);
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(BattlefieldBounds.MaxZ);
    }

    [TestCase]
    public void ClampToValidSpawnZone_Enemy_OutsideBounds_ClampsToOuterBounds()
    {
        // Position is on enemy side (X > 0) but outside battlefield bounds
        var position = new Vector3(100, 5, -100);

        var clamped = BattlefieldBounds.ClampToValidSpawnZone(position, 1);

        // Should be clamped to outer bounds
        AssertThat(clamped.X).IsEqual(BattlefieldBounds.MaxX);
        AssertThat(clamped.Y).IsEqual(5); // Preserved
        AssertThat(clamped.Z).IsEqual(BattlefieldBounds.MinZ);
    }

    // =========================================================================
    // Constants Verification Tests
    // =========================================================================

    [TestCase]
    public void Constants_HaveCorrectValues()
    {
        AssertThat(BattlefieldBounds.HalfWidth).IsEqual(50.0f);
        AssertThat(BattlefieldBounds.HalfDepth).IsEqual(40.0f);
        AssertThat(BattlefieldBounds.MinX).IsEqual(-50.0f);
        AssertThat(BattlefieldBounds.MaxX).IsEqual(50.0f);
        AssertThat(BattlefieldBounds.MinZ).IsEqual(-40.0f);
        AssertThat(BattlefieldBounds.MaxZ).IsEqual(40.0f);
        AssertThat(BattlefieldBounds.SpawnBoundaryX).IsEqual(0.0f);
        AssertThat(BattlefieldBounds.SpawnBoundaryEpsilon).IsEqual(0.001f);
    }
}
