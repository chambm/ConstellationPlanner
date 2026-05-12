using System;
using System.Collections.Generic;
using System.IO;

namespace ConstellationPlanner.Core
{
    /// <summary>One Skopos <c>connection { }</c> block — a service the telecom network is
    /// expected to provide. Has a single transmitter and one or more receivers (point-to-
    /// multipoint), a required data rate, a latency cap (light-time only — Skopos's
    /// <c>latency</c> is a propagation budget not a per-hop processing one), and a window
    /// over which the achieved availability is measured (e.g. 90 sec rolling).</summary>
    public sealed class SkoposConnection
    {
        public string Name = "";
        public string TxStation = "";
        public List<string> RxStations = new();
        public double LatencySec;            // == "latency" cfg value (== light-time cap)
        public double DataRateBps;           // == "rate" cfg value
        public double WindowSec;             // == "window" cfg value (availability window)
        public bool Exclusive;
    }

    /// <summary>Pulls station coordinates + Skopos connection definitions out of RealAntennas
    /// + Skopos cfg files. Antenna parameters are intentionally ignored for the MVP — every
    /// station uses the shared <see cref="LinkBudget"/>. Station-specific antennas can be
    /// plumbed through later.</summary>
    public static class StationLoader
    {
        /// <summary>RA RSS planet pack: City2 blocks under Body[Kerbin].PQS.Mods.
        /// Display name comes from objectName (e.g. "DSS 14 - Goldstone").</summary>
        public static List<GroundStation> LoadRATracking(string raCfgPath)
        {
            var stations = new List<GroundStation>();
            if (!File.Exists(raCfgPath)) return stations;
            var root = KspCfgReader.ParseFile(raCfgPath);
            foreach (var c in root.FindAll("City2"))
            {
                double? lat = KspCfgReader.ParseDouble(c.GetValue("lat"));
                double? lon = KspCfgReader.ParseDouble(c.GetValue("lon"));
                if (lat == null || lon == null) continue;
                string name = c.GetValue("objectName") ?? c.GetValue("name") ?? "(unnamed)";
                stations.Add(new GroundStation(name, lat.Value, lon.Value, StationKind.Tracking));
            }
            return stations;
        }

        /// <summary>Skopos telecom.cfg: station blocks under skopos_telecom. Uses objectName
        /// as display name, falling back to the lower-case key. If <paramref name="catalog"/>
        /// is supplied, also parses each station's Antenna+UPGRADE blocks into it.</summary>
        public static List<GroundStation> LoadSkoposTelecom(string skoposCfgPath,
                                                              StationAntennaCatalog? catalog = null)
        {
            var stations = new List<GroundStation>();
            if (!File.Exists(skoposCfgPath)) return stations;
            var root = KspCfgReader.ParseFile(skoposCfgPath);
            foreach (var c in root.FindAll("station"))
            {
                double? lat = KspCfgReader.ParseDouble(c.GetValue("lat"));
                double? lon = KspCfgReader.ParseDouble(c.GetValue("lon"));
                if (lat == null || lon == null) continue;
                string key = c.GetValue("name") ?? c.GetValue("objectName") ?? "(unnamed)";
                StationRole role = (c.GetValue("role") ?? "trx").Trim().ToLowerInvariant() switch
                {
                    "tx" => StationRole.TxOnly,
                    "rx" => StationRole.RxOnly,
                    _    => StationRole.Trx,
                };
                stations.Add(new GroundStation(key, lat.Value, lon.Value, StationKind.Telecom, role));

                if (catalog == null) continue;
                // Walk this station's *direct* Antenna children (not c.FindAll which recurses
                // through UPGRADE descendants). Each Antenna can have multiple UPGRADE
                // children that override fields at higher tech levels.
                foreach (var antNode in c.Children)
                {
                    if (!IsName(antNode, "Antenna")) continue;
                    var spec = new StationAntennaSpec
                    {
                        Band = antNode.GetValue("RFBand") ?? "",
                        BaseTechLevel = ParseInt(antNode.GetValue("TechLevel")) ?? 0,
                        GainDbi = KspCfgReader.ParseDouble(antNode.GetValue("referenceGain")) ?? 0,
                        TxPowerDbm = KspCfgReader.ParseDouble(antNode.GetValue("TxPower")) ?? 0,
                        NoiseTempK = KspCfgReader.ParseDouble(antNode.GetValue("AMWTemp")) ?? 0,
                        ModBits = ParseInt(antNode.GetValue("ModulationBits")) ?? 1,
                        ReferenceFrequencyMHz = KspCfgReader.ParseDouble(antNode.GetValue("referenceFrequency")) ?? 0,
                    };
                    foreach (var upgNode in antNode.Children)
                    {
                        if (!IsName(upgNode, "UPGRADE")) continue;
                        spec.Upgrades.Add(new StationAntennaSpec.UpgradeStep
                        {
                            TechLevel  = ParseInt(upgNode.GetValue("TechLevel")) ?? 0,
                            GainDbi    = KspCfgReader.ParseDouble(upgNode.GetValue("referenceGain")),
                            TxPowerDbm = KspCfgReader.ParseDouble(upgNode.GetValue("TxPower")),
                            NoiseTempK = KspCfgReader.ParseDouble(upgNode.GetValue("AMWTemp")),
                            ModBits    = ParseInt(upgNode.GetValue("ModulationBits")),
                        });
                    }
                    // Sort once at load time so Resolve is read-only and safe under parallel queries.
                    spec.Upgrades.Sort((a, b) => a.TechLevel.CompareTo(b.TechLevel));
                    catalog.Add(key, spec);
                }
            }
            return stations;
        }

