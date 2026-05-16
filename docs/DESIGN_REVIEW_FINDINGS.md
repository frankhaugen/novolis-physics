# Novolis.Physics — Design Review Findings

**Review date:** 2026-05-16  
**Version reviewed:** 0.1.0-alpha (`net10.0`)  
**Scope:** Full review per library design review plan (API stability, architecture, consumer ergonomics, correctness, performance spot-check)

**Test run:** `dotnet run --project tests/Novolis.Physics.Unit -c Release` — **46/46 passed** (967 ms)

---

## Executive summary

Novolis.Physics delivers a coherent **force-first** core (`IForceModel` → `SimulationPipeline` → `IIntegrator`) with well-factored packages and strong scenario tests. Before stable release, address **orphan public API** and **integration-path documentation**. **KspLite was removed** (2026-05-16): it was an example-only DI shim, not a product package; registration patterns live in [examples/dependency-injection.md](examples/dependency-injection.md). No physics blockers were found in the test suite.

---

## Phase 1 — Public API inventory

### Package dependency graph

```
Numerics
  └── Abstractions
        ├── Motion, Gravity, Aerodynamics, Collision.Simple
        ├── Ballistics (+ Collision.Simple)
        └── Orbits (Numerics only)
              Novolis.Physics (meta) → all product packages
```

### Public type classification (49 source files, ~52 public types)

| Package | Type | Role |
|---------|------|------|
| **Numerics** | `Vector3d`, `Quaterniond`, `Ray3d`, `Sphere3d`, `Capsule3d`, `AxisAlignedBox3d` | Value / geometry |
| **Abstractions** | `IForceModel<>`, `IIntegrator<>`, `IStaticWorld` | Contract |
| **Abstractions** | `IContactResolver<>` | Contract (**orphan**) |
| **Abstractions** | `RigidBodyState`, `ForceSample`, `HitInfo` | State / sample |
| **Motion** | `SimulationPipeline<>`, `SemiImplicitEulerRigidBodyIntegrator`, `FixedStepAccumulator` | Algorithm / orchestration |
| **Motion** | `UniformAccelerationEnergy` | Helper |
| **Gravity** | `PointMassField`, `PatchedConicPairField` | Environment |
| **Gravity** | `PointMassGravityModel`, `PatchedConicGravityModel` | Algorithm (`IForceModel`) |
| **Aerodynamics** | `IAtmosphereModel`, `ExponentialAtmosphereModel` | Contract / algorithm |
| **Aerodynamics** | `SimpleAeroEnvironment`, `SimpleLiftDragModel` | Environment / algorithm |
| **Collision.Simple** | `StaticTriangleMesh`, `BvhStaticWorld`, `EmptyStaticWorld` | Environment / algorithm |
| **Collision.Simple** | `BvhStaticSphereIntegrator`, `SphereContactKinematics` | Standalone integrator / helper |
| **Ballistics** | `ProjectileState`, `ProjectileProfile`, `*Environment` | State / environment |
| **Ballistics** | `ProjectileQuadraticDragModel`, `ProjectileSemiImplicitIntegrator` | Pipeline building blocks |
| **Ballistics** | `ProjectileBallisticSimulation` | Facade (monolithic step) |
| **Ballistics** | `ProjectileMath`, `BallisticsQueries`, `GroundImpact` | Helper / query |
| **Orbits** | `OrbitState`, `LeapfrogCentralBodySoA`, `CentralOrbitSimulator` | Parallel integration stack |
| **Orbits** | `OrbitalMath`, `OrbitalTestConstants`, `OrbitalTestState`, `KernelMode` | Helper / test fixtures (**in product package**) |
| **Novolis.Physics** | (none) | Meta-package only |

*Former `Novolis.Physics.KspLite` package removed — see [examples/dependency-injection.md](examples/dependency-injection.md).*

**Internal (non-public):** `TriangleRay` in Collision.Simple.

### Orphan and unused-contract flags

| Item | Status |
|------|--------|
| `IContactResolver<TBody>` | **Zero implementations** in repo; contact handled via `SphereContactKinematics` + `BvhStaticSphereIntegrator` |
| `IForceModel.Evaluate(..., timeSeconds)` | Parameter **unused** in all shipped force models |
| `OrbitalTestConstants` / `OrbitalTestState` | Public in **Orbits** product package; naming implies test-only |

---

## Phase 2 — Architecture coherence

### Force-first alignment by package

