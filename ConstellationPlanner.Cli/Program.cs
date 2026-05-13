using ConstellationPlanner.Core;
using ConstellationPlanner.Cli;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

const double EarthSiderealDay = 86_164.0;

// Default scenario — current GEO + HG-61 + andover→goonhilly path. Tweak by editing this
// PlannerInput; the web GUI lets you do the same thing interactively.
var cfg = new PlannerInput
{
    AltitudeKm = 35786,
    InclinationDeg = 0,
    T = 4, P = 1, F = 0,
    PhaseOffsetDeg = 45,
    MinElevDeg = 10,
    TechLevel = 3,
    GroundAntennaDiameterM = 1.22,
    GroundFrequencyGHz = 1.5975,
    GroundBandwidthMHz = 128,
    GroundAntennas = new()
    {
        new AntennaAim { Name = "HG-61 nadir", AzimuthDeg = 270, ElevationDeg = 0 },
    },
    IslMode = IslMode.None,
    IslAntennaDiameterM = 1.22,
    IslFrequencyGHz = 1.5975,
    IslBandwidthMHz = 128,
    PathFromName = "andover",
    PathToName = "goonhilly_downs",
    RequiredRateMbps = 0,
    LatencyLimitSec = 30,
    Upscale = 6,
    FullCaption = true,
};

string outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
string patternLabel = cfg.OrbitType switch
{
    ConstellationPlanner.Cli.OrbitType.WalkerStar => "Walker star",
    ConstellationPlanner.Cli.OrbitType.Molniya    => "Molniya",
    ConstellationPlanner.Cli.OrbitType.Tundra     => "Tundra",
    ConstellationPlanner.Cli.OrbitType.Custom     => "Custom",
    _                                              => "Walker delta",
};
Console.WriteLine($"Constellation: {patternLabel} {cfg.T}/{cfg.P}/{cfg.F}, alt={cfg.AltitudeKm:F0} km, inc={cfg.InclinationDeg}°");
Console.WriteLine($"Ground antenna: {cfg.GroundAntennaDiameterM:F2}m dish @ {cfg.GroundFrequencyGHz:F2} GHz, TL{cfg.TechLevel}");

// ---------- Static snapshots ----------
var snapshots = new (double timeSec, string label, string filename)[]
{
    (0,           "t = 0",    "coverage_t0.png"),
    ( 4 * 3600.0, "t =  4 h", "coverage_t4h.png"),
    ( 6 * 3600.0, "t =  6 h", "coverage_t6h.png"),
    ( 8 * 3600.0, "t =  8 h", "coverage_t8h.png"),
    (12 * 3600.0, "t = 12 h", "coverage_t12h.png"),
};

PlannerOutput? lastOut = null;
foreach (var snap in snapshots)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    cfg.TimeOffsetSec = snap.timeSec;
    cfg.FullCaption = true;
    cfg.Upscale = 6;
    var output = Planner.Render(cfg);
    sw.Stop();
    File.WriteAllBytes(Path.Combine(outDir, snap.filename), output.PngBytes);
    lastOut = output;
    Console.WriteLine($"  {snap.label}: ISLs={output.IslCount}, ground links={output.GroundLinkCount} ({sw.ElapsedMilliseconds} ms) → {snap.filename}");
}
if (lastOut != null)
{
    Console.WriteLine($"  → footprint {lastOut.FootprintHalfAngleDeg:F1}° GC radius, gain {lastOut.GainDbi:F1} dBi, HPBW {lastOut.BeamwidthDeg:F1}°, noise floor {lastOut.NoiseFloorDbm:F1} dBm");
    if (lastOut.PathConnected)
        Console.WriteLine($"  → path: {lastOut.PathHops} hops, {lastOut.PathLatencyMs:F1} ms, rate {Planner.FmtRate(lastOut.PathRateBps)}");
    else
        Console.WriteLine($"  → path: UNREACHABLE");
}

// ---------- Animation: 24 h at 30 frames/hour. Skip for static (geostationary) cases. ----------
double period = 2 * Math.PI * Math.Sqrt(Math.Pow((cfg.AltitudeKm * 1000 + 6_371_000), 3) / 3.986e14);
bool isGeostationary = Math.Abs(period - EarthSiderealDay) / EarthSiderealDay < 0.001 && cfg.InclinationDeg < 0.1;
if (isGeostationary)
{
    Console.WriteLine();
    Console.WriteLine("Animation skipped — constellation is geostationary (static in body-fixed frame).");
    return;
}

const int FramesPerHour = 30;
// Pick the ground-track repeat period as the loop length: the constellation pattern returns
// to (within ~1°) of its body-fixed start at this point, so the GIF can loop without a snap.
bool cycleIsCircular = cfg.OrbitType == ConstellationPlanner.Cli.OrbitType.WalkerCircular
                    || cfg.OrbitType == ConstellationPlanner.Cli.OrbitType.WalkerStar;
double apForCycle = cycleIsCircular ? cfg.AltitudeKm : cfg.ApogeeAltitudeKm;
var repeat = Planner.GroundTrackRepeat(cfg.AltitudeKm, apForCycle);
double animDurationH = repeat.CycleSec / 3600.0;
int NumFrames = Math.Max(1, (int)Math.Round(FramesPerHour * animDurationH));
double FrameStepSec = animDurationH * 3600.0 / NumFrames;
const int AnimUpscale = 2;
const int FrameDelayCs = 5;

string animPath = Path.Combine(outDir, "coverage_24h.gif");
Console.WriteLine();
Console.WriteLine($"Animating {NumFrames} frames over {animDurationH:F2} h "
                + $"({repeat.Orbits} orbits ≈ {repeat.SiderealDays} sidereal days, residual snap {repeat.ErrorDeg:F2}°) → {animPath}");

