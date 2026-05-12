using System;

namespace ConstellationPlanner.Core
{
    /// <summary>
    /// Shim mirroring Unity.Mathematics.math entrypoints used by copied RA files.
    /// Lowercase class name preserved on purpose so copied bodies stay byte-identical to upstream.
    /// netstandard2.0 has no MathF, so float overloads cast through Math (double).
    /// </summary>
    internal static class math
    {
        public const double PI = Math.PI;

        // float overloads
        public static float sqrt(float x) => (float)Math.Sqrt(x);
        public static float max(float a, float b) => Math.Max(a, b);
        public static float log10(float x) => (float)Math.Log10(x);
        public static float sin(float x) => (float)Math.Sin(x);
        public static float cos(float x) => (float)Math.Cos(x);
        public static float pow(float x, float y) => (float)Math.Pow(x, y);
        public static float abs(float x) => Math.Abs(x);
        public static float atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float acos(float x) => (float)Math.Acos(x);
        public static float radians(float deg) => deg * (float)Math.PI / 180f;
        public static float degrees(float rad) => rad * 180f / (float)Math.PI;
        public static float remap(float a1, float a2, float b1, float b2, float t)
            => b1 + (b2 - b1) * (t - a1) / (a2 - a1);

        // double overloads
        public static double sqrt(double x) => Math.Sqrt(x);
        public static double max(double a, double b) => Math.Max(a, b);
        public static double log10(double x) => Math.Log10(x);
        public static double sin(double x) => Math.Sin(x);
        public static double cos(double x) => Math.Cos(x);
        public static double pow(double x, double y) => Math.Pow(x, y);
        public static double abs(double x) => Math.Abs(x);
        public static double atan2(double y, double x) => Math.Atan2(y, x);
        public static double acos(double x) => Math.Acos(x);
        public static double radians(double deg) => deg * Math.PI / 180.0;
        public static double degrees(double rad) => rad * 180.0 / Math.PI;
        public static double remap(double a1, double a2, double b1, double b2, double t)
            => b1 + (b2 - b1) * (t - a1) / (a2 - a1);

        // Vec3d (serves as both double3 and float3)
        public static double length(Vec3d v) => v.Magnitude;
    }

    /// <summary>
    /// Shim mirroring UnityEngine.Mathf entrypoints used by copied RA files.
    /// </summary>
    internal static class Mathf
    {
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
