# Novolis.Physics

Force-first physics simulation for .NET — numerics, motion pipeline, gravity, ballistics, collision, and orbits.

## Install

```bash
dotnet add package Novolis.Physics
```

Or reference individual packages (`Novolis.Physics.Motion`, `Novolis.Physics.Orbits`, …).

## Quick start

+Y is up; gravity for ballistics uses **−Y**. Build a pipeline, then step each fixed timestep:

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
    Quaterniond.Identity,
    Vector3d.Zero,
    mass: 1.0,
    inertiaDiagonalBody: new Vector3d(1, 1, 1));

var acc = new FixedStepAccumulator(1.0 / 60.0);
double time = 0;
acc.AddTimeAndDrain(1.0 / 30.0, dt =>
{
    body = pipeline.Step(body, field, dt, time);
    time += dt;
});
```

See [docs/INTEGRATION.md](docs/INTEGRATION.md) for ballistics, collision, and orbits. Optional DI wiring: [docs/examples/dependency-injection.md](docs/examples/dependency-injection.md).

## Packages

| Package | Role |
|---------|------|
| `Novolis.Physics` | Aggregate — all product packages |
| `Novolis.Physics.Numerics` | Vectors, rays, primitives |
| `Novolis.Physics.Abstractions` | Force models, integrators, contacts |
| `Novolis.Physics.Motion` | Rigid-body motion pipeline |
| `Novolis.Physics.Gravity` | Point / patched-conic gravity |
| `Novolis.Physics.Aerodynamics` | Lift / drag models |
| `Novolis.Physics.Collision.Simple` | Static mesh BVH queries |
| `Novolis.Physics.Ballistics` | Projectile drag and sweeps |
| `Novolis.Physics.Orbits` | Two-body orbital helpers |

## Build

```bash
dotnet build Novolis.Physics.slnx
dotnet run --project tests/Novolis.Physics.Unit -c Release
```
