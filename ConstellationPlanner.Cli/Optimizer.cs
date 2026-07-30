using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConstellationPlanner.Core;

namespace ConstellationPlanner.Cli;

/// <summary>The search space the optimizer explores: per-parameter min/max ranges plus the
/// set of allowed orbit types. A "locked" parameter is just one with Min == Max == baseline.
/// Continuous parameters sample uniformly in [Min, Max]; integer parameters likewise; orbit
/// type is drawn uniformly from <see cref="AllowedOrbitTypes"/>.</summary>
public sealed class OptimizerSearchSpace
{
    public HashSet<OrbitType> AllowedOrbitTypes { get; set; } = new();

    public int TMin { get; set; }
    public int TMax { get; set; }
    public int PMin { get; set; }
    public int PMax { get; set; }
    public int FMin { get; set; }
    public int FMax { get; set; }

    public double AltitudeKmMin { get; set; }
    public double AltitudeKmMax { get; set; }
    public double ApogeeAltitudeKmMin { get; set; }
    public double ApogeeAltitudeKmMax { get; set; }
    public double InclinationDegMin { get; set; }
    public double InclinationDegMax { get; set; }
    public double LanOffsetDegMin { get; set; }
    public double LanOffsetDegMax { get; set; }
    public double ArgPerigeeDegMin { get; set; }
    public double ArgPerigeeDegMax { get; set; }
    public double PhaseOffsetDegMin { get; set; }
    public double PhaseOffsetDegMax { get; set; }

    /// <summary>Start a search space with every parameter locked to the baseline's values
    /// (single-element orbit-type set, min=max for everything else). Caller widens the ranges
    /// for the parameters they want the optimizer to vary.</summary>
    public static OptimizerSearchSpace LockedToBaseline(PlannerInput b) => new()
    {
        AllowedOrbitTypes = new HashSet<OrbitType> { b.OrbitType },
        TMin = b.T, TMax = b.T,
        PMin = b.P, PMax = b.P,
        FMin = b.F, FMax = b.F,
        AltitudeKmMin = b.AltitudeKm, AltitudeKmMax = b.AltitudeKm,
        ApogeeAltitudeKmMin = b.ApogeeAltitudeKm, ApogeeAltitudeKmMax = b.ApogeeAltitudeKm,
        InclinationDegMin = b.InclinationDeg, InclinationDegMax = b.InclinationDeg,
        LanOffsetDegMin = b.LanOffsetDeg, LanOffsetDegMax = b.LanOffsetDeg,
        ArgPerigeeDegMin = b.ArgPerigeeDeg, ArgPerigeeDegMax = b.ArgPerigeeDeg,
        PhaseOffsetDegMin = b.PhaseOffsetDeg, PhaseOffsetDegMax = b.PhaseOffsetDeg,
    };

    public bool VariesT => TMin < TMax;
    public bool VariesP => PMin < PMax;
    public bool VariesF => FMin < FMax;
    public bool VariesAltitude       => AltitudeKmMin < AltitudeKmMax;
    public bool VariesApogee         => ApogeeAltitudeKmMin < ApogeeAltitudeKmMax;
    public bool VariesInclination    => InclinationDegMin < InclinationDegMax;
    public bool VariesLan            => LanOffsetDegMin < LanOffsetDegMax;
    public bool VariesArgPe          => ArgPerigeeDegMin < ArgPerigeeDegMax;
    public bool VariesPhase          => PhaseOffsetDegMin < PhaseOffsetDegMax;

    /// <summary>True if at least one parameter has a non-trivial range / multi-option set,
    /// i.e. the optimizer has *anything* to vary. If false, every trial would yield the same
    /// config and the caller should refuse to start the run.</summary>
    public bool HasAnyVariation =>
        AllowedOrbitTypes.Count > 1
        || VariesT || VariesP || VariesF
        || VariesAltitude || VariesApogee || VariesInclination
        || VariesLan || VariesArgPe || VariesPhase;
}

public enum OptimizerPhase
{
    /// <summary>First-phase random sampling — broad exploration of the search space.</summary>
    Exploration,
    /// <summary>Second-phase refinement — small perturbations of a top-K seed from the
    /// exploration phase, to climb the local basin.</summary>
    Refinement,
}

