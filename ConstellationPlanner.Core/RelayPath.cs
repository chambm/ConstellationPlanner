using System;
using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>One hop in a relay path. <see cref="FromAnt"/>/<see cref="ToAnt"/> identify
    /// the specific antennas on each end so callers can credit per-antenna usage in a
    /// <see cref="NetworkUsage"/> after the path resolves.</summary>
    public readonly struct RelayHop
    {
        public readonly bool FromIsStation;
        public readonly int FromIdx;
        public readonly bool ToIsStation;
        public readonly int ToIdx;
        public readonly double DistanceM;
        public readonly double RxPowerDbm;
        public readonly double MaxDataRateBps;
        public readonly AntennaKey FromAnt;
        public readonly AntennaKey ToAnt;

        public RelayHop(bool fromIsStation, int fromIdx, bool toIsStation, int toIdx,
                        double dist, double rxDbm, double maxRateBps,
                        AntennaKey fromAnt, AntennaKey toAnt)
        {
            FromIsStation = fromIsStation; FromIdx = fromIdx;
            ToIsStation = toIsStation; ToIdx = toIdx;
            DistanceM = dist; RxPowerDbm = rxDbm; MaxDataRateBps = maxRateBps;
            FromAnt = fromAnt; ToAnt = toAnt;
        }
    }

    public sealed class RelayPathResult
    {
        public bool Connected;
        public List<RelayHop> Hops = new List<RelayHop>();
        public double BottleneckRxPowerDbm = double.NegativeInfinity;
        public double BottleneckDataRateBps;
        public double TotalDistanceM;
        public double TotalLatencySec;
    }

    public static class Relay
    {
        const double SpeedOfLight = 299_792_458.0;

        /// <summary>Find a relay path between two ground stations, matching Skopos's
        /// algorithm: Dijkstra over the {stations + sats} graph weighted by Euclidean
        /// distance, with a latency cap (= distance / c) and a per-link data-rate filter.
        /// Stations and sat positions must be in the same body-fixed frame.
        ///
        /// Multi-antenna: each sat carries an array of ground antennas (used for sat↔ground
        /// hops) and an array of ISL antennas (used for sat↔sat hops). For each candidate
        /// hop we pick the antenna(s) that maximise rx-power. ISL hops are skipped entirely
        /// when <paramref name="islAntennas"/> is empty.
        ///
        /// When <paramref name="islTargets"/> is supplied (shape [N, Ai]), ISL edges are
        /// restricted to per-sat target assignments: <c>islTargets[i, a] = j</c> means sat
        /// i's antenna a is locked to track sat j (KSP "lock to vessel" mode). The reciprocal
        /// antenna on sat j must also target i for the link to form. Each ISL antenna is
        /// expected to be marked <c>IsOmnidirectional=true</c> so its perfect-tracking gain
        /// applies on-axis without a pointing-loss penalty. <c>-1</c> entries mean "no target
        /// — antenna unused." When null, the all-pairs visibility scan runs as before.
        ///
        /// When <paramref name="usage"/> is supplied, the rate filter switches from raw
        /// achievable rate to <see cref="NetworkUsage.CapacityWithUsage"/> — i.e. the
        /// remaining capacity on each link after accounting for already-routed connections'
        /// tx-power and spectrum claims (Skopos's <c>CapacityWithUsage</c>). On a successful
        /// path the routed connection's usage is automatically accrued: each hop's tx antenna
        /// takes <c>data_rate / max_data_rate</c> tx-power fraction and <c>data_rate /
        /// bits_per_symbol</c> Hz of spectrum; rx antenna takes spectrum only.</summary>
        public static RelayPathResult Find(int fromStationIdx, int toStationIdx,
                                            IList<GroundStation> stations,
                                            IList<Vec3d> satPositionsBodyFixed,
                                            IList<SatAntenna> groundAntennas,
                                            Vec3d[,] groundBoresightsBf,
                                            IList<SatAntenna> islAntennas,
                                            Vec3d[,] islBoresightsBf,
                                            double bodyRadius,
                                            double atmosphereMarginM,
                                            double minElevDeg,
                                            double latencyLimitSec,
                                            double requiredDataRateBps,
                                            int[,]? islTargets = null,
                                            NetworkUsage? usage = null)
        {
            // Skopos role gating: rx-only stations can't transmit (so they can't be the source
            // of a connection); tx-only stations can't receive (so they can't be the destination).
            // Mirrors Skopos's `rx_only_`/`tx_only_` checks in Routing.FindChannels.
            if (stations[fromStationIdx].Role == StationRole.RxOnly
                || stations[toStationIdx].Role == StationRole.TxOnly)
            {
                return new RelayPathResult(); // Connected = false
            }

            int N = satPositionsBodyFixed.Count;
            int FROM = N, TO = N + 1;
            int total = N + 2;
            // Treat 0 or negative as "no limit" — a literal 0 cap means no path can ever fit
            // (the very first Dijkstra check would break immediately), which is almost always
            // the user typing 0 to mean "I don't care" rather than "must be instantaneous."
            double maxTotalDistance = (double.IsPositiveInfinity(latencyLimitSec) || latencyLimitSec <= 0)
                ? double.PositiveInfinity : latencyLimitSec * SpeedOfLight;

            // adj[u] = list of (neighbour v, distance, rx-power for u→v direction, max rate
            // for u→v, tx antenna at u, rx antenna at v, link budget for u→v). The budget is
            // kept so that on path success we can credit usage with the right modulation /
            // bandwidth for the data-rate-to-spectrum conversion.
            var adj = new List<(int To, double Dist, double Rx, double Rate, AntennaKey TxAnt, AntennaKey RxAnt, LinkBudget Budget)>[total];
            for (int i = 0; i < total; i++) adj[i] = new();

            double sinMinElev = Math.Sin(minElevDeg * Math.PI / 180.0);
            int Ag = groundAntennas.Count;
            int Ai = islAntennas.Count;

            // Per-link rate using the picked antenna's budget (mirrors LinkBudget.MaxDataRateBps
            // halvings logic but starts from a realised rx-power instead of recomputing).
            static double RateFromRx(double rxDbm, LinkBudget b)
            {
                if (b.BandwidthHz <= 0) return double.PositiveInfinity;
                double snrDb = rxDbm - b.NoiseFloorDbm;
                double specEff = b.MaxBitsPerSymbol * b.CodingRate;
                double ebn0Db = snrDb - 10.0 * Math.Log10(specEff);
                double maxRate = b.BandwidthHz * specEff;
                if (ebn0Db >= b.RequiredEbN0Db) return maxRate;
                double shortfallDb = b.RequiredEbN0Db - ebn0Db;
                int halvings = (int)Math.Ceiling(shortfallDb / 3.0);
                if (halvings >= 32) return 0;
                return maxRate / Math.Pow(2, halvings);
            }

            // Common edge-eligibility test: rate filter (no usage) or capacity-with-usage filter
            // (with usage). Returns true when the link can carry requiredDataRateBps.
            bool LinkPasses(double maxRate, LinkBudget b, AntennaKey txAnt, AntennaKey rxAnt)
            {
                if (usage == null) return maxRate >= requiredDataRateBps;
                double cap = usage.CapacityWithUsage(maxRate, b, txAnt, rxAnt);
                return cap >= requiredDataRateBps;
            }

            void TryStationToSat(int stationNode, int stationIdx, GroundStation st)
            {
                if (Ag == 0) return;
                Vec3d stPos = st.Position(bodyRadius);
                Vec3d normal = stPos.Normalized;
                AntennaKey stAnt = AntennaKey.Station(stationIdx);
                for (int k = 0; k < N; k++)
                {
                    Vec3d satPos = satPositionsBodyFixed[k];
                    Vec3d toSat = satPos - stPos;
                    double dist = toSat.Magnitude;
                    if (dist <= 0) continue;
                    double sinElev = Vec3d.Dot(normal, toSat) / dist;
                    if (sinElev < sinMinElev) continue;
                    double bestRx = double.NegativeInfinity;
                    LinkBudget bestBudget = default;
                    int bestA = -1;
                    for (int a = 0; a < Ag; a++)
                    {
                        var ant = groundAntennas[a];
                        var b = ant.Budget;
                        double rx = b.RxPowerDbm(dist);
                        if (!ant.IsOmnidirectional)
                            rx -= AntennaPointing.PointingLossDb(satPos, groundBoresightsBf[k, a],
                                                                  stPos, b.BeamwidthDeg);
                        if (rx > bestRx) { bestRx = rx; bestBudget = b; bestA = a; }
                    }
                    if (bestA < 0) continue;
                    double rate = RateFromRx(bestRx, bestBudget);
                    AntennaKey satAnt = AntennaKey.SatGround(k, bestA);
                    // Direction station→sat: tx = station antenna, rx = sat antenna.
                    if (LinkPasses(rate, bestBudget, stAnt, satAnt))
                        adj[stationNode].Add((k, dist, bestRx, rate, stAnt, satAnt, bestBudget));
                    // Direction sat→station: tx = sat antenna, rx = station antenna. Capacity
                    // check uses the swapped tx/rx since spectrum/power are tracked per antenna.
                    if (LinkPasses(rate, bestBudget, satAnt, stAnt))
                        adj[k].Add((stationNode, dist, bestRx, rate, satAnt, stAnt, bestBudget));
                }
            }
            TryStationToSat(FROM, fromStationIdx, stations[fromStationIdx]);
            TryStationToSat(TO,   toStationIdx,   stations[toStationIdx]);

            if (Ai > 0)
            {
                double minClearance = bodyRadius + atmosphereMarginM;
                double minClearanceSq = minClearance * minClearance;

                // Inner-loop helper: evaluate one (i, j) sat-pair given a body-fixed positions and
                // either a fixed antenna pair (Targeted mode) or an open antenna scan (legacy).
                // Adds the bidirectional edge to adj when LoS clears and capacity ≥ required.
                void EvalIslEdge(int i, int j, int fixedAa, int fixedBb)
                {
                    Vec3d ap = satPositionsBodyFixed[i];
                    Vec3d bp = satPositionsBodyFixed[j];
                    Vec3d d = bp - ap;
                    double dd = Vec3d.Dot(d, d);
                    if (dd < 1e-6) return;
                    double tStar = -Vec3d.Dot(ap, d) / dd;
                    Vec3d closest;
                    if (tStar <= 0) closest = ap;
                    else if (tStar >= 1) closest = bp;
                    else closest = ap + tStar * d;
                    if (closest.SqrMagnitude < minClearanceSq) return;
                    double dist = Math.Sqrt(dd);

                    double bestRx = double.NegativeInfinity;
                    LinkBudget bestBudget = default;
                    int bestAa = -1, bestBb = -1;
                    int aaLo = fixedAa >= 0 ? fixedAa : 0;
                    int aaHi = fixedAa >= 0 ? fixedAa + 1 : Ai;
                    int bbLo = fixedBb >= 0 ? fixedBb : 0;
                    int bbHi = fixedBb >= 0 ? fixedBb + 1 : Ai;
                    for (int aa = aaLo; aa < aaHi; aa++)
                    {
                        var antA = islAntennas[aa];
                        var ba = antA.Budget;
                        double plA = antA.IsOmnidirectional
                            ? 0
                            : AntennaPointing.PointingLossDb(ap, islBoresightsBf[i, aa], bp, ba.BeamwidthDeg);
                        for (int bb = bbLo; bb < bbHi; bb++)
                        {
                            var antB = islAntennas[bb];
                            var bbud = antB.Budget;
                            double plB = antB.IsOmnidirectional
                                ? 0
                                : AntennaPointing.PointingLossDb(bp, islBoresightsBf[j, bb], ap, bbud.BeamwidthDeg);
                            double rx = ba.TxPowerDbm + ba.TxGainDbi + bbud.RxGainDbi
                                      - (float)Physics.PathLoss(dist, ba.FrequencyHz)
                                      - plA - plB;
                            if (rx > bestRx) { bestRx = rx; bestBudget = ba; bestAa = aa; bestBb = bb; }
                        }
                    }
                    if (bestAa < 0) return;
                    double rate = RateFromRx(bestRx, bestBudget);
                    AntennaKey antI = AntennaKey.SatIsl(i, bestAa);
                    AntennaKey antJ = AntennaKey.SatIsl(j, bestBb);
                    if (LinkPasses(rate, bestBudget, antI, antJ))
                        adj[i].Add((j, dist, bestRx, rate, antI, antJ, bestBudget));
                    if (LinkPasses(rate, bestBudget, antJ, antI))
                        adj[j].Add((i, dist, bestRx, rate, antJ, antI, bestBudget));
                }

                if (islTargets != null)
                {
                    // Targeted: each sat's antenna a is locked to islTargets[i, a]. The link only
                    // forms when the partner sat has a reciprocal antenna locked back at i.
                    for (int i = 0; i < N; i++)
                    {
                        for (int a = 0; a < Ai; a++)
                        {
                            int j = islTargets[i, a];
                            if (j < 0 || j == i || j < i) continue;   // dedupe undirected pair: only when i < j
                            int b = -1;
                            for (int bb = 0; bb < Ai; bb++)
                                if (islTargets[j, bb] == i) { b = bb; break; }
                            if (b < 0) continue;                       // no reciprocal lock → no link
                            EvalIslEdge(i, j, a, b);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < N; i++)
                        for (int j = i + 1; j < N; j++)
                            EvalIslEdge(i, j, -1, -1);
                }
            }

            // Dijkstra by total Euclidean distance — same metric as Skopos's FindChannels.
            var dist_ = new double[total];
            var prev = new int[total];
            var prevDist = new double[total];
            var prevRx = new double[total];
            var prevRate = new double[total];
            var prevTxAnt = new AntennaKey[total];
            var prevRxAnt = new AntennaKey[total];
            var prevBudget = new LinkBudget[total];
            for (int i = 0; i < total; i++) { dist_[i] = double.PositiveInfinity; prev[i] = -1; }
            dist_[FROM] = 0;

            var heap = new SortedSet<(double Pri, int Node)>(Comparer<(double, int)>.Create((a, b) =>
            {
                int c = a.Item1.CompareTo(b.Item1);
                return c != 0 ? c : a.Item2.CompareTo(b.Item2);
            }));
            heap.Add((0.0, FROM));

            while (heap.Count > 0)
            {
                var top = heap.Min;
                heap.Remove(top);
                int u = top.Node;
                double du = top.Pri;
                if (du != dist_[u]) continue;
                if (u == TO) break;
                if (du > maxTotalDistance) break;
                foreach (var (v, edgeDist, rx, rate, txAnt, rxAnt, budget) in adj[u])
                {
                    double nd = du + edgeDist;
                    if (nd > maxTotalDistance) continue;
                    if (nd < dist_[v])
                    {
                        if (!double.IsPositiveInfinity(dist_[v]))
                            heap.Remove((dist_[v], v));
                        dist_[v] = nd;
                        prev[v] = u;
                        prevDist[v] = edgeDist;
                        prevRx[v] = rx;
                        prevRate[v] = rate;
                        prevTxAnt[v] = txAnt;
                        prevRxAnt[v] = rxAnt;
                        prevBudget[v] = budget;
                        heap.Add((nd, v));
                    }
                }
            }

            var result = new RelayPathResult();
            if (double.IsPositiveInfinity(dist_[TO])) return result;

            result.Connected = true;
            result.TotalDistanceM = dist_[TO];
            result.TotalLatencySec = dist_[TO] / SpeedOfLight;
            double minRx = double.PositiveInfinity;
            double minRate = double.PositiveInfinity;

            var nodes = new List<int>();
            for (int u = TO; u != -1; u = prev[u]) nodes.Add(u);
            nodes.Reverse();
            for (int k = 1; k < nodes.Count; k++)
            {
                int u = nodes[k - 1], v = nodes[k];
                bool uIsStation = (u == FROM || u == TO);
                bool vIsStation = (v == FROM || v == TO);
                int uIdx = uIsStation ? (u == FROM ? fromStationIdx : toStationIdx) : u;
                int vIdx = vIsStation ? (v == FROM ? fromStationIdx : toStationIdx) : v;
                result.Hops.Add(new RelayHop(uIsStation, uIdx, vIsStation, vIdx,
                                              prevDist[v], prevRx[v], prevRate[v],
                                              prevTxAnt[v], prevRxAnt[v]));
                if (prevRx[v] < minRx) minRx = prevRx[v];
                if (prevRate[v] < minRate) minRate = prevRate[v];

                // Credit this hop's tx/rx antennas to the shared usage state so subsequent
                // connections see depleted capacity on shared antennas. Skopos's Routing does
                // the same in UseLinks after FindAndUseAvailableChannels.
                if (usage != null && requiredDataRateBps > 0)
                    usage.UseLink(requiredDataRateBps, prevRate[v], prevBudget[v],
                                   prevTxAnt[v], prevRxAnt[v]);
            }
            result.BottleneckRxPowerDbm = minRx;
            result.BottleneckDataRateBps = minRate;
            return result;
        }
    }
}
