using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>One inter-satellite link evaluated at a snapshot. <see cref="AntennaA"/> /
    /// <see cref="AntennaB"/> are the indices into the ISL antenna list chosen at each end
    /// (best-pointing pair).</summary>
    public readonly struct Isl
    {
        public readonly int A;
        public readonly int B;
        public readonly int AntennaA;
        public readonly int AntennaB;
        public readonly double DistanceM;
        public readonly double RxPowerDbm;

        public Isl(int a, int b, int antA, int antB, double distM, double rxDbm)
        { A = a; B = b; AntennaA = antA; AntennaB = antB; DistanceM = distM; RxPowerDbm = rxDbm; }
    }

    public static class IslAnalysis
    {
        /// <summary>Find inter-satellite links from a snapshot. ISL antennas are
        /// fixed-pointed in the orbit-local LVLH frame; both ends incur pointing loss based
        /// on their chosen antenna's boresight. We pick the (antA, antB) pair that maximises
        /// rx-power. <paramref name="antennaBoresightsBf"/>[si, ai] is the body-fixed boresight
        /// of antenna ai on sat si.
        ///
        /// When <paramref name="islTargets"/> is supplied (shape [N, A]), enumeration is
        /// restricted to per-sat target locks: <c>islTargets[i, a] = j</c> means antenna a on
        /// sat i is aimed at sat j. The reciprocal antenna on j must also point at i for the
        /// link to be reported. <c>-1</c> entries are unused antenna slots.</summary>
        public static List<Isl> FindLinks(IList<Vec3d> satPositions,
                                           IList<SatAntenna> islAntennas,
                                           Vec3d[,] antennaBoresightsBf,
                                           double bodyRadius,
                                           double atmosphereMarginM = 50_000,
                                           double minRxPowerDbm = double.NegativeInfinity,
                                           int[,]? islTargets = null)
        {
            var links = new List<Isl>();
            if (islAntennas.Count == 0) return links;
            int A = islAntennas.Count;
            double minClearance = bodyRadius + atmosphereMarginM;
            double minClearanceSq = minClearance * minClearance;

            void EvalPair(int i, int j, int fixedAa, int fixedBb)
            {
                Vec3d a = satPositions[i];
                Vec3d b = satPositions[j];
                Vec3d d = b - a;
                double dd = Vec3d.Dot(d, d);
                if (dd < 1e-6) return;
                double tStar = -Vec3d.Dot(a, d) / dd;
                Vec3d closest;
                if (tStar <= 0) closest = a;
                else if (tStar >= 1) closest = b;
                else closest = a + tStar * d;
                if (closest.SqrMagnitude < minClearanceSq) return;

                double dist = System.Math.Sqrt(dd);
                int bestAntA = -1, bestAntB = -1;
                double bestRx = double.NegativeInfinity;
                int aaLo = fixedAa >= 0 ? fixedAa : 0;
                int aaHi = fixedAa >= 0 ? fixedAa + 1 : A;
                int bbLo = fixedBb >= 0 ? fixedBb : 0;
                int bbHi = fixedBb >= 0 ? fixedBb + 1 : A;
                for (int aa = aaLo; aa < aaHi; aa++)
                {
                    var antA = islAntennas[aa];
                    var budgetA = antA.Budget;
                    double plA = antA.IsOmnidirectional
                        ? 0
                        : AntennaPointing.PointingLossDb(a, antennaBoresightsBf[i, aa], b, budgetA.BeamwidthDeg);
                    for (int bb = bbLo; bb < bbHi; bb++)
                    {
                        var antB = islAntennas[bb];
                        var budgetB = antB.Budget;
                        double plB = antB.IsOmnidirectional
                            ? 0
                            : AntennaPointing.PointingLossDb(b, antennaBoresightsBf[j, bb], a, budgetB.BeamwidthDeg);
                        double rx = budgetA.TxPowerDbm + budgetA.TxGainDbi + budgetB.RxGainDbi
                                  - (float)Physics.PathLoss(dist, budgetA.FrequencyHz)
                                  - plA - plB;
                        if (rx > bestRx) { bestRx = rx; bestAntA = aa; bestAntB = bb; }
                    }
                }
                if (bestRx < minRxPowerDbm) return;
                links.Add(new Isl(i, j, bestAntA, bestAntB, dist, bestRx));
            }

            int N = satPositions.Count;
            if (islTargets != null)
            {
                for (int i = 0; i < N; i++)
                {
                    for (int a = 0; a < A; a++)
                    {
                        int j = islTargets[i, a];
                        if (j < 0 || j == i || j < i) continue;
                        int b = -1;
                        for (int bb = 0; bb < A; bb++)
                            if (islTargets[j, bb] == i) { b = bb; break; }
                        if (b < 0) continue;
                        EvalPair(i, j, a, b);
                    }
                }
            }
            else
            {
                for (int i = 0; i < N; i++)
                    for (int j = i + 1; j < N; j++)
                        EvalPair(i, j, -1, -1);
            }
            return links;
        }
    }
}
