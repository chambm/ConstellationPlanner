// Copied from RealAntennas/src/RealAntennasProject/Physics.cs.
// Divergences vs upstream:
//   - using UnityEngine[.Profiling]; / using Unity.Mathematics; removed.
//   - double3 / float3 aliased to Vec3d (file-level using alias).
//   - Class made public (was internal-by-default).
//   - SolarLuminosity property dropped (uses PhysicsGlobals + Planetarium).
//   - All RealAntenna/CelestialBody/Vector3/Vector3d-binding overloads dropped:
//       ReceivedPower(RealAntenna...), PointingLoss(RealAntenna, Vector3),
//       GetEquilibriumTemperature(CelestialBody), BodyBaseTemperature(CelestialBody),
//       BodyNoiseTemp(RealAntenna, CelestialBody, Vector3d), NoiseTemperature(RealAntenna...),
//       AntennaMicrowaveTemp(RealAntenna), AtmosphericTemp(RealAntenna, Vector3d) wrapper,
//       CosmicBackgroundTemp(RealAntenna, Vector3d) private wrapper, AllBodyTemps.
//     The pure-math overloads of BodyNoiseTemp / AtmosphericTemp / CosmicBackgroundTemp
//     are kept; ConstellationPlanner.Cli supplies their inputs from YAML, the future
//     Ksp project will reintroduce the KSP-binding wrappers.
//   - UnityEngine.Profiling.Profiler.* calls were only inside AllBodyTemps (dropped).

using System;
using double3 = ConstellationPlanner.Core.Vec3d;

namespace ConstellationPlanner.Core
{
    public class Physics
    {
        //public static readonly double boltzmann_dBW = 10 * Math.Log10(1.38064852e-23);      //-228.59917;
        public const float boltzmann_dBW = -228.599168683097f;
        public const float boltzmann_dBm = boltzmann_dBW + 30;
        public const float MaxPointingLoss = 200;
        public const float MaxOmniGain = 5;
        public const float c = 2.998e8f;
        public const float CMB = 2.725f;

        //private static readonly double path_loss_constant = 20 * Math.Log10(4 * Math.PI / (2.998 * Math.Pow(10, 8)));
        private const float path_loss_constant = -147.552435289803f;

        public static float GainFromDishDiamater(float diameter, float freq, float efficiency =1)
        {
            float gain = 0;
            if (diameter > 0 && efficiency > 0)
            {
                float wavelength = Physics.c / freq;
                gain = RATools.LogScale(9.87f * efficiency * diameter * diameter / (wavelength * wavelength));
            }
            return gain;
        }
        public static float GainFromReference(float refGain, float refFreq, float newFreq)
        {
            float gain = 0;
            if (refGain > 0)
            {
                gain = refGain;
                gain += (refGain <= MaxOmniGain) ? 0 : RATools.LogScale(newFreq / refFreq);
            }
            return gain;
        }

        public static double Beamwidth(double gain) => Beamwidth(Convert.ToSingle(gain));
        public static float Beamwidth(float gain) => math.sqrt(52525 / RATools.LinearScale(gain));

        public static double PathLoss(double distance, double frequency = 1e9)
        {
            //FSPL = 20 log D + 20 log freq + 20 log (4pi/c)
            double df = math.max(distance * frequency, 0.1);
            return (20 * math.log10(df)) + path_loss_constant;
        }
        public static float PathLoss(float distance, float frequency = 1e9f)
        {
            float df = math.max(distance * frequency, 0.1f);
            return (20 * math.log10(df)) + path_loss_constant;
        }

