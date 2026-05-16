using Novolis.Physics.Ballistics;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>Quadratic drag vs vacuum using 3D state with planar motion; regression-style inequalities (not closed-form drag).</summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class BallisticRegressionTests
{
    [Test]
    public async Task Projectile_WithQuadraticDrag_HasShorterRangeThanVacuum()
    {
        const double speed = 50.0;
        const double angle = 35.0 * Math.PI / 180.0;
        const double dt = 1.0 / 240.0;
        const double gravity = 9.80665;

        var vx = speed * Math.Cos(angle);
        var vy = speed * Math.Sin(angle);
        var initial = new ProjectileState(
            Vector3d.Zero,
            new Vector3d(vx, vy, 0),
            massKg: 0.145,
            timeSeconds: 0);

        var profile = new ProjectileProfile(
            massKg: 0.145,
            referenceAreaM2: Math.PI * Math.Pow(0.073 / 2.0, 2),
            dragCoefficient: 0.47);

        var environment = new ProjectileBallisticEnvironment(gravity, AirDensityKgPerM3: 1.225);

        var vacuum = SimulateUntilImpact(initial, dragProfile: null, environment, dt);
        var dragged = SimulateUntilImpact(initial, dragProfile: profile, environment, dt);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Baseball-like drag (compact)");
        o.Line(
            "setup",
            FormattableString.Invariant(
                $"v0={speed} m/s, angle=35deg, dt={dt} s, g={gravity}, rho={environment.AirDensityKgPerM3} kg/m3, m={profile.MassKg} kg, Cd={profile.DragCoefficient}, A={profile.ReferenceAreaM2} m2"));

        PhysicsDashboard.ResultsAndTable(
            NovolisPhysicsTestTrace.Out,
            "Baseball-like drag - outcome (vacuum vs quadratic drag)",
            new[]
            {
                new TrajectorySnapshotRow("vacuum", vacuum.Range, vacuum.MaxHeight, vacuum.Impact.TimeSeconds, vacuum.ImpactSpeed, vacuum.Steps),
                new TrajectorySnapshotRow("quadratic drag", dragged.Range, dragged.MaxHeight, dragged.Impact.TimeSeconds, dragged.ImpactSpeed, dragged.Steps),
            },
            new TableOptions { MaxCellWidth = 22, RightAlignNumericColumns = true },
            tableCaption: "Trajectory snapshots (same initial state; drag row uses Cd, A, rho from setup)");
        o.Line(
            "deltas (vac - drag) range, maxH",
            FormattableString.Invariant($"{vacuum.Range - dragged.Range:G9}, {vacuum.MaxHeight - dragged.MaxHeight:G9}"));
        o.Line(
            "expects",
            "drag: shorter range and lower apex than vacuum; impact speed < v0; vacuum range in band 235..245 m");

        await Assert.That(dragged.Range).IsLessThan(vacuum.Range);
        await Assert.That(dragged.MaxHeight).IsLessThan(vacuum.MaxHeight);
        await Assert.That(dragged.ImpactSpeed).IsLessThan(speed);

        // Documented vacuum reference for this geometry (same integrator, no drag): ~239.6 m range.
        await Assert.That(vacuum.Range).IsGreaterThan(235).And.IsLessThan(245);
    }

    private static TrajectoryOutcome SimulateUntilImpact(
        ProjectileState initial,
        ProjectileProfile? dragProfile,
        ProjectileBallisticEnvironment environment,
        double dt)
    {
        var simulation = new ProjectileBallisticSimulation(dragProfile);
        var state = initial;
        var maxHeight = state.Position.Y;
        var previous = state;
        var steps = 0;
        while (state.Position.Y >= 0)
        {
            previous = state;
            state = simulation.Step(state, dt, environment);
            maxHeight = Math.Max(maxHeight, state.Position.Y);
            steps++;
        }

        var impact = ProjectileMath.InterpolateGroundImpact(previous, state);
        return new TrajectoryOutcome(impact, maxHeight, steps);
    }

    private sealed record TrajectorySnapshotRow(
        string Case,
        double RangeM,
        double MaxHeightM,
        double ImpactTimeS,
        double ImpactSpeedMps,
        int Steps);

    private readonly struct TrajectoryOutcome(GroundImpact impact, double maxHeight, int steps)
    {
        public GroundImpact Impact { get; } = impact;
        public double MaxHeight { get; } = maxHeight;
        public int Steps { get; } = steps;
        public double Range => Impact.Position.X;
        public double ImpactSpeed => Impact.ImpactSpeed;
    }
}
