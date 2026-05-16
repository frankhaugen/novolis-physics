using Novolis.Physics.Ballistics;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class BallisticPropertyTests
{
    [Test]
    public async Task Drag_ReducesHorizontalRange_VersusSameInitialVacuum()
    {
        var initial = new ProjectileState(
            Vector3d.Zero,
            new Vector3d(40, 30, 0),
            massKg: 1.0,
            timeSeconds: 0);
        var envAir = new ProjectileBallisticEnvironment(9.80665, 1.225);
        var envVacuum = envAir with { AirDensityKgPerM3 = 0 };
        var profile = new ProjectileProfile(1.0, 0.01, 0.4);
        var vacuum = SimulateRange(initial, null, envVacuum, dt: 1 / 120.0);
        var drag = SimulateRange(initial, profile, envAir, dt: 1 / 120.0);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Property drag vs vacuum (compact)");
        o.Line(
            "setup",
            FormattableString.Invariant(
                $"v0=({initial.Velocity.X},{initial.Velocity.Y},{initial.Velocity.Z}) m/s, dt={1 / 120.0} s, rho_air={envAir.AirDensityKgPerM3}"));

        o.Results("Property drag vs vacuum - outcome");
        o.Table(
            new[]
            {
                new RangeCompareRow("vacuum (rho=0)", vacuum),
                new RangeCompareRow("drag (rho air)", drag),
                new RangeCompareRow("delta (vac - drag)", vacuum - drag),
            },
            new TableOptions { RightAlignNumericColumns = true },
            caption: "Horizontal range to impact (m); expect drag < vacuum");

        await Assert.That(drag).IsLessThan(vacuum);
    }

    [Test]
    public async Task Gravity_ReducesApogee_WhenComparingStrongerG()
    {
        var initial = new ProjectileState(Vector3d.Zero, new Vector3d(20, 40, 0), 1.0, 0);
        var lowG = new ProjectileBallisticEnvironment(9.8, 0);
        var highG = new ProjectileBallisticEnvironment(15.0, 0);
        var hLow = MaxHeight(initial, lowG, null, 1 / 120.0);
        var hHigh = MaxHeight(initial, highG, null, 1 / 120.0);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Property gravity vs apogee (compact)");
        o.Line("setup", FormattableString.Invariant($"v0=({initial.Velocity.X},{initial.Velocity.Y}) m/s, dt={1 / 120.0} s"));

        o.Results("Property gravity vs apogee - outcome");
        o.Table(
            new[]
            {
                new ApogeeRow($"low-g (g={lowG.GravityMetersPerSecondSquared})", hLow),
                new ApogeeRow($"high-g (g={highG.GravityMetersPerSecondSquared})", hHigh),
                new ApogeeRow("delta (low - high)", hLow - hHigh),
            },
            new TableOptions { RightAlignNumericColumns = true },
            caption: "Max height (m) before ground impact; expect high-g < low-g");

        await Assert.That(hHigh).IsLessThan(hLow);
    }

    private static double SimulateRange(ProjectileState initial, ProjectileProfile? drag, ProjectileBallisticEnvironment env, double dt)
    {
        var sim = new ProjectileBallisticSimulation(drag);
        var s = initial;
        var prev = s;
        while (s.Position.Y >= 0)
        {
            prev = s;
            s = sim.Step(s, dt, env);
        }

        return ProjectileMath.InterpolateGroundImpact(prev, s).Position.X;
    }

    private static double MaxHeight(ProjectileState initial, ProjectileBallisticEnvironment env, ProjectileProfile? drag, double dt)
    {
        var sim = new ProjectileBallisticSimulation(drag);
        var s = initial;
        var max = s.Position.Y;
        while (s.Position.Y >= 0)
        {
            s = sim.Step(s, dt, env);
            max = Math.Max(max, s.Position.Y);
        }

        return max;
    }

    private sealed record RangeCompareRow(string Case, double RangeM);

    private sealed record ApogeeRow(string Case, double MaxHeightM);
}
