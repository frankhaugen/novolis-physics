using Novolis.Physics.Abstractions;
using Novolis.Physics.Gravity;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>
/// End-to-end example: fixed timestep accumulator + <see cref="SimulationPipeline"/> + point-mass gravity.
/// </summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class MinimalSimulationExampleTests
{
    [Test]
    public async Task FixedStepAccumulator_WithSimulationPipeline_DrainsExpectedSteps()
    {
        var acc = new FixedStepAccumulator(0.1);
        var integrator = new SemiImplicitEulerRigidBodyIntegrator();
        var gravity = new PointMassGravityModel();
        var pipeline = new SimulationPipeline<RigidBodyState, PointMassField>(integrator, gravity);

        var field = new PointMassField(new[] { (Vector3d.Zero, 4.0e11) });
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
            "Minimal simulation example outcome",
            new[]
            {
                new StepRow("fixed dt (s)", acc.FixedDeltaSeconds),
                new StepRow("elapsed wall (s)", wallSeconds),
                new StepRow("steps drained", n),
                new StepRow("body |r| (m)", body.Position.Length()),
            },
            new TableOptions { MaxCellWidth = 22, RightAlignNumericColumns = true },
            tableCaption: "FixedStepAccumulator + SimulationPipeline + PointMassGravityModel");

        await Assert.That(n).IsEqualTo(expectedSteps);
        await Assert.That(stepCount).IsEqualTo(expectedSteps);
        await Assert.That(double.IsFinite(body.Position.X)).IsTrue();
    }

    private sealed record StepRow(string Metric, double Value);
}
