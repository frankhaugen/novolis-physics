using Novolis.Physics.KspLite;
using Novolis.Physics.Motion;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;
using IRigidIntegrator = global::Novolis.Physics.Abstractions.IIntegrator<global::Novolis.Physics.Abstractions.RigidBodyState>;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class KspLiteDiTests
{
    [Test]
    public async Task KspLite_Extensions_RegisterCoreServices()
    {
        var services = new ServiceCollection();
        _ = services
            .AddPhysics()
            .AddMotion()
            .AddGravity()
            .AddBallistics()
            .AddAerodynamics()
            .AddSimpleCollision()
            .UseFixedStep(1.0 / 60.0);

        await using var sp = services.BuildServiceProvider();
        var integrator = sp.GetService<IRigidIntegrator>();
        var acc = sp.GetService<FixedStepAccumulator>();

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("KspLite DI (compact)");
        o.Line("chain", "AddPhysics..AddMotion..AddGravity..Ballistics..Aerodynamics..AddSimpleCollision..UseFixedStep(1/60)");

        o.Results("KspLite DI - outcome");
        o.Line("IIntegrator<RigidBodyState> resolved", (integrator is not null).ToString());
        o.Line("FixedStepAccumulator resolved", (acc is not null).ToString());
        o.Line("FixedStepAccumulator.dt (s)", acc!.FixedDeltaSeconds);

        await Assert.That(integrator).IsNotNull();
        await Assert.That(acc).IsNotNull();
        await Assert.That(acc!.FixedDeltaSeconds).IsEqualTo(1.0 / 60.0);
    }
}
