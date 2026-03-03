namespace Fateforged.Tests.Projectiles;

using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
/// Tests for projectile acceleration/deceleration clamping behavior.
/// Pure math tests: speed + acceleration * delta, clamped to MinSpeed.
/// </summary>
[TestSuite]
public class ProjectileAccelerationTest
{
    private static float ApplyAcceleration(float currentSpeed, float acceleration, float minSpeed, float delta)
    {
        currentSpeed += acceleration * delta;
        if (acceleration < 0f)
            currentSpeed = Godot.Mathf.Max(currentSpeed, minSpeed);
        return currentSpeed;
    }

    [TestCase]
    public void NegativeAcceleration_DecreasesSpeed()
    {
        float speed = ApplyAcceleration(
            currentSpeed: 20f, acceleration: -10f, minSpeed: 5f, delta: 1f);

        AssertThat(speed).IsEqual(10f);
    }

    [TestCase]
    public void Speed_ClampsToMinSpeed()
    {
        // 3 seconds at -10/s would go to -10 without clamping
        float speed = ApplyAcceleration(
            currentSpeed: 20f, acceleration: -10f, minSpeed: 5f, delta: 3f);

        AssertThat(speed).IsEqual(5f);
    }

    [TestCase]
    public void Speed_NeverGoesBelowMinSpeed()
    {
        // Very aggressive deceleration
        float speed = ApplyAcceleration(
            currentSpeed: 10f, acceleration: -100f, minSpeed: 3f, delta: 1f);

        AssertThat(speed).IsEqual(3f);
    }

    [TestCase]
    public void PositiveAcceleration_IncreasesSpeed()
    {
        float currentSpeed = 10f;
        float acceleration = 5f;
        float delta = 2f;

        currentSpeed += acceleration * delta;

        AssertThat(currentSpeed).IsEqual(20f);
    }

    [TestCase]
    public void ZeroAcceleration_MaintainsSpeed()
    {
        float currentSpeed = 15f;
        float acceleration = 0f;
        float delta = 5f;

        if (acceleration != 0f)
        {
            currentSpeed += acceleration * delta;
            if (acceleration < 0f)
                currentSpeed = Godot.Mathf.Max(currentSpeed, 1f);
        }

        AssertThat(currentSpeed).IsEqual(15f);
    }

    [TestCase]
    public void IncrementalDeceleration_OverMultipleFrames()
    {
        float currentSpeed = 25f;
        float acceleration = -12f;
        float minSpeed = 5f;
        float delta = 0.1f;
        int iterations = 10;

        for (int i = 0; i < iterations; i++)
        {
            currentSpeed = ApplyAcceleration(currentSpeed, acceleration, minSpeed, delta);
        }

        // After 1 second at -12/s: 25 - 12 = 13
        AssertThat(Godot.Mathf.Abs(currentSpeed - 13f) < 0.01f).IsTrue();
    }

    [TestCase]
    public void WindPuffConfig_ReachesMinSpeedAndClamps()
    {
        float initialSpeed = 25f;
        float acceleration = -12f;
        float minSpeed = 5f;

        // Time to reach MinSpeed: (25 - 5) / 12 = 1.67s
        float timeToMin = (initialSpeed - minSpeed) / Godot.Mathf.Abs(acceleration);
        AssertThat(Godot.Mathf.Abs(timeToMin - 1.67f) < 0.01f).IsTrue();

        // Simulate 2 seconds (beyond time to reach min)
        float speed = ApplyAcceleration(initialSpeed, acceleration, minSpeed, delta: 2f);

        AssertThat(speed).IsEqual(5f);
    }
}
