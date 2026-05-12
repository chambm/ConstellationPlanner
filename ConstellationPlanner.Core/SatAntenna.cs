using System;
using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>One antenna on a satellite, with a body-fixed boresight expressed in the
    /// orbit-local LVLH frame (R/T/N). Convention chosen to match user input from chat:
    ///   <list type="bullet">
    ///   <item><c>ElevationDeg = 0</c> → boresight along nadir (toward body centre);
    ///         azimuth is then irrelevant.</item>
    ///   <item><c>ElevationDeg = 90</c> → boresight in the local horizontal plane; the
    ///         azimuth picks the direction within that plane.</item>
    ///   <item><c>AzimuthDeg = 0</c> → forward, along the inertial velocity (T axis).
    ///         <c>90</c> → orbit-normal "left" (N axis). <c>180</c> → trailing.
    ///         <c>270</c> → −N. (Az doesn't matter at El = 0.)</item>
    ///   </list>
    /// Boresight = nadir·cos(El) + (T·cos(Az) + N·sin(Az))·sin(El).</summary>
    public sealed class SatAntenna
    {
        public string Name;
        public double AzimuthDeg;
        public double ElevationDeg;
        public LinkBudget Budget;
        /// <summary>Whether this antenna is intended for sat↔sat (ISL) or sat↔ground links.
        /// Coverage / GroundLinkAnalysis use only ground antennas; IslAnalysis uses only ISL ones.</summary>
        public bool IsIsl;
        /// <summary>If true, pointing loss is treated as zero — the antenna is modelled as
        /// omnidirectional (or as a phased-array that can steer freely). Path-loss and
        /// gain still apply per the LinkBudget; only the directional-pattern penalty is skipped.</summary>
        public bool IsOmnidirectional;

        public SatAntenna(string name, double azDeg, double elDeg, LinkBudget budget,
                           bool isIsl = false, bool isOmnidirectional = false)
        {
            Name = name; AzimuthDeg = azDeg; ElevationDeg = elDeg; Budget = budget;
            IsIsl = isIsl; IsOmnidirectional = isOmnidirectional;
        }

        /// <summary>Boresight unit vector in the inertial frame, given the satellite's
        /// inertial position+velocity. Nadir = −r̂, T = v̂, N = (r×v)̂.</summary>
        public Vec3d BoresightInertial(Vec3d satPos, Vec3d satVel)
        {
            Vec3d nadir = -satPos.Normalized;
            Vec3d t = satVel.Normalized;
            Vec3d n = Vec3d.Cross(satPos, satVel).Normalized;
            double az = AzimuthDeg * Math.PI / 180.0;
            double el = ElevationDeg * Math.PI / 180.0;
            Vec3d horiz = t * Math.Cos(az) + n * Math.Sin(az);
            return (nadir * Math.Cos(el) + horiz * Math.Sin(el)).Normalized;
        }
    }

    /// <summary>Pre-computed per-(timestep, sat, antenna) boresight in body-fixed frame so
    /// callers don't have to redo trig per cell. Used by Coverage/GroundLinks/Isl/Relay.</summary>
    public static class AntennaPointing
    {
        /// <summary>Compute body-fixed boresight unit vectors for every (sat, antenna) pair at
        /// a given epoch. <paramref name="bodyRotationRateRadPerSec"/> ≠ 0 rotates inertial →
        /// body-fixed by −ω·t (same convention as <see cref="Geometry.ToBodyFixed"/>).</summary>
        public static Vec3d[,] ComputeBodyFixed(IList<Satellite> sats, IList<SatAntenna> antennas,
                                                  double mu, double t,
                                                  double bodyRotationRateRadPerSec)
        {
            int N = sats.Count, A = antennas.Count;
            var result = new Vec3d[N, A];
            double rotAngle = -bodyRotationRateRadPerSec * t;
            bool rotates = rotAngle != 0;
            for (int s = 0; s < N; s++)
            {
                Vec3d posI = sats[s].Position(mu, t);
                Vec3d velI = sats[s].Velocity(mu, t);
                for (int a = 0; a < A; a++)
                {
                    Vec3d biI = antennas[a].BoresightInertial(posI, velI);
                    result[s, a] = rotates ? Geometry.RotZ(biI, rotAngle) : biI;
                }
            }
            return result;
        }

        /// <summary>Pointing loss (dB) from antenna's boresight to a target point.
        /// Uses RA's piecewise-linear gain curve via <see cref="Physics.PointingLoss"/>.</summary>
        public static double PointingLossDb(Vec3d satPos, Vec3d boresightUnit,
                                              Vec3d targetPos, double beamwidthDeg)
        {
            Vec3d toTarget = targetPos - satPos;
            double d = toTarget.Magnitude;
            if (d <= 0) return 0;
            double cosAngle = Vec3d.Dot(boresightUnit, toTarget) / d;
            if (cosAngle > 1) cosAngle = 1; else if (cosAngle < -1) cosAngle = -1;
            double angleDeg = Math.Acos(cosAngle) * 180.0 / Math.PI;
            return Physics.PointingLoss(angleDeg, beamwidthDeg);
        }
    }
}