        // Beamwidth is full side-to-side HPBW, == 0 to -10dB offset angle
        public static float PointingLoss(double angle, double beamwidth)
        //            => (angle > beamwidth) ? MaxPointingLoss : -1 * AntennaGainCurve.Evaluate(Convert.ToSingle(angle / beamwidth));
        {
            float norm = Convert.ToSingle(angle / beamwidth);
            if (norm > 1) return MaxPointingLoss;
            if (norm < 0.14f) return math.remap(0, 0.14f, 0, 0.25f, norm);
            else if (norm < 0.2f) return math.remap(0.14f, 0.2f, 0.25f, 0.5f, norm);
            else if (norm < 0.29f) return math.remap(0.2f, 0.29f, 0.5f, 1, norm);
            else if (norm < 0.41f) return math.remap(0.29f, 0.41f, 1, 2, norm);
            else if (norm < 0.5f) return math.remap(0.41f, 0.5f, 2, 3, norm);
            else if (norm < 0.57f) return math.remap(0.5f, 0.57f, 3, 4, norm);
            else if (norm < 0.61f) return math.remap(0.57f, 0.61f, 4, 4.5f, norm);
            else if (norm < 0.64f) return math.remap(0.61f, 0.64f, 4.5f, 5, norm);
            else if (norm < 0.7f) return math.remap(0.64f, 0.7f, 5, 6, norm);
            else if (norm < 0.76f) return math.remap(0.7f, 0.76f, 6, 7, norm);
            else if (norm < 0.81f) return math.remap(0.76f, 0.81f, 7, 8, norm);
            else if (norm < 0.86f) return math.remap(0.81f, 0.86f, 8, 9, norm);
            else return math.remap(0.86f, 1, 9, 10, norm);
        }

        public static float GainAtAngle(float gain, float angle) => gain - PointingLoss(math.abs(angle), Beamwidth(gain));
        // Beamwidth is the 3dB full beamwidth contour, ~= the offset angle to the 10dB contour.
        // 10dBi: Beamwidth = 72 = 4dB full beamwidth contour
        // 10dBi @ .6 efficiency: 57 = 3dB full beamwidth contour
        // 20dBi: Beamwidth = 23 = 4dB full beamwidth countour
        // 20dBi @ .6 efficiency: Beamwidth = 17.75 = 3dB full beamwidth contour

        // Sun Temp vs Freq from https://deepspace.jpl.nasa.gov/dsndocs/810-005/Binder/810-005_Binder_Change51.pdf Manual 105 Eq 14: T = 5672 * lambda ^ 0.24517, lambda units is mm
        public static float StarRadioTemp(float surfaceTemp, float frequency) => surfaceTemp * math.pow(1000 * c / frequency, .24517f);   // QUIET Sun Temp, active can be 2-3x higher
        public static float AtmosphereMeanEffectiveTemp(float CD) => 255 + (25 * CD); // 0 <= CD <= 1
        public static float AtmosphereNoiseTemperature(float elevationAngle, float frequency =1e9f)
        {
            float CD = 0.5f;
            float Atheta = AtmosphereAttenuation(CD, elevationAngle, frequency);
            float LossFactor = RATools.LinearScale(Atheta);  // typical values = 1.01 to 2.0 (A = 0.04 dB to 3 dB)
            float meanTemp = AtmosphereMeanEffectiveTemp(CD);
            float result = meanTemp * (1 - (1 / LossFactor));
            return result;
        }
        public static float AtmosphereAttenuation(float CD, float elevationAngle, float frequency =1e9f)
        {
            float AirMasses = (1 / math.sin(math.radians(math.abs(elevationAngle))));
            return AtmosphereZenithAttenuation(CD, frequency) * AirMasses;
        }
        public static float AtmosphereZenithAttenuation(float CD, float frequency = 1e9f)
        {
            // This would be a gigantic table lookup per ground station.
            if (frequency < 3e9) return 0.035f;          // S/C/L band, didn't really vary by CD
            else if (frequency < 10e9)                  // X-Band, varied 0.4 to 0.6
            {
                return Mathf.Lerp(0.4f, 0.6f, CD);
            }
            else if (frequency < 27e9)                  // Ka-Band, varied .116-.239, .124-.384, .121-.407
            {
                return Mathf.Lerp(0.121f, 0.384f, CD);
            }
            else                                      // K-Band, 0.084-.226, .086-.375, .084-.373
            {
                return Mathf.Lerp(0.084f, 0.373f, CD);
            }
        }


