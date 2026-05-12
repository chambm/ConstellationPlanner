# RealAntennas upstream divergence

Track every intentional change made to files copied from
`RealAntennas/src/RealAntennasProject/` so re-syncing from upstream stays mechanical.

Source tree at copy time: `C:\Users\Matt\Downloads\ksp-claude\RealAntennas\` (local clone).

## Conventions (apply to all copied files)

- `using UnityEngine;` / `using UnityEngine.Profiling;` / `using Unity.Mathematics;` removed.
- `UnityEngine.Vector3d` and `Unity.Mathematics.double3` → `ConstellationPlanner.Core.Vec3d`
  via file-level `using double3 = ConstellationPlanner.Core.Vec3d;` aliases.
- Same for `float3` (Vec3d serves both, since the planner runs in double precision regardless).
- `Unity.Mathematics.math.*` and `UnityEngine.Mathf.*` resolved by internal shim classes
  in `Math.cs` (lowercase `math`, capitalized `Mathf`) — copied method bodies stay byte-identical.
- `MathF` is unavailable in netstandard2.0, so float overloads in the `math` shim cast through
  `Math.X` (double) and back to `float`. Numerically equivalent.
- Class access defaulted to `internal` upstream; bumped to `public` so Cli/Ksp can reference.

## Per-file entries

### `Tools.cs` (subset of upstream `Tools.cs`)
- Only `RATools.LinearScale` and `RATools.LogScale` (4 overloads) pulled in. Everything else
  in upstream `Tools.cs` is KSP-binding (PrettyPrint formatters, ConfigNode helpers) — drop.

### `MathUtils.cs`
- Direct copy. No body changes; only Unity using-directives stripped, double3/float3 aliased.

### `Physics.cs`
- **Dropped (KSP-binding wrappers, will reappear in `Ksp` project):**
  - `SolarLuminosity` property (uses `PhysicsGlobals` + `Planetarium`)
  - `ReceivedPower(RealAntenna, RealAntenna, float, float)`
  - `PointingLoss(RealAntenna, Vector3)`
  - `GetEquilibriumTemperature(CelestialBody)`
  - `BodyBaseTemperature(CelestialBody)`
  - `BodyNoiseTemp(RealAntenna, CelestialBody, Vector3d)` overload
  - `NoiseTemperature(RealAntenna, Vector3d)`
  - `AntennaMicrowaveTemp(RealAntenna)`
  - `AtmosphericTemp(RealAntenna, Vector3d)` wrapper (the `(double3, double3, double3, float)` pure overload is kept)
  - `CosmicBackgroundTemp(RealAntenna, Vector3d)` private wrapper (the pure overload kept)
  - `AllBodyTemps(RealAntenna, Vector3d)` (and the only `Profiler.Begin/EndSample` calls in this file)
- **Body changes:**
  - In `BodyNoiseTemp(double3...)`: `math.PI` (Unity returns `float`) → `(float)math.PI` casts
    in three places, because our shim's `math.PI` is `double` (`= Math.PI`). Numerically identical.

### `Math.cs` (new — not from upstream)
- Internal `math` and `Mathf` shim classes mirroring the Unity APIs the copied files use.
  Adding entries here as more files come in is the expected pattern — keeps copied bodies
  byte-identical to upstream so re-syncs are mechanical.

### `Vec3d.cs` (new — not from upstream)
- Replaces Unity's `Vector3d` + `Unity.Mathematics.double3`/`float3` with a pure-math
  double-precision struct. Manual `GetHashCode` (no `System.HashCode` in netstandard2.0).
