# Architecture

## Projects

| Project | Target | Role |
| --- | --- | --- |
| `ConstellationPlanner.Core` | `netstandard2.0` | Math kernel — physics, propagator, Walker generator, coverage grid, relay router, station + connection loaders. No Unity or KSP dependencies. |
| `ConstellationPlanner.Cli`  | `net8.0` | CLI orchestrator. Renders PNGs / GIFs via [ImageSharp](https://github.com/SixLabors/ImageSharp). |
| `ConstellationPlanner.Gui`  | `net8.0-windows` | WinForms front-end — the day-to-day designer. |
| `ConstellationPlanner.Ksp`  | `net48` | Placeholder for a future KSP plugin. Currently empty. |

Core targets `netstandard2.0` so the same compiled DLL works under both modern
.NET (Cli, Gui) and Mono / .NET Framework 4.8 (the eventual KSP plugin), without
multi-targeting hassle.

## Source provenance

Code from upstream mods is copied byte-identical where it can be, and tracked
in `Core/UPSTREAM_DIVERGENCE.md`:

- **RealAntennas math** — `Physics.cs`, `MathUtils.cs`, `Tools.cs`, `Math.cs`
  are direct copies of the same files from RA, with Unity-only types stripped
  (replaced by our `Vec3d` and a couple of `math` shims). Link-budget output
  matches what RA produces in-game to floating-point precision.

- **Skopos routing** — `RelayPath.cs` is a *structural* mirror of Skopos's
  `Routing.FindChannels` rather than a literal copy. The original is too
  tightly bound to KSP's `CommNet` and RA's `RealAntennaDigital` types to
  drop in. The algorithm — Dijkstra by Euclidean distance, latency cap,
  per-link rate / capacity filter, tx-only / rx-only role gating, and
  sequential multi-connection capacity contention — follows Skopos's source
  line-for-line. See [Skopos parity](skopos-parity.md) for details.

## Math kernel

- `Vec3d`, `Math`, `MathUtils`, `Tools` — math primitives + RA's dB helpers.
- `Geometry` — Kepler-equation propagator for circular and elliptical orbits,
  `Walker.Delta` shell generator + `NeighborMap` for ISL targeting,
  ground-track repeat-period finder.
- `Coverage` — parallel grid evaluator over (sat × lat/lon × time) for the
  heatmap.
- `IslAnalysis`, `GroundLinkAnalysis` — per-snapshot link enumeration, optional
  Walker-neighbor target map for the "Targeted" ISL mode.
- `RelayPath` — Dijkstra router with `NetworkUsage` capacity accumulation.
- `Station`-prefixed types + `KspCfgReader` — parsers for RA's `City2` blocks
  (DSN sites) and Skopos's `station` + `connection` blocks in `telecom.cfg`.

## Animation loop closure

A constellation's body-fixed appearance only truly repeats every `LCM(T_orb,
T_sid)`, which for general altitudes is irrational. The planner caps the
animation duration at the best continued-fraction convergent of `T_sid / T_orb`
within a 168 h window, so playback loops with at most a few degrees of residual
body-rotation snap.

For altitudes with a clean rational ratio (GEO, 2:1 GPS-like at ~20,180 km,
sun-synchronous, etc.) the snap drops to <1°. For altitudes that don't admit a
short convergent (e.g., 15 000 km MEO), the residual is visible and worth
documenting in a tooltip.

## Where to start reading the code

- `Cli/Planner.cs::Render` — the top-level "build a constellation snapshot,
  evaluate everything" function. Both the standalone CLI and the GUI's per-frame
  render call it.
- `Core/RelayPath.cs::Find` — the routing algorithm where Skopos parity lives.
- `Gui/MainForm.cs::WireEvents` — every control's behavior in one place.
