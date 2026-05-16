using Novolis.Physics.Abstractions;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using TUnit.Core;

namespace Novolis.Physics.Unit;

file static class TestBodyEnv
{
    public readonly record struct Body(Vector3d Position, double Mass);

    public readonly record struct Env(double Dummy);

    public sealed class ConstantForceModel(Vector3d force) : IForceModel<Body, Env>
    {
        public ForceSample Evaluate(Body body, Env environment, double timeSeconds) => new(force, Vector3d.Zero);
    }

    public sealed class PointMassIntegrator : IIntegrator<Body>
    {
        public Body Step(Body body, in ForceSample totalForcesAndTorques, double dtSeconds)
        {
            var invM = 1.0 / body.Mass;
            var v = totalForcesAndTorques.Force * invM * dtSeconds;
            return body with { Position = body.Position + v };
        }
    }
}

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class SimulationPipelineTests
{
    [Test]
    public async Task SimulationPipeline_SumsForcesAndIntegrates()
    {
        var body = new TestBodyEnv.Body(new Vector3d(0, 0, 0), Mass: 2);
        var env = new TestBodyEnv.Env(0);
        var f1 = new TestBodyEnv.ConstantForceModel(new Vector3d(2, 0, 0));
        var f2 = new TestBodyEnv.ConstantForceModel(new Vector3d(0, 4, 0));
        var pipe = new SimulationPipeline<TestBodyEnv.Body, TestBodyEnv.Env>(new TestBodyEnv.PointMassIntegrator(), f1, f2);
        var next = pipe.Step(body, env, dtSeconds: 1, timeSeconds: 0);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("SimulationPipeline - what this test does");
        o.Line("start", "Body at origin, mass = 2 kg.");
        o.Line(
            "forces",
            "Two constant IForceModel instances: (2,0,0) N and (0,4,0) N. SimulationPipeline adds them → net (2,4,0) N.");
        o.Line(
            "integrator",
            "PointMassIntegrator: new Position = old + (net force / mass) * dt. With dt = 1 s → +((2,4,0)/2)*1 = +(1,2,0) m.");

        o.Results("SimulationPipeline - outcome");
        o.Line(
            "Position after one Step (m)",
            FormattableString.Invariant($"x={next.Position.X}, y={next.Position.Y}, z={next.Position.Z} (expect x=1, y=2, z=0)"));

        await Assert.That(next.Position.X).IsEqualTo(1);
        await Assert.That(next.Position.Y).IsEqualTo(2);
    }
}