| Package | Aligns with pipeline? | Notes |
|---------|----------------------|-------|
| Numerics | N/A (foundation) | Right-handed 3D; no axis convention doc at type level |
| Abstractions | Yes | Clean split: state vs `ForceSample` vs `HitInfo` |
| Motion | Yes | Canonical orchestration; semi-implicit Euler for rigid bodies |
| Gravity | Yes | `PointMassField` uses `ReadOnlyMemory` + `Span` safely |
| Aerodynamics | Yes | `SimpleLiftDragModel` is proper `IForceModel`; atmosphere via `IAtmosphereModel` |
| Collision.Simple | Partial | Query-only `IStaticWorld`; integration via separate `BvhStaticSphereIntegrator` |
| Ballistics | Partial | Pipeline path exists; `ProjectileBallisticSimulation` duplicates gravity+drag inline |
| Orbits | **No** | Leapfrog SoA; does not use `IForceModel` / `SimulationPipeline` |
| ~~KspLite~~ | Removed | DI example moved to docs |

### Integration paths (validated)

Four distinct simulation styles coexist:

1. **`SimulationPipeline` + `IForceModel` + `IIntegrator`** — rigid body and projectile (with adapters).
2. **`ProjectileBallisticSimulation.Step`** — convenience facade; **parity-tested** against pipeline (`ProjectileDragPipelineParityTests`).
3. **`BvhStaticSphereIntegrator`** — sphere vs static mesh with contact resolution; used in room/billiards tests; **not** wired through `IContactResolver`.
4. **`CentralOrbitSimulator` / `LeapfrogCentralBodySoA`** — central-body leapfrog; scalar/vectorized kernels.

### Architecture decision records (recommended)

#### ADR-1: Orbits remains a separate stack (ACCEPT)

**Decision:** Keep `Novolis.Physics.Orbits` as a leapfrog SoA integrator, not folded into `IForceModel` for v1.

**Rationale:** Different numerical method (symplectic leapfrog vs semi-implicit Euler), SoA layout for N bodies, and existing energy/angular-momentum tests. `PointMassGravityModel` already covers pipeline-style gravity for games.

**Follow-up:** Document when to use Orbits vs Gravity+Motion; consider moving `OrbitalTestConstants`/`OrbitalTestState` to test assembly or renaming to `OrbitalReferenceOrbit` before stable.

#### ADR-2: Collision stays query-only in v1 (ACCEPT)

**Decision:** `IStaticWorld` provides ray/sweep queries only; no full rigid-body contact solver in the pipeline.

**Rationale:** Matches scope; `BvhStaticSphereIntegrator` covers sphere-in-room scenarios. XML docs already flag approximate sweeps.

**Follow-up:** Add consumer doc section on sweep limitations and when `BvhStaticSphereIntegrator` vs manual pipeline stepping applies.

#### ADR-3: Ballistics — pipeline is canonical; facade is supported (ACCEPT)

**Decision:** Recommend `SimulationPipeline` + `ProjectileQuadraticDragModel` + `ProjectileSemiImplicitIntegrator` for extensibility; keep `ProjectileBallisticSimulation` as ergonomic entry point.

**Rationale:** `ProjectileDragPipelineParityTests` proves equivalence for gravity + quadratic drag. Facade reduces boilerplate for cannon-style problems.

**Follow-up:** README example showing both patterns and when to add custom `IForceModel` instances.

#### ADR-4: `IContactResolver` — remove or defer before stable (ACCEPT removal)

**Decision:** Remove from public API in next breaking window, or move to `Novolis.Physics.Abstractions.Experimental` if contact pipeline is planned.

**Rationale:** Misleading contract; actual API is `SphereContactKinematics.ReflectWithRestitution` inside `BvhStaticSphereIntegrator`.

---

## Phase 3 — Consumer ergonomics

### Persona walkthroughs

#### Minimal (point mass + Euler)

**Path:** `MinimalSimulationExampleTests` — direct construction of `FixedStepAccumulator`, `SimulationPipeline`, and `PointMassGravityModel`.

**Friction:** Low; see README quick start and [INTEGRATION.md](INTEGRATION.md).

#### Game room (gravity + collision + bounce)

**Path:** `BasketballEarthRoomCollisionTests`, `BouncingBallCollisionTests` — build `BvhStaticWorld` from mesh, loop `BvhStaticSphereIntegrator.AdvanceOneStep` or `AdvanceWithUniformAccelerationAndLinearDrag` with `UniformAccelerationEnergy` for traces.