/// <summary>Outcome of one optimizer trial: the randomly-sampled config and the aggregate
/// score across all Skopos (connection × rx) pairs evaluated over the constellation's repeat
/// cycle. Trial numbers are 1-based for display.</summary>
public sealed class OptimizeTrialResult
{
    public int TrialNumber;
    public OptimizerPhase Phase;
    public PlannerInput Config = null!;
    public int TotalConnections;
    public int Connected;
    public int MetWindow;
    public double MeanUptimePct;
    public double MeanMetWindowPct;
    /// <summary>Percentage of connections whose cycle-uptime cleared the 90% Skopos-contract
    /// threshold. The primary "is this a good constellation" score; ties broken by mean
    /// met-window % so the optimizer has a continuous gradient even when no connection has
    /// crossed the threshold yet.</summary>
    public double PassPct => TotalConnections > 0 ? 100.0 * MetWindow / TotalConnections : 0;
}

public static class Optimizer
{
    /// <summary>Run a random-sample search over <paramref name="trials"/> configurations,
    /// each one a sample from <paramref name="space"/> overlaid on <paramref name="baseCfg"/>'s
    /// non-search-space fields (antennas, ground stations, paths, etc.). Cycle-stats sweep at
    /// <paramref name="samplesPerTrial"/> samples — lower than the standalone animation's 5000
    /// to keep total optimizer runtime reasonable, just enough resolution to rank trials.
    /// <para>Results are streamed to <paramref name="onResult"/> on the calling SynchronizationContext
    /// (so the GUI can append a row without cross-thread Invoke). Cancellation is observed
    /// between trials and inside each EvaluateConnectionsOverCycle's parallel sweep.</para></summary>
    public static async Task RunAsync(
        PlannerInput baseCfg,
        IReadOnlyList<(SkoposConnection Conn, int RxIndex)> connections,
        OptimizerSearchSpace space,
        int trials,
        int samplesPerTrial,
        Action<OptimizeTrialResult> onResult,
        CancellationToken ct)
    {
        var ctx = SynchronizationContext.Current;
        void Post(Action a)
        {
            if (ctx != null) ctx.Post(_ => a(), null);
            else a();
        }

        var rng = new Random();
        // Precompute every (T, P) tuple with T % P == 0 inside the user's bounds. Sampling
        // picks one of these directly so we don't have to reject-loop on divisibility.
        var validTPPairs = Enumerable.Range(Math.Max(1, space.TMin), Math.Max(0, space.TMax - space.TMin + 1))
            .SelectMany(t => Enumerable.Range(Math.Max(1, space.PMin), Math.Max(0, space.PMax - space.PMin + 1))
                .Where(p => t % p == 0)
                .Select(p => (T: t, P: p)))
            .ToArray();

        // Two-phase search: first half is pure random sampling for exploration; second half
        // is hill-climbing refinement where each trial restarts from a randomly-chosen top-K
        // result so far and perturbs by small steps within the user's ranges. Catches local
        // maxima by re-sampling among the best basins.
        int explorationCount = Math.Max(1, trials / 2);
        int topK = Math.Max(3, trials / 10);
        var bestSoFar = new List<OptimizeTrialResult>();

        for (int i = 0; i < trials; i++)
        {
            ct.ThrowIfCancellationRequested();

            bool refining = i >= explorationCount && bestSoFar.Count > 0;
            PlannerInput trial;
            if (refining)
            {
                var seed = bestSoFar[rng.Next(bestSoFar.Count)];
                trial = CloneCfg(seed.Config);
                PerturbConfig(trial, space, rng);
            }
            else
            {
                trial = CloneCfg(baseCfg);
                SampleConfig(trial, space, rng, validTPPairs);
            }

            // Tame the cycle sweep to the configured sample count so optimizer trials are
            // ~10× faster than the animation's full-resolution sweep.
            double durationSec;
            try
            {
                bool isCircular = trial.OrbitType == OrbitType.WalkerCircular
                                || trial.OrbitType == OrbitType.WalkerStar;
                double apForCycle = isCircular ? trial.AltitudeKm : trial.ApogeeAltitudeKm;
                var repeat = Planner.GroundTrackRepeat(trial.AltitudeKm, apForCycle);
                durationSec = repeat.CycleSec;
            }
            catch
            {
                durationSec = 86164;
            }

            var connList = connections as IList<(SkoposConnection Conn, int RxIndex)>
                        ?? connections.ToList();
            var results = await Task.Run(() =>
                Planner.EvaluateConnectionsOverCycle(trial, connList, durationSec, samplesPerTrial),
                ct);

            int total = results.Count(r => r.FromIdx >= 0 && r.ToIdx >= 0);
            int connectedCount = results.Count(r => r.UptimePct > 0);
            int metCount = results.Count(r => r.MetWindowPct >= 90.0);
            double meanUp  = total > 0 ? results.Where(r => r.FromIdx >= 0 && r.ToIdx >= 0).Average(r => r.UptimePct)    : 0;
            double meanMet = total > 0 ? results.Where(r => r.FromIdx >= 0 && r.ToIdx >= 0).Average(r => r.MetWindowPct) : 0;

            var summary = new OptimizeTrialResult
            {
                TrialNumber = i + 1,
                Phase = refining ? OptimizerPhase.Refinement : OptimizerPhase.Exploration,
                Config = trial,
                TotalConnections = total,
                Connected = connectedCount,
                MetWindow = metCount,
                MeanUptimePct = meanUp,
                MeanMetWindowPct = meanMet,
            };

            bestSoFar.Add(summary);
            if (bestSoFar.Count > topK)
                bestSoFar = bestSoFar
                    .OrderByDescending(r => r.PassPct)
                    .ThenByDescending(r => r.MeanMetWindowPct)
                    .Take(topK)
                    .ToList();

            Post(() => onResult(summary));
        }
    }

