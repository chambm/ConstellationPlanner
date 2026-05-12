using System;

namespace ConstellationPlanner.Core;

/// <summary>
/// Replacement for Unity's Vector3d in copied RealAntennas math.
/// Pure math, no Unity surface — keeps Core referenceable from non-Unity hosts.
/// </summary>
public readonly struct Vec3d : IEquatable<Vec3d>
{
    public readonly double X, Y, Z;

    public Vec3d(double x, double y, double z) { X = x; Y = y; Z = z; }

    public static Vec3d Zero => default;

    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);
    public double SqrMagnitude => X * X + Y * Y + Z * Z;

    public Vec3d Normalized
    {
        get
        {
            var m = Magnitude;
            return m > 0 ? new Vec3d(X / m, Y / m, Z / m) : Zero;
        }
    }

    public static Vec3d operator +(Vec3d a, Vec3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3d operator -(Vec3d a, Vec3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3d operator -(Vec3d a) => new(-a.X, -a.Y, -a.Z);
    public static Vec3d operator *(Vec3d a, double s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vec3d operator *(double s, Vec3d a) => a * s;
    public static Vec3d operator /(Vec3d a, double s) => new(a.X / s, a.Y / s, a.Z / s);

    public static double Dot(Vec3d a, Vec3d b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static Vec3d Cross(Vec3d a, Vec3d b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public static double Distance(Vec3d a, Vec3d b) => (a - b).Magnitude;
    public static double Angle(Vec3d a, Vec3d b)
    {
        var d = Dot(a.Normalized, b.Normalized);
        if (d > 1.0) d = 1.0; else if (d < -1.0) d = -1.0;
        return Math.Acos(d);
    }

    public bool Equals(Vec3d other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Vec3d v && Equals(v);
    public override int GetHashCode()
    {
        unchecked
        {
            var h = X.GetHashCode();
            h = (h * 397) ^ Y.GetHashCode();
            h = (h * 397) ^ Z.GetHashCode();
            return h;
        }
    }
    public override string ToString() => $"({X:G6}, {Y:G6}, {Z:G6})";
}
