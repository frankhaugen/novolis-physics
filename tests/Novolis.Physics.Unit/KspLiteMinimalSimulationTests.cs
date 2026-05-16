using Novolis.Physics.Abstractions;
using Novolis.Physics.Gravity;
using Novolis.Physics.KspLite;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class KspLiteMinimalSimulationTests
{
    [Test]
    public async Task KspLiteDi_FixedStepAccumulator_DrainsExpectedStepsWithResolvedIntegrator()
    {
        var services = new ServiceCollection();
        services.AddPhysics()
            .AddMotion()
            .AddGravity()
            .UseFixedStep(0.1)
            .AddBallistics()
            .AddAerodynamics()
            .AddSimpleCollision();

        using var provider = services.BuildServiceProvider();
        var acc = provider.GetRequiredService<FixedStepAccumulator>();
        var integrator = provider.GetRequiredService<IIntegrator<RigidBodyState>>();
        var gravity = provider.GetRequiredService<PointMassGravityModel>();
        _ = provider.GetRequiredService<PatchedConicGravityModel>();

        var field = new PointMassField(new[] { (Vector3d.Zero, 4.0e11) });
        var pipeline = new SimulationPipeline<RigidBodyState, PointMassField>(integrator, gravity);
        var body = new RigidBodyState(
            new Vector3d(20_000.0, 0, 0),
            new Vector3d(0, Math.Sqrt(4.0e11 / 20_000.0), 0),
            Quaterniond.Identity,
            Vector3d.Zero,
            mass: 1.0,
            inertiaDiagonalBody: new Vector3d(1e6, 1e6, 1e6));

        var stepCount = 0;
        const int expectedSteps = 10;
        var wallSeconds = expectedSteps * acc.FixedDeltaSeconds + 1e-9;
        var n = acc.AddTimeAndDrain(wallSeconds, dt =>
        {
            body = pipeline.Step(body, field, dt, stepCount * dt);
            stepCount++;
        });

        var o = NovolisPhysicsTestTrace.Out;
        PhysicsDashboard.ResultsAndTable(
            o,
            "KspLite DI minimal step outcome",
            new[]
            {
                new KspLiteStepRow("fixed dt (s)", acc.FixedDeltaSeconds),
                new KspLiteStepRow("elapsed wall (s)", wallSeconds),
                new KspLiteStepRow("steps drained", n),
                new KspLiteStepRow("body |r| (m)", body.Position.Length()),
            },
            new TableOptions { MaxCellWidth = 22, RightAlignNumericColumns = true },
            tableCaption: "resolved FixedStepAccumulator + IIntegrator + PointMassGravityModel");

        await Assert.That(n).IsEqualTo(expectedSteps);
        await Assert.That(stepCount).IsEqualTo(expectedSteps);
        await Assert.That(double.IsFinite(body.Position.X)).IsTrue();
    }

    private sealed record KspLiteStepRow(string Metric, double Value);
}
