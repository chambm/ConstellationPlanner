using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConstellationPlanner.Core
{
    /// <summary>Link-budget params used by Coverage.Evaluate to compute rx-power maps, by
    /// IslAnalysis to grade ISLs, and by Relay.Find to filter edges that can't carry the
    /// required data rate. dB-domain. Frequency in Hz. Bandwidth/encoder fields are optional —
    /// when BandwidthHz = 0 the data-rate model is disabled and MaxDataRateBps returns infinity.</summary>
    public readonly struct LinkBudget
    {
        public readonly float TxPowerDbm;
        public readonly float TxGainDbi;
        public readonly float RxGainDbi;
        public readonly float FrequencyHz;

        // Rate model — RA-style. Disabled when BandwidthHz <= 0.
        public readonly float BandwidthHz;
        public readonly float SystemNoiseTempK;
        public readonly float MaxBitsPerSymbol;   // e.g. 2 for QPSK, 4 for 16-QAM
        public readonly float CodingRate;         // e.g. 0.5 for rate-1/2 FEC
        public readonly float RequiredEbN0Db;     // encoder threshold

        public LinkBudget(float txPowerDbm, float txGainDbi, float rxGainDbi, float frequencyHz,
                           float bandwidthHz = 0,
                           float systemNoiseTempK = 290,
                           float maxBitsPerSymbol = 2,
                           float codingRate = 0.5f,
                           float requiredEbN0Db = 6f)
        {
            TxPowerDbm = txPowerDbm; TxGainDbi = txGainDbi; RxGainDbi = rxGainDbi; FrequencyHz = frequencyHz;
            BandwidthHz = bandwidthHz; SystemNoiseTempK = systemNoiseTempK;
            MaxBitsPerSymbol = maxBitsPerSymbol; CodingRate = codingRate; RequiredEbN0Db = requiredEbN0Db;
        }

        /// <summary>RX power for a free-space link at the given distance (m).</summary>
        public float RxPowerDbm(double distanceM)
            => TxPowerDbm + TxGainDbi + RxGainDbi - (float)Physics.PathLoss(distanceM, FrequencyHz);

        /// <summary>Receiver noise floor (dBm) at this bandwidth + system noise temperature.</summary>
        public float NoiseFloorDbm
            => Physics.NoiseFloor(BandwidthHz, SystemNoiseTempK);

        /// <summary>HPBW (degrees) implied by TxGainDbi via RA's Beamwidth(gain) =
        /// √(52525 / linear(gain)). Same value RA uses internally for pointing-loss curves.</summary>
        public float BeamwidthDeg => Physics.Beamwidth(TxGainDbi);

        /// <summary>RA-style achievable data rate at the given distance. The modulator runs at
        /// max symbol rate when Eb/N0 margin is positive; below threshold the symbol rate halves
        /// per 3 dB of shortfall (each halving reclaims 3 dB of Eb/N0 because P/N0 stays put while
        /// R drops by 2). Returns +infinity when the rate model is disabled (BandwidthHz ≤ 0) so
        /// that callers without rate awareness see no filtering.</summary>
        public double MaxDataRateBps(double distanceM)
        {
            if (BandwidthHz <= 0) return double.PositiveInfinity;
            double rxDbm = RxPowerDbm(distanceM);
            double snrDb = rxDbm - NoiseFloorDbm;
            double specEff = MaxBitsPerSymbol * CodingRate;          // bits/sec/Hz at max
            double ebn0Db = snrDb - 10.0 * Math.Log10(specEff);
            double maxRate = BandwidthHz * specEff;
            if (ebn0Db >= RequiredEbN0Db) return maxRate;
            double shortfallDb = RequiredEbN0Db - ebn0Db;
            int halvings = (int)Math.Ceiling(shortfallDb / 3.0);
            if (halvings >= 32) return 0;                            // effectively dead
            return maxRate / Math.Pow(2, halvings);
        }
    }

    /// <summary>Per-cell coverage stats from a single grid eval pass.</summary>
    public sealed class CoverageGrid
    {
        public int LatCells { get; }
        public int LonCells { get; }
        public double LatStepDeg { get; }
        public double LonStepDeg { get; }

        /// <summary>Fraction of timesteps with at least one sat above min elevation. [0..1]</summary>
        public double[,] FractionVisible { get; }

        /// <summary>Mean count of visible sats across timesteps.</summary>
        public double[,] MeanVisibleCount { get; }

        /// <summary>Max count of visible sats observed in any single timestep.</summary>
        public int[,] MaxVisibleCount { get; }

        /// <summary>Mean (over timesteps) of best-rx-power across visible sats.
        /// Timesteps with no visible sat contribute the NoVisibilityFloorDbm sentinel,
        /// so cells never visible read close to that floor. Null unless link budget supplied.</summary>
        public double[,]? MeanRxPowerDbm { get; internal set; }

        /// <summary>Max rx-power ever observed across (timestep × visible sat). Null unless link budget supplied.</summary>
        public double[,]? MaxRxPowerDbm { get; internal set; }

        /// <summary>Max achievable data rate (bps) observed across (timestep × visible sat) using
        /// the rx-power-winner antenna's budget. Null unless link budget supplied.</summary>
        public double[,]? MaxDataRateBps { get; internal set; }

        public CoverageGrid(int latCells, int lonCells, double latStep, double lonStep)
        {
            LatCells = latCells; LonCells = lonCells;
            LatStepDeg = latStep; LonStepDeg = lonStep;
            FractionVisible = new double[latCells, lonCells];
            MeanVisibleCount = new double[latCells, lonCells];
            MaxVisibleCount = new int[latCells, lonCells];
        }

        /// <summary>Latitude (deg) at row index. Row 0 = +90 (north pole), row Lat-1 = -90.</summary>
        public double LatAt(int row) => 90.0 - (row + 0.5) * LatStepDeg;
        /// <summary>Longitude (deg) at column index. Col 0 = -180, col Lon-1 = +180-step.</summary>
        public double LonAt(int col) => -180.0 + (col + 0.5) * LonStepDeg;
    }

    public static class Coverage
    {
        /// <summary>Sentinel rx-power value used at timesteps where no satellite is visible.
        /// Anything well below practical noise floors works; -200 dBm is conventional.</summary>
        public const double NoVisibilityFloorDbm = -200.0;

        /// <summary>Compute achievable data rate from a known rx-power. Mirrors
        /// LinkBudget.MaxDataRateBps but takes pre-snapshotted antenna params for the
        /// inner-loop fast path.</summary>
        internal static double RateFromRx(double rxDbm, double bwHz, double noiseFloorDbm,
                                            double specEff, double requiredEbN0Db)
        {
            if (bwHz <= 0) return double.PositiveInfinity;
            double snrDb = rxDbm - noiseFloorDbm;
            double ebn0Db = snrDb - 10.0 * Math.Log10(specEff);
            double maxRate = bwHz * specEff;
            if (ebn0Db >= requiredEbN0Db) return maxRate;
            double shortfallDb = requiredEbN0Db - ebn0Db;
            int halvings = (int)Math.Ceiling(shortfallDb / 3.0);
            if (halvings >= 32) return 0;
            return maxRate / Math.Pow(2, halvings);
        }

        /// <summary>
        /// Evaluate coverage of a constellation over a regular lat/lon grid.
        /// </summary>
        /// <param name="sats">Constellation (orbit elements in inertial frame).</param>
        /// <param name="bodyRadius">Central body radius (m).</param>
        /// <param name="mu">Central body μ (m³/s²).</param>
        /// <param name="latCells">Number of latitude rows.</param>
        /// <param name="lonCells">Number of longitude columns.</param>
        /// <param name="timesteps">Number of evenly-spaced samples over the duration.</param>
        /// <param name="durationSec">Simulated duration (s). Default = first sat's orbital period.
        /// If body is rotating, supply ≥ one rotation period for full longitude smoothing.</param>
        /// <param name="minElevationDeg">Above-horizon threshold for visibility.</param>
        /// <param name="bodyRotationRateRadPerSec">Sidereal rotation rate. 0 = inertial Earth.</param>
        /// <param name="groundAntennas">List of antennas every sat carries (homogeneous fleet).
        /// Each antenna has a body-fixed boresight in the orbit-local LVLH frame; for each cell
        /// the planner picks the antenna with lowest pointing loss and reports its rx-power.
        /// Pass null/empty for visibility-only evaluation (no rx fields populated).</param>
        public static CoverageGrid Evaluate(IList<Satellite> sats,
                                            double bodyRadius,
                                            double mu,
                                            int latCells = 180,
                                            int lonCells = 360,
                                            int timesteps = 64,
                                            double durationSec = 0,
                                            double minElevationDeg = 10.0,
                                            double bodyRotationRateRadPerSec = 0,
                                            IList<SatAntenna>? groundAntennas = null,
                                            double startTimeSec = 0)
        {
            if (sats.Count == 0) throw new ArgumentException("Empty constellation.");
            // Instantaneous mode: timesteps=1, durationSec=0 (handled below). For multi-step
            // mode an unspecified duration defaults to one orbital period.
            if (durationSec <= 0 && timesteps > 1)
                durationSec = Geometry.OrbitalPeriod(sats[0].SemiMajorAxis, mu);

            double latStep = 180.0 / latCells;
            double lonStep = 360.0 / lonCells;
            var grid = new CoverageGrid(latCells, lonCells, latStep, lonStep);
            bool computeLink = groundAntennas != null && groundAntennas.Count > 0;
            int A = computeLink ? groundAntennas!.Count : 0;
            if (computeLink)
            {
                grid.MeanRxPowerDbm = new double[latCells, lonCells];
                grid.MaxRxPowerDbm = new double[latCells, lonCells];
                grid.MaxDataRateBps = new double[latCells, lonCells];
            }

            // Precompute body-fixed sat positions [ti, si] and antenna boresights [ti, si, a].
            var satPos = new Vec3d[timesteps, sats.Count];
            Vec3d[,,]? boresights = computeLink ? new Vec3d[timesteps, sats.Count, A] : null;
            for (int ti = 0; ti < timesteps; ti++)
            {
                double t = startTimeSec + ((timesteps == 1) ? 0 : ti * durationSec / (timesteps - 1));
                double rotAngle = -bodyRotationRateRadPerSec * t;
                bool rotates = rotAngle != 0;
                for (int si = 0; si < sats.Count; si++)
                {
                    Vec3d posI = sats[si].Position(mu, t);
                    satPos[ti, si] = rotates ? Geometry.RotZ(posI, rotAngle) : posI;
                    if (computeLink)
                    {
                        Vec3d velI = sats[si].Velocity(mu, t);
                        for (int a = 0; a < A; a++)
                        {
                            Vec3d biI = groundAntennas![a].BoresightInertial(posI, velI);
                            boresights![ti, si, a] = rotates ? Geometry.RotZ(biI, rotAngle) : biI;
                        }
                    }
                }
            }

            double sinMinElev = Math.Sin(minElevationDeg * Math.PI / 180.0);
            // Snapshot antenna params into local arrays for the inner loop.
            float[] antTx = new float[A], antTxG = new float[A], antRxG = new float[A];
            float[] antFreq = new float[A], antBw = new float[A];
            float[] antNoise = new float[A], antBwHz = new float[A];
            float[] antSpecEff = new float[A], antEbN0 = new float[A];
            bool[] antIsOmni = new bool[A];
            for (int a = 0; a < A; a++)
            {
                var b = groundAntennas![a].Budget;
                antTx[a] = b.TxPowerDbm; antTxG[a] = b.TxGainDbi; antRxG[a] = b.RxGainDbi;
                antFreq[a] = b.FrequencyHz; antBw[a] = b.BeamwidthDeg;
                antNoise[a] = b.NoiseFloorDbm;
                antBwHz[a] = b.BandwidthHz;
                antSpecEff[a] = b.MaxBitsPerSymbol * b.CodingRate;
                antEbN0[a] = b.RequiredEbN0Db;
                antIsOmni[a] = groundAntennas[a].IsOmnidirectional;
            }

            Parallel.For(0, latCells, row =>
            {
                double lat = grid.LatAt(row);
                for (int col = 0; col < lonCells; col++)
                {
                    double lon = grid.LonAt(col);
                    Vec3d ground = Geometry.LatLonToEcef(lat, lon, bodyRadius);
                    Vec3d normal = ground.Normalized;

                    int visibleSteps = 0;
                    int totalVisible = 0;
                    int maxVisible = 0;
                    double rxSum = 0;
                    double rxBest = double.NegativeInfinity;
                    double rateBest = 0;
                    for (int ti = 0; ti < timesteps; ti++)
                    {
                        int visibleHere = 0;
                        double bestRxThisStep = NoVisibilityFloorDbm;
                        int bestAntThisStep = -1;
                        for (int si = 0; si < sats.Count; si++)
                        {
                            Vec3d sp = satPos[ti, si];
                            Vec3d toSat = sp - ground;
                            double mag = toSat.Magnitude;
                            if (mag <= 0) continue;
                            double sinElev = Vec3d.Dot(normal, toSat) / mag;
                            if (sinElev >= sinMinElev)
                            {
                                visibleHere++;
                                if (computeLink)
                                {
                                    for (int a = 0; a < A; a++)
                                    {
                                        double pl = Physics.PathLoss(mag, antFreq[a]);
                                        double rx = antTx[a] + antTxG[a] + antRxG[a] - pl;
                                        if (!antIsOmni[a])
                                        {
                                            Vec3d toGroundDir = ground - sp;
                                            double cosOff = Vec3d.Dot(boresights![ti, si, a], toGroundDir) / mag;
                                            if (cosOff > 1) cosOff = 1; else if (cosOff < -1) cosOff = -1;
                                            double offDeg = Math.Acos(cosOff) * 180.0 / Math.PI;
                                            rx -= Physics.PointingLoss(offDeg, antBw[a]);
                                        }
                                        if (rx > bestRxThisStep) { bestRxThisStep = rx; bestAntThisStep = a; }
                                    }
                                }
                            }
                        }
                        if (visibleHere > 0) visibleSteps++;
                        totalVisible += visibleHere;
                        if (visibleHere > maxVisible) maxVisible = visibleHere;
                        if (computeLink)
                        {
                            rxSum += bestRxThisStep;
                            if (bestRxThisStep > rxBest)
                            {
                                rxBest = bestRxThisStep;
                                if (bestAntThisStep >= 0)
                                    rateBest = RateFromRx(bestRxThisStep,
                                        antBwHz[bestAntThisStep], antNoise[bestAntThisStep],
                                        antSpecEff[bestAntThisStep], antEbN0[bestAntThisStep]);
                            }
                        }
                    }
                    grid.FractionVisible[row, col] = (double)visibleSteps / timesteps;
                    grid.MeanVisibleCount[row, col] = (double)totalVisible / timesteps;
                    grid.MaxVisibleCount[row, col] = maxVisible;
                    if (computeLink)
                    {
                        grid.MeanRxPowerDbm![row, col] = rxSum / timesteps;
                        grid.MaxRxPowerDbm![row, col] = rxBest;
                        grid.MaxDataRateBps![row, col] = rateBest;
                    }
                }
            });

            return grid;
        }
    }
}
