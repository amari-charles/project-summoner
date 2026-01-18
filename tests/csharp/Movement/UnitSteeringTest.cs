namespace ProjectSummoner.Tests.Movement;

using System.Reflection;
using GdUnit4;
using Godot;
using ProjectSummoner.Movement;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for UnitSteering steering throttling behavior.
/// Uses reflection to verify internal throttling state since full steering behavior
/// requires Godot runtime (SpatialGrid, Unit3D nodes).
/// Full integration tests should be run through Godot editor.
/// </summary>
[TestSuite]
public class UnitSteeringTest
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [TestCase]
    public void CalculateSeparationForce_IncrementsFrameCounter_OnEachCall()
    {
        var steering = new UnitSteering();
        var frameCounterField = typeof(UnitSteering).GetField("_frameCounter", PrivateInstance);
        AssertThat(frameCounterField).IsNotNull();

        // Initial value should be 0
        var initialCount = (int)frameCounterField!.GetValue(steering)!;
        AssertThat(initialCount).IsEqual(0);

        // Each call increments the counter (even when SpatialGrid is null)
        steering.CalculateSeparationForce(null!, null);
        AssertThat((int)frameCounterField.GetValue(steering)!).IsEqual(1);

        steering.CalculateSeparationForce(null!, null);
        AssertThat((int)frameCounterField.GetValue(steering)!).IsEqual(2);

        steering.CalculateSeparationForce(null!, null);
        AssertThat((int)frameCounterField.GetValue(steering)!).IsEqual(3);
    }

    [TestCase]
    public void CalculateSeparationForce_TracksLastTargetId()
    {
        var steering = new UnitSteering();
        var lastTargetIdField = typeof(UnitSteering).GetField("_lastTargetId", PrivateInstance);
        AssertThat(lastTargetIdField).IsNotNull();

        // Initial value should be 0 (no target)
        var initialId = (ulong)lastTargetIdField!.GetValue(steering)!;
        AssertThat(initialId).IsEqual(0UL);

        // Calling with null target keeps ID at 0
        steering.CalculateSeparationForce(null!, null);
        AssertThat((ulong)lastTargetIdField.GetValue(steering)!).IsEqual(0UL);
    }

    [TestCase]
    public void Reset_ClearsCachedValues()
    {
        var steering = new UnitSteering();
        var cachedSeparationField = typeof(UnitSteering).GetField("_cachedSeparation", PrivateInstance);
        var frameCounterField = typeof(UnitSteering).GetField("_frameCounter", PrivateInstance);
        var lastTargetIdField = typeof(UnitSteering).GetField("_lastTargetId", PrivateInstance);

        AssertThat(cachedSeparationField).IsNotNull();
        AssertThat(frameCounterField).IsNotNull();
        AssertThat(lastTargetIdField).IsNotNull();

        // Set non-default values via reflection
        cachedSeparationField!.SetValue(steering, new Vector3(1, 2, 3));
        frameCounterField!.SetValue(steering, 42);
        lastTargetIdField!.SetValue(steering, 12345UL);

        // Verify values are set
        AssertThat((Vector3)cachedSeparationField.GetValue(steering)!).IsEqual(new Vector3(1, 2, 3));
        AssertThat((int)frameCounterField.GetValue(steering)!).IsEqual(42);
        AssertThat((ulong)lastTargetIdField.GetValue(steering)!).IsEqual(12345UL);

        // Reset should clear all throttling state
        steering.Reset();

        AssertThat((Vector3)cachedSeparationField.GetValue(steering)!).IsEqual(Vector3.Zero);
        AssertThat((int)frameCounterField.GetValue(steering)!).IsEqual(0);
        AssertThat((ulong)lastTargetIdField.GetValue(steering)!).IsEqual(0UL);
    }

    [TestCase]
    public void UnitSteering_HasThrottlingConstants()
    {
        // Verify the steering update interval constant exists
        var intervalField = typeof(UnitSteering).GetField(
            "SteeringUpdateInterval",
            BindingFlags.NonPublic | BindingFlags.Static);
        AssertThat(intervalField).IsNotNull();

        // Should update every 3 frames (performance optimization)
        var interval = (int)intervalField!.GetValue(null)!;
        AssertThat(interval).IsEqual(3);
    }

    [TestCase]
    public void UnitSteering_HasExpectedPublicMethods()
    {
        var type = typeof(UnitSteering);

        AssertThat(type.GetMethod("CalculateSeparationForce")).IsNotNull();
        AssertThat(type.GetMethod("CalculateFlankForce")).IsNotNull();
        AssertThat(type.GetMethod("CorrectOverlaps")).IsNotNull();
        AssertThat(type.GetMethod("UpdateBlockedState")).IsNotNull();
        AssertThat(type.GetMethod("Reset")).IsNotNull();
    }
}