    /// <summary>Uniform sample of one config from the user's search space. Continuous params
    /// sample from [Min, Max]; T/P jointly from precomputed divisibility-valid tuples; F from
    /// the integer range clamped to [0, P-1]; orbit type uniform over the allowed set.</summary>
    static void SampleConfig(PlannerInput trial, OptimizerSearchSpace space, Random rng,
                              (int T, int P)[] validTPPairs)
    {
        if (space.AllowedOrbitTypes.Count > 0)
            trial.OrbitType = space.AllowedOrbitTypes.ElementAt(rng.Next(space.AllowedOrbitTypes.Count));

        // If the user-defined T×P bounds yield at least one valid divisibility pair, pick one.
        // Otherwise leave trial.T/trial.P at the baseline values (clone'd in).
        if (validTPPairs.Length > 0)
        {
            var pick = validTPPairs[rng.Next(validTPPairs.Length)];
            trial.T = pick.T;
            trial.P = pick.P;
        }

        // F is sampled from [max(0, FMin), min(FMax, P-1)]. When the range collapses to empty
        // (e.g. P==1 so the only valid F is 0), force F=0.
        int fLo = Math.Max(0, space.FMin);
        int fHi = Math.Min(space.FMax, Math.Max(0, trial.P - 1));
        trial.F = fHi >= fLo ? rng.Next(fLo, fHi + 1) : 0;

        trial.AltitudeKm     = SampleRange(rng, space.AltitudeKmMin, space.AltitudeKmMax);
        // Ap must be ≥ Pe so the apogee floor is the max of the user's Ap-min and the sampled Pe.
        double apLo = Math.Max(space.ApogeeAltitudeKmMin, trial.AltitudeKm);
        double apHi = Math.Max(apLo, space.ApogeeAltitudeKmMax);
        trial.ApogeeAltitudeKm = SampleRange(rng, apLo, apHi);
        trial.InclinationDeg = SampleRange(rng, space.InclinationDegMin, space.InclinationDegMax);
        trial.LanOffsetDeg   = SampleRange(rng, space.LanOffsetDegMin,   space.LanOffsetDegMax);
        trial.ArgPerigeeDeg  = SampleRange(rng, space.ArgPerigeeDegMin,  space.ArgPerigeeDegMax);
        trial.PhaseOffsetDeg = SampleRange(rng, space.PhaseOffsetDegMin, space.PhaseOffsetDegMax);
    }

