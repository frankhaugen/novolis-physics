using Novolis.Physics.Abstractions;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class QuaternionIntegrationSmokeTests
{
    [Test]
    public async Task ConstantTorque_QuaternionStaysNormalized_AndOmegaGrowsAlongAxis()
    {
        var integrator = new SemiImplicitEulerRigidBodyIntegrator();
        var torque = new Vector3d(0, 0, 0.6);
        var pipeline = new SimulationPipeline<RigidBodyState, int>(integrator, new ConstantTorqueWorldForce(torque));
        var body = new RigidBodyState(
            Vector3d.Zero,
            Vector3d.Zero,
            Quaterniond.Identity,
            Vector3d.Zero,
            mass: 1.0,
            inertiaDiagonalBody: new Vector3d(1, 2, 4));
        const double dt = 1.0 / 240.0;
        const int steps = 400;
        var samples = new List<SpinSampleRow>(capacity: 9);
        for (var i = 0; i < steps; i++)
        {
            body = pipeline.Step(body, 0, dt, i * dt);
            if (i % 100 == 0)
                samples.Add(new SpinSampleRow(i, body.AngularVelocity.Z, QuaternionNorm(body.Orientation)));
        }

        var o = NovolisPhysicsTestTrace.Out;
        PhysicsDashboard.ResultsAndTable(
            o,
            "Constant torque spin - samples",
            TestOutputSequences.EveryNth(samples, 1),
            new TableOptions { MaxCellWidth = 22, RightAlignNumericColumns = true },
            tableCaption: "world torque (0,0,0.6) Nm, Iz=4 kg m2; expect |q|~1, omega_z ~ alpha*t");

        await Assert.That(Math.Abs(QuaternionNorm(body.Orientation) - 1.0)).IsLessThanOrEqualTo(1e-8);
        var alphaZ = torque.Z / body.InertiaDiagonalBody.Z;
        var expectedOmega = alphaZ * steps * dt;
        await Assert.That(Math.Abs(body.AngularVelocity.Z - expectedOmega)).IsLessThanOrEqualTo(0.02 * Math.Max(1, Math.Abs(expectedOmega)));
    }

    private sealed class ConstantTorqueWorldForce(Vector3d torqueWorld) : IForceModel<RigidBodyState, int>
    {
        public ForceSample Evaluate(RigidBodyState body, int environment, double timeSeconds) =>
            new(Vector3d.Zero, torqueWorld);
    }

    private sealed record SpinSampleRow(int Step, double OmegaZ, double OrientationNorm);

    private static double QuaternionNorm(Quaterniond q) =>
        Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
}