var animSw = System.Diagnostics.Stopwatch.StartNew();
Image<Rgba32>? gif = null;
try
{
    for (int i = 0; i < NumFrames; i++)
    {
        cfg.TimeOffsetSec = i * FrameStepSec;
        cfg.FullCaption = false;
        cfg.Upscale = AnimUpscale;
        var output = Planner.Render(cfg);

        using var frameMs = new MemoryStream(output.PngBytes);
        var frame = Image.Load<Rgba32>(frameMs);
        if (gif == null)
        {
            gif = frame;
            gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = FrameDelayCs;
        }
        else
        {
            var added = gif.Frames.AddFrame(frame.Frames.RootFrame);
            added.Metadata.GetGifMetadata().FrameDelay = FrameDelayCs;
            frame.Dispose();
        }
        if ((i + 1) % (NumFrames / 5) == 0)
            Console.WriteLine($"  frame {i+1}/{NumFrames}");
    }
    gif!.Metadata.GetGifMetadata().RepeatCount = 0;
    gif.SaveAsGif(animPath, new GifEncoder { ColorTableMode = GifColorTableMode.Local });
}
finally { gif?.Dispose(); }
animSw.Stop();
Console.WriteLine($"Animation done in {animSw.ElapsedMilliseconds/1000.0:F1} s → {new FileInfo(animPath).Length / (1024.0 * 1024.0):F1} MB");

// Path stats — separate sweep at ~1 sample/min over the full cycle (capped at 5000 samples)
// using SkipHeatmap so each eval is a cheap Walker+Relay rather than a full render.
double durationSec = animDurationH * 3600.0;
int Nstats = Math.Min(5000, Math.Max(NumFrames, (int)(durationSec / 60)));
double requiredBpsThresh = cfg.RequiredRateMbps * 1e6;
int connectedCount = 0, metReqCount = 0;
double sumLatencyMs = 0, sumRateBps = 0;
var statSw = System.Diagnostics.Stopwatch.StartNew();
System.Threading.Tasks.Parallel.For(0, Nstats, i =>
{
    var local = new PlannerInput
    {
        OrbitType = cfg.OrbitType,
        AltitudeKm = cfg.AltitudeKm, ApogeeAltitudeKm = cfg.ApogeeAltitudeKm,
        InclinationDeg = cfg.InclinationDeg, ArgPerigeeDeg = cfg.ArgPerigeeDeg,
        LanOffsetDeg = cfg.LanOffsetDeg,
        T = cfg.T, P = cfg.P, F = cfg.F, PhaseOffsetDeg = cfg.PhaseOffsetDeg,
        MinElevDeg = cfg.MinElevDeg, TechLevel = cfg.TechLevel,
        GroundAntennaDiameterM = cfg.GroundAntennaDiameterM, GroundFrequencyGHz = cfg.GroundFrequencyGHz, GroundBandwidthMHz = cfg.GroundBandwidthMHz,
        GroundStationGainDbi = cfg.GroundStationGainDbi, GroundStationTxPowerDbm = cfg.GroundStationTxPowerDbm,
        GroundAntennas = cfg.GroundAntennas.Select(a => new AntennaAim { Name = a.Name, AzimuthDeg = a.AzimuthDeg, ElevationDeg = a.ElevationDeg }).ToList(),
        IslMode = cfg.IslMode,
        IslAntennaDiameterM = cfg.IslAntennaDiameterM, IslFrequencyGHz = cfg.IslFrequencyGHz, IslBandwidthMHz = cfg.IslBandwidthMHz,
        IslGainDbiOverride = cfg.IslGainDbiOverride,
        PathFromName = cfg.PathFromName, PathToName = cfg.PathToName,
        RequiredRateMbps = cfg.RequiredRateMbps, LatencyLimitSec = cfg.LatencyLimitSec,
        TimeOffsetSec = i * durationSec / Nstats,
        SkipHeatmap = true,
    };
    var statOut = Planner.Render(local);
    if (statOut.PathConnected)
    {
        System.Threading.Interlocked.Increment(ref connectedCount);
        if (!statOut.PathBelowRequired) System.Threading.Interlocked.Increment(ref metReqCount);
        double prevLat, newLat;
        do { prevLat = sumLatencyMs; newLat = prevLat + statOut.PathLatencyMs; }
        while (System.Threading.Interlocked.CompareExchange(ref sumLatencyMs, newLat, prevLat) != prevLat);
        double prevRate, newRate;
        do { prevRate = sumRateBps; newRate = prevRate + statOut.PathRateBps; }
        while (System.Threading.Interlocked.CompareExchange(ref sumRateBps, newRate, prevRate) != prevRate);
    }
});
statSw.Stop();
double uptimePct = Nstats > 0 ? 100.0 * connectedCount / Nstats : 0;
double metReqPct = Nstats > 0 ? 100.0 * metReqCount / Nstats : 0;
string reqPart = requiredBpsThresh > 0 ? $" ({metReqPct:F1}% meets {Planner.FmtRate(requiredBpsThresh)})" : "";
if (connectedCount > 0)
    Console.WriteLine($"  path {cfg.PathFromName}→{cfg.PathToName}: uptime {uptimePct:F1}%{reqPart} ({connectedCount}/{Nstats} samples in {statSw.ElapsedMilliseconds} ms), avg lat {sumLatencyMs/connectedCount:F1} ms, avg rate {Planner.FmtRate(sumRateBps/connectedCount)}");
else
    Console.WriteLine($"  path {cfg.PathFromName}→{cfg.PathToName}: never connected over {Nstats} samples");
