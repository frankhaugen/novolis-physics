using Novolis.Physics.Abstractions;
using Novolis.Physics.Aerodynamics;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Gravity;
using Novolis.Physics.Motion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Novolis.Physics.KspLite;

public static class KspLitePhysicsServiceCollectionExtensions
{
    /// <summary>Entry point for the fluent registration chain (no-op; use chained methods).</summary>
    public static IServiceCollection AddPhysics(this IServiceCollection services) => services;

    public static IServiceCollection AddMotion(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrator<RigidBodyState>, SemiImplicitEulerRigidBodyIntegrator>();
        return services;
    }

    public static IServiceCollection AddGravity(this IServiceCollection services)
    {
        services.TryAddSingleton<PointMassGravityModel>();
        services.TryAddSingleton<PatchedConicGravityModel>();
        return services;
    }

    public static IServiceCollection AddBallistics(this IServiceCollection services)
    {
        services.TryAddSingleton<ProjectileSemiImplicitIntegrator>();
        return services;
    }

    public static IServiceCollection AddAerodynamics(this IServiceCollection services)
    {
        services.TryAddSingleton<SimpleLiftDragModel>();
        services.TryAddSingleton<IAtmosphereModel>(_ => new ExponentialAtmosphereModel(1.225, 8500));
        return services;
    }

    public static IServiceCollection AddSimpleCollision(this IServiceCollection services)
    {
        services.TryAddSingleton<IStaticWorld, EmptyStaticWorld>();
        return services;
    }

    public static IServiceCollection UseFixedStep(this IServiceCollection services, double fixedDeltaSeconds)
    {
        services.TryAddSingleton(_ => new FixedStepAccumulator(fixedDeltaSeconds));
        return services;
    }
}
