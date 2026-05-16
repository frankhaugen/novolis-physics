# Integration guide

Novolis.Physics is **force-first**: `IForceModel` computes forces, `SimulationPipeline` sums them, `IIntegrator` advances state.

## 1. Rigid body (recommended default)

```csharp
var pipeline = new SimulationPipeline<RigidBodyState, PointMassField>(
    new SemiImplicitEulerRigidBodyIntegrator(),
    new PointMassGravityModel());

var field = new PointMassField([(position, gm), ...]);
body = pipeline.Step(body, field, dtSeconds, timeSeconds);
```

Add more forces to the pipeline constructor: `SimpleLiftDragModel`, custom `IForceModel` implementations, etc.

Use `FixedStepAccumulator` to drain variable frame time into fixed physics steps.

## 2. Ballistics

**Facade (simplest):** `ProjectileBallisticSimulation` — uniform −Y gravity and optional quadratic drag.

**Pipeline (extensible):** `ProjectileSemiImplicitIntegrator` + `ProjectileQuadraticDragModel` + gravity `IForceModel` in `SimulationPipeline<ProjectileState, TEnv>`.

Both paths are equivalent for default drag; see `ProjectileDragPipelineParityTests` in the unit project.

Convention: +Y up, range often along +X, set `Z = 0` for planar cannon problems.

## 3. Collision (query + sphere integrator)

`IStaticWorld` (`BvhStaticWorld`, `EmptyStaticWorld`) provides raycast and **approximate** sphere/capsule sweeps. Not a full rigid-body engine.

For a bouncing sphere in a static mesh, use `BvhStaticSphereIntegrator.AdvanceOneStep` (or `AdvanceWithUniformAccelerationAndLinearDrag`) alongside your gravity model. Contact resolution uses `SphereContactKinematics`, not `IContactResolver` (reserved/unused in current releases).

## 4. Orbits (separate stack)

`CentralOrbitSimulator` / `LeapfrogCentralBodySoA` use symplectic leapfrog for central-body problems. Does **not** plug into `SimulationPipeline`. Use `Novolis.Physics.Gravity` point-mass models for game-style gravity instead.

## Decision tree

| Goal | Use |
|------|-----|
| Rigid body + arbitrary forces | `SimulationPipeline` + `SemiImplicitEulerRigidBodyIntegrator` |
| Cannon / projectile with drag | `ProjectileBallisticSimulation` or ballistics pipeline |
| Sphere in a static room | `BvhStaticSphereIntegrator` + mesh world |
| Long-term two-body orbit test | `CentralOrbitSimulator` |
