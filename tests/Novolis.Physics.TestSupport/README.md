# StarConflictsRevolt.Tests.Support

Shared **non-test** helpers for unit and integration test projects: console diagnostics, ASCII grids, pipe tables, and small utilities so TUnit does not discover extra tests in this assembly.

This library complements **`StarConflictsRevolt.Tests.TestKit`** (ASP.NET hosts, Raven, fixtures). **`TestOutput`** and related types live **here**, not in TestKit. Many test projects reference TestKit, which in turn references Support; **`StarConflictsRevolt.Tests.Novolis.Physics.Unit`** references Support only.

## Seeing console output (Microsoft.Testing.Platform + TUnit)

Standard test runs often hide stdout. To print `TestOutput` lines locally, pass MTP arguments **after** `--`:

```bash
dotnet test --project tests/unit/StarConflictsRevolt.Tests.Novolis.Physics.Unit/StarConflictsRevolt.Tests.Novolis.Physics.Unit.csproj -- --show-stdout All
```

Optional: `--output Detailed` for more verbose test progress. With `global.json` opting into MTP, always use **`--project <path>`**; a bare `.csproj` path as the first argument is rejected.

## TestOutput

Thread-safe writes to `Console.Out` with a fixed `[scope]` prefix on every line.

- **`TestOutput.ForScope(string scope, bool useSharedConsoleLock = false)`** — convenience factory. Use **`useSharedConsoleLock: true`** when multiple fixtures or types share one trace stream so lines do not interleave unpredictably.
- **`TestOutput(string scope, object? gate)`** — full constructor; pass **`TestOutput.SharedConsoleLock`** as `gate` to coordinate with other instances.
- **`Section(string title)`** — blank line + `=== title ===` for setup or “what this test does.”
- **`Results(string title)`** — two blank lines + banner so numeric outcomes stand out in long logs.
- **`Line(...)`** — labeled scalars or free-form messages; doubles use invariant `G17` formatting. **`Line(label, x, y, z)`** prints one invariant triple (e.g. `Vector3d`-style). **`Line(label, bool)`** prints `true` / `false`.
- **`Blank()`** — single blank line.
- **`AsciiGrid(char[,] cells, string? caption)`** / **`AsciiGrid(int[,] paletteIndices, ReadOnlySpan<char> palette, ...)`** — monospace rasters for trajectories or spatial sanity checks.

### Tables

**`TestOutput.Table<T>(...)`** (partial class in `TestOutputTable.cs`) reflects public instance properties on `T` (records work well). Configure **`TableOptions`**: `MaxRows`, `MaxCellWidth`, `RightAlignNumericColumns`, etc.

## PhysicsDashboard

Static helpers that combine banners with tables:

- **`SectionAndTable`** — `Section` then `Table` (inputs, parameters, intermediate snapshots).
- **`ResultsAndTable`** — `Results` then `Table` (final metrics, parity rows, sweep summaries).

Use **`columnPropertyOrder`** when property metadata order is not the column order you want.

### Novolis.Physics sweep integrator traces

Some scenario tests report a **reflection** or **Refl** count from **`Novolis.Physics.Collision.Simple.BvhStaticSphereIntegrator`**. That value is the number of **wall contact resolutions** inside one sweep step (including grazing contacts and multi-hit substeps), **not** a one-to-one count of macroscopic bounces you would hear in a room. Compare cumulative Refl to milestone tables only as a stability signal, not as “number of impacts.”

## PhysicsTraceFormatting

**`PhysicsTraceFormatting.Rd` / `Rs`** — invariant rounding helpers for physics scenario tables (used by Novolis.Physics room tests and similar traces).

## TestOutputSequences

**`TestOutputSequences.EveryNth<T>(source, stride)`** — yield every Nth element for long simulations so tables stay readable.

## Parallelism and console traces

When several tests write to the console, use TUnit **`[NotInParallel(...)]`** with a stable key so traces stay readable.

- **`TestOutput.NotInParallelKey(string scope)`** — derive a key from a short scope label.
- **`TraceParallelismKeys`** — shared constants (e.g. **`NovolisPhysicsBallistics`**) for one policy across files.

**Novolis.Physics** example: [`NovolisPhysicsTestTrace`](../../unit/StarConflictsRevolt.Tests.Novolis.Physics.Unit/NovolisPhysicsTestTrace.cs) exposes a single **`TestOutput`** instance and a **`NotInParallel`** key; test classes use **`[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]`** and **`var o = NovolisPhysicsTestTrace.Out`** for consistent, non-interleaved `[Novolis.Physics]` lines.

## Further reading

- Cursor skill: [`.cursor/skills/tunit-testing-platform/SKILL.md`](../../../.cursor/skills/tunit-testing-platform/SKILL.md)
- Tooling doc: [`docs/tooling/tunit-playwright.md`](../../../docs/tooling/tunit-playwright.md)
