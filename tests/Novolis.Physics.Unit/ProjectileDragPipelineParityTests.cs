using Novolis.Physics.Abstractions;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>Ensures <see cref="ProjectileBallisticSimulation"/> stays aligned with pipeline gravity+quadratic drag.</summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class ProjectileDragPipelineParityTests
{
    [Test]
    public async Task OneStep_PipelineMatchesBallisticSimulation_WithDragAndGravity()
    {
        var profile = new ProjectileProfile(1.0, 0.5, 0.35);
        var env = new ProjectileBallisticEnvironment(9.80665, 1.225);
        var parity = new ParityEnv(env, new ProjectileDragEnvironment(env.AirDensityKgPerM3));
        var state = new ProjectileState(new Vector3d(10, 120, 0), new Vector3d(35, 12, -2), massKg: 1.2, 0);
        const double dt = 1.0 / 120.0;

        var ballistic = new ProjectileBallisticSimulation(profile).Step(state, dt, env);
        var pipeline = BuildPipeline(profile);
        var piped = pipeline.Step(state, parity, dt, 0);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Projectile drag parity - single step");
        o.Table(
            new[]
            {
                new ParityRow("pos.X", ballistic.Position.X, piped.Position.X),
                new ParityRow("pos.Y", ballistic.Position.Y, piped.Position.Y),
                new ParityRow("pos.Z", ballistic.Position.Z, piped.Position.Z),
                new ParityRow("vel.X", ballistic.Velocity.X, piped.Velocity.X),
                new ParityRow("vel.Y", ballistic.Velocity.Y, piped.Velocity.Y),
                new ParityRow("vel.Z", ballistic.Velocity.Z, piped.Velocity.Z),
            },
            new TableOptions { MaxCellWidth = 22, RightAlignNumericColumns = true },
            caption: "BallisticSimulation vs SimulationPipeline+split forces");

        await AssertParityAsync(ballistic, piped);
    }

    [Test]
    public async Task MultiStep_PipelineMatchesBallisticSimulation()
    {
        var profile = new ProjectileProfile(0.9, 0.4, 0.42);
        var env = new ProjectileBallisticEnvironment(9.80665, 1.225);
        var parity = new ParityEnv(env, new ProjectileDragEnvironment(env.AirDensityKgPerM3));
        var ballisticSim = new ProjectileBallisticSimulation(profile);
        var pipeline = BuildPipeline(profile);
        var stateA = new ProjectileState(Vector3d.Zero, new Vector3d(40, 55, 0), massKg: 1.0, 0);
        var stateB = stateA;
        const double dt = 1.0 / 240.0;
        const int steps = 200;
        for (var i = 0; i < steps; i++)
        {
            stateA = ballisticSim.Step(stateA, dt, env);
            stateB = pipeline.Step(stateB, parity, dt, 0);
        }

        var o = NovolisPhysicsTestTrace.Out;
        PhysicsDashboard.ResultsAndTable(
            o,
            "Projectile drag parity - multi-step outcome",
            new[]
            {
                new ParityRow("pos.X", stateA.Position.X, stateB.Position.X),
                new ParityRow("pos.Y", stateA.Position.Y, stateB.Position.Y),
                new ParityRow("pos.Z", stateA.Position.Z, stateB.Position.Z),
                new ParityRow("vel.X", stateA.Velocity.X, stateB.Velocity.X),
                new ParityRow("vel.Y", stateA.Velocity.Y, stateB.Velocity.Y),
                new ParityRow("vel.Z", stateA.Velocity.Z, stateB.Velocity.Z),
            },
            new TableOptions { MaxCellWidth = 24, RightAlignNumericColumns = true },
            tableCaption: $"after {steps} steps, dt={dt}");

        await AssertParityAsync(stateA, stateB);
    }

    private static SimulationPipeline<ProjectileState, ParityEnv> BuildPipeline(ProjectileProfile profile)
    {
        var integrator = new ProjectileSemiImplicitIntegrator();
        var drag = new ProjectileQuadraticDragModel(profile);
        return new SimulationPipeline<ProjectileState, ParityEnv>(
            integrator,
            new UniformProjectileGravity(),
            new ProjectileDragAdapter(drag));
    }

    private static async Task AssertParityAsync(ProjectileState a, ProjectileState b)
    {
        var eps = 1e-9 * (1.0 + Math.Max(a.Position.Length(), b.Position.Length()));
        var ev = 1e-9 * (1.0 + Math.Max(a.Velocity.Length(), b.Velocity.Length()));
        await Assert.That(Math.Abs(a.Position.X - b.Position.X)).IsLessThanOrEqualTo(eps);
        await Assert.That(Math.Abs(a.Position.Y - b.Position.Y)).IsLessThanOrEqualTo(eps);
        await Assert.That(Math.Abs(a.Position.Z - b.Position.Z)).IsLessThanOrEqualTo(eps);
        await Assert.That(Math.Abs(a.Velocity.X - b.Velocity.X)).IsLessThanOrEqualTo(ev);
        await Assert.That(Math.Abs(a.Velocity.Y - b.Velocity.Y)).IsLessThanOrEqualTo(ev);
        await Assert.That(Math.Abs(a.Velocity.Z - b.Velocity.Z)).IsLessThanOrEqualTo(ev);
    }

    private readonly record struct ParityEnv(ProjectileBallisticEnvironment Ballistic, ProjectileDragEnvironment Drag);

    private sealed class UniformProjectileGravity : IForceModel<ProjectileState, ParityEnv>
    {
        public ForceSample Evaluate(ProjectileState body, ParityEnv env, double timeSeconds) =>
            new(new Vector3d(0, -body.MassKg * env.Ballistic.GravityMetersPerSecondSquared, 0), Vector3d.Zero);
    }

    private sealed class ProjectileDragAdapter(ProjectileQuadraticDragModel inner) : IForceModel<ProjectileState, ParityEnv>
    {
        public ForceSample Evaluate(ProjectileState body, ParityEnv env, double timeSeconds) =>
            inner.Evaluate(body, env.Drag, timeSeconds);
    }

    private sealed record ParityRow(string Axis, double Ballistic, double Pipeline);
}
