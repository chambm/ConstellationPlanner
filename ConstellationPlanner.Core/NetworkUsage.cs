using System;
using System.Collections.Generic;

namespace ConstellationPlanner.Core
{
    /// <summary>Identifies one antenna for the purpose of capacity-with-usage accounting.
    /// Three kinds: a sat-mounted ground-link antenna (sat_idx, antenna_idx in the ground
    /// list), a sat-mounted ISL antenna (sat_idx, antenna_idx in the ISL list), or a ground
    /// station (station_idx). The kind discriminator keeps namespaces separate so e.g. ground
    /// antenna 0 on sat 5 is distinct from ISL antenna 0 on sat 5.</summary>
    public readonly struct AntennaKey : IEquatable<AntennaKey>
    {
        /// <summary>0 = sat ground antenna, 1 = sat ISL antenna, 2 = ground station.</summary>
        public readonly byte Kind;
        public readonly int Node;
        public readonly int Ant;

        public AntennaKey(byte kind, int node, int ant) { Kind = kind; Node = node; Ant = ant; }
        public static AntennaKey SatGround(int satIdx, int antennaIdx) => new(0, satIdx, antennaIdx);
        public static AntennaKey SatIsl(int satIdx, int antennaIdx)    => new(1, satIdx, antennaIdx);
        public static AntennaKey Station(int stationIdx)               => new(2, stationIdx, 0);

        public bool Equals(AntennaKey o) => Kind == o.Kind && Node == o.Node && Ant == o.Ant;
        public override bool Equals(object? o) => o is AntennaKey k && Equals(k);
        public override int GetHashCode()
            => unchecked(((Node * 397) ^ Ant) * 397 ^ Kind);
        public override string ToString() => Kind switch
        {
            0 => $"sat{Node}.gnd{Ant}",
            1 => $"sat{Node}.isl{Ant}",
            2 => $"sta{Node}",
            _ => $"?{Kind}.{Node}.{Ant}",
        };
    }

    /// <summary>Per-antenna capacity bookkeeping used by <see cref="Relay.Find"/> when
    /// evaluating multiple connections sequentially. Mirrors Skopos's
    /// <c>RoutingNetworkUsage</c> — each successfully-routed connection accrues TX-power
    /// fraction (0..1) on the link's tx antenna and spectrum (Hz) on both endpoints. Later
    /// connections see the remaining capacity via <see cref="CapacityWithUsage"/> and may be
    /// turned away on links the earlier ones saturated.
    /// <para>Kept thread-safe-friendly via a single-writer, multi-reader assumption: the
    /// orchestrator routes connections sequentially and only one writer touches the
    /// dictionaries between reads. No internal locking — callers ensure ordering.</para></summary>
    public sealed class NetworkUsage
    {
        readonly Dictionary<AntennaKey, double> _txPowerFraction = new();
        readonly Dictionary<AntennaKey, double> _spectrumHz = new();

        public double TxPowerFraction(AntennaKey k)
            => _txPowerFraction.TryGetValue(k, out var v) ? v : 0;

        public double SpectrumHz(AntennaKey k)
            => _spectrumHz.TryGetValue(k, out var v) ? v : 0;

        public void AccrueTxPower(AntennaKey k, double fraction)
        {
            _txPowerFraction.TryGetValue(k, out var v);
            _txPowerFraction[k] = v + fraction;
        }

        public void AccrueSpectrum(AntennaKey k, double hz)
        {
            _spectrumHz.TryGetValue(k, out var v);
            _spectrumHz[k] = v + hz;
        }

        public void Clear()
        {
            _txPowerFraction.Clear();
            _spectrumHz.Clear();
        }

        /// <summary>Compute the link's effective capacity (bps) given current usage. Mirrors
        /// Skopos's <c>OrientedLink.CapacityWithUsage</c>:
        /// <code>
        ///   bw_limited    = min(max_symbol_rate, channel_bw − max(tx_used, rx_used)) × bits/symbol
        ///   power_limited = max_data_rate × (1 − tx_power_used)
        ///   capacity      = min(bw_limited, power_limited)
        /// </code>
        /// <paramref name="maxDataRateBps"/> is the link's achievable rate at the snapshot's SNR
        /// (after halvings) — the analogue of Skopos's <c>OrientedLink.max_data_rate</c>.</summary>
        public double CapacityWithUsage(double maxDataRateBps, LinkBudget budget, AntennaKey txAnt, AntennaKey rxAnt)
        {
            double bitsPerSymbol = budget.MaxBitsPerSymbol * budget.CodingRate;
            if (maxDataRateBps <= 0 || bitsPerSymbol <= 0 || budget.BandwidthHz <= 0)
                return maxDataRateBps;
            double maxSymbolRate = maxDataRateBps / bitsPerSymbol;
            double txSpec = SpectrumHz(txAnt);
            double rxSpec = SpectrumHz(rxAnt);
            double available = budget.BandwidthHz - Math.Max(txSpec, rxSpec);
            if (available < 0) available = 0;
            double bwLimited = Math.Min(maxSymbolRate, available) * bitsPerSymbol;
            double powerUsed = TxPowerFraction(txAnt);
            double powerLimited = maxDataRateBps * Math.Max(0, 1 - powerUsed);
            return Math.Min(bwLimited, powerLimited);
        }

        /// <summary>Mark one link as carrying <paramref name="dataRateBps"/> of traffic.
        /// TX antenna takes both power (<c>data_rate / max_data_rate</c> fraction) and spectrum;
        /// RX antenna takes spectrum only. Spectrum per link = <c>data_rate / bits_per_symbol</c>
        /// — the channel width consumed by this stream at its modulation/coding.</summary>
        public void UseLink(double dataRateBps, double maxDataRateBps, LinkBudget budget,
                             AntennaKey txAnt, AntennaKey rxAnt)
        {
            if (dataRateBps <= 0) return;
            if (maxDataRateBps > 0)
                AccrueTxPower(txAnt, dataRateBps / maxDataRateBps);
            double bitsPerSymbol = budget.MaxBitsPerSymbol * budget.CodingRate;
            if (bitsPerSymbol > 0)
            {
                double spec = dataRateBps / bitsPerSymbol;
                AccrueSpectrum(txAnt, spec);
                AccrueSpectrum(rxAnt, spec);
            }
        }
    }
}
