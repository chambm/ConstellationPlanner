using System;
using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>Geocentric/orbital primitives. ECEF here means a body-fixed inertial frame —
    /// rotation of the central body is ignored for the MVP heatmap. Distances in metres,
    /// angles in degrees on public APIs.</summary>
    public static class Geometry
    {
        const double Deg2Rad = Math.PI / 180.0;
        const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>Surface point on a sphere of radius r at given latitude/longitude.</summary>
        public static Vec3d LatLonToEcef(double latDeg, double lonDeg, double r)
        {
            double lat = latDeg * Deg2Rad;
            double lon = lonDeg * Deg2Rad;
            double cl = Math.Cos(lat);
            return new Vec3d(r * cl * Math.Cos(lon), r * cl * Math.Sin(lon), r * Math.Sin(lat));
        }

        /// <summary>Local zenith / outward surface normal at a point on a sphere.</summary>
        public static Vec3d SurfaceNormal(Vec3d ecef) => ecef.Normalized;

        /// <summary>Rotate v about Z by angle (radians). Standard right-hand rule.</summary>
        public static Vec3d RotZ(Vec3d v, double angleRad)
        {
            double c = Math.Cos(angleRad), s = Math.Sin(angleRad);
            return new Vec3d(c * v.X - s * v.Y, s * v.X + c * v.Y, v.Z);
        }

        /// <summary>Rotate v about X by angle (radians).</summary>
        public static Vec3d RotX(Vec3d v, double angleRad)
        {
            double c = Math.Cos(angleRad), s = Math.Sin(angleRad);
            return new Vec3d(v.X, c * v.Y - s * v.Z, s * v.Y + c * v.Z);
        }

        /// <summary>Elevation angle (degrees) of <paramref name="target"/> as seen from
        /// <paramref name="ground"/>. Negative = below horizon.</summary>
        public static double ElevationDeg(Vec3d ground, Vec3d target)
        {
            Vec3d normal = ground.Normalized;
            Vec3d toTarget = (target - ground).Normalized;
            double sinElev = Vec3d.Dot(normal, toTarget);
            if (sinElev > 1.0) sinElev = 1.0; else if (sinElev < -1.0) sinElev = -1.0;
            return Math.Asin(sinElev) * Rad2Deg;
        }

        /// <summary>Iterate Kepler's equation M = E − e·sin(E) for E using Newton's method
        /// starting from E₀ = M. Converges in ~5 iterations for e ≤ 0.8 and ~10–15 for the
        /// e ≈ 0.74 of a Molniya orbit. Tolerance 1e-12 is well below floating-point noise.</summary>
        static double SolveKepler(double M, double e)
        {
            double E = M;
            for (int iter = 0; iter < 20; iter++)
            {
                double f = E - e * Math.Sin(E) - M;
                double fp = 1 - e * Math.Cos(E);
                double dE = -f / fp;
                E += dE;
                if (Math.Abs(dE) < 1e-12) break;
            }
            return E;
        }

        /// <summary>Two-body propagator returning ECI position at time t for the orbit defined
        /// by classical elements (a, e, i, Ω, ω, ν₀). For e = 0 the math collapses to the
        /// circular case; ω is then degenerate but harmless. Rotation chain is the standard
        /// perifocal → ECI: R₃(Ω) · R₁(i) · R₃(ω) · PQW.</summary>
        public static Vec3d EllipticalOrbitPos(double semiMajorAxis, double eccentricity,
                                                double inclinationDeg, double raanDeg,
                                                double argPerigeeDeg, double trueAnomalyAtT0Deg,
                                                double mu, double t)
        {
            // Advance via mean anomaly: ν₀ → E₀ → M₀, then M(t) = M₀ + n·t, solve back to ν.
            double nu0 = trueAnomalyAtT0Deg * Deg2Rad;
            double E0 = 2 * Math.Atan2(Math.Sqrt(1 - eccentricity) * Math.Sin(nu0 / 2),
                                        Math.Sqrt(1 + eccentricity) * Math.Cos(nu0 / 2));
            double M0 = E0 - eccentricity * Math.Sin(E0);
            double n  = Math.Sqrt(mu / (semiMajorAxis * semiMajorAxis * semiMajorAxis));
            double M  = M0 + n * t;
            double E  = SolveKepler(M, eccentricity);
            double nu = 2 * Math.Atan2(Math.Sqrt(1 + eccentricity) * Math.Sin(E / 2),
                                        Math.Sqrt(1 - eccentricity) * Math.Cos(E / 2));
            double r  = semiMajorAxis * (1 - eccentricity * eccentricity) / (1 + eccentricity * Math.Cos(nu));
            Vec3d pqw = new Vec3d(r * Math.Cos(nu), r * Math.Sin(nu), 0);
            Vec3d afterOmega = RotZ(pqw, argPerigeeDeg * Deg2Rad);
            Vec3d afterInc   = RotX(afterOmega, inclinationDeg * Deg2Rad);
            return RotZ(afterInc, raanDeg * Deg2Rad);
        }

        /// <summary>Inertial velocity at time t for the same orbit. Perifocal velocity is
        /// √(μ/p) · (−sin ν, e + cos ν, 0) where p = a(1−e²); same rotation chain as position.</summary>
        public static Vec3d EllipticalOrbitVel(double semiMajorAxis, double eccentricity,
                                                double inclinationDeg, double raanDeg,
                                                double argPerigeeDeg, double trueAnomalyAtT0Deg,
                                                double mu, double t)
        {
            double nu0 = trueAnomalyAtT0Deg * Deg2Rad;
            double E0 = 2 * Math.Atan2(Math.Sqrt(1 - eccentricity) * Math.Sin(nu0 / 2),
                                        Math.Sqrt(1 + eccentricity) * Math.Cos(nu0 / 2));
            double M0 = E0 - eccentricity * Math.Sin(E0);
            double n  = Math.Sqrt(mu / (semiMajorAxis * semiMajorAxis * semiMajorAxis));
            double M  = M0 + n * t;
            double E  = SolveKepler(M, eccentricity);
            double nu = 2 * Math.Atan2(Math.Sqrt(1 + eccentricity) * Math.Sin(E / 2),
                                        Math.Sqrt(1 - eccentricity) * Math.Cos(E / 2));
            double p  = semiMajorAxis * (1 - eccentricity * eccentricity);
            double sqrtMuOverP = Math.Sqrt(mu / p);
            Vec3d vqw = new Vec3d(-sqrtMuOverP * Math.Sin(nu),
                                    sqrtMuOverP * (eccentricity + Math.Cos(nu)), 0);
            Vec3d afterOmega = RotZ(vqw, argPerigeeDeg * Deg2Rad);
            Vec3d afterInc   = RotX(afterOmega, inclinationDeg * Deg2Rad);
            return RotZ(afterInc, raanDeg * Deg2Rad);
        }

        /// <summary>Circular-orbit shortcut — same as <see cref="EllipticalOrbitPos"/> with
        /// e = 0, ω = 0. Kept as a separate entry point so existing callers compile unchanged.</summary>
        public static Vec3d CircularOrbitPos(double semiMajorAxis, double inclinationDeg,
                                             double raanDeg, double trueAnomalyAtT0Deg,
                                             double mu, double t)
            => EllipticalOrbitPos(semiMajorAxis, 0, inclinationDeg, raanDeg, 0, trueAnomalyAtT0Deg, mu, t);

        /// <summary>Circular-orbit shortcut for velocity — see <see cref="CircularOrbitPos"/>.</summary>
        public static Vec3d CircularOrbitVel(double semiMajorAxis, double inclinationDeg,
                                              double raanDeg, double trueAnomalyAtT0Deg,
                                              double mu, double t)
            => EllipticalOrbitVel(semiMajorAxis, 0, inclinationDeg, raanDeg, 0, trueAnomalyAtT0Deg, mu, t);

        /// <summary>Orbital period (seconds) for a circular orbit at given semi-major axis.</summary>
        public static double OrbitalPeriod(double semiMajorAxis, double mu)
            => 2 * Math.PI * Math.Sqrt(semiMajorAxis * semiMajorAxis * semiMajorAxis / mu);

        /// <summary>Project an inertial-frame position to (lat, lon) in degrees on the central body.
        /// Lat in [-90, 90]; lon in (-180, 180].</summary>
        public static (double LatDeg, double LonDeg) EcefToLatLon(Vec3d pos)
        {
            double r = pos.Magnitude;
            if (r <= 0) return (0, 0);
            double lat = Math.Asin(pos.Z / r) * Rad2Deg;
            double lon = Math.Atan2(pos.Y, pos.X) * Rad2Deg;
            return (lat, lon);
        }

        /// <summary>Rotate an inertial-frame position into the body-fixed frame at time t,
        /// given the body's sidereal rotation rate (rad/s). At t=0 frames coincide.</summary>
        public static Vec3d ToBodyFixed(Vec3d inertial, double rotationRateRadPerSec, double t)
            => RotZ(inertial, -rotationRateRadPerSec * t);

        /// <summary>Great-circle radius (in degrees of central angle) of an antenna footprint
        /// on the body's surface, for a beam aimed at nadir from altitude h with full HPBW
        /// <paramref name="beamwidthDeg"/>. Returns the radius from the sub-satellite point to
        /// the −3 dB contour. If the half-beamwidth exceeds the angle to the limb, the
        /// footprint is clamped to the visible-disc half-angle (entire visible Earth).</summary>
        public static double FootprintHalfAngleDeg(double beamwidthDeg, double altitudeM, double bodyRadius)
        {
            double thetaRad = (beamwidthDeg / 2.0) * Deg2Rad;
            double ratio = (bodyRadius + altitudeM) / bodyRadius;
            // Visible-disc limit: ray from sat is tangent to body at nadir-angle θ_max where
            // sin(θ_max) = R/(R+h); the corresponding central angle is π/2 − θ_max.
            double sinThetaMax = 1.0 / ratio;
            if (Math.Sin(thetaRad) >= sinThetaMax)
                return (Math.PI / 2.0 - Math.Asin(sinThetaMax)) * Rad2Deg;
            // Law of sines in triangle (body-centre, sat, surface-point):
            //   sin(β)/(R+h) = sin(θ)/R, take the supplement (above-horizon intersection).
            double betaRad = Math.PI - Math.Asin(ratio * Math.Sin(thetaRad));
            double alphaRad = Math.PI - betaRad - thetaRad;
            return alphaRad * Rad2Deg;
        }

        /// <summary>Near surface-intersection of a ray from <paramref name="origin"/> along
        /// unit-vector <paramref name="dir"/> with a sphere of radius <paramref name="bodyRadius"/>
        /// centered on the same origin frame. Returns null when the ray misses the sphere
        /// (boresight points above the local horizon). For a nadir-pointing antenna, the result
        /// equals the sub-satellite point projected to the surface.</summary>
        public static Vec3d? RaySphereNearIntersect(Vec3d origin, Vec3d dir, double bodyRadius)
        {
            double oDotD = Vec3d.Dot(origin, dir);
            double oMagSq = origin.SqrMagnitude;
            double rSq = bodyRadius * bodyRadius;
            double disc = oDotD * oDotD - (oMagSq - rSq);
            if (disc < 0) return null;
            double sqrtDisc = Math.Sqrt(disc);
            double tNear = -oDotD - sqrtDisc;
            if (tNear < 0)
            {
                double tFar = -oDotD + sqrtDisc;
                if (tFar < 0) return null;
                tNear = tFar;
            }
            return origin + tNear * dir;
        }

        /// <summary>Off-nadir angle (degrees) from a satellite at <paramref name="satPos"/> to
        /// a target at <paramref name="targetPos"/>. Nadir = direction from sat toward body
        /// centre; 0° = target directly under sat, 90° = horizon. Used to drive pointing-loss
        /// calculations for nadir-aimed sat antennas.</summary>
        public static double NadirAngleDeg(Vec3d satPos, Vec3d targetPos)
        {
            double satMag = satPos.Magnitude;
            if (satMag <= 0) return 0;
            Vec3d toTarget = targetPos - satPos;
            double dist = toTarget.Magnitude;
            if (dist <= 0) return 0;
            // cos(angle) = dot(-sat/|sat|, toTarget/|toTarget|)
            double cosAngle = -Vec3d.Dot(satPos, toTarget) / (satMag * dist);
            if (cosAngle > 1) cosAngle = 1; else if (cosAngle < -1) cosAngle = -1;
            return Math.Acos(cosAngle) * Rad2Deg;
        }

        /// <summary>Sample the boundary of a small circle on the unit sphere with great-circle
        /// radius <paramref name="radiusDeg"/> around (<paramref name="centerLatDeg"/>,
        /// <paramref name="centerLonDeg"/>). Returns <paramref name="samples"/>+1 points so
        /// the loop closes. Longitudes wrapped to (−180, 180].</summary>
        public static List<(double LatDeg, double LonDeg)> SmallCircle(
            double centerLatDeg, double centerLonDeg, double radiusDeg, int samples = 72)
        {
            var pts = new List<(double, double)>(samples + 1);
            double lat0 = centerLatDeg * Deg2Rad;
            double lon0 = centerLonDeg * Deg2Rad;
            double r = radiusDeg * Deg2Rad;
            double sinLat0 = Math.Sin(lat0), cosLat0 = Math.Cos(lat0);
            double sinR = Math.Sin(r), cosR = Math.Cos(r);
            for (int i = 0; i <= samples; i++)
            {
                double az = 2 * Math.PI * i / samples;
                double sinLat = sinLat0 * cosR + cosLat0 * sinR * Math.Cos(az);
                if (sinLat > 1) sinLat = 1; else if (sinLat < -1) sinLat = -1;
                double lat = Math.Asin(sinLat);
                double cosLat = Math.Cos(lat);
                double lon = lon0 + Math.Atan2(Math.Sin(az) * sinR * cosLat0,
                                                cosR - sinLat0 * sinLat);
                while (lon > Math.PI) lon -= 2 * Math.PI;
                while (lon <= -Math.PI) lon += 2 * Math.PI;
                pts.Add((lat * Rad2Deg, lon * Rad2Deg));
            }
            return pts;
        }
    }

    /// <summary>One satellite's classical orbital elements. Defaults to circular (e = 0,
    /// ω = 0) for back-compat with existing Walker shells.</summary>
    public readonly struct Satellite
    {
        public readonly double SemiMajorAxis;     // m
        public readonly double Eccentricity;
        public readonly double InclinationDeg;
        public readonly double RaanDeg;
        public readonly double ArgPerigeeDeg;
        public readonly double TrueAnomalyAtT0Deg;

        /// <summary>Circular-orbit constructor — e = 0, ω = 0 implicit.</summary>
        public Satellite(double sma, double incDeg, double raanDeg, double nu0Deg)
            : this(sma, 0, incDeg, raanDeg, 0, nu0Deg) { }

        /// <summary>Full classical-elements constructor.</summary>
        public Satellite(double sma, double eccentricity, double incDeg, double raanDeg,
                          double argPerigeeDeg, double nu0Deg)
        {
            SemiMajorAxis = sma; Eccentricity = eccentricity;
            InclinationDeg = incDeg; RaanDeg = raanDeg;
            ArgPerigeeDeg = argPerigeeDeg; TrueAnomalyAtT0Deg = nu0Deg;
        }

        public Vec3d Position(double mu, double t)
            => Geometry.EllipticalOrbitPos(SemiMajorAxis, Eccentricity, InclinationDeg,
                                            RaanDeg, ArgPerigeeDeg, TrueAnomalyAtT0Deg, mu, t);

        public Vec3d Velocity(double mu, double t)
            => Geometry.EllipticalOrbitVel(SemiMajorAxis, Eccentricity, InclinationDeg,
                                            RaanDeg, ArgPerigeeDeg, TrueAnomalyAtT0Deg, mu, t);
    }

    /// <summary>Walker Delta constellation: T total sats, P planes, F phasing parameter (0..P-1).</summary>
    public static class Walker
    {
        /// <summary>Generate a Walker Delta constellation. T must be divisible by P. Defaults
        /// produce a circular shell; passing <paramref name="eccentricity"/>/<paramref name="argPerigeeDeg"/>
        /// builds an elliptical Walker (e.g., a Molniya / Tundra constellation when paired with
        /// the right SMA + critical inclination). <paramref name="raanOffsetDeg"/> rotates the
        /// whole shell — useful for parking apogee over a target longitude in body-fixed frame.</summary>
        public static IList<Satellite> Delta(double altitude, double bodyRadius, double inclinationDeg,
                                             int t, int p, int f,
                                             double eccentricity = 0,
                                             double argPerigeeDeg = 0,
                                             double raanOffsetDeg = 0)
        {
            if (t % p != 0)
                throw new ArgumentException($"Walker T={t} must be divisible by P={p}.");
            int s = t / p;
            double sma = bodyRadius + altitude;
            var sats = new List<Satellite>(t);
            for (int planeIdx = 0; planeIdx < p; planeIdx++)
            {
                double raan = raanOffsetDeg + planeIdx * 360.0 / p;
                for (int satIdx = 0; satIdx < s; satIdx++)
                {
                    double nu0 = (satIdx * 360.0 / s) + (planeIdx * f * 360.0 / t);
                    sats.Add(new Satellite(sma, eccentricity, inclinationDeg, raan, argPerigeeDeg, nu0));
                }
            }
            return sats;
        }

        /// <summary>Construct a Walker shell directly from perigee + apogee altitudes — the
        /// natural input pair for elliptical orbits where SMA isn't what users think about.
        /// SMA = (r_pe + r_ap)/2 and e = (r_ap − r_pe)/(r_ap + r_pe).</summary>
        public static IList<Satellite> DeltaPeAp(double perigeeAltKm, double apogeeAltKm,
                                                  double bodyRadius, double inclinationDeg,
                                                  int t, int p, int f,
                                                  double argPerigeeDeg = 0,
                                                  double raanOffsetDeg = 0)
        {
            double rPe = bodyRadius + perigeeAltKm * 1000;
            double rAp = bodyRadius + apogeeAltKm * 1000;
            double sma = (rPe + rAp) / 2;
            double e   = (rAp - rPe) / (rAp + rPe);
            return Delta(sma - bodyRadius, bodyRadius, inclinationDeg, t, p, f, e, argPerigeeDeg, raanOffsetDeg);
        }

        /// <summary>For each sat in a Walker δT/P shell, return the four named ISL neighbour
        /// indices in column order [forward_in_plane, aft_in_plane, port_cross_plane,
        /// starboard_cross_plane]. Indices match <see cref="Delta"/>'s flat ordering.
        /// Cross-plane neighbours are at the same in-plane slot in plane±1 (RAAN±360°/P);
        /// in-plane neighbours are slot±1 in the same plane. Set to −1 when the category
        /// has no neighbour (S=1 → no in-plane; P=1 → no cross-plane). When S=2 the forward
        /// and aft entries point to the same satellite (180° away in the orbit), which is
        /// usually Earth-blocked — callers detect that via the LoS check.</summary>
        public static int[,] NeighborMap(int t, int p)
        {
            if (t % p != 0)
                throw new ArgumentException($"Walker T={t} must be divisible by P={p}.");
            int s = t / p;
            var map = new int[t, 4];
            for (int planeIdx = 0; planeIdx < p; planeIdx++)
            {
                for (int slotIdx = 0; slotIdx < s; slotIdx++)
                {
                    int satIdx = planeIdx * s + slotIdx;
                    map[satIdx, 0] = (s > 1) ? planeIdx * s + (slotIdx + 1) % s         : -1;
                    map[satIdx, 1] = (s > 1) ? planeIdx * s + (slotIdx - 1 + s) % s     : -1;
                    map[satIdx, 2] = (p > 1) ? ((planeIdx + 1) % p) * s + slotIdx       : -1;
                    map[satIdx, 3] = (p > 1) ? ((planeIdx - 1 + p) % p) * s + slotIdx   : -1;
                }
            }
            return map;
        }
    }
}
