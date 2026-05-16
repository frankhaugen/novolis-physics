# Optional: Microsoft.Extensions.DependencyInjection

Novolis.Physics does not ship a DI package. If your app already uses `Microsoft.Extensions.DependencyInjection`, you can register types yourself:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Novolis.Physics.Abstractions;
using Novolis.Physics.Aerodynamics;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Gravity;
using Novolis.Physics.Motion;

static void AddNovolisPhysics(IServiceCollection services, double fixedDeltaSeconds = 1.0 / 60.0)
{
    services.TryAddSingleton<IIntegrator<RigidBodyState>, SemiImplicitEulerRigidBodyIntegrator>();
    services.TryAddSingleton<PointMassGravityModel>();
    services.TryAddSingleton<PatchedConicGravityModel>();
    services.TryAddSingleton<ProjectileSemiImplicitIntegrator>();
    services.TryAddSingleton<SimpleLiftDragModel>();
    services.TryAddSingleton<IAtmosphereModel>(_ => new ExponentialAtmosphereModel(1.225, 8500));
    services.TryAddSingleton<IStaticWorld, EmptyStaticWorld>();
    services.TryAddSingleton(_ => new FixedStepAccumulator(fixedDeltaSeconds));
}
```

You still construct `SimulationPipeline<TBody, TEnvironment>` and environment values (`PointMassField`, meshes, etc.) in application code — DI only holds shared algorithms.