    /// <summary>Refinement step: each free parameter gets a small uniform perturbation —
    /// roughly 10% of its allowed range — and is then clamped back to [Min, Max]. Orbit type,
    /// T, and P aren't perturbed during refinement (discrete + constrained, perturbation would
    /// throw the search off the basin it just found). F can shift ±1.</summary>
    static void PerturbConfig(PlannerInput trial, OptimizerSearchSpace space, Random rng)
    {
        double Step(double range) => (rng.NextDouble() * 2 - 1) * range;
        double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

        if (space.VariesAltitude)
        {
            double r = (space.AltitudeKmMax - space.AltitudeKmMin) * 0.10;
            trial.AltitudeKm = Clamp(trial.AltitudeKm + Step(r), space.AltitudeKmMin, space.AltitudeKmMax);
        }
        if (space.VariesApogee)
        {
            double r = (space.ApogeeAltitudeKmMax - space.ApogeeAltitudeKmMin) * 0.10;
            double lo = Math.Max(trial.AltitudeKm, space.ApogeeAltitudeKmMin);
            trial.ApogeeAltitudeKm = Clamp(trial.ApogeeAltitudeKm + Step(r), lo, space.ApogeeAltitudeKmMax);
        }
        if (space.VariesInclination)
        {
            double r = (space.InclinationDegMax - space.InclinationDegMin) * 0.10;
            trial.InclinationDeg = Clamp(trial.InclinationDeg + Step(r), space.InclinationDegMin, space.InclinationDegMax);
        }
        if (space.VariesLan)
        {
            double r = (space.LanOffsetDegMax - space.LanOffsetDegMin) * 0.10;
            trial.LanOffsetDeg = Clamp(trial.LanOffsetDeg + Step(r), space.LanOffsetDegMin, space.LanOffsetDegMax);
        }
        if (space.VariesArgPe)
        {
            double r = (space.ArgPerigeeDegMax - space.ArgPerigeeDegMin) * 0.10;
            trial.ArgPerigeeDeg = Clamp(trial.ArgPerigeeDeg + Step(r), space.ArgPerigeeDegMin, space.ArgPerigeeDegMax);
        }
        if (space.VariesPhase)
        {
            double r = (space.PhaseOffsetDegMax - space.PhaseOffsetDegMin) * 0.10;
            trial.PhaseOffsetDeg = Clamp(trial.PhaseOffsetDeg + Step(r), space.PhaseOffsetDegMin, space.PhaseOffsetDegMax);
        }
        if (space.VariesF && trial.P > 1)
        {
            int fLo = Math.Max(0, space.FMin);
            int fHi = Math.Min(space.FMax, trial.P - 1);
            int newF = trial.F + (rng.Next(3) - 1);
            trial.F = Math.Max(fLo, Math.Min(fHi, newF));
        }
    }

    static double SampleRange(Random rng, double lo, double hi)
        => hi <= lo ? lo : lo + rng.NextDouble() * (hi - lo);

    static PlannerInput CloneCfg(PlannerInput src) => new()
    {
        OrbitType = src.OrbitType,
        AltitudeKm = src.AltitudeKm, ApogeeAltitudeKm = src.ApogeeAltitudeKm,
        InclinationDeg = src.InclinationDeg, ArgPerigeeDeg = src.ArgPerigeeDeg,
        LanOffsetDeg = src.LanOffsetDeg,
        T = src.T, P = src.P, F = src.F,
        PhaseOffsetDeg = src.PhaseOffsetDeg, MinElevDeg = src.MinElevDeg,
        TechLevel = src.TechLevel,
        GroundAntennaDiameterM = src.GroundAntennaDiameterM, GroundFrequencyGHz = src.GroundFrequencyGHz, GroundBandwidthMHz = src.GroundBandwidthMHz,
        GroundStationGainDbi = src.GroundStationGainDbi, GroundStationTxPowerDbm = src.GroundStationTxPowerDbm,
        GroundTxPowerDbm = src.GroundTxPowerDbm,
        IslMode = src.IslMode,
        IslAntennaDiameterM = src.IslAntennaDiameterM, IslFrequencyGHz = src.IslFrequencyGHz, IslBandwidthMHz = src.IslBandwidthMHz,
        IslGainDbiOverride = src.IslGainDbiOverride, IslTxPowerDbm = src.IslTxPowerDbm,
        GroundAntennas = src.GroundAntennas.Select(a => new AntennaAim { AzimuthDeg = a.AzimuthDeg, ElevationDeg = a.ElevationDeg, Band = a.Band, GainDbi = a.GainDbi, TxPowerDbm = a.TxPowerDbm }).ToList(),
        SelectedGroundAntennaIndex = src.SelectedGroundAntennaIndex,
        Metric = src.Metric, CoverageMode = src.CoverageMode,
        PathFromName = src.PathFromName, PathToName = src.PathToName,
        RequiredRateMbps = src.RequiredRateMbps, LatencyLimitSec = src.LatencyLimitSec,
        TimeOffsetSec = src.TimeOffsetSec, Upscale = src.Upscale, FullCaption = src.FullCaption,
        ShowTrackingLinks = src.ShowTrackingLinks, ShowTelecomLinks = src.ShowTelecomLinks,
        ShowIsls = src.ShowIsls, ShowFootprints = src.ShowFootprints,
    };
}
