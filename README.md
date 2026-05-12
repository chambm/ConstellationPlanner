# ConstellationPlanner

External design tool for satellite constellations in KSP/RP-1+RealAntennas+Skopos.
Math is copied (not ported) from [RealAntennas](https://github.com/KSP-RO/RealAntennas)
so the link budget is byte-identical to what RA computes in-game. Connection
evaluation mirrors the algorithm in [Skopos](https://github.com/eggrobin/Skopos)'s
`Routing.FindChannels`.

## What it does

- **Walker / Molniya / Tundra / Custom** constellations with full classical orbital
  elements (a, e, i, Ω, ω, ν₀). Molniya/Tundra presets lock period + critical
  inclination; Custom is free-form.
- **Live coverage heatmap** with ground tracks, sat sub-points, footprints, ISLs,
  and a configurable relay path overlay.
- **ISL modes**: None / Omni / Directional (fixed boresight) / Targeted (each
  antenna locked to a specific Walker neighbour, mirroring RA's lock-to-vessel).
- **Skopos integration**: parses `telecom.cfg` station + connection blocks. The
  GUI's connection picker auto-fills From/To/RequiredRate/LatencyLimit from any
  `connection { }` block. A "Test all Skopos connections" button evaluates every
  (connection × rx) pair over the constellation's repeat cycle, tracking
  capacity contention (Skopos's `CapacityWithUsage`) so connections later in the
  declaration order see depleted tx-power / spectrum on shared antennas.
- **Cycle-aware animation**: the animation duration auto-caps at the
  ground-track repeat period via continued-fraction approximation of T_orb/T_sid,
  so the GIF loops with minimum body-rotation snap. Mid-playback config changes
  trigger an instant live-frame preview while the full cycle re-renders in the
  background; in-flight renders are cancellable.
- **Per-cycle stats**: uptime / met-window / avg latency / avg rate at high
  temporal resolution (up to 5000 samples per cycle, decoupled from frame count),
  plus min / max / average ISL link rate over the same window.
- **Per-role TX power + DC consumption**, with per-tech-level amplifier efficiency.

## Layout

| Project | Target | Role |
| --- | --- | --- |
| `ConstellationPlanner.Core` | `netstandard2.0` | Math kernel — copied RA physics, propagator (Kepler for elliptical), Walker generator, coverage grid, relay (Skopos-equivalent), station + connection loaders. No Unity deps. |
| `ConstellationPlanner.Cli`  | `net10.0` | Headless CLI. Renders coverage PNGs and animated GIFs via ImageSharp. |
| `ConstellationPlanner.Gui`  | `net10.0-windows` | WinForms front-end — full interactive constellation designer. The day-to-day tool. |
| `ConstellationPlanner.Ksp`  | `net48` | KSP plugin slot. Currently empty — placeholder for the planned in-game integration. |

`Core` has no ImageSharp / Unity dependency so it compiles cleanly under both
`net10` (Cli/Gui) and `net48` (the future Ksp plugin).

## Build & run

```
dotnet build
dotnet run --project ConstellationPlanner.Gui    # interactive designer (Windows)
dotnet run --project ConstellationPlanner.Cli    # CLI: outputs coverage_24h.gif + per-snapshot PNGs
```

The first run hard-codes paths to `C:\Program Files (x86)\Steam\...\GameData\Skopos\telecom.cfg`
and `RealAntennas\PlanetPacks\RealSolarSystem.cfg`. If your install is elsewhere,
edit `Planner.cs::EnsureLoaded` — there's no config file yet.

## Source provenance

- `Core/Physics.cs`, `MathUtils.cs`, `Tools.cs`, `Math.cs` are byte-identical
  copies of RealAntennas equivalents with Unity bits stripped (see
  `Core/UPSTREAM_DIVERGENCE.md`). Re-syncing from upstream is mechanical.
- `Core/RelayPath.cs` is a structural mirror of Skopos's `Routing.FindChannels`
  — Dijkstra-by-distance with latency cap, per-link rate filter, tx-only /
  rx-only role gating, and `CapacityWithUsage`-equivalent multi-connection
  capacity tracking. Known gaps from upstream Skopos: no point-to-multipoint
  atomicity (multi-rx connections are evaluated as separate rows), no
  station-to-station relays (only sat intermediates), no `multiple_tracking_`
  distinction.

## License

The Skopos and RealAntennas copies retain their upstream licenses (Skopos is
MIT, RA is MIT). Project-original code is MIT — see headers per file or the
license you add when forking.
