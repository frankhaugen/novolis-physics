# Ballistics example

Convention: **+Y up**, range along **+X**, set **Z = 0** for a planar cannon.

## Facade (simplest)

`ProjectileBallisticSimulation` applies uniform **−Y** gravity and optional quadratic drag:

```csharp
using Novolis.Physics.Ballistics;
using Novolis.Physics.Numerics;

var sim = new ProjectileBallisticSimulation(
    new ProjectileProfile(massKg: 10, referenceAreaM2: 0.01, dragCoefficient: 0.47));

var env = new ProjectileBallisticEnvironment(
    GravityMetersPerSecondSquared: 9.81,
    AirDensityKgPerM3: 1.225);

var state = new ProjectileState(
    new Vector3d(0, 0, 0),
    new Vector3d(100, 45, 0),
    massKg: 10);

for (var i = 0; i < 600; i++)
    state = sim.Step(state, dtSeconds: 1.0 / 60.0, env);
```

## Pipeline (extensible)

Compose gravity and drag as separate `IForceModel` instances when you need custom forces or shared integrators:

```csharp
using Novolis.Physics.Abstractions;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;

var profile = new ProjectileProfile(10, 0.01, 0.47);
var ballisticEnv = new ProjectileBallisticEnvironment(9.81, 1.225);
var env = new Env(ballisticEnv, new ProjectileDragEnvironment(ballisticEnv.AirDensityKgPerM3));

var pipeline = new SimulationPipeline<ProjectileState, Env>(
    new ProjectileSemiImplicitIntegrator(),
    new UniformGravity(),
    new ProjectileQuadraticDragModel(profile));

var state = new ProjectileState(new Vector3d(0, 0, 0), new Vector3d(100, 45, 0), profile.MassKg);
for (var i = 0; i < 600; i++)
    state = pipeline.Step(state, env, 1.0 / 60.0, state.TimeSeconds);

readonly record struct Env(ProjectileBallisticEnvironment Ballistic, ProjectileDragEnvironment Drag);

sealed class UniformGravity : IForceModel<ProjectileState, Env>
{
    public ForceSample Evaluate(ProjectileState body, Env env, double timeSeconds) =>
        new(new Vector3d(0, -body.MassKg * env.Ballistic.GravityMetersPerSecondSquared, 0), Vector3d.Zero);
}
```

For default gravity + drag only, prefer the facade. Parity with the pipeline is covered by `ProjectileDragPipelineParityTests` in the unit project.

## Sweeps against terrain

Use `BallisticsQueries.SweepProjectileSphere` (wrapper over `IStaticWorld.SweepSphere`) for line-of-sight and impact prediction. See [INTEGRATION.md](../INTEGRATION.md) §3 for sweep limitations.