**Friction:** Collision is **outside** `SimulationPipeline`; gravity/drag applied inside integrator helper, not as `IForceModel`. Two mental models required.

#### Ballistics (drag + ground impact)

**Path:** Most tests use `ProjectileBallisticSimulation`; parity tests prove pipeline equivalence. `BallisticsQueries` wraps `IStaticWorld` sweeps.

**Friction:** Low for ballistics-only users; unclear which path to choose without reading `ProjectileDragPipelineParityTests`.

### Ergonomics rubric (1 = poor, 5 = excellent)

| Criterion | Score | Evidence |
|-----------|-------|----------|
| Discoverability | **4** | README quick start + INTEGRATION.md (post-review) |
| Composition | **4** | `SimulationPipeline` constructor is simple; README + INTEGRATION.md cover wiring |
| Type clarity | **4** | `TBody`/`TEnvironment` pairs are consistent; projectile vs rigid body types are distinct |
| Convention docs | **3** | +Y up, −Y gravity documented on `ProjectileBallisticSimulation`; not centralized |
| Error surfaces | **3** | `FixedStepAccumulator` and mesh ctor validate inputs; zero mass/inertia guarded in integrator |

### Documentation gaps

- [TestSupport README](../tests/Novolis.Physics.TestSupport/README.md) — references **StarConflictsRevolt** paths and project names.
- No worked example of sweep **failure modes** (fast sphere, thin geometry, capsule endpoint sampling).

---

## Phase 4 — Correctness and performance

### Test → invariant mapping

| Test class | Invariant proven |
|------------|------------------|
| `SimulationPipelineTests` | Forces sum before integration |
| `ProjectileDragPipelineParityTests` | Facade ≡ pipeline (1 and 200 steps) |
| `PatchedConicGravityTests` | SOI switching uses correct μ and source |
| `EllipticalOrbitTwoBodyTests` | Closed/half-orbit geometry; energy drift ≤ 1e-5; Lz drift ≤ 1e-6; scalar ≡ vectorized |
| `CollisionSweepScenarioTests` | Sweep hits ground before full displacement |
| `FixedStepAccumulatorTests` | Deterministic step count; fractional carry across frames |
| `AnalyticalProjectileTests` / `BallisticPropertyTests` | Vacuum/drag trajectories vs analytic expectations |
| `BvhStaticWorldTests` | Raycast/sweep basics on simple meshes |
| `AerodynamicsModelTests` | Lift/drag direction and magnitude sanity |
| `MinimalSimulationExampleTests` | Fixed step + pipeline + gravity end-to-end |
| `BasketballEarthRoomCollisionTests` | Long-run stability; energy traces (scenario, not strict conservation) |

### Performance spot-check (hot `Step` paths)

| Location | Heap allocation in steady-state step? | Notes |
|----------|--------------------------------------|-------|
| `SimulationPipeline.Step` | **No** | foreach over force array |
| `SemiImplicitEulerRigidBodyIntegrator.Step` | **No** | stack `Vector3d` / `Quaterniond` |
| `ProjectileSemiImplicitIntegrator.Step` | **No** | struct return |
| `ProjectileBallisticSimulation.Step` | **No** | reuses instance integrator |
| `BvhStaticSphereIntegrator.AdvanceOneStep` | **No** per iteration | `Sphere3d` on stack; loop bounded by `maxReflectionsPerStep` |
| `LeapfrogCentralBodySoA.Step` (scalar) | **No** | SoA arrays allocated at ctor |
| `LeapfrogCentralBodySoA.Step` (vectorized) | **No** | `stackalloc` lanes + `Vector<double>` (struct, no heap) |
| `BvhStaticWorld` ctor | **Yes (once)** | `List<BvhNode>` → `ToArray()` at build |
| `CentralOrbitSimulator.SimulateFor` | **Yes per call** | new `LeapfrogCentralBodySoA` each invocation |

**Conclusion:** Sim-loop paths are allocation-conscious. Avoid calling `CentralOrbitSimulator.SimulateFor` inside per-frame hot paths without pooling/reuse.

---

## Findings register

