# Skopos parity

How the planner's relay matches and diverges from Skopos's in-game
`Routing.FindChannels`.

## What's identical

| Concern | Skopos | Planner |
| --- | --- | --- |
| Dijkstra metric | Euclidean distance | Euclidean distance |
| Latency cap | strict `>` against `latency × c` | identical (plus a `0 = no limit` convenience) |
| Rate / capacity filter | `CapacityWithUsage(usage) < required` ⇒ skip edge | identical |
| `tx_only` / `rx_only` gating | rx-only nodes can't have outgoing edges; tx-only nodes can't be intermediates | identical |
| Multi-connection capacity contention | per-antenna tx-power + spectrum bookkeeping; later connections see depleted budgets | identical, via shared `NetworkUsage` instance routed connections in declaration order |
| `CapacityWithUsage` formula | `min(bw-limited, power-limited)` from per-antenna spectrum / tx-power use | identical |
| Equidistant-node handling | "will fail" (TODO comment in Skopos source) | `SortedSet<(double, int)>` deduped — more robust |

## Known gaps

- **Point-to-multipoint atomicity.** Skopos's `FindChannels` evaluates a
  connection's *set* of rx stations as one unit — the connection is `Available`
  only if all rx are reached. The planner treats each `(connection × rx)` pair
  independently and reports per-rx uptime. Useful in practice, but for a multi-rx
  connection like `l0_andover_europe` (rx = `pleumeur_bodou` *and*
  `goonhilly_downs`) Skopos's verdict would be the AND of our two rows; ours
  shows them as separate uptime numbers.

- **Station-to-station relays.** Skopos lets a path traverse multiple ground
  stations via direct station-station LoS edges in CommNet. The planner only
  routes `ground → sat → … → sat → ground` — no inter-station hops.

- **`multiple_tracking_` stations.** Skopos models DSN-style sites as
  multi-tracking (each physical antenna stands in for N independent receivers;
  tx-power / spectrum don't get consumed when used). The planner treats every
  station as single-tracking, so a DSN site that Skopos would let serve many
  simultaneous connections will be capacity-limited in the planner.

- **Antenna identity granularity.** Skopos tracks usage per
  `RealAntennaDigital` instance. The planner tracks per `(sat_idx, role,
  antenna_idx)` tuple where role is "ground" or "ISL". Equivalent for
  single-antenna roles; could diverge if a future feature gives one sat multiple
  ground antennas at distinct frequencies.

## Why mirror rather than copy

`Routing.FindChannels` accesses `RACommNode.Keys`, `RACommLink.FwdAntennaTx`,
`precisePosition`, `BandInfo.ChannelWidth`, `RAModulator.ModulationBits`, etc. —
all KSP / RA runtime types. Compiling the file as-is into `Core` would require
cloning large parts of `CommNet` and `RealAntennas`, which defeats the point of
a lightweight external tool that doesn't need to drag the whole KSP runtime
along.

Mirroring the algorithm — same control flow, same exit conditions, the same
variable names where it makes sense — gets us byte-identical output on the
inputs we care about, without the runtime baggage. Future Skopos changes can be
diffed against `RelayPath.cs` and ported by inspection.
