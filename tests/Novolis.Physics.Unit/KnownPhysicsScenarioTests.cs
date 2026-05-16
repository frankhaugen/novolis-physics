using Novolis.Physics.Abstractions;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Gravity;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

file sealed record ScenarioCompareRow(string Metric, double Expected, double Simulated, double AbsError);

file sealed record OrbitAxisRow(string Axis, double ExpectedM, double SimulatedM, double AbsDeltaM);

file sealed record EnergySummaryRow(string Label, double ValueJPerKg);

/// <summary>
/// Textbook scenarios with closed-form checks: superposition, symmetry, central motion, drag balance.
/// Tolerances allow semi-implicit Euler drift but still fail wrong gravity, drag, or coupling.
/// </summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class KnownPhysicsScenarioTests
{
    private const double GStd = 9.80665;

    /// <summary>Horizontal range from height H with zero initial vertical speed: x = vx * sqrt(2H/g).</summary>
    [Test]
    public async Task Range_FromHeight_OnlyHorizontalVelocity_MatchesSuperposition()
    {
        const double H = 500.0;
        const double vx = 12.0;
        const double dt = 1.0 / 120.0;
        var expectedRange = vx * Math.Sqrt(2.0 * H / GStd);

        var env = new ProjectileBallisticEnvironment(GStd, AirDensityKgPerM3: 0);
        var sim = new ProjectileBallisticSimulation(dragProfile: null);
        var start = new ProjectileState(new Vector3d(0, H, 0), new Vector3d(vx, 0, 0), massKg: 1.0, timeSeconds: 0);
        var impact = RunUntilGroundCrossing(sim, start, env, dt);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Scenario - range from height (superposition)");
        o.Line("setup", FormattableString.Invariant($"H={H} m, vx={vx} m/s, g={GStd}, vacuum, dt={dt}"));
        o.Line("formula", "range = vx * sqrt(2H/g) (time to fall from rest at height H)");

        o.Results("Scenario - range from height - outcome");
        o.Table(
            new[]
            {
                new ScenarioCompareRow(
                    "range X (m)",
                    expectedRange,
                    impact.Position.X,
                    Math.Abs(impact.Position.X - expectedRange)),
            },
            new TableOptions { MaxCellWidth = 24 },
            caption: "expected = vx * sqrt(2H/g)");

        var relTol = 0.008 * Math.Max(1.0, Math.Abs(expectedRange));
        await Assert.That(Math.Abs(impact.Position.X - expectedRange)).IsLessThanOrEqualTo(relTol);
        await Assert.That(Math.Abs(impact.Position.Z)).IsLessThanOrEqualTo(1e-9);
    }

    /// <summary>Ground-to-ground vacuum range: R = 2 vx vy / g (no drag).</summary>
    [Test]
    public async Task Range_GroundToGround_ClassicParabolaFormula()
    {
        const double vx = 50.0;
        const double vy = 30.0;
        const double dt = 1.0 / 120.0;
        var expectedRange = 2.0 * vx * vy / GStd;

        var env = new ProjectileBallisticEnvironment(GStd, 0);
        var sim = new ProjectileBallisticSimulation(null);
        var start = new ProjectileState(Vector3d.Zero, new Vector3d(vx, vy, 0), massKg: 1.0, 0);
        var impact = RunUntilGroundCrossing(sim, start, env, dt);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Scenario - ground-to-ground range (classic R = 2vx vy / g)");
        o.Line("setup", FormattableString.Invariant($"vx={vx}, vy={vy} m/s, vacuum, dt={dt}"));

        o.Results("Scenario - ground-to-ground - outcome");
        o.Table(
            new[]
            {
                new ScenarioCompareRow(
                    "range X (m)",
                    expectedRange,
                    impact.Position.X,
                    Math.Abs(impact.Position.X - expectedRange)),
            },
            new TableOptions { MaxCellWidth = 24 },
            caption: "expected = 2 vx vy / g");

        var relTol = 0.01 * Math.Max(1.0, Math.Abs(expectedRange));
        await Assert.That(Math.Abs(impact.Position.X - expectedRange)).IsLessThanOrEqualTo(relTol);
    }

    /// <summary>Vacuum vertical toss: impact speed magnitude should match launch (energy); Euler only approximates.</summary>
    [Test]
    public async Task VerticalToss_Vacuum_ImpactSpeedMatchesLaunchWithinIntegratorTolerance()
    {
        const double v0 = 25.0;
        const double dt = 1.0 / 120.0;
        var env = new ProjectileBallisticEnvironment(GStd, 0);
        var sim = new ProjectileBallisticSimulation(null);
        var start = new ProjectileState(Vector3d.Zero, new Vector3d(0, v0, 0), massKg: 1.0, 0);
        var impact = RunUntilGroundCrossing(sim, start, env, dt);
        var speedImpact = impact.Velocity.Length();

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Scenario - vertical toss (symmetric speed vacuum)");
        o.Line("setup", FormattableString.Invariant($"v0_y={v0} m/s up from origin, g={GStd}, vacuum, dt={dt}"));
        o.Line("expectation", "Continuous model: |v| at landing equals launch speed (here ~v0 in y).");

        o.Results("Scenario - vertical toss - outcome");
        o.Table(
            new[]
            {
                new ScenarioCompareRow("|v| at impact (m/s)", v0, speedImpact, Math.Abs(speedImpact - v0)),
            },
            new TableOptions { MaxCellWidth = 24 },
            caption: "continuous vacuum: impact speed equals launch speed (Euler drift in AbsError)");
        o.Line("relative error", Math.Abs(speedImpact - v0) / v0);

        await Assert.That(Math.Abs(speedImpact - v0) / v0).IsLessThanOrEqualTo(0.025);
    }

    /// <summary>Circular orbit: v = sqrt(GM/r). After quarter period, body should sit near +Y on the circle.</summary>
    [Test]
    public async Task CentralGravity_OrbitQuarterPeriod_ReachesExpectedQuadrature()
    {
        const double R = 10_000.0;
        const double Gm = 4.0e11;
        var vOrb = Math.Sqrt(Gm / R);
        var quarterPeriod = 0.5 * Math.PI * Math.Sqrt((R * R * R) / Gm);
        const double dt = 0.001;
        var steps = (int)Math.Ceiling(quarterPeriod / dt);

        var sources = new[] { (Vector3d.Zero, Gm) };
        var field = new PointMassField(sources);
        var gravity = new PointMassGravityModel();
        var integrator = new SemiImplicitEulerRigidBodyIntegrator();
        var pipeline = new SimulationPipeline<RigidBodyState, PointMassField>(integrator, gravity);

        var body = new RigidBodyState(
            new Vector3d(R, 0, 0),
            new Vector3d(0, vOrb, 0),
            Quaterniond.Identity,
            Vector3d.Zero,
            mass: 1.0,
            inertiaDiagonalBody: new Vector3d(1e9, 1e9, 1e9));

        var t = 0.0;
        for (var i = 0; i < steps; i++)
        {
            body = pipeline.Step(body, field, dt, t);
            t += dt;
        }

        var expected = new Vector3d(0, R, 0);
        var err = (body.Position - expected).Length();
        var rErr = Math.Abs(body.Position.Length() - R);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Scenario - central gravity quarter orbit");
        o.Line("setup", FormattableString.Invariant($"R={R} m, GM={Gm} m3/s2, v_orb=sqrt(GM/R)={vOrb:G9} m/s"));
        o.Line("integrator", $"SemiImplicitEulerRigidBody + PointMassGravity, dt={dt} s, steps={steps}");
        o.Line("target", "After T/4, position ~ (0, R, 0) in the orbital plane.");

        o.Results("Scenario - quarter orbit - outcome");
        o.Table(
            new[]
            {
                new OrbitAxisRow("x", 0, body.Position.X, Math.Abs(body.Position.X)),
                new OrbitAxisRow("y", R, body.Position.Y, Math.Abs(body.Position.Y - R)),
                new OrbitAxisRow("z", 0, body.Position.Z, Math.Abs(body.Position.Z)),
            },
            new TableOptions { MaxCellWidth = 22 },
            caption: "per-axis vs ideal quadrature (m)");
        o.Line("position error Euclid (m)", err);
        o.Line("radius drift |r-R| (m)", rErr);

        await Assert.That(err).IsLessThanOrEqualTo(0.02 * R);
        await Assert.That(rErr).IsLessThanOrEqualTo(0.03 * R);
        await Assert.That(Math.Abs(body.Position.Z)).IsLessThanOrEqualTo(1.0);
    }

    /// <summary>Quadratic drag vs weight: terminal speed |v_y| -> sqrt(2 m g / (rho Cd A)).</summary>
    [Test]
    public async Task VerticalFall_WithDrag_ApproachesTerminalSpeed()
    {
        const double m = 1.0;
        const double rho = 1.225;
        const double cd = 0.45;
        const double area = 0.35;
        var vTerm = Math.Sqrt(2.0 * m * GStd / (rho * cd * area));

        var profile = new ProjectileProfile(m, area, cd);
        var env = new ProjectileBallisticEnvironment(GStd, rho);
        var sim = new ProjectileBallisticSimulation(profile);
        const double dt = 1.0 / 120.0;
        var state = new ProjectileState(new Vector3d(0, 4000, 0), Vector3d.Zero, m, 0);

        const int warmSteps = 120 * 25;
        for (var i = 0; i < warmSteps; i++)
            state = sim.Step(state, dt, env);

        var vy = state.Velocity.Y;
        var measured = Math.Abs(vy);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Scenario - terminal velocity (drag balances weight)");
        o.Line("setup", FormattableString.Invariant($"drop from 4000 m, m={m}, rho={rho}, Cd={cd}, A={area}, dt={dt}"));
        o.Line("formula", FormattableString.Invariant($"v_term = sqrt(2 m g / (rho Cd A)) = {vTerm:G9} m/s"));

        o.Results("Scenario - terminal velocity - outcome");
        o.Table(
            new[]
            {
                new ScenarioCompareRow("|vy| after warm fall (m/s)", vTerm, measured, Math.Abs(measured - vTerm)),
            },
            new TableOptions { MaxCellWidth = 24 },
            caption: "expected v_term = sqrt(2 m g / (rho Cd A))");
        o.Line("relative gap", Math.Abs(measured - vTerm) / vTerm);

        await Assert.That(Math.Abs(measured - vTerm) / vTerm).IsLessThanOrEqualTo(0.06);
    }

    /// <summary>Specific energy in uniform vacuum field (no drag): E = v^2/2 + g y should drift slowly for this integrator.</summary>
    [Test]
    public async Task ObliqueLaunch_Vacuum_SpecificEnergyDriftIsBounded()
    {
        const double vx = 40.0;
        const double vy = 55.0;
        const double dt = 1.0 / 240.0;
        var env = new ProjectileBallisticEnvironment(GStd, 0);
        var sim = new ProjectileBallisticSimulation(null);
        var state = new ProjectileState(Vector3d.Zero, new Vector3d(vx, vy, 0), massKg: 1.0, 0);

        static double SpecificEnergy(ProjectileState s) =>
            0.5 * s.Velocity.LengthSquared() + GStd * s.Position.Y;

        var e0 = SpecificEnergy(state);
        var peakE = e0;
        var minE = e0;

        while (state.Position.Y >= 0)
        {
            state = sim.Step(state, dt, env);
            var e = SpecificEnergy(state);
            peakE = Math.Max(peakE, e);
            minE = Math.Min(minE, e);
        }

        var relSpread = (peakE - minE) / Math.Max(1e-9, Math.Abs(e0));

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Scenario - specific energy drift (vacuum, oblique)");
        o.Line("setup", FormattableString.Invariant($"v=({vx},{vy},0) m/s from origin, g={GStd}, dt={dt}"));
        o.Line("metric", "E = v^2/2 + g y (per kg); continuous vacuum keeps E constant.");

        o.Results("Scenario - specific energy - outcome");
        o.Table(
            new[]
            {
                new EnergySummaryRow("E at launch (J/kg)", e0),
                new EnergySummaryRow("min E in flight", minE),
                new EnergySummaryRow("max E in flight", peakE),
                new EnergySummaryRow("(max-min)/|E0|", relSpread),
            },
            new TableOptions { MaxCellWidth = 22 },
            caption: "E = v^2/2 + g y per kg; continuous vacuum keeps E constant");

        await Assert.That(relSpread).IsLessThanOrEqualTo(0.04);
    }

    private static GroundImpact RunUntilGroundCrossing(
        ProjectileBallisticSimulation sim,
        ProjectileState start,
        ProjectileBallisticEnvironment env,
        double dt,
        int maxSteps = 2_000_000)
    {
        var state = start;
        var previous = state;
        var steps = 0;
        while (state.Position.Y >= 0 && steps++ < maxSteps)
        {
            previous = state;
            state = sim.Step(state, dt, env);
        }

        return ProjectileMath.InterpolateGroundImpact(previous, state);
    }
}