| ID | Severity | Area | Finding | Recommendation | Effort |
|----|----------|------|---------|----------------|--------|
| DR-001 | **Major** | Abstractions | `IContactResolver<TBody>` is public with **no implementations**; contact logic lives in `SphereContactKinematics` / `BvhStaticSphereIntegrator` | Remove before stable, or move to experimental namespace; document actual contact API | S |
| DR-002 | ~~Major~~ **Resolved** | Architecture | Four integration styles | [INTEGRATION.md](INTEGRATION.md) added | — |
| DR-003 | ~~Major~~ **Resolved** | KspLite | Removed example package | README + INTEGRATION.md + optional DI doc | — |
| DR-004 | ~~Major~~ **Resolved** | Packaging | KspLite/meta mismatch | KspLite removed; meta = product packages only | — |
| DR-005 | **Major** | Orbits | `OrbitalTestConstants` / `OrbitalTestState` are **public product API** with test-oriented names | Rename to product names (e.g. `ReferenceEllipticalOrbit`) or move to test project before stable | M |
| DR-006 | **Major** | Collision | `IStaticWorld` sweeps are **approximate** (documented in XML) but no consumer example of failure cases | Add doc + optional test demonstrating conservative vs missed hit | M |
| DR-007 | Minor | Abstractions | `timeSeconds` on `IForceModel.Evaluate` is **unused** by all implementations | Use for time-varying forces, or mark `[Obsolete]` / document as reserved | S |
| DR-008 | ~~Minor~~ **Resolved** | Docs | Root README had no code example | README quick start added | — |
| DR-009 | Minor | Docs | TestSupport README references **StarConflictsRevolt** paths | Update to Novolis.Physics paths and project names | S |
| DR-010 | Minor | Ballistics | Dual path (facade vs pipeline) is **parity-tested but undocumented** for consumers | Cross-link in README; state facade = pipeline for default drag | S |
| DR-011 | Minor | Motion | `SimulationPipeline` does not advance `timeSeconds` automatically | Document caller responsibility to pass `timeSeconds + dt` on each step | S |
| DR-012 | ~~Minor~~ **Resolved** | KspLite | N/A | Package removed | — |
| DR-013 | Nit | Orbits | `CentralOrbitSimulator.SimulateFor` allocates new SoA **per call** | Document; offer overload accepting pre-allocated `LeapfrogCentralBodySoA` for hot paths | M |
| DR-014 | Nit | Collision | `BallisticsQueries` is thin wrapper over `IStaticWorld` | Keep as discoverability alias; optional merge into docs only | S |
| DR-015 | Nit | Numerics | Axis convention (+Y up) not stated on core types | One paragraph in README or Numerics package description | S |

**Blockers:** None identified in this review.

---

## Prioritized post-review backlog

### P0 — Before stable (0.2.0)

1. **DR-001** — Resolve `IContactResolver` (remove or implement).
2. **DR-005** — Orbit reference API naming or relocation.

### P1 — Early stable

5. **DR-006** — Sweep limitations with worked example.
6. **DR-010** — Ballistics path documentation (partially covered by INTEGRATION.md).
7. **Semver policy** — Document what 0.1.0-alpha guarantees (breaking changes allowed, orphan API may be removed).

### P2 — Nice to have

8. **DR-007** — Time-varying forces or API cleanup for `timeSeconds`.
9. **DR-013** — Orbit simulator reuse API.
10. **DR-009** — TestSupport README cleanup.

---

## Appendix A — Verified pre-seeded hypotheses

| Hypothesis | Result |
|------------|--------|
| `IContactResolver` has zero implementations | **Confirmed** |
| Multiple integration styles coexist | **Confirmed** (four paths) |
| KspLite was example-only | **Removed**; DI recipe in docs/examples |
| Sweeps are approximate | **Confirmed** (XML on `IStaticWorld`) |

## Appendix B — Suggested README example (for DR-008)

```csharp
using Novolis.Physics.Abstractions;
using Novolis.Physics.Gravity;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;

var integrator = new SemiImplicitEulerRigidBodyIntegrator();
var gravity = new PointMassGravityModel();
var pipeline = new SimulationPipeline<RigidBodyState, PointMassField>(integrator, gravity);

var field = new PointMassField([(Vector3d.Zero, 3.986e14)]);
var body = new RigidBodyState(
    new Vector3d(6_771_000, 0, 0),
    new Vector3d(0, 7_500, 0),
    Quaterniond.Identity, Vector3d.Zero, mass: 1.0,
    inertiaDiagonalBody: new Vector3d(1, 1, 1));

body = pipeline.Step(body, field, dtSeconds: 1.0);
```

---

*End of design review findings.*