        /// <summary>Parse all <c>connection { }</c> blocks from Skopos's telecom.cfg. Returns
        /// one <see cref="SkoposConnection"/> per block, preserving declaration order. Multiple
        /// <c>rx</c> entries on a single connection are kept as a list — the connection is
        /// considered satisfied (in Skopos's terms) when *all* rx stations are reached.</summary>
        public static List<SkoposConnection> LoadSkoposConnections(string skoposCfgPath)
        {
            var conns = new List<SkoposConnection>();
            if (!File.Exists(skoposCfgPath)) return conns;
            var root = KspCfgReader.ParseFile(skoposCfgPath);
            foreach (var c in root.FindAll("connection"))
            {
                var conn = new SkoposConnection
                {
                    Name      = c.GetValue("name") ?? "",
                    TxStation = c.GetValue("tx") ?? "",
                    LatencySec  = KspCfgReader.ParseDouble(c.GetValue("latency")) ?? 0,
                    DataRateBps = KspCfgReader.ParseDouble(c.GetValue("rate"))    ?? 0,
                    WindowSec   = KspCfgReader.ParseDouble(c.GetValue("window"))  ?? 0,
                    Exclusive   = string.Equals((c.GetValue("exclusive") ?? "false").Trim(),
                                                  "true", StringComparison.OrdinalIgnoreCase),
                };
                foreach (var rx in c.GetValues("rx"))
                    if (!string.IsNullOrWhiteSpace(rx))
                        conn.RxStations.Add(rx.Trim());
                if (string.IsNullOrEmpty(conn.TxStation) || conn.RxStations.Count == 0) continue;
                conns.Add(conn);
            }
            return conns;
        }

        static bool IsName(CfgNode node, string target) =>
            string.Equals(node.Name, target, StringComparison.OrdinalIgnoreCase);

        static int? ParseInt(string? v)
        {
            if (v == null) return null;
            if (int.TryParse(v.Trim(), System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out int n))
                return n;
            // Skopos sometimes writes ints as floats (e.g. "1.0"); fall back to that.
            var d = KspCfgReader.ParseDouble(v);
            return d.HasValue ? (int)d.Value : null;
        }

        /// <summary>Find a station by name, case-insensitive. Returns -1 if not found.</summary>
        public static int IndexOf(IList<GroundStation> stations, string name)
        {
            for (int i = 0; i < stations.Count; i++)
                if (string.Equals(stations[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
    }
}
