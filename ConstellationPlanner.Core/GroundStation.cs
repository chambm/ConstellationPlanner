using System;
using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>Source / role of a ground station for visualization purposes.</summary>
    public enum StationKind
    {
        /// <summary>RA / RSS DSN-style tracking station (DSN trio, NIPs, ASF, KSAT etc).</summary>
        Tracking,
        /// <summary>Skopos telecom-network station (commercial earth-stations).</summary>
        Telecom,
        /// <summary>User-supplied / unknown.</summary>
        Other,
    }

    /// <summary>Skopos station role: full duplex (default), transmit-only (e.g., uplink TV
    /// distribution sites), or receive-only (e.g., TV-broadcast endpoints whose downlinks
    /// terminate locally). Mirrors the <c>role = tx|rx|trx</c> field in Skopos's telecom.cfg.
    /// Routing forbids tx-only nodes from being intermediate hops and rx-only nodes from
    /// having outgoing edges.</summary>
    public enum StationRole
    {
        /// <summary>Default duplex — can both transmit and receive.</summary>
        Trx,
        /// <summary>Can only originate (no relay-through, no destination usage).</summary>
        TxOnly,
        /// <summary>Can only terminate (no relay-through, no source usage).</summary>
        RxOnly,
    }

    /// <summary>A fixed location on the central body's surface. Antenna params live in the
    /// shared <see cref="LinkBudget"/>; per-station gain variation can be added later.</summary>
    public readonly struct GroundStation
    {
        public readonly string Name;
        public readonly double LatDeg;
        public readonly double LonDeg;
        public readonly StationKind Kind;
        public readonly StationRole Role;

        public GroundStation(string name, double latDeg, double lonDeg,
                             StationKind kind = StationKind.Tracking,
                             StationRole role = StationRole.Trx)
        {
            Name = name; LatDeg = latDeg; LonDeg = lonDeg; Kind = kind; Role = role;
        }

        /// <summary>Body-fixed position on a sphere of given radius.</summary>
        public Vec3d Position(double bodyRadius) => Geometry.LatLonToEcef(LatDeg, LonDeg, bodyRadius);
    }

    /// <summary>One station↔satellite link evaluated at a single snapshot. <see cref="AntennaIdx"/>
    /// is the index of the satellite-side antenna that gave the best (highest) rx-power.</summary>
    public readonly struct GroundLink
    {
        public readonly int StationIdx;
        public readonly int SatIdx;
        public readonly int AntennaIdx;
        public readonly double DistanceM;
        public readonly double RxPowerDbm;
        public readonly double ElevationDeg;

        public GroundLink(int stationIdx, int satIdx, int antennaIdx, double distM, double rxDbm, double elevDeg)
        {
            StationIdx = stationIdx; SatIdx = satIdx; AntennaIdx = antennaIdx;
            DistanceM = distM; RxPowerDbm = rxDbm; ElevationDeg = elevDeg;
        }
    }

    public static class GroundLinkAnalysis
    {
        /// <summary>Find sat↔ground links from a snapshot. Both stations and sat positions must
        /// be in the same body-fixed frame; <paramref name="antennaBoresightsBf"/>[si, ai] is the
        /// body-fixed boresight unit vector of antenna ai on sat si at this snapshot. For each
        /// (station, sat) pair we pick the sat's antenna with lowest pointing loss.</summary>
        public static List<GroundLink> FindLinks(IList<GroundStation> stations,
                                                  IList<Vec3d> satPositionsBodyFixed,
                                                  IList<SatAntenna> groundAntennas,
                                                  Vec3d[,] antennaBoresightsBf,
                                                  double bodyRadius,
                                                  double minElevDeg = 10.0,
                                                  double minRxPowerDbm = double.NegativeInfinity)
        {
            var links = new List<GroundLink>();
            if (groundAntennas.Count == 0) return links;
            double sinMinElev = Math.Sin(minElevDeg * Math.PI / 180.0);
            int A = groundAntennas.Count;

            for (int s = 0; s < stations.Count; s++)
            {
                Vec3d stationPos = stations[s].Position(bodyRadius);
                Vec3d normal = stationPos.Normalized;
                for (int k = 0; k < satPositionsBodyFixed.Count; k++)
                {
                    Vec3d satPos = satPositionsBodyFixed[k];
                    Vec3d toSat = satPos - stationPos;
                    double dist = toSat.Magnitude;
                    if (dist <= 0) continue;
                    double sinElev = Vec3d.Dot(normal, toSat) / dist;
                    if (sinElev < sinMinElev) continue;
                    double clamped = sinElev > 1 ? 1 : (sinElev < -1 ? -1 : sinElev);
                    double elevDeg = Math.Asin(clamped) * 180.0 / Math.PI;

                    int bestAnt = -1;
                    double bestRx = double.NegativeInfinity;
                    for (int a = 0; a < A; a++)
                    {
                        var ant = groundAntennas[a];
                        var budget = ant.Budget;
                        double rx = budget.RxPowerDbm(dist);
                        if (!ant.IsOmnidirectional)
                            rx -= AntennaPointing.PointingLossDb(satPos, antennaBoresightsBf[k, a],
                                                                  stationPos, budget.BeamwidthDeg);
                        if (rx > bestRx) { bestRx = rx; bestAnt = a; }
                    }
                    if (bestRx < minRxPowerDbm) continue;
                    links.Add(new GroundLink(s, k, bestAnt, dist, bestRx, elevDeg));
                }
            }
            return links;
        }
    }
}
