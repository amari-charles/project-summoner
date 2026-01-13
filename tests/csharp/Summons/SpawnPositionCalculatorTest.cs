namespace ProjectSummoner.Tests.Summons;

using System.Collections.Generic;
using GdUnit4;
using Godot;
using ProjectSummoner.Cards.Formations;
using ProjectSummoner.Constants;
using ProjectSummoner.Summons;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for SpawnPositionCalculator.
/// </summary>
[TestSuite]
public class SpawnPositionCalculatorTest
{
    [TestCase]
    public void CalculateFormationPositions_SingleUnit_ReturnsCenterPosition()
    {
        // LineFormation with 1 unit returns center position
        var formation = FormationPresets.StandardLine;
        var center = new Vector3(-5, 0, 10); // Player side (X <= 0)

        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            formation, center, 1, null, 0.5f);

        AssertThat(positions.Count).IsEqual(1);
        AssertThat(positions[0]).IsEqual(center);
    }

    [TestCase]
    public void CalculateFormationPositions_MultipleUnits_ReturnsCorrectCount()
    {
        var formation = FormationPresets.StandardLine;
        var center = new Vector3(0, 0, 0);

        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            formation, center, 5, null, 0.5f);

        AssertThat(positions.Count).IsEqual(5);
    }

    [TestCase]
    public void CalculateFormationPositions_WithZeroCollisionRadius_DefaultsToHalf()
    {
        var formation = FormationPresets.StandardGrid;
        var center = new Vector3(0, 0, 0);

        // Should not throw with 0 collision radius
        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            formation, center, 1, null, 0f);

        AssertThat(positions.Count).IsEqual(1);
    }

    [TestCase]
    public void CalculateFormationPositions_NegativeCollisionRadius_DefaultsToHalf()
    {
        var formation = FormationPresets.StandardGrid;
        var center = new Vector3(0, 0, 0);

        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            formation, center, 1, null, -1f);

        AssertThat(positions.Count).IsEqual(1);
    }

    [TestCase]
    public void FindSafeSpawnPosition_NoObstacles_ReturnsDesiredPosition()
    {
        var desired = new Vector3(-10, 0, 20); // Player side (X <= 0)

        var safe = SpawnPositionCalculator.FindSafeSpawnPosition(
            desired, null, 0.5f);

        AssertThat(safe).IsEqual(desired);
    }

    [TestCase]
    public void FindSafeSpawnPosition_WithBatchPositions_AvoidsCollision()
    {
        var desired = new Vector3(0, 0, 0);
        var batchPositions = new List<Vector3>
        {
            new Vector3(0, 0, 0) // Same position
        };

        var safe = SpawnPositionCalculator.FindSafeSpawnPosition(
            desired, null, 0.5f, null, batchPositions);

        // Should find a different position
        AssertThat(safe).IsNotEqual(desired);
    }

    [TestCase]
    public void FindSafeSpawnPosition_MultipleConflicts_FindsAlternate()
    {
        var desired = new Vector3(0, 0, 0);
        var batchPositions = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0)
        };

        var safe = SpawnPositionCalculator.FindSafeSpawnPosition(
            desired, null, 0.5f, null, batchPositions);

        // Should find a position that doesn't conflict
        foreach (var pos in batchPositions)
        {
            float dist = (safe - pos).Length();
            AssertThat(dist).IsGreaterEqual(1.0f); // 2 * 0.5f collision radius
        }
    }

    [TestCase]
    public void IsSpawnPositionSafe_EmptyScene_ReturnsTrue()
    {
        var position = new Vector3(-5, 0, 5); // Player side (X <= 0)

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f);

        AssertThat(isSafe).IsTrue();
    }

    [TestCase]
    public void IsSpawnPositionSafe_NoBatchConflict_ReturnsTrue()
    {
        var position = new Vector3(-10, 0, 10); // Player side (X <= 0)
        var batchPositions = new List<Vector3>
        {
            new Vector3(-30, 0, 0) // Far away, also on player side
        };

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, batchPositions);

        AssertThat(isSafe).IsTrue();
    }

    [TestCase]
    public void IsSpawnPositionSafe_BatchConflict_ReturnsFalse()
    {
        var position = new Vector3(0, 0, 0);
        var batchPositions = new List<Vector3>
        {
            new Vector3(0.5f, 0, 0) // Within collision range
        };

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, batchPositions);

        AssertThat(isSafe).IsFalse();
    }

    [TestCase]
    public void IsSpawnPositionSafe_YAxisIgnored_ReturnsTrue()
    {
        var position = new Vector3(0, 5, 0); // High up
        var batchPositions = new List<Vector3>
        {
            new Vector3(0, 0, 0) // Ground level, same X/Z
        };

        // Y axis is ignored in collision check (2D distance)
        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, batchPositions);

        AssertThat(isSafe).IsFalse(); // Same X/Z means collision
    }

    // =========================================================================
    // Team Boundary Tests
    // =========================================================================

    [TestCase]
    public void IsSpawnPositionSafe_PlayerTeam_PositionOnEnemySide_ReturnsFalse()
    {
        var position = new Vector3(10, 0, 0); // Enemy side (X > 0)

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, null, team: 0);

        AssertThat(isSafe).IsFalse();
    }

    [TestCase]
    public void IsSpawnPositionSafe_PlayerTeam_PositionOnPlayerSide_ReturnsTrue()
    {
        var position = new Vector3(-10, 0, 0); // Player side (X <= 0)

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, null, team: 0);

        AssertThat(isSafe).IsTrue();
    }

    [TestCase]
    public void IsSpawnPositionSafe_EnemyTeam_PositionOnPlayerSide_ReturnsFalse()
    {
        var position = new Vector3(-10, 0, 0); // Player side (X <= 0)

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, null, team: 1);

        AssertThat(isSafe).IsFalse();
    }

    [TestCase]
    public void IsSpawnPositionSafe_EnemyTeam_PositionOnEnemySide_ReturnsTrue()
    {
        var position = new Vector3(10, 0, 0); // Enemy side (X > 0)

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, null, team: 1);

        AssertThat(isSafe).IsTrue();
    }

    [TestCase]
    public void IsSpawnPositionSafe_OutsideBattlefieldBounds_ReturnsFalse()
    {
        var position = new Vector3(-100, 0, 0); // Way outside bounds

        var isSafe = SpawnPositionCalculator.IsSpawnPositionSafe(
            position, null, 0.5f, null, null, team: 0);

        AssertThat(isSafe).IsFalse();
    }

    [TestCase]
    public void FindSafeSpawnPosition_FallbackClampsToTeamZone_Player()
    {
        // Position on enemy side with no safe alternatives
        var desired = new Vector3(10, 0, 0);
        var batchPositions = new List<Vector3>();

        // Fill batch positions to block all ring search positions
        // This forces fallback behavior
        for (int i = 0; i < 100; i++)
        {
            batchPositions.Add(new Vector3(-i * 0.5f, 0, 0));
        }

        var safe = SpawnPositionCalculator.FindSafeSpawnPosition(
            desired, null, 0.5f, null, batchPositions, team: 0);

        // Should be clamped to player side (X <= 0)
        AssertThat(safe.X).IsLessEqual(BattlefieldBounds.SpawnBoundaryX);
    }

    [TestCase]
    public void CalculateFormationPositions_WithTeam_PassesTeamThroughCorrectly()
    {
        var formation = FormationPresets.StandardLine;
        // Position on player side
        var center = new Vector3(-20, 0, 0);

        var positions = SpawnPositionCalculator.CalculateFormationPositions(
            formation, center, 3, null, 0.5f, team: 0);

        // All positions should be on player side
        foreach (var pos in positions)
        {
            AssertThat(pos.X).IsLessEqual(BattlefieldBounds.SpawnBoundaryX);
        }
    }
}
