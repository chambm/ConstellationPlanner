using ConstellationPlanner.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ConstellationPlanner.Cli;

/// <summary>JSON-friendly antenna aim. Convention matches SatAntenna: ElevationDeg = 0 →
/// nadir; AzimuthDeg measured from forward (velocity) toward orbit-normal "left".</summary>
public sealed class AntennaAim
{
    public double AzimuthDeg { get; set; }
    public double ElevationDeg { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>Inter-satellite-link configuration mode.</summary>
public enum IslMode
{
    /// <summary>No ISLs — sat↔sat hops disabled in routing.</summary>
    None,
    /// <summary>One omnidirectional antenna per sat (gain still applies, no pointing loss).
    /// Effectively models a phased-array or set of antennas blanketing every direction.</summary>
    Omni,
    /// <summary>Four fixed-aim antennas per sat at 45° off nadir: forward/aft (in-plane
    /// neighbours within the same orbit) and port/starboard (cross-plane neighbours in
    /// adjacent planes). Matches a typical Iridium-style four-ISL crosslink layout.</summary>
    Directional,
    /// <summary>Four target-locked antennas per sat, each aimed at a fixed Walker neighbour
    /// (forward/aft in-plane, port/starboard cross-plane). Matches RA's "lock to vessel"
    /// targeting in KSP — boresight tracks the target perfectly so on-axis gain applies and
    /// pointing loss is zero. The relay graph is restricted to those four neighbour edges
    /// per sat, mirroring real on-orbit hardware that can only relay to its assigned partners.
    /// In-plane antennas degenerate to dead weight when sats-per-plane = 2 (target 180° away,
    /// Earth-blocked); the planner still creates them but the LoS check filters them out.</summary>
    Targeted,
}

/// <summary>What the coverage heatmap colours represent.</summary>
public enum HeatmapMetric
{
    /// <summary>Best achievable rx-power in dBm (current default).</summary>
    RxPower,
    /// <summary>Best achievable data rate in bps, displayed log10-scaled.</summary>
    DataRate,
}

/// <summary>Whether the heatmap shows coverage averaged across a sidereal day or coverage
/// at a single instant in time (the latter is what animations want).</summary>
public enum CoverageMode
{
    /// <summary>Best-ever coverage across one sidereal day at 256 timesteps. Useful for
    /// "is this region ever covered?" questions.</summary>
    DailyAverage,
    /// <summary>Coverage at exactly cfg.TimeOffsetSec — single timestep. Useful for animation
    /// and "where is the constellation right now" snapshots.</summary>
    Instantaneous,
}

/// <summary>What kind of orbit each sat in the constellation flies. Selects which orbital
/// elements are actively edited vs. derived from a preset (period, inclination, etc.).</summary>
public enum OrbitType
{
    /// <summary>Walker-delta circular shell — eccentricity 0, ω undefined. Planes span the full
    /// 360° of RAAN. Inclination editable. LAN/ArgPe are computed by the Walker generator
    /// (per-plane RAAN, ω = 0).</summary>
    WalkerCircular,
    /// <summary>Walker-star polar circular shell — same orbit shape as <see cref="WalkerCircular"/>
    /// but planes span only 180° of RAAN, typical of polar / near-polar constellations
    /// (Iridium's 66/6/2 at 86.4° is the canonical example). Cross-plane ISL targeting omits
    /// the seam between plane 0 and plane P-1 because those neighbours are counter-rotating.</summary>
    WalkerStar,
    /// <summary>12-sidereal-hour critical-inclination shell. Inclination locked at 63.4°,
    /// SMA fixed by the period; user picks perigee, apogee/eccentricity follow. ArgPe defaults
    /// to 270° (apogee at northern peak) but is editable.</summary>
    Molniya,
    /// <summary>24-sidereal-hour critical-inclination shell — geosync period with non-zero
    /// eccentricity. Inclination locked at 63.4°, SMA fixed at GEO; user picks perigee.
    /// ArgPe defaults to 270°.</summary>
    Tundra,
    /// <summary>Free-form — perigee, apogee, inclination, LAN offset, and ω all editable.</summary>
    Custom,
}

/// <summary>All knobs for one render. Defaults to the current GEO-HG-61 scenario.</summary>
public sealed class PlannerInput
{
    // Constellation
    public OrbitType OrbitType { get; set; } = OrbitType.WalkerCircular;
    /// <summary>Perigee altitude (km) for elliptical orbits, or just "altitude" when
    /// <see cref="OrbitType"/> is WalkerCircular. Same field — UI relabels.</summary>
    public double AltitudeKm { get; set; } = 35786;
    /// <summary>Apogee altitude (km). Equals <see cref="AltitudeKm"/> for circular; differs
    /// for elliptical. Derived from period for Molniya/Tundra; freely editable in Custom.</summary>
    public double ApogeeAltitudeKm { get; set; } = 35786;
    public double InclinationDeg { get; set; } = 0;
    /// <summary>Argument of perigee (degrees) — orientation of the orbit's apse line within
    /// the orbit plane. 270° puts apogee at the northern apex (Molniya/Tundra default).</summary>
    public double ArgPerigeeDeg { get; set; } = 270;
    /// <summary>Constellation-wide RAAN offset (degrees) — rotates the whole Walker shell.
    /// Each plane's RAAN = LanOffsetDeg + planeIdx · 360°/P.</summary>
    public double LanOffsetDeg { get; set; } = 0;
    public int T { get; set; } = 4;
    public int P { get; set; } = 1;
    public int F { get; set; } = 0;
    public double PhaseOffsetDeg { get; set; } = 45;
    public double MinElevDeg { get; set; } = 10;

    // Antenna roster — separate ground (sat↔ground) vs ISL (sat↔sat) configs. Each role
    // has its own antenna model + band; sats carry N copies of the model with the per-aim
    // azimuth/elevation taken from the *Antennas lists below. Tech level drives the encoder
    // (modulation bits, coding rate, required Eb/N0) along with tx power and noise temp.
    public int TechLevel { get; set; } = 3;

    public double GroundAntennaDiameterM { get; set; } = 1.22;
    public double GroundFrequencyGHz { get; set; } = 1.5975;
    public double GroundBandwidthMHz { get; set; } = 128;
    /// <summary>Effective gain (dBi) of the ground-station-side antenna on sat↔ground links.
    /// Skopos models commercial earth stations (andover, goonhilly, pleumeur_bodou) at ~58 dBi
    /// for L/C-band 25-30 m dishes; DSN tracking sites are 49-63 dBi. Default 50 dBi sits between
    /// "small tracking" and "large telecom".</summary>
    public double GroundStationGainDbi { get; set; } = 50;
    /// <summary>Ground-station transmit power (dBm). Skopos telecom stations: 63-70 dBm
    /// (2-10 kW). Defaults to a value high enough that uplink isn't artificially the bottleneck.</summary>
    public double GroundStationTxPowerDbm { get; set; } = 63;

    /// <summary>Sat-side transmit power (dBm) for the ground-link antenna. <c>0</c> means
    /// "use TL.MaxPowerDbm" — the power the era's amplifier tech can realistically drive.
    /// Lowering this trades link margin for power consumption (consumption scales as 10^(P/10)
    /// for the radiated component plus 1/PowerEfficiency for the DC draw).</summary>
    public double GroundTxPowerDbm { get; set; } = 0;

    public IslMode IslMode { get; set; } = IslMode.None;
    public double IslAntennaDiameterM { get; set; } = 1.22;
    public double IslFrequencyGHz { get; set; } = 1.5975;
    public double IslBandwidthMHz { get; set; } = 128;
    /// <summary>Sat-side transmit power (dBm) for ISL antennas. Same semantics as
    /// <see cref="GroundTxPowerDbm"/>.</summary>
    public double IslTxPowerDbm { get; set; } = 0;
    /// <summary>When >0, treat the ISL antenna as omni with this fixed gain (dBi) and ignore
    /// IslAntennaDiameterM. Set by the GUI when Mode=Omni and an entry from the OmniAntennas
    /// catalog is selected.</summary>
    public double IslGainDbiOverride { get; set; } = 0;

    // Antenna aiming — name is informational; az/el define the boresight.
    public List<AntennaAim> GroundAntennas { get; set; } = new() { new AntennaAim { AzimuthDeg = 270, ElevationDeg = 0, Name = "nadir" } };

    // Heatmap metric — what the colours represent. RxPower (dBm) or DataRate (bps).
    public HeatmapMetric Metric { get; set; } = HeatmapMetric.RxPower;
    public CoverageMode CoverageMode { get; set; } = CoverageMode.DailyAverage;

    // Relay path
    public string PathFromName { get; set; } = "andover";
    public string PathToName { get; set; } = "goonhilly_downs";
    public double RequiredRateMbps { get; set; } = 0;
    public double LatencyLimitSec { get; set; } = 30;

    // Render
    public double TimeOffsetSec { get; set; } = 0;
    public int Upscale { get; set; } = 4;
    /// <summary>When true, skip the expensive parts of <see cref="Planner.Render"/>: the
    /// 360×180 coverage grid, ISL/ground-link enumeration, ground-track sampling, footprint
    /// computation, and PNG encoding. Only the path-relevant output fields (PathConnected,
    /// PathLatencyMs, PathRateBps, PathHops) are populated. Used by animation stats sweeps to
    /// sample the relay path at higher temporal resolution than the user-visible frame count
    /// without paying the heatmap cost on every sample.</summary>
    public bool SkipHeatmap { get; set; } = false;
    /// <summary>Multiplier applied to in-image text sizes on top of the upscale-driven
    /// auto-scaling. 1.0 keeps on-screen text size constant across upscale values; values
    /// above 1.0 grow text without affecting the heatmap resolution, below 1.0 shrink it.</summary>
    public double TextScale { get; set; } = 1.0;
    public bool FullCaption { get; set; } = true;

    // Overlay layer toggles (omitted layers are simply not drawn — heatmap colours are
    // unaffected). The relay path is always drawn when From/To are configured.
    public bool ShowTrackingLinks { get; set; } = true;
    public bool ShowTelecomLinks { get; set; } = true;
    public bool ShowIsls { get; set; } = true;
    public bool ShowFootprints { get; set; } = true;
}

/// <summary>What the planner returns for one render.</summary>
public sealed class PlannerOutput
{
    public byte[] PngBytes { get; set; } = Array.Empty<byte>();
    public string Status { get; set; } = "";
    public bool PathConnected { get; set; }
    public int PathHops { get; set; }
    public double PathRateBps { get; set; }
    public double PathLatencyMs { get; set; }
    /// <summary>True when the path connected but its bottleneck rate is below the user's
    /// configured RequiredRateMbps. The map renders the path in red and stats can split
    /// "uptime" from "uptime meeting required rate."</summary>
    public bool PathBelowRequired { get; set; }
    /// <summary>Per-antenna power figures (W) for one satellite. <c>Tx</c> is the radiated RF
    /// power, <c>Dc</c> includes amplifier inefficiency from the era's PowerEfficiency curve.
    /// Multiply <c>SatTotalDcW</c> by the constellation size for the bus-power total.</summary>
    public double GroundTxW { get; set; }
    public double GroundDcW { get; set; }
    public double IslTxW { get; set; }
    public double IslDcW { get; set; }
    public double SatTotalDcW { get; set; }
    public int IslCount { get; set; }
    public int GroundLinkCount { get; set; }
    /// <summary>Per-timestep min / max / mean of the achievable rate (bps) across ISL links
    /// that survive LoS + the rate-floor filter. Zero when no ISLs are present. Used by the
    /// animation stats sweep to aggregate cycle-wide min/max/avg ISL bandwidth.</summary>
    public double IslMinRateBps { get; set; }
    public double IslMaxRateBps { get; set; }
    public double IslMeanRateBps { get; set; }
    public double FootprintHalfAngleDeg { get; set; }
    public double GainDbi { get; set; }
    public double BeamwidthDeg { get; set; }
    public double NoiseFloorDbm { get; set; }

    /// <summary>Raw heatmap values laid out the same way as the rendered image: row 0 = lat
    /// +90 → row Rows-1 = lat -90; col 0 = lon -180 → col Cols-1 = lon +180. Units depend
    /// on the active metric: log10(bps) for DataRate, dBm for RxPower. Used by the GUI's
    /// hover-value readout.</summary>
    public double[,]? HeatmapData { get; set; }
    /// <summary>Pixel width of the map region within <see cref="PngBytes"/> (= Cols × Upscale).
    /// The colorbar/caption sit below this.</summary>
    public int HeatmapMapPixelWidth { get; set; }
    /// <summary>Pixel height of the map region within <see cref="PngBytes"/>.</summary>
    public int HeatmapMapPixelHeight { get; set; }
    /// <summary>Formats one cell value for display — same formatter the colorbar uses, prefixed
    /// with units (e.g. "12.0 Mbps" or "-94.3 dBm").</summary>
    public Func<double, string>? HeatmapValueFormatter { get; set; }
}

/// <summary>RA RealismOverhaul TechLevel table (subset). Indexed by TL 0..10. The encoder
/// fields (ModBits/CodingRate/RequiredEbN0Db) reflect what's typical for each era — TL 0-4
/// are mostly BPSK/FM with no FEC; TL5 unlocks block coding (rate-1/2); TL6 brings
/// convolutional coding; TL7+ adds higher-order modulation (QPSK, 8PSK, 16QAM) with
/// concatenated/turbo codes.</summary>
public static class TechLevels
{
    public record Params(
        double MaxPowerDbm,
        double NoiseTempK,
        double ReflectorEff,
        int ModulationBits,
        double CodingRate,
        double RequiredEbN0Db,
        double PowerEfficiency);   // DC→RF efficiency of the transmit amplifier (0..1)

    // Power-efficiency progression follows the historical amp-tech curve: tube/klystron amps
    // ~5–15% in early eras, solid-state TWTAs and SSPAs reaching 30–50% by the 80s, modern
    // GaN HPAs hitting 50–60%. Used to compute DC consumption from radiated power.
    static readonly Params[] _byLevel =
    {
        new Params(20, 27000, 0.50, 1, 1.0,  10.0, 0.05), // TL0 — WW2-era, AM/FM, no FEC
        new Params(30, 11500, 0.52, 1, 1.0,  10.0, 0.07), // TL1 — Lunar range comms
        new Params(37,  7000, 0.54, 1, 1.0,  10.0, 0.10), // TL2 — Digital comms 1959-60
        new Params(37,  5800, 0.56, 1, 1.0,  10.0, 0.15), // TL3 — Interplanetary 1961-63
        new Params(40,  4500, 0.58, 1, 1.0,   8.0, 0.20), // TL4 — Improved 1964-66
        new Params(43,  3000, 0.60, 1, 0.5,  4.4, 0.25),  // TL5 — Block coding 1969
        new Params(43,  1540, 0.62, 1, 0.5,  3.5, 0.30),  // TL6 — Convolutional ~1973
        new Params(46,  1100, 0.64, 2, 0.5,  3.0, 0.35),  // TL7 — QPSK + concatenated 1976-80
        new Params(46,   500, 0.66, 3, 0.5,  2.5, 0.42),  // TL8 — 8PSK + Reed-Solomon 1986-97
        new Params(50,   200, 0.68, 4, 0.5,  2.0, 0.50),  // TL9 — 16QAM + turbo 1998-2008
        new Params(50,   200, 0.70, 4, 0.5,  1.5, 0.60),  // TL10 — modern 2009+
    };

    public static Params Get(int tl)
    {
        if (tl < 0) tl = 0;
        if (tl >= _byLevel.Length) tl = _byLevel.Length - 1;
        return _byLevel[tl];
    }
}

public static class Planner
{
    const double EarthRadius = 6_371_000.0;
    const double EarthMu = 3.986e14;
    const double EarthSiderealDay = 86_164.0;
    static readonly double EarthRotationRate = 2 * Math.PI / EarthSiderealDay;

    static List<GroundStation>? _stationsCache;
    static StationAntennaCatalog? _catalogCache;
    static List<SkoposConnection>? _connectionsCache;
    static readonly object _stationsLock = new();

    /// <summary>Orbital period (seconds) for a circular orbit at altitude <paramref name="altitudeKm"/>
    /// above Earth's surface.</summary>
    public static double OrbitalPeriodSec(double altitudeKm)
        => Geometry.OrbitalPeriod(EarthRadius + altitudeKm * 1000, EarthMu);

    /// <summary>Orbital period from perigee + apogee altitudes — depends only on SMA per
    /// Kepler's third law, so eccentric orbits with the same a have the same period.</summary>
    public static double OrbitalPeriodSec(double perigeeAltKm, double apogeeAltKm)
    {
        double rPe = EarthRadius + perigeeAltKm * 1000;
        double rAp = EarthRadius + apogeeAltKm  * 1000;
        return Geometry.OrbitalPeriod((rPe + rAp) / 2, EarthMu);
    }

    /// <summary>SMA (m) such that the resulting orbit has period <paramref name="periodSec"/>.
    /// Inverse of Kepler's third law — used by Molniya/Tundra presets to pin the period.</summary>
    public static double SmaForPeriod(double periodSec)
        => Math.Pow(EarthMu * periodSec * periodSec / (4 * Math.PI * Math.PI), 1.0 / 3.0);

    /// <summary>One row of multi-connection-evaluation results. Aggregates over the cycle
    /// sweep: connection-existence uptime regardless of rate, met-window uptime where both
    /// rate and latency requirements were satisfied, mean rate / latency over the connected
    /// samples (the same definitions the GUI's animation summary uses for the path stats).</summary>
    public sealed class ConnectionEvalResult
    {
        public SkoposConnection Connection = null!;
        public string FromName = "";
        public string ToName = "";
        public int FromIdx;
        public int ToIdx;
        public int Samples;
        public int ConnectedCount;     // physical path existed
        public int MetCount;           // path existed AND rate ≥ required AND latency ≤ limit
        public double SumLatencyMs;
        public double SumRateBps;
        public double UptimePct => Samples > 0 ? 100.0 * ConnectedCount / Samples : 0;
        public double MetWindowPct => Samples > 0 ? 100.0 * MetCount / Samples : 0;
        public double AvgLatencyMs => ConnectedCount > 0 ? SumLatencyMs / ConnectedCount : 0;
        public double AvgRateBps   => ConnectedCount > 0 ? SumRateBps   / ConnectedCount : 0;
    }

    /// <summary>Evaluate a list of Skopos connections over the constellation's repeat cycle,
    /// with a shared <see cref="NetworkUsage"/> at each timestep so each connection sees
    /// remaining capacity after earlier ones (in declaration order) consume their share.
    /// Mirrors how Skopos evaluates connections sequentially within one network tick. Each
    /// connection's per-rx pair is evaluated independently — multi-rx Skopos connections are
    /// expanded into multiple <c>(SkoposConnection, rx_index)</c> rows by the caller.</summary>
    public static List<ConnectionEvalResult> EvaluateConnectionsOverCycle(
        PlannerInput cfg,
        IList<(SkoposConnection Conn, int RxIndex)> connections,
        double durationSec,
        int samples,
        Action<int, int>? progress = null)
    {
        var stations = LoadStations();
        var results = new List<ConnectionEvalResult>(connections.Count);
        // Resolve station indices once — they don't change across timesteps.
        foreach (var (conn, rxIdx) in connections)
        {
            string fromName = conn.TxStation;
            string toName = conn.RxStations[rxIdx];
            results.Add(new ConnectionEvalResult
            {
                Connection = conn,
                FromName = fromName,
                ToName = toName,
                FromIdx = StationLoader.IndexOf(stations, fromName),
                ToIdx   = StationLoader.IndexOf(stations, toName),
                Samples = samples,
            });
        }

        // Walker shell construction depends on cfg, not time — build the satellite roster once
        // and just re-propagate per timestep. ISL antennas + targets follow the same pattern.
        var (groundAntennas, pathGroundAntennasPerConn, islAntennas, islTargets, ratePerConn) =
            BuildAntennasForConnections(cfg, connections, stations);

        // sats roster depends on cfg only (no per-connection variation). Build once.
        var sats = BuildSats(cfg);

        // Per-timestep loop. Within each timestep, evaluate connections sequentially with
        // shared NetworkUsage so capacity allocations propagate.
        var locker = new object();
        System.Threading.Tasks.Parallel.For(0, samples, i =>
        {
            double tSec = i * durationSec / Math.Max(1, samples);
            var satBfPositions = sats.Select(s =>
                Geometry.ToBodyFixed(s.Position(EarthMu, tSec), EarthRotationRate, tSec)).ToList();
            var groundBoresights = AntennaPointing.ComputeBodyFixed(sats, groundAntennas, EarthMu, tSec, EarthRotationRate);
            var islBoresights    = AntennaPointing.ComputeBodyFixed(sats, islAntennas,    EarthMu, tSec, EarthRotationRate);

            var usage = new NetworkUsage();
            for (int c = 0; c < connections.Count; c++)
            {
                var r = results[c];
                if (r.FromIdx < 0 || r.ToIdx < 0) continue;
                double reqBps = ratePerConn[c];
                double latLimit = connections[c].Conn.LatencySec;
                var path = Relay.Find(r.FromIdx, r.ToIdx, stations, satBfPositions,
                                       pathGroundAntennasPerConn[c], groundBoresights,
                                       islAntennas, islBoresights,
                                       EarthRadius, atmosphereMarginM: 50_000,
                                       cfg.MinElevDeg, latencyLimitSec: latLimit,
                                       requiredDataRateBps: 0,    // unfiltered; we measure the achieved rate
                                       islTargets: islTargets,
                                       usage: usage);
                if (!path.Connected) continue;
                bool met = path.BottleneckDataRateBps >= reqBps
                            && (latLimit <= 0 || path.TotalLatencySec <= latLimit);
                lock (locker)
                {
                    r.ConnectedCount++;
                    if (met) r.MetCount++;
                    r.SumLatencyMs += path.TotalLatencySec * 1000;
                    r.SumRateBps   += path.BottleneckDataRateBps;
                }
            }
            progress?.Invoke(i + 1, samples);
        });

        return results;
    }

    static List<Satellite> BuildSats(PlannerInput cfg)
    {
        bool isWalkerCircular = cfg.OrbitType == OrbitType.WalkerCircular;
        bool isWalkerStar     = cfg.OrbitType == OrbitType.WalkerStar;
        bool isCircular       = isWalkerCircular || isWalkerStar;
        double pe = cfg.AltitudeKm;
        double ap = isCircular ? cfg.AltitudeKm : cfg.ApogeeAltitudeKm;
        double rPe = EarthRadius + pe * 1000;
        double rAp = EarthRadius + ap * 1000;
        double smaM = (rPe + rAp) / 2;
        double ecc = Math.Max(0, (rAp - rPe) / (rAp + rPe));
        double argPeDeg = isCircular ? 0 : cfg.ArgPerigeeDeg;
        double lanOffsetDeg = isWalkerCircular ? 0 : cfg.LanOffsetDeg;
        var raw = (isWalkerStar
            ? Walker.Star(altitude: smaM - EarthRadius, bodyRadius: EarthRadius,
                           inclinationDeg: cfg.InclinationDeg,
                           t: cfg.T, p: cfg.P, f: cfg.F,
                           eccentricity: ecc,
                           argPerigeeDeg: argPeDeg,
                           raanOffsetDeg: lanOffsetDeg)
            : Walker.Delta(altitude: smaM - EarthRadius, bodyRadius: EarthRadius,
                            inclinationDeg: cfg.InclinationDeg,
                            t: cfg.T, p: cfg.P, f: cfg.F,
                            eccentricity: ecc,
                            argPerigeeDeg: argPeDeg,
                            raanOffsetDeg: lanOffsetDeg));
        return raw.Select(s => new Satellite(
            s.SemiMajorAxis, s.Eccentricity, s.InclinationDeg, s.RaanDeg, s.ArgPerigeeDeg,
            s.TrueAnomalyAtT0Deg + cfg.PhaseOffsetDeg)).ToList();
    }

    /// <summary>Build per-connection antennas + a shared ISL config. Each connection routes
    /// against a budget tuned to its specific from/to stations' catalog gain so the rate
    /// computation is correct when the two endpoints have different antenna sizes; the ISL
    /// path is the same for all connections (one constellation hardware spec).</summary>
    static (List<SatAntenna> groundAntennas,
             List<List<SatAntenna>> pathGroundAntennasPerConn,
             List<SatAntenna> islAntennas,
             int[,]? islTargets,
             double[] ratePerConn)
        BuildAntennasForConnections(PlannerInput cfg,
                                     IList<(SkoposConnection Conn, int RxIndex)> connections,
                                     IList<GroundStation> stations)
    {
        var tl = TechLevels.Get(cfg.TechLevel);

        LinkBudget MakeBudget(double diameterM, double freqGHz, double bwMHz,
                              double gainDbiOverride = 0, double otherEndGainDbi = -1,
                              double txPowerDbmOverride = 0)
        {
            float freq = (float)(freqGHz * 1e9);
            float bw   = (float)(bwMHz * 1e6);
            float gain = gainDbiOverride > 0
                ? (float)gainDbiOverride
                : Physics.GainFromDishDiamater((float)diameterM, freq, (float)tl.ReflectorEff);
            float rxGain = otherEndGainDbi >= 0 ? (float)otherEndGainDbi : gain;
            float txPower = txPowerDbmOverride > 0 ? (float)txPowerDbmOverride : (float)tl.MaxPowerDbm;
            return new LinkBudget(
                txPowerDbm: txPower, txGainDbi: gain, rxGainDbi: rxGain,
                frequencyHz: freq, bandwidthHz: bw,
                systemNoiseTempK: (float)tl.NoiseTempK,
                maxBitsPerSymbol: tl.ModulationBits,
                codingRate: (float)tl.CodingRate,
                requiredEbN0Db: (float)tl.RequiredEbN0Db);
        }

        // Coverage-side ground antenna (same as the standalone snapshot path) — used here only
        // for Relay.Find's general-purpose ground antenna list (which is mostly irrelevant for
        // multi-connection eval since each connection picks its own pathGroundAntennas, but the
        // signature requires it).
        var coverageGroundBudget = MakeBudget(cfg.GroundAntennaDiameterM, cfg.GroundFrequencyGHz, cfg.GroundBandwidthMHz, txPowerDbmOverride: cfg.GroundTxPowerDbm);
        var groundAntennas = cfg.GroundAntennas.Select((a, i) =>
            new SatAntenna(string.IsNullOrEmpty(a.Name) ? $"ground{i}" : a.Name,
                           a.AzimuthDeg, a.ElevationDeg, coverageGroundBudget)).ToList();

        // Per-connection path-side ground antennas tailored to the from/to station's catalog
        // gain at this band+TL — same logic as the single-snapshot path in Render.
        string band = cfg.GroundFrequencyGHz < 0.3 ? "VHF"
                    : cfg.GroundFrequencyGHz < 1.0 ? "UHF"
                    : cfg.GroundFrequencyGHz < 2.0 ? "L"
                    : cfg.GroundFrequencyGHz < 4.0 ? "S"
                    : cfg.GroundFrequencyGHz < 8.0 ? "C"
                    : cfg.GroundFrequencyGHz < 12.0 ? "X"
                    : cfg.GroundFrequencyGHz < 18.0 ? "Ku" : "Ka";
        var pathGroundAntennasPerConn = new List<List<SatAntenna>>(connections.Count);
        var ratePerConn = new double[connections.Count];
        var cat = StationAntennas;
        for (int c = 0; c < connections.Count; c++)
        {
            var (conn, rxIdx) = connections[c];
            ratePerConn[c] = conn.DataRateBps;
            var fromAnt = cat.Get(conn.TxStation, band, cfg.TechLevel);
            var toAnt   = cat.Get(conn.RxStations[rxIdx], band, cfg.TechLevel);
            double pathGain = fromAnt.HasValue && toAnt.HasValue ? Math.Min(fromAnt.Value.GainDbi, toAnt.Value.GainDbi)
                            : fromAnt.HasValue ? fromAnt.Value.GainDbi
                            : toAnt.HasValue   ? toAnt.Value.GainDbi
                            : cfg.GroundStationGainDbi;
            var pathBudget = MakeBudget(cfg.GroundAntennaDiameterM, cfg.GroundFrequencyGHz, cfg.GroundBandwidthMHz,
                                         otherEndGainDbi: pathGain, txPowerDbmOverride: cfg.GroundTxPowerDbm);
            pathGroundAntennasPerConn.Add(cfg.GroundAntennas.Select((a, i) =>
                new SatAntenna(string.IsNullOrEmpty(a.Name) ? $"ground{i}" : a.Name,
                               a.AzimuthDeg, a.ElevationDeg, pathBudget)).ToList());
        }

        var islBudget = MakeBudget(cfg.IslAntennaDiameterM, cfg.IslFrequencyGHz, cfg.IslBandwidthMHz,
                                    cfg.IslGainDbiOverride, txPowerDbmOverride: cfg.IslTxPowerDbm);
        var islAntennas = cfg.IslMode switch
        {
            IslMode.None => new List<SatAntenna>(),
            IslMode.Omni => new List<SatAntenna> {
                new SatAntenna("omni", azDeg: 0, elDeg: 0, islBudget, isIsl: true, isOmnidirectional: true),
            },
            IslMode.Directional => new List<SatAntenna> {
                new SatAntenna("forward",   azDeg: 0,   elDeg: 45, islBudget, isIsl: true),
                new SatAntenna("aft",       azDeg: 180, elDeg: 45, islBudget, isIsl: true),
                new SatAntenna("port",      azDeg: 90,  elDeg: 45, islBudget, isIsl: true),
                new SatAntenna("starboard", azDeg: 270, elDeg: 45, islBudget, isIsl: true),
            },
            IslMode.Targeted => new List<SatAntenna> {
                new SatAntenna("fwd-target",  azDeg: 0,   elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
                new SatAntenna("aft-target",  azDeg: 180, elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
                new SatAntenna("port-target", azDeg: 90,  elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
                new SatAntenna("stbd-target", azDeg: 270, elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
            },
            _ => new List<SatAntenna>(),
        };
        int[,]? islTargets = cfg.IslMode == IslMode.Targeted
            ? Walker.NeighborMap(cfg.T, cfg.P, isStar: cfg.OrbitType == OrbitType.WalkerStar)
            : null;

        return (groundAntennas, pathGroundAntennasPerConn, islAntennas, islTargets, ratePerConn);
    }

    /// <summary>Approximate ground-track repeat period: smallest cycle length within
    /// <paramref name="maxCycleHours"/> after which the constellation's body-fixed appearance
    /// returns to (within <paramref name="maxToleranceDeg"/> of) its starting state. Computed
    /// via continued-fraction convergents of T_sid/T_orb — each convergent p/q means
    /// "p orbits ≈ q sidereal days" with some residual angular error. Returns the convergent
    /// whose error is below tolerance and whose cycle fits the window, falling back to the
    /// best-found convergent within the window when no candidate hits the tolerance.
    /// <para>Animations capped at this length loop with at most the returned <c>ErrorDeg</c>
    /// of body-rotation snap at the loop transition. For altitudes where no rational
    /// approximation works in the window, the snap is unavoidable — pick a different altitude
    /// (e.g. one whose orbit divides cleanly into a sidereal day) for shorter cycles.</para></summary>
    public static (double CycleSec, double ErrorDeg, int Orbits, int SiderealDays) GroundTrackRepeat(
        double perigeeAltKm, double apogeeAltKm, double maxToleranceDeg = 1.0, double maxCycleHours = 168.0)
    {
        double tOrb = OrbitalPeriodSec(perigeeAltKm, apogeeAltKm);
        double tSid = EarthSiderealDay;
        double maxCycleSec = maxCycleHours * 3600;
        // Continued-fraction expansion of tSid/tOrb. Convergents p_k/q_k from the recurrence
        // p_k = a_k·p_{k-1} + p_{k-2}, q_k similarly, where a_k = floor of the running fraction.
        // Standard seed: p_{-2}=0, p_{-1}=1, q_{-2}=1, q_{-1}=0 → the first convergent ends up
        // p_0/q_0 = a_0/1, which is what we want when x ≈ a_0 + small fraction.
        double xRem = tSid / tOrb;
        long pPrev2 = 0, pPrev1 = 1;
        long qPrev2 = 1, qPrev1 = 0;
        double bestCycle = tOrb;            // fallback: one orbit (will snap by ω·T_orb degrees)
        double bestErr = Math.Abs(tOrb % tSid) / tSid * 360.0;
        int bestP = 1, bestQ = 0;
        for (int iter = 0; iter < 30; iter++)
        {
            long ai = (long)Math.Floor(xRem);
            long pCur = ai * pPrev1 + pPrev2;
            long qCur = ai * qPrev1 + qPrev2;
            double cycle = qCur > 0 ? qCur * tSid : pCur * tOrb;
            if (cycle > maxCycleSec) break;
            // Residual: |p·T_orb − q·T_sid| → angular error when wrapped onto Earth's rotation.
            double errSec = Math.Abs(pCur * tOrb - qCur * tSid);
            double errDeg = errSec / tSid * 360.0;
            if (errDeg < bestErr) { bestErr = errDeg; bestCycle = cycle; bestP = (int)pCur; bestQ = (int)qCur; }
            if (errDeg < maxToleranceDeg) break;
            double frac = xRem - ai;
            if (frac < 1e-12) break;
            xRem = 1.0 / frac;
            pPrev2 = pPrev1; pPrev1 = pCur;
            qPrev2 = qPrev1; qPrev1 = qCur;
        }
        return (bestCycle, bestErr, bestP, bestQ);
    }

    public static List<GroundStation> LoadStations()
    {
        EnsureLoaded();
        return _stationsCache!;
    }

    public static StationAntennaCatalog StationAntennas
    {
        get { EnsureLoaded(); return _catalogCache!; }
    }

    /// <summary>Skopos <c>connection { }</c> blocks parsed from telecom.cfg. Lets the GUI
    /// auto-fill the path picker with real Skopos service definitions instead of relying on
    /// the user to remember each connection's required rate / latency / window manually.</summary>
    public static List<SkoposConnection> SkoposConnections
    {
        get { EnsureLoaded(); return _connectionsCache!; }
    }

    /// <summary>Default Steam install path for KSP — used when no user override is set. The
    /// GUI can call <see cref="ReloadSkoposCfg"/> to point at a different telecom.cfg location
    /// (e.g., a non-default install, or KSP installed via a Linux distro's package manager).</summary>
    const string DefaultGameDataRoot = @"C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program\GameData";

    /// <summary>User-overridden full path to Skopos's telecom.cfg. When set, takes precedence
    /// over the Steam default. The GameData root is derived as that file's grandparent dir, so
    /// RealAntennas's RealSolarSystem.cfg is looked up alongside.</summary>
    static string? _userSkoposCfgPath;

    /// <summary>Tell the planner to load station/connection data from a user-chosen telecom.cfg
    /// instead of the Steam default. Invalidates the cache so the next <see cref="LoadStations"/>
    /// re-parses. The GUI invokes this when the user picks a file from the connection
    /// dropdown's "browse for telecom.cfg" entry.</summary>
    public static void ReloadSkoposCfg(string telecomCfgPath)
    {
        lock (_stationsLock)
        {
            _userSkoposCfgPath = telecomCfgPath;
            _stationsCache = null;
            _catalogCache = null;
            _connectionsCache = null;
        }
    }

    static void EnsureLoaded()
    {
        if (_stationsCache != null && _catalogCache != null && _connectionsCache != null) return;
        lock (_stationsLock)
        {
            if (_stationsCache != null && _catalogCache != null && _connectionsCache != null) return;

            // Resolve cfg paths: user override (if set + file exists) takes precedence,
            // otherwise fall back to the Steam default. GameData root for RA is derived from
            // the chosen telecom.cfg's grandparent (…/GameData/Skopos/telecom.cfg → …/GameData).
            string skoposCfg;
            string raCfg;
            if (!string.IsNullOrEmpty(_userSkoposCfgPath) && File.Exists(_userSkoposCfgPath))
            {
                skoposCfg = _userSkoposCfgPath!;
                string? gameData = Path.GetDirectoryName(Path.GetDirectoryName(skoposCfg));
                raCfg = gameData != null
                    ? Path.Combine(gameData, "RealAntennas", "PlanetPacks", "RealSolarSystem.cfg")
                    : "";
            }
            else
            {
                skoposCfg = Path.Combine(DefaultGameDataRoot, "Skopos", "telecom.cfg");
                raCfg = Path.Combine(DefaultGameDataRoot, "RealAntennas", "PlanetPacks", "RealSolarSystem.cfg");
            }

            var catalog = new StationAntennaCatalog();
            var s = new List<GroundStation>();
            s.AddRange(StationLoader.LoadRATracking(raCfg));
            s.AddRange(StationLoader.LoadSkoposTelecom(skoposCfg, catalog));
            if (s.Count == 0)
            {
                s.Add(new GroundStation("goldstone",  35.4267, -116.8900, StationKind.Tracking));
                s.Add(new GroundStation("madrid",     40.4314,   -4.2480, StationKind.Tracking));
                s.Add(new GroundStation("canberra",  -35.4014,  148.9819, StationKind.Tracking));
            }
            _catalogCache = catalog;
            _stationsCache = s;
            _connectionsCache = StationLoader.LoadSkoposConnections(skoposCfg);
        }
    }

    /// <summary>Look up a station's effective antenna at the configured ground band and TL.
    /// Returns null when the station is in the catalog but has no antenna for this band, or
    /// when the station isn't in the catalog at all (e.g. RA tracking sites whose antennas
    /// aren't per-station).</summary>
    public static StationAntennaSpec.Effective? GetStationAntenna(string stationName, double freqGHz, int techLevel)
    {
        if (string.IsNullOrEmpty(stationName)) return null;
        string band = BandPrefixFor(freqGHz);
        return StationAntennas.Get(stationName, band, techLevel);
    }

    /// <summary>Map a ground frequency to the catalog's band-prefix string.</summary>
    static string BandPrefixFor(double freqGHz)
    {
        if (freqGHz < 0.3)   return "VHF";
        if (freqGHz < 1.0)   return "UHF";
        if (freqGHz < 2.0)   return "L";
        if (freqGHz < 4.0)   return "S";
        if (freqGHz < 8.0)   return "C";
        if (freqGHz < 12.0)  return "X";
        if (freqGHz < 18.0)  return "Ku";
        return "Ka";
    }

    /// <summary>Receive-side gain (dBi) used in the heatmap's coverage budget. Prefers the
    /// bottleneck of the FROM/TO pair when both have antennas at the chosen band+TL — that way
    /// the heatmap colors mean "rate this specific path's worse end can pick up from here". Falls
    /// back to whichever endpoint is catalogued, then to the global minimum across all catalog
    /// stations, then to null (caller uses symmetric sat-dish budget).</summary>
    static double? ResolveCoverageStationGain(PlannerInput cfg)
    {
        string band = BandPrefixFor(cfg.GroundFrequencyGHz);
        var fromAnt = !string.IsNullOrEmpty(cfg.PathFromName)
            ? StationAntennas.Get(cfg.PathFromName, band, cfg.TechLevel) : null;
        var toAnt = !string.IsNullOrEmpty(cfg.PathToName)
            ? StationAntennas.Get(cfg.PathToName, band, cfg.TechLevel) : null;
        if (fromAnt.HasValue && toAnt.HasValue)
            return Math.Min(fromAnt.Value.GainDbi, toAnt.Value.GainDbi);
        if (fromAnt.HasValue) return fromAnt.Value.GainDbi;
        if (toAnt.HasValue)   return toAnt.Value.GainDbi;
        // Neither path endpoint has an antenna at this band — fall back to the global minimum.
        double? minGain = null;
        foreach (var name in StationAntennas.Stations)
        {
            var ant = StationAntennas.Get(name, band, cfg.TechLevel);
            if (ant == null) continue;
            if (minGain == null || ant.Value.GainDbi < minGain.Value) minGain = ant.Value.GainDbi;
        }
        return minGain;
    }

    /// <summary>Gain (dBi) for the FROM/TO ground stations at the configured band+TL, or null
    /// when the link can't form because at least one endpoint is in the catalog but has no
    /// antenna for the chosen band (e.g. andover at UHF — Skopos doesn't ship a UHF dish for it).
    /// Distinguishes "uncatalogued station → fall back to cfg" from "catalogued but wrong band
    /// → unreachable" so we don't hallucinate a 50 dBi UHF dish where Skopos has none.</summary>
    static double? ResolvePathStationGain(PlannerInput cfg)
    {
        string band = BandPrefixFor(cfg.GroundFrequencyGHz);
        var cat = StationAntennas;
        bool fromKnown = !string.IsNullOrEmpty(cfg.PathFromName) && cat.Contains(cfg.PathFromName);
        bool toKnown   = !string.IsNullOrEmpty(cfg.PathToName)   && cat.Contains(cfg.PathToName);
        var fromAnt = fromKnown ? cat.Get(cfg.PathFromName, band, cfg.TechLevel) : null;
        var toAnt   = toKnown   ? cat.Get(cfg.PathToName,   band, cfg.TechLevel) : null;
        // If a known catalog station has no antenna at the requested band, the link can't form.
        if (fromKnown && fromAnt == null) return null;
        if (toKnown   && toAnt   == null) return null;
        if (fromAnt.HasValue && toAnt.HasValue)
            return Math.Min(fromAnt.Value.GainDbi, toAnt.Value.GainDbi);
        if (fromAnt.HasValue) return fromAnt.Value.GainDbi;
        if (toAnt.HasValue)   return toAnt.Value.GainDbi;
        // Neither endpoint is in the catalog — the user added custom stations; respect their
        // override knob.
        return cfg.GroundStationGainDbi;
    }

    public static PlannerOutput Render(PlannerInput cfg)
    {
        var stations = LoadStations();

        var tl = TechLevels.Get(cfg.TechLevel);

        LinkBudget MakeBudget(double diameterM, double freqGHz, double bwMHz, double gainDbiOverride = 0, double otherEndGainDbi = -1, double txPowerDbmOverride = 0)
        {
            float freq = (float)(freqGHz * 1e9);
            float bw = (float)(bwMHz * 1e6);
            float gain = gainDbiOverride > 0
                ? (float)gainDbiOverride
                : Physics.GainFromDishDiamater((float)diameterM, freq, (float)tl.ReflectorEff);
            // For asymmetric links (sat ↔ ground): tx side = sat antenna (gain),
            // rx side = ground station antenna (otherEndGainDbi). Symmetric otherwise.
            float rxGain = otherEndGainDbi >= 0 ? (float)otherEndGainDbi : gain;
            float txPower = txPowerDbmOverride > 0 ? (float)txPowerDbmOverride : (float)tl.MaxPowerDbm;
            return new LinkBudget(
                txPowerDbm: txPower,
                txGainDbi: gain, rxGainDbi: rxGain,
                frequencyHz: freq,
                bandwidthHz: bw,
                systemNoiseTempK: (float)tl.NoiseTempK,
                maxBitsPerSymbol: tl.ModulationBits,
                codingRate: (float)tl.CodingRate,
                requiredEbN0Db: (float)tl.RequiredEbN0Db);
        }

        // Two budgets for sat↔ground links:
        //   - coverageGroundBudget: symmetric sat-dish on both ends. The heatmap shows
        //     "what a generic small receiver could pick up" — using the user's station gain
        //     here would inflate the colormap with values that only apply to the specific
        //     telecom dish.
        //   - pathGroundBudget: asymmetric (sat-dish tx, ground station rx). The ground gain
        //     is auto-derived from the FROM/TO stations' Skopos antenna entries when both are
        //     in the catalog at the configured band+TL; otherwise falls back to the cfg value.
        double? pathStationGainDbiNullable = ResolvePathStationGain(cfg);
        double pathStationGainDbi = pathStationGainDbiNullable ?? cfg.GroundStationGainDbi;
        bool pathBandUnsupported = pathStationGainDbiNullable == null;
        // Heatmap coverage budget — receive-side uses the lowest catalog-station gain at this
        // band so the colors mean "rate the worst-equipped real station could pick up". Falls
        // back to the symmetric sat-dish-on-both-ends budget when no station has an antenna at
        // the chosen band (e.g. UHF/VHF with default catalog).
        double? coverageStationGainDbi = ResolveCoverageStationGain(cfg);
        var coverageGroundBudget = coverageStationGainDbi.HasValue
            ? MakeBudget(cfg.GroundAntennaDiameterM, cfg.GroundFrequencyGHz, cfg.GroundBandwidthMHz, otherEndGainDbi: coverageStationGainDbi.Value, txPowerDbmOverride: cfg.GroundTxPowerDbm)
            : MakeBudget(cfg.GroundAntennaDiameterM, cfg.GroundFrequencyGHz, cfg.GroundBandwidthMHz, txPowerDbmOverride: cfg.GroundTxPowerDbm);
        var pathGroundBudget     = MakeBudget(cfg.GroundAntennaDiameterM, cfg.GroundFrequencyGHz, cfg.GroundBandwidthMHz, otherEndGainDbi: pathStationGainDbi, txPowerDbmOverride: cfg.GroundTxPowerDbm);
        var islBudget            = MakeBudget(cfg.IslAntennaDiameterM,    cfg.IslFrequencyGHz,    cfg.IslBandwidthMHz, cfg.IslGainDbiOverride, txPowerDbmOverride: cfg.IslTxPowerDbm);

        var groundAntennas = cfg.GroundAntennas.Select((a, i) =>
            new SatAntenna(string.IsNullOrEmpty(a.Name) ? $"ground{i}" : a.Name,
                           a.AzimuthDeg, a.ElevationDeg, coverageGroundBudget)).ToList();
        var pathGroundAntennas = cfg.GroundAntennas.Select((a, i) =>
            new SatAntenna(string.IsNullOrEmpty(a.Name) ? $"ground{i}" : a.Name,
                           a.AzimuthDeg, a.ElevationDeg, pathGroundBudget)).ToList();

        // Derive ISL antenna list from mode. Omni = single omnidirectional antenna;
        // Directional = forward+aft at 45° elevation; None = empty.
        var islAntennas = cfg.IslMode switch
        {
            IslMode.None => new List<SatAntenna>(),
            IslMode.Omni => new List<SatAntenna>
            {
                new SatAntenna("omni", azDeg: 0, elDeg: 0, islBudget, isIsl: true, isOmnidirectional: true),
            },
            IslMode.Directional => new List<SatAntenna>
            {
                new SatAntenna("forward",   azDeg: 0,   elDeg: 45, islBudget, isIsl: true),
                new SatAntenna("aft",       azDeg: 180, elDeg: 45, islBudget, isIsl: true),
                new SatAntenna("port",      azDeg: 90,  elDeg: 45, islBudget, isIsl: true),
                new SatAntenna("starboard", azDeg: 270, elDeg: 45, islBudget, isIsl: true),
            },
            // Targeted: four omnidirectional-flagged slots — each is a distinct logical antenna
            // (forward/aft/port/stbd) but lock-on-target means the per-antenna pointing loss is
            // skipped. The az/el here are descriptive only; what actually controls reachability
            // is the per-sat islTargets map computed below.
            IslMode.Targeted => new List<SatAntenna>
            {
                new SatAntenna("fwd-target",  azDeg: 0,   elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
                new SatAntenna("aft-target",  azDeg: 180, elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
                new SatAntenna("port-target", azDeg: 90,  elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
                new SatAntenna("stbd-target", azDeg: 270, elDeg: 45, islBudget, isIsl: true, isOmnidirectional: true),
            },
            _ => new List<SatAntenna>(),
        };

        // Targeted ISLs: build the per-sat neighbour map from Walker (T, P). Antenna index 0
        // tracks forward, 1 = aft, 2 = port, 3 = starboard — matches the antenna list above so
        // Relay.Find/IslAnalysis can use the antenna's slot directly as the column key.
        int[,]? islTargets = cfg.IslMode == IslMode.Targeted
            ? Walker.NeighborMap(cfg.T, cfg.P, isStar: cfg.OrbitType == OrbitType.WalkerStar)
            : null;
        // Header values used by output struct + caption come from the ground budget (the
        // role most users care about for the heatmap colours).
        float gain = coverageGroundBudget.TxGainDbi;
        float beamwidth = coverageGroundBudget.BeamwidthDeg;

        // Build constellation. For circular orbits (Walker-delta / Walker-star), AltitudeKm is
        // the orbital altitude and ApogeeAltitudeKm is ignored. For elliptical (Molniya / Tundra
        // / Custom), AltitudeKm is the perigee altitude and the apogee comes from the cfg
        // directly. Walker-star is the same shell shape but planes span 180° of RAAN instead of
        // 360° (polar / Iridium-style); Walker.Star vs Walker.Delta picks the right RAAN span.
        bool isWalkerStar    = cfg.OrbitType == OrbitType.WalkerStar;
        bool isWalkerCircular = cfg.OrbitType == OrbitType.WalkerCircular;
        bool isCircular      = isWalkerCircular || isWalkerStar;
        double perigeeAltKm = cfg.AltitudeKm;
        double apogeeAltKm  = isCircular ? cfg.AltitudeKm : cfg.ApogeeAltitudeKm;
        double rPe = EarthRadius + perigeeAltKm * 1000;
        double rAp = EarthRadius + apogeeAltKm  * 1000;
        double smaM = (rPe + rAp) / 2;
        double ecc = (rAp - rPe) / (rAp + rPe);
        if (ecc < 0) ecc = 0;          // Pe > Ap: silently swap to avoid negative-e math
        double argPeDeg = isCircular ? 0 : cfg.ArgPerigeeDeg;
        double lanOffsetDeg = isWalkerCircular ? 0 : cfg.LanOffsetDeg;
        var rawSats = isWalkerStar
            ? Walker.Star(altitude: smaM - EarthRadius, bodyRadius: EarthRadius,
                           inclinationDeg: cfg.InclinationDeg,
                           t: cfg.T, p: cfg.P, f: cfg.F,
                           eccentricity: ecc,
                           argPerigeeDeg: argPeDeg,
                           raanOffsetDeg: lanOffsetDeg)
            : Walker.Delta(altitude: smaM - EarthRadius, bodyRadius: EarthRadius,
                            inclinationDeg: cfg.InclinationDeg,
                            t: cfg.T, p: cfg.P, f: cfg.F,
                            eccentricity: ecc,
                            argPerigeeDeg: argPeDeg,
                            raanOffsetDeg: lanOffsetDeg);
        var sats = rawSats.Select(s => new Satellite(
            s.SemiMajorAxis, s.Eccentricity, s.InclinationDeg, s.RaanDeg, s.ArgPerigeeDeg,
            s.TrueAnomalyAtT0Deg + cfg.PhaseOffsetDeg)).ToList();
        double period = Geometry.OrbitalPeriod(sats[0].SemiMajorAxis, EarthMu);

        // Fast path for stats sweeps: skip the heatmap grid, overlays, footprints, captions,
        // and PNG encoding. Just compute sat body-fixed positions + boresights + relay path so
        // the caller can see PathConnected / PathLatencyMs / PathRateBps for this snapshot.
        if (cfg.SkipHeatmap)
        {
            double tSecFast = cfg.TimeOffsetSec;
            var satBfPositionsFast = sats.Select(s =>
                Geometry.ToBodyFixed(s.Position(EarthMu, tSecFast), EarthRotationRate, tSecFast)).ToList();
            var groundBoresightsFast = AntennaPointing.ComputeBodyFixed(sats, groundAntennas, EarthMu, tSecFast, EarthRotationRate);
            var islBoresightsFast    = AntennaPointing.ComputeBodyFixed(sats, islAntennas,    EarthMu, tSecFast, EarthRotationRate);
            int pathFromIdxFast = StationLoader.IndexOf(stations, cfg.PathFromName);
            int pathToIdxFast   = StationLoader.IndexOf(stations, cfg.PathToName);
            bool pathConfiguredFast = pathFromIdxFast >= 0 && pathToIdxFast >= 0;
            double fastGroundTxW = DbmToWatts(coverageGroundBudget.TxPowerDbm);
            double fastIslTxW    = DbmToWatts(islBudget.TxPowerDbm);
            double fastGroundDcW = tl.PowerEfficiency > 0 ? fastGroundTxW / tl.PowerEfficiency : double.PositiveInfinity;
            double fastIslDcW    = tl.PowerEfficiency > 0 ? fastIslTxW    / tl.PowerEfficiency : double.PositiveInfinity;
            var fastOutput = new PlannerOutput
            {
                GainDbi = coverageGroundBudget.TxGainDbi,
                BeamwidthDeg = coverageGroundBudget.BeamwidthDeg,
                NoiseFloorDbm = coverageGroundBudget.NoiseFloorDbm,
                GroundTxW = fastGroundTxW,
                GroundDcW = fastGroundDcW,
                IslTxW = fastIslTxW,
                IslDcW = fastIslDcW,
                SatTotalDcW = fastGroundDcW * groundAntennas.Count + fastIslDcW * islAntennas.Count,
            };
            // ISL rate stats per timestep — feed the animation sweep with min/max/mean across
            // all working ISLs at this snapshot, so the cycle-wide aggregate becomes meaningful.
            if (islAntennas.Count > 0)
            {
                var islsFast = IslAnalysis.FindLinks(satBfPositionsFast, islAntennas, islBoresightsFast,
                                                      EarthRadius, atmosphereMarginM: 50_000,
                                                      islTargets: islTargets);
                double sumRate = 0, minRate = double.PositiveInfinity, maxRate = 0;
                int countNonZero = 0;
                foreach (var l in islsFast)
                {
                    var b = islAntennas[l.AntennaA].Budget;
                    double rate = RateFromRx(l.RxPowerDbm, b);
                    if (rate <= 0) continue;
                    sumRate += rate;
                    if (rate < minRate) minRate = rate;
                    if (rate > maxRate) maxRate = rate;
                    countNonZero++;
                }
                fastOutput.IslCount = countNonZero;
                fastOutput.IslMinRateBps  = countNonZero > 0 ? minRate : 0;
                fastOutput.IslMaxRateBps  = countNonZero > 0 ? maxRate : 0;
                fastOutput.IslMeanRateBps = countNonZero > 0 ? sumRate / countNonZero : 0;
            }
            if (pathConfiguredFast && !pathBandUnsupported)
            {
                double reqBpsFast = cfg.RequiredRateMbps * 1e6;
                var pathResult = Relay.Find(pathFromIdxFast, pathToIdxFast, stations, satBfPositionsFast,
                                             pathGroundAntennas, groundBoresightsFast,
                                             islAntennas, islBoresightsFast,
                                             EarthRadius, atmosphereMarginM: 50_000,
                                             cfg.MinElevDeg, latencyLimitSec: cfg.LatencyLimitSec,
                                             requiredDataRateBps: 0,
                                             islTargets: islTargets);
                fastOutput.PathConnected = pathResult.Connected;
                fastOutput.PathHops = pathResult.Hops.Count;
                fastOutput.PathRateBps = pathResult.BottleneckDataRateBps;
                fastOutput.PathLatencyMs = pathResult.TotalLatencySec * 1000;
                fastOutput.PathBelowRequired = pathResult.Connected && reqBpsFast > 0
                                                && pathResult.BottleneckDataRateBps < reqBpsFast;
            }
            return fastOutput;
        }

        // Coverage map — instantaneous (single timestep at TimeOffsetSec) or daily-average
        // (256 timesteps over one sidereal day, max'd cell-wise).
        bool instant = cfg.CoverageMode == CoverageMode.Instantaneous;
        var grid = Coverage.Evaluate(sats, EarthRadius, EarthMu,
                                     latCells: 180, lonCells: 360,
                                     timesteps: instant ? 1 : 256,
                                     durationSec: instant ? 0 : EarthSiderealDay,
                                     startTimeSec: instant ? cfg.TimeOffsetSec : 0,
                                     minElevationDeg: cfg.MinElevDeg,
                                     bodyRotationRateRadPerSec: EarthRotationRate,
                                     groundAntennas: groundAntennas);

        // Display range — choose data array + scale based on metric.
        double meanFrac = 0;
        for (int r = 0; r < grid.LatCells; r++)
            for (int c = 0; c < grid.LonCells; c++)
                meanFrac += grid.FractionVisible[r, c];
        meanFrac /= (grid.LatCells * grid.LonCells);

        double[,] heatmapData;
        double displayMin, displayMax;
        string colorbarLabel;
        Func<double, string> colorbarFmt;
        if (cfg.Metric == HeatmapMetric.DataRate)
        {
            // Convert per-cell rate → log10(rate) clamped at 0 (= 1 bps). Cells with 0 rate
            // become 0 → mapped to floor of the colour scale.
            heatmapData = new double[grid.LatCells, grid.LonCells];
            double logMin = double.PositiveInfinity, logMax = double.NegativeInfinity;
            for (int r = 0; r < grid.LatCells; r++)
                for (int c = 0; c < grid.LonCells; c++)
                {
                    double rate = grid.MaxDataRateBps![r, c];
                    double lg = rate > 1 ? Math.Log10(rate) : 0;
                    heatmapData[r, c] = lg;
                    if (lg > 0)
                    {
                        if (lg < logMin) logMin = lg;
                        if (lg > logMax) logMax = lg;
                    }
                }
            if (double.IsInfinity(logMin)) { logMin = 0; logMax = 6; }
            displayMin = Math.Floor(logMin) - 0.5;
            displayMax = Math.Ceiling(logMax) + 0.5;
            colorbarLabel = "best achievable rate";
            colorbarFmt = lg =>
            {
                double bps = Math.Pow(10, lg);
                return bps >= 1e6 ? $"{bps/1e6:F1}M"
                     : bps >= 1e3 ? $"{bps/1e3:F1}k"
                     : $"{bps:F0}";
            };
        }
        else
        {
            heatmapData = grid.MaxRxPowerDbm!;
            double maxRxMin = double.PositiveInfinity, maxRxMax = double.NegativeInfinity;
            for (int r = 0; r < grid.LatCells; r++)
                for (int c = 0; c < grid.LonCells; c++)
                {
                    double rx = heatmapData[r, c];
                    if (rx > Coverage.NoVisibilityFloorDbm + 1)
                    {
                        if (rx < maxRxMin) maxRxMin = rx;
                        if (rx > maxRxMax) maxRxMax = rx;
                    }
                }
            if (double.IsInfinity(maxRxMin)) { maxRxMin = -150; maxRxMax = -100; }
            displayMin = Math.Floor(maxRxMin) - 3;
            displayMax = Math.Ceiling(maxRxMax) + 1;
            colorbarLabel = "best achievable rx-power (dBm)";
            colorbarFmt = v => v.ToString("F1");
        }

        // Snapshot
        double tSec = cfg.TimeOffsetSec;
        var satBfPositions = sats.Select(s =>
            Geometry.ToBodyFixed(s.Position(EarthMu, tSec), EarthRotationRate, tSec)).ToList();

        var groundBoresights = AntennaPointing.ComputeBodyFixed(sats, groundAntennas, EarthMu, tSec, EarthRotationRate);
        var islBoresights    = AntennaPointing.ComputeBodyFixed(sats, islAntennas,    EarthMu, tSec, EarthRotationRate);

        var isls = IslAnalysis.FindLinks(satBfPositions, islAntennas, islBoresights,
                                          EarthRadius, atmosphereMarginM: 50_000,
                                          islTargets: islTargets);
        // IslAnalysis returns every line-of-sight pair (best-pointing antenna combo) — for
        // directional setups that includes pairs where both antennas are wildly misaligned and
        // the link delivers fractional bps. Drop links whose RA-style rate model has collapsed
        // to zero (~96+ dB short of Eb/N0); they're geometrically possible but useless.
        isls = isls.Where(l =>
        {
            var b = islAntennas[l.AntennaA].Budget;
            return RateFromRx(l.RxPowerDbm, b) > 0;
        }).ToList();
        var groundLinks = GroundLinkAnalysis.FindLinks(stations, satBfPositions,
                                                        groundAntennas, groundBoresights,
                                                        EarthRadius, cfg.MinElevDeg);

        // Ground tracks
        const int trackSamples = 360;
        int satsPerPlane = cfg.T / cfg.P;
        var groundTracks = new List<List<(double, double)>>();
        for (int planeIdx = 0; planeIdx < cfg.P; planeIdx++)
        {
            var firstSatInPlane = sats[planeIdx * satsPerPlane];
            var track = new List<(double, double)>(trackSamples);
            for (int i = 0; i < trackSamples; i++)
            {
                double tt = i * period / trackSamples;
                var posI = firstSatInPlane.Position(EarthMu, tt);
                var posBf = Geometry.ToBodyFixed(posI, EarthRotationRate, tSec);
                track.Add(Geometry.EcefToLatLon(posBf));
            }
            groundTracks.Add(track);
        }

        var satSubPoints = satBfPositions.Select(Geometry.EcefToLatLon).ToList();
        // Footprint scaling uses perigee altitude — at apogee the same beam paints a larger
        // footprint, but for a quick "how big is the beam on the ground" sanity check the
        // perigee figure is the conservative one. (We could pass per-sat altitude but a single
        // representative number is fine for the legend.)
        double footprintHalfAngleDeg = Geometry.FootprintHalfAngleDeg(beamwidth, perigeeAltKm * 1000, EarthRadius);
        // One footprint per (sat × ground antenna) — centered where each antenna's boresight
        // ray actually hits the surface. Skipped when the boresight points above the horizon.
        var footprints = new List<Heatmap.Footprint>();
        for (int s = 0; s < satBfPositions.Count; s++)
        {
            var satPos = satBfPositions[s];
            for (int a = 0; a < groundAntennas.Count; a++)
            {
                var hit = Geometry.RaySphereNearIntersect(satPos, groundBoresights[s, a], EarthRadius);
                if (hit == null) continue;
                var (lat, lon) = Geometry.EcefToLatLon(hit.Value);
                footprints.Add(new Heatmap.Footprint
                {
                    Boundary = Geometry.SmallCircle(lat, lon, footprintHalfAngleDeg, samples: 90),
                });
            }
        }

        var islForRender = isls.Select(l =>
        {
            var (latA, lonA) = satSubPoints[l.A];
            var (latB, lonB) = satSubPoints[l.B];
            double metric;
            if (cfg.Metric == HeatmapMetric.DataRate)
            {
                var b = islAntennas[l.AntennaA].Budget;
                double rate = RateFromRx(l.RxPowerDbm, b);
                metric = rate > 1 ? Math.Log10(rate) : 0;
            }
            else
            {
                metric = l.RxPowerDbm;
            }
            return new Heatmap.Isl { LatA = latA, LonA = lonA, LatB = latB, LonB = lonB, MetricValue = metric };
        }).ToList();

        var groundLinksForRender = groundLinks
            .Where(g =>
            {
                var kind = stations[g.StationIdx].Kind;
                if (kind == StationKind.Tracking && !cfg.ShowTrackingLinks) return false;
                if (kind == StationKind.Telecom  && !cfg.ShowTelecomLinks)  return false;
                return true;
            })
            .Select(g =>
            {
                var (latS, lonS) = satSubPoints[g.SatIdx];
                return new Heatmap.GroundLink
                {
                    LatStation = stations[g.StationIdx].LatDeg,
                    LonStation = stations[g.StationIdx].LonDeg,
                    LatSat = latS, LonSat = lonS, RxDbm = g.RxPowerDbm,
                };
            }).ToList();

        // Path endpoints
        int pathFromIdx = StationLoader.IndexOf(stations, cfg.PathFromName);
        int pathToIdx   = StationLoader.IndexOf(stations, cfg.PathToName);
        bool pathConfigured = pathFromIdx >= 0 && pathToIdx >= 0;

        var stationMarkers = new List<Heatmap.Station>(stations.Count);
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            var kind = s.Kind switch
            {
                StationKind.Tracking => Heatmap.StationKind.Tracking,
                StationKind.Telecom  => Heatmap.StationKind.Telecom,
                _                    => Heatmap.StationKind.Tracking,
            };
            if (pathConfigured && (i == pathFromIdx || i == pathToIdx))
                kind = Heatmap.StationKind.Endpoint;
            stationMarkers.Add(new Heatmap.Station { Name = s.Name, LatDeg = s.LatDeg, LonDeg = s.LonDeg, Kind = kind });
        }

        // Power figures — TX is radiated W from one antenna; DC accounts for the era's amp
        // efficiency. Sat total sums per-antenna across both ground and ISL roles.
        double groundTxW = DbmToWatts(coverageGroundBudget.TxPowerDbm);
        double islTxW    = DbmToWatts(islBudget.TxPowerDbm);
        double groundDcW = tl.PowerEfficiency > 0 ? groundTxW / tl.PowerEfficiency : double.PositiveInfinity;
        double islDcW    = tl.PowerEfficiency > 0 ? islTxW    / tl.PowerEfficiency : double.PositiveInfinity;
        double satTotalDcW = groundDcW * groundAntennas.Count + islDcW * islAntennas.Count;

        // ISL rate distribution stats — same per-timestep summary the fast path computes.
        // Values feed the animation sweep's cycle-wide ISL min/max/avg display.
        double islSumRate = 0, islMinRate = double.PositiveInfinity, islMaxRate = 0;
        int islRateCount = 0;
        foreach (var l in isls)
        {
            var b = islAntennas[l.AntennaA].Budget;
            double rate = RateFromRx(l.RxPowerDbm, b);
            if (rate <= 0) continue;
            islSumRate += rate;
            if (rate < islMinRate) islMinRate = rate;
            if (rate > islMaxRate) islMaxRate = rate;
            islRateCount++;
        }

        // Relay path
        var relayLines = new List<Heatmap.RelayHopLine>();
        string relayCaption = "";
        var output = new PlannerOutput
        {
            FootprintHalfAngleDeg = footprintHalfAngleDeg,
            GainDbi = gain,
            BeamwidthDeg = beamwidth,
            NoiseFloorDbm = coverageGroundBudget.NoiseFloorDbm,
            IslCount = isls.Count,
            IslMinRateBps = islRateCount > 0 ? islMinRate : 0,
            IslMaxRateBps = islRateCount > 0 ? islMaxRate : 0,
            IslMeanRateBps = islRateCount > 0 ? islSumRate / islRateCount : 0,
            GroundLinkCount = groundLinks.Count,
            GroundTxW = groundTxW,
            GroundDcW = groundDcW,
            IslTxW = islTxW,
            IslDcW = islDcW,
            SatTotalDcW = satTotalDcW,
        };
        if (pathConfigured && pathBandUnsupported)
        {
            string band = BandPrefixFor(cfg.GroundFrequencyGHz);
            output.PathConnected = false;
            output.PathHops = 0;
            output.PathRateBps = 0;
            output.PathLatencyMs = 0;
            relayCaption = $"path {cfg.PathFromName}→{cfg.PathToName}: UNREACHABLE — no {band}-band antenna at one or both stations";
        }
        else if (pathConfigured)
        {
            double requiredBps = cfg.RequiredRateMbps * 1e6;
            // Don't filter by required rate at the Dijkstra level — find the best geometric
            // path regardless and report the achieved rate. The caller (and the renderer) flag
            // sub-required paths in red so the user sees a path exists, just at degraded
            // bandwidth. Latency cap is still enforced — paths that exceed light-time budget
            // really aren't usable.
            var pathResult = Relay.Find(pathFromIdx, pathToIdx, stations, satBfPositions,
                                         pathGroundAntennas, groundBoresights,
                                         islAntennas, islBoresights,
                                         EarthRadius, atmosphereMarginM: 50_000,
                                         cfg.MinElevDeg, latencyLimitSec: cfg.LatencyLimitSec,
                                         requiredDataRateBps: 0,
                                         islTargets: islTargets);
            output.PathConnected = pathResult.Connected;
            output.PathHops = pathResult.Hops.Count;
            output.PathRateBps = pathResult.BottleneckDataRateBps;
            output.PathLatencyMs = pathResult.TotalLatencySec * 1000;
            bool belowRequired = pathResult.Connected && requiredBps > 0
                                  && pathResult.BottleneckDataRateBps < requiredBps;
            output.PathBelowRequired = belowRequired;
            if (pathResult.Connected)
            {
                foreach (var hop in pathResult.Hops)
                {
                    (double latA, double lonA) = hop.FromIsStation
                        ? (stations[hop.FromIdx].LatDeg, stations[hop.FromIdx].LonDeg)
                        : satSubPoints[hop.FromIdx];
                    (double latB, double lonB) = hop.ToIsStation
                        ? (stations[hop.ToIdx].LatDeg, stations[hop.ToIdx].LonDeg)
                        : satSubPoints[hop.ToIdx];
                    relayLines.Add(new Heatmap.RelayHopLine { LatA = latA, LonA = lonA, LatB = latB, LonB = lonB, RxDbm = hop.RxPowerDbm });
                }
                string reqStr = requiredBps > 0
                    ? (belowRequired
                        ? $" (BELOW req {FmtRate(requiredBps)})"
                        : $" (req {FmtRate(requiredBps)})")
                    : "";
                relayCaption =
                    $"path {cfg.PathFromName}→{cfg.PathToName}: {pathResult.Hops.Count} hops, "
                    + $"{pathResult.TotalDistanceM/1000:F0} km / {pathResult.TotalLatencySec*1000:F1} ms, "
                    + $"rate {FmtRate(pathResult.BottleneckDataRateBps)}{reqStr}, "
                    + $"rx {pathResult.BottleneckRxPowerDbm:F1} dBm";
            }
            else
            {
                relayCaption = $"path {cfg.PathFromName}→{cfg.PathToName}: UNREACHABLE — no LoS chain or latency cap exceeded ({cfg.LatencyLimitSec:F0}s)";
            }
        }

        // Caption
        int hh = (int)(tSec / 3600);
        int mm = (int)((tSec - hh * 3600) / 60);
        string timeLabel = $"t = {hh:D2}:{mm:D2}";
        int telecomCount = stations.Count(s => s.Kind == StationKind.Telecom);
        // Pattern label reflects the orbit type so a Walker-star shell or a Molniya / Tundra
        // / Custom orbit isn't mis-labelled "Walker delta" on the caption.
        string patternLabel = cfg.OrbitType switch
        {
            OrbitType.WalkerStar => "Walker star",
            OrbitType.Molniya    => "Molniya",
            OrbitType.Tundra     => "Tundra",
            OrbitType.Custom     => "Custom",
            _                    => "Walker delta",
        };
        string caption;
        if (cfg.FullCaption)
        {
            string altLabel = isCircular ? $"alt={cfg.AltitudeKm:F0} km" : $"Pe={perigeeAltKm:F0}/Ap={apogeeAltKm:F0} km";
            caption =
                $"{cfg.T} sats — {patternLabel} {cfg.T}/{cfg.P}/{cfg.F}, {altLabel}, inc={cfg.InclinationDeg}° | rotating Earth | {timeLabel}\n"
                + $"ground antenna: {cfg.GroundAntennaDiameterM:F2}m @ {cfg.GroundFrequencyGHz:F2} GHz, gain {gain:F1} dBi, HPBW {beamwidth:F1}°, ch {cfg.GroundBandwidthMHz:F0} MHz | TL{cfg.TechLevel}, tx {coverageGroundBudget.TxPowerDbm:F0} dBm | ground×{groundAntennas.Count}, ISL×{islAntennas.Count}\n"
                + $"ISLs: {isls.Count} pairs | ground links: {groundLinks.Count} pairs\n"
                + $"yellow □ = RA tracking station, cyan ● = Skopos telecom ({telecomCount}), magenta ◎ = path endpoint, lime line = best relay path, pink dashed = footprint (−3 dB, {footprintHalfAngleDeg:F0}° GC radius)\n"
                + (relayCaption.Length > 0 ? relayCaption : "no relay path configured");
        }
        else
        {
            caption = $"{timeLabel} · {patternLabel} {cfg.T}/{cfg.P}/{cfg.F} · ISLs {isls.Count} · gnd {groundLinks.Count}"
                    + (relayCaption.Length > 0 ? $" · {relayCaption}" : "");
        }

        var overlay = new Heatmap.Overlay
        {
            GroundTracks = groundTracks,
            SatPositions = satSubPoints,
            Isls = cfg.ShowIsls ? islForRender : new List<Heatmap.Isl>(),
            IslMetricMin = displayMin,
            IslMetricMax = displayMax,
            Stations = stationMarkers,
            GroundLinks = groundLinksForRender,
            RelayPath = relayLines,
            RelayBelowRequired = output.PathBelowRequired,
            Footprints = cfg.ShowFootprints ? footprints : new List<Heatmap.Footprint>(),
            Caption = caption,
        };

        // Render PNG to bytes. fontScale auto-tracks upscale (relative to upscale=4 baseline) so
        // on-screen text size stays roughly constant when the GUI's PictureBox zooms the image
        // to fit. cfg.TextScale is a user-facing override on top of that.
        double fontScale = cfg.Upscale * cfg.TextScale / 4.0;
        using var img = Heatmap.RenderToImage(heatmapData, displayMin, displayMax,
                                               upscale: cfg.Upscale, overlay: overlay,
                                               colorbarLabel: colorbarLabel,
                                               colorbarFormatter: colorbarFmt,
                                               fontScale: fontScale);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        output.PngBytes = ms.ToArray();
        output.Status = caption.Replace("\n", " | ");
        output.HeatmapData = heatmapData;
        output.HeatmapMapPixelWidth = heatmapData.GetLength(1) * cfg.Upscale;
        output.HeatmapMapPixelHeight = heatmapData.GetLength(0) * cfg.Upscale;
        output.HeatmapValueFormatter = cfg.Metric == HeatmapMetric.DataRate
            ? lg => FmtRate(Math.Pow(10, lg))
            : v => $"{v:F1} dBm";
        return output;
    }

    public static string FmtRate(double bps) =>
        bps >= 1e6 ? $"{bps/1e6:F1} Mbps" :
        bps >= 1e3 ? $"{bps/1e3:F1} kbps" :
        $"{bps:F0} bps";

    /// <summary>Convert dBm to watts: 0 dBm = 1 mW = 0.001 W; +10 dBm per ×10 in W.</summary>
    public static double DbmToWatts(double dbm) => Math.Pow(10, (dbm - 30) / 10);

    /// <summary>Format a watts value compactly: kW for kW-scale, W for W-scale, mW for sub-W.</summary>
    public static string FmtWatts(double w) =>
        w >= 1000   ? $"{w/1000:F1} kW" :
        w >= 1      ? $"{w:F1} W" :
        w >= 0.001  ? $"{w*1000:F1} mW" :
                      $"{w*1e6:F1} µW";

    /// <summary>Achievable data rate (bps) from a known rx-power and link-budget. Mirrors
    /// LinkBudget.MaxDataRateBps but skips path-loss recomputation since rx-power is given.</summary>
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
}