        public static float BodyNoiseTemp(double3 antPos,
                                            float gain,
                                            double3 dir,
                                            double3 bodyPos,
                                            float bodyRadius,
                                            float bodyTemp,
                                            float beamwidth = -1)
        {
            if (gain < MaxOmniGain) return 0;
            if (bodyTemp < float.Epsilon) return 0;
            double3 toBody = bodyPos - antPos;
            float angle = (float) MathUtils.Angle2(toBody, dir);
            float distance = (float) math.length(toBody);
            beamwidth = (beamwidth < 0) ? Beamwidth(gain) : beamwidth;
            float bodyRadiusAngularRad = (distance > 10 * bodyRadius)
                    ? math.atan2(bodyRadius, distance)
                    : math.radians(MathUtils.AngularRadius(bodyRadius, distance));
            float bodyRadiusAngularDeg = math.degrees(bodyRadiusAngularRad);
            if (beamwidth < angle - bodyRadiusAngularDeg) return 0;  // Pointed too far away

            float angleRad = math.radians(angle);
            float beamwidthRad = math.radians(beamwidth);
            float gainDelta; // Antenna Pointing adjustment
            float viewedAreaBase;

            // How much of the body is in view of the antenna?
            if (beamwidth < bodyRadiusAngularDeg - angle)    // Antenna viewable area completely enclosed by body
            {
                viewedAreaBase = (float)math.PI * beamwidthRad * beamwidthRad;
                gainDelta = 0;
            }
            else if (beamwidth > bodyRadiusAngularDeg + angle)   // Antenna viewable area completely encloses body
            {
                viewedAreaBase = (float)math.PI * bodyRadiusAngularRad * bodyRadiusAngularRad;
                gainDelta = -PointingLoss(angle, beamwidth);
            }
            else
            {
                viewedAreaBase = MathUtils.CircleCircleIntersectionArea(beamwidthRad, bodyRadiusAngularRad, angleRad);
                float intersectionCenter = MathUtils.CircleCircleIntersectionOffset(beamwidthRad, bodyRadiusAngularRad, angleRad);
                gainDelta = -PointingLoss(math.degrees(intersectionCenter + beamwidthRad) / 2, beamwidth);
            }

            // How much of the antenna viewable area is occupied by the body
            float antennaViewableArea = (float)math.PI * beamwidthRad * beamwidthRad;
            float viewableAreaRatio = viewedAreaBase / antennaViewableArea;

            float result = bodyTemp * viewableAreaRatio * RATools.LinearScale(gainDelta);
            return result;
        }

        public static float MinimumTheoreticalEbN0(float SpectralEfficiency)
        {
            // Given SpectralEfficiency in bits/sec/Hz (= Channel Capacity / Bandwidth)
            // Solve Shannon Hartley for Eb/N0 >= (2^(C/B) - 1) / (C/B)
            return RATools.LogScale(math.pow(2, SpectralEfficiency) - 1) / SpectralEfficiency;
        }
        public static float NoiseFloor(float bandwidth, float noiseTemp) => NoiseSpectralDensity(noiseTemp) + (10 * math.log10(bandwidth));
        public static float NoiseSpectralDensity(float noiseTemp) => boltzmann_dBm + (10 * math.log10(noiseTemp));

        public static float AtmosphericTemp(double3 position, double3 surfaceNormal, double3 origin, float frequency)
        {
            float elevation = MathUtils.ElevationAngle(position, surfaceNormal, origin);
            return AtmosphereNoiseTemperature(elevation, frequency);
        }

        public static float CosmicBackgroundTemp(double3 surfaceNormal, double3 toOrigin, float freq, bool isHome)
        {
            float lossFactor = 1;
            if (isHome)
            {
                float CD = 0.5f;
                float angle = (float) MathUtils.Angle2(surfaceNormal, toOrigin);
                float elevation = math.max(0, 90.0f - angle);
                lossFactor = Convert.ToSingle(RATools.LinearScale(AtmosphereAttenuation(CD, elevation, freq)));
            }
            return CMB / lossFactor;
        }
    }
}
