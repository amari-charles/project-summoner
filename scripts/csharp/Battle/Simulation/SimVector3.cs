using System;

namespace Fateforged.Simulation;

/// <summary>
/// Simulation-local 3D vector. Replaces Godot.Vector3 in simulation code
/// to ensure no Godot dependency in the deterministic simulation layer.
/// </summary>
public struct SimVector3 : IEquatable<SimVector3>
{
    public float X;
    public float Y;
    public float Z;

    public SimVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static readonly SimVector3 Zero = new(0, 0, 0);
    public static readonly SimVector3 One = new(1, 1, 1);
    public static readonly SimVector3 Up = new(0, 1, 0);
    public static readonly SimVector3 Down = new(0, -1, 0);
    public static readonly SimVector3 Right = new(1, 0, 0);
    public static readonly SimVector3 Left = new(-1, 0, 0);
    public static readonly SimVector3 Forward = new(0, 0, -1);
    public static readonly SimVector3 Back = new(0, 0, 1);

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public float LengthSquared() => X * X + Y * Y + Z * Z;

    public float DistanceTo(SimVector3 other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        float dz = Z - other.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public float DistanceSquaredTo(SimVector3 other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        float dz = Z - other.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    public SimVector3 Normalized()
    {
        float len = Length();
        if (len < 1e-8f)
            return Zero;
        return new SimVector3(X / len, Y / len, Z / len);
    }

    public float Dot(SimVector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public SimVector3 Cross(SimVector3 other)
    {
        return new SimVector3(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X
        );
    }

    public SimVector3 Lerp(SimVector3 to, float weight)
    {
        return new SimVector3(
            X + (to.X - X) * weight,
            Y + (to.Y - Y) * weight,
            Z + (to.Z - Z) * weight
        );
    }

    // Operators
    public static SimVector3 operator +(SimVector3 a, SimVector3 b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static SimVector3 operator -(SimVector3 a, SimVector3 b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static SimVector3 operator *(SimVector3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);

    public static SimVector3 operator *(float s, SimVector3 v) => new(v.X * s, v.Y * s, v.Z * s);

    public static SimVector3 operator /(SimVector3 v, float s) => new(v.X / s, v.Y / s, v.Z / s);

    public static SimVector3 operator -(SimVector3 v) => new(-v.X, -v.Y, -v.Z);

    public static bool operator ==(SimVector3 a, SimVector3 b) =>
        a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(SimVector3 a, SimVector3 b) => !(a == b);

    public bool Equals(SimVector3 other) => this == other;

    public override bool Equals(object? obj) => obj is SimVector3 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";
}

/// <summary>
/// Math helpers for simulation code, replacing Godot.Mathf usage.
/// </summary>
public static class SimMath
{
    public const float Pi = MathF.PI;
    public const float Tau = MathF.PI * 2f;

    public static float DegToRad(float degrees) => degrees * (MathF.PI / 180f);

    public static float RadToDeg(float radians) => radians * (180f / MathF.PI);

    public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
}
