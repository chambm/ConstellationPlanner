using System;
using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>One Antenna block from a Skopos station's telecom.cfg, with its UPGRADE chain.
    /// Mirrors RealAntennas' <c>ModuleRealAntenna</c> shape: base values for the antenna at its
    /// minimum tech level, plus a sequence of UPGRADE blocks that override individual fields
    /// once the era is unlocked.</summary>
    public sealed class StationAntennaSpec
    {
        public string Band = "";                    // RFBand string from cfg, e.g. "L (Wideband)"
        public int BaseTechLevel;
        public double GainDbi;
        public double TxPowerDbm;
        public double NoiseTempK;
        public int ModBits = 1;
        public double ReferenceFrequencyMHz;
        public List<UpgradeStep> Upgrades = new();

        public sealed class UpgradeStep
        {
            public int TechLevel;
            public double? GainDbi;
            public double? TxPowerDbm;
            public double? NoiseTempK;
            public int? ModBits;
        }

        public readonly struct Effective
        {
            public readonly double GainDbi;
            public readonly double TxPowerDbm;
            public readonly double NoiseTempK;
            public readonly int ModBits;
            public Effective(double gainDbi, double txPowerDbm, double noiseTempK, int modBits)
            {
                GainDbi = gainDbi; TxPowerDbm = txPowerDbm; NoiseTempK = noiseTempK; ModBits = modBits;
            }
        }

        /// <summary>Roll up base + every UPGRADE whose TechLevel ≤ techLevel. Returns null if
        /// the antenna isn't unlocked yet at this TL. Read-only — assumes
        /// <see cref="Upgrades"/> is already sorted by TechLevel ascending (sorting is done
        /// once at load time so this method is safe to call from parallel threads).</summary>
        public Effective? Resolve(int techLevel)
        {
            if (techLevel < BaseTechLevel) return null;
            double gain = GainDbi, power = TxPowerDbm, temp = NoiseTempK;
            int mod = ModBits;
            foreach (var u in Upgrades)
            {
                if (u.TechLevel > techLevel) break;
                if (u.GainDbi.HasValue)    gain  = u.GainDbi.Value;
                if (u.TxPowerDbm.HasValue) power = u.TxPowerDbm.Value;
                if (u.NoiseTempK.HasValue) temp  = u.NoiseTempK.Value;
                if (u.ModBits.HasValue)    mod   = u.ModBits.Value;
            }
            return new Effective(gain, power, temp, mod);
        }
    }

    /// <summary>Per-station antenna registry — map of station name → list of band-specific
    /// antennas. Populated by <c>StationLoader.LoadSkoposTelecom</c> when a catalog is
    /// supplied; queryable by (station, band-prefix, tech level).</summary>
    public sealed class StationAntennaCatalog
    {
        readonly Dictionary<string, List<StationAntennaSpec>> _byStation =
            new(StringComparer.OrdinalIgnoreCase);

        public void Add(string stationName, StationAntennaSpec ant)
        {
            if (!_byStation.TryGetValue(stationName, out var list))
            {
                list = new List<StationAntennaSpec>();
                _byStation[stationName] = list;
            }
            list.Add(ant);
        }

        public bool Contains(string stationName) => _byStation.ContainsKey(stationName);

        /// <summary>All station names that have any antenna registered. Iteration order matches
        /// the catalog's load order (Skopos's STATION block order).</summary>
        public IEnumerable<string> Stations => _byStation.Keys;

        /// <summary>Resolve the station's antenna for a given band at a given tech level. The
        /// band prefix matches the leading token of <see cref="StationAntennaSpec.Band"/>: "L"
        /// matches "L (Wideband)" but not "Ku".</summary>
        public StationAntennaSpec.Effective? Get(string stationName, string bandPrefix, int techLevel)
        {
            if (!_byStation.TryGetValue(stationName, out var list)) return null;
            // Iterate antennas, pick the best matching band whose Resolve doesn't return null.
            foreach (var ant in list)
            {
                if (!BandMatches(ant.Band, bandPrefix)) continue;
                var eff = ant.Resolve(techLevel);
                if (eff != null) return eff;
            }
            return null;
        }

        /// <summary>Same as <see cref="Get"/> but returns the first available antenna at any
        /// tech level (highest base-TL ≤ user TL). Useful for a "best you can do" UI hint.</summary>
        public StationAntennaSpec.Effective? GetBest(string stationName, string bandPrefix, int techLevel)
        {
            if (!_byStation.TryGetValue(stationName, out var list)) return null;
            StationAntennaSpec? best = null;
            foreach (var ant in list)
            {
                if (!BandMatches(ant.Band, bandPrefix)) continue;
                if (techLevel < ant.BaseTechLevel) continue;
                if (best == null || ant.BaseTechLevel > best.BaseTechLevel) best = ant;
            }
            return best?.Resolve(techLevel);
        }

        static bool BandMatches(string antennaBand, string bandPrefix)
        {
            if (string.IsNullOrEmpty(antennaBand) || string.IsNullOrEmpty(bandPrefix)) return false;
            if (!antennaBand.StartsWith(bandPrefix, StringComparison.OrdinalIgnoreCase)) return false;
            // Avoid "K" matching "Ku" by requiring the next char be non-letter or end-of-string.
            if (antennaBand.Length == bandPrefix.Length) return true;
            char next = antennaBand[bandPrefix.Length];
            return !char.IsLetterOrDigit(next);
        }
    }
}
