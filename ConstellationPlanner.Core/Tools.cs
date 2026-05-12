// Copied from RealAntennas/src/RealAntennasProject/Tools.cs (subset).
// Only the dB conversion helpers used by Physics/MathUtils are pulled in.
// Divergences logged in UPSTREAM_DIVERGENCE.md.

namespace ConstellationPlanner.Core
{
    public static class RATools
    {
        public static double LinearScale(double x) => math.pow(10, x / 10);
        public static float LinearScale(float x) => math.pow(10, x / 10);
        public static double LogScale(double x) => 10 * math.log10(x);
        public static float LogScale(float x) => 10 * math.log10(x);
    }
}
