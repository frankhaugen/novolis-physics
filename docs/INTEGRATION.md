# Integration guide

Novolis.Physics is **force-first**: `IForceModel` computes forces, `SimulationPipeline` sums them, `IIntegrator` advances state.

Pass **simulation time** explicitly on each `Step`: the pipeline does not advance `timeSeconds` for you — use `timeSeconds + dtSeconds` after each fixed step (see README quick start).

## 1. Rigid body (recommended default)

```csharp
var pipeline = new SimulationPipeline<RigidBodyState, PointMassField>(
    new SemiImplicitEulerRigidBodyIntegrator(),
    new PointMassGravityModel());

var field = new PointMassField([(position, gm), ...]);
double time = 0;
body = pipeline.Step(body, field, dtSeconds, time);
time += dtSeconds;
```

Add more forces to the pipeline constructor: `SimpleLiftDragModel`, custom `IForceModel` implementations, etc.

Use `FixedStepAccumulator` to drain variable frame time into fixed physics steps.

## 2. Ballistics

| Goal | API |
|------|-----|
| Cannon / quick prototype | `ProjectileBallisticSimulation` |
| Custom forces / composition | `SimulationPipeline<ProjectileState, TEnv>` + `ProjectileSemiImplicitIntegrator` + `ProjectileQuadraticDragModel` (+ gravity `IForceModel`) |

**Facade (simplest):** `ProjectileBallisticSimulation` — uniform −Y gravity and optional quadratic drag.

**Pipeline (extensible):** compose `IForceModel` instances in `SimulationPipeline<ProjectileState, TEnv>`.

For default uniform gravity and quadratic drag, the facade and pipeline are **equivalent** (see `ProjectileDragPipelineParityTests` in the unit project).

`BallisticsQueries.SweepProjectileSphere` is a discoverability wrapper over `IStaticWorld.SweepSphere` for projectile-sized spheres.

Convention: +Y up, range often along +X, set `Z = 0` for planar cannon problems.

## 3. Collision (query + sphere integrator)

`IStaticWorld` (`BvhStaticWorld`, `EmptyStaticWorld`) provides raycast and **approximate** sphere/capsule sweeps. Not a full rigid-body engine.

For a bouncing sphere in a static mesh, use `BvhStaticSphereIntegrator.AdvanceOneStep` (or `AdvanceWithUniformAccelerationAndLinearDrag`) alongside your gravity model. Contact resolution is handled inside the integrator via `SphereContactKinematics.ReflectWithRestitution` (see `Novolis.Physics.Collision.Simple`).

### Sweep limitations

`BvhStaticWorld.SweepSphere` performs a **radius-inflated raycast** along the displacement direction (not continuous CCD).

| Behavior | When |
|----------|------|
| Reliable hit | Displacement per step is small vs mesh features; shallow penetration near a surface |
| May return **no hit** | Displacement overshoots the first contact (`adjusted > displacement length`); fast motion tunneling past thin geometry; `SweepCapsule` only samples **endpoint spheres** |

**Mitigation:** smaller physics steps, larger sphere radius margin, or custom CCD for critical paths.

**Examples in the unit project:**

- Partial-travel hit: `CollisionSweepScenarioTests.SweepProjectileSphere_HitsGroundTriangle`
- Large-step miss with sub-step hit: `SweepLimitationScenarioTests.SweepSphere_LargeStepOvershoot_MissesWhileSubStepsHit`

## 4. Orbits (separate stack)

`CentralOrbitSimulator` / `LeapfrogCentralBodySoA` use symplectic leapfrog for central-body problems. Does **not** plug into `SimulationPipeline`. Use `Novolis.Physics.Gravity` point-mass models for game-style gravity instead.

For repeated propagation, reuse one `LeapfrogCentralBodySoA` via `CentralOrbitSimulator.SimulateFor(initial, integrator, bodyIndex, ...)` instead of the convenience overload that allocates each call.

## Decision tree

| Goal | Use |
|------|-----|
| Rigid body + arbitrary forces | `SimulationPipeline` + `SemiImplicitEulerRigidBodyIntegrator` |
| Cannon / projectile with drag | `ProjectileBallisticSimulation` or ballistics pipeline |
| Sphere in a static room | `BvhStaticSphereIntegrator` + mesh world |
| Long-term two-body orbit test | `CentralOrbitSimulator` |
