# Novolis.Physics.TestSupport

Shared **non-test** helpers for the Novolis.Physics unit project: console diagnostics, ASCII grids, pipe tables, and small utilities so TUnit does not discover extra tests in this assembly.

## Running tests with console output

```bash
dotnet run --project tests/Novolis.Physics.Unit -c Release -- --show-stdout All
```

Optional: `--output Detailed` for more verbose test progress.

## TestOutput

Thread-safe writes to `Console.Out` with a fixed `[scope]` prefix on every line.

- **`TestOutput.ForScope(string scope, bool useSharedConsoleLock = false)`** — convenience factory. Use **`useSharedConsoleLock: true`** when multiple fixtures share one trace stream.
- **`Section(string title)`** — blank line + `=== title ===`
- **`Results(string title)`** — banner for numeric outcomes
- **`Line(...)`** — labeled scalars; doubles use invariant `G17` formatting
- **`AsciiGrid(...)`** — monospace rasters for trajectories

### Tables

**`TestOutput.Table<T>(...)`** reflects public instance properties on `T`. Configure **`TableOptions`**: `MaxRows`, `MaxCellWidth`, `RightAlignNumericColumns`, etc.

## PhysicsDashboard

- **`SectionAndTable`** — `Section` then `Table`
- **`ResultsAndTable`** — `Results` then `Table`

## PhysicsTraceFormatting

**`PhysicsTraceFormatting.Rd` / `Rs`** — invariant rounding helpers for physics scenario tables.

## TestOutputSequences

**`TestOutputSequences.EveryNth<T>(source, stride)`** — sample long simulations for readable tables.

## Parallelism and console traces

Use TUnit **`[NotInParallel(...)]`** with a stable key when traces must not interleave.

- **`TestOutput.NotInParallelKey(string scope)`**
- **`TraceParallelismKeys`** — shared constants (e.g. **`NovolisPhysicsBallistics`**)

Example: [`NovolisPhysicsTestTrace.cs`](../Novolis.Physics.Unit/NovolisPhysicsTestTrace.cs) in the unit project.

## Orbits test fixtures

**`Novolis.Physics.TestSupport.Orbits`** contains reference orbit data used by unit tests (not shipped in product packages):

- **`OrbitalTestConstants`** — Earth-centered elliptical orbit parameters
- **`OrbitalTestState`** — `CreatePeriapsisState()` initial conditions

## Sweep integrator traces

**Refl** counts from `BvhStaticSphereIntegrator` are wall contact resolutions inside one sweep step (including grazing and multi-hit substeps), not a one-to-one count of macroscopic bounces.
