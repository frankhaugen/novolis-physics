using Novolis.Physics.Abstractions;
using Novolis.Physics.Gravity;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class PatchedConicGravityTests
{
    [Test]
    public async Task PatchedConic_InsidePrimarySoi_UsesPrimaryGmAndDirection()
    {
        const double mass = 50.0;
        var primaryGm = 4.0e12;
        const double soi = 8_000.0;
        var secondaryGm = 1.0e8;
        var field = new PatchedConicPairField(
            primaryPosition: Vector3d.Zero,
            primaryGm,
            primarySphereOfInfluenceRadius: soi,
            secondaryPosition: new Vector3d(5_000_000.0, 0, 0),
            secondaryGm);

        var bodyPos = new Vector3d(2_000.0, 0, 0);
        var body = new RigidBodyState(bodyPos, Vector3d.Zero, Quaterniond.Identity, Vector3d.Zero, mass, new Vector3d(1, 1, 1));
        var model = new PatchedConicGravityModel();
        var sample = model.Evaluate(body, field, timeSeconds: 0);

        var expected = NewtonTowardSource(bodyPos, Vector3d.Zero, primaryGm, mass);
        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Patched conic - inside primary SOI");
        o.Table(
            new[]
            {
                new ForceCompareRow("Fx", expected.X, sample.Force.X, Math.Abs(expected.X - sample.Force.X)),
                new ForceCompareRow("Fy", expected.Y, sample.Force.Y, Math.Abs(expected.Y - sample.Force.Y)),
                new ForceCompareRow("Fz", expected.Z, sample.Force.Z, Math.Abs(expected.Z - sample.Force.Z)),
            },
            new TableOptions { MaxCellWidth = 28 },
            caption: "Force vs hand inverse-square toward primary",
            columnPropertyOrder: new[] { "Label", "Expected", "Actual", "AbsError" });

        var rel = 1e-9 * Math.Max(1.0, expected.Length());
        await Assert.That((sample.Force - expected).Length()).IsLessThanOrEqualTo(rel);
    }

    [Test]
    public async Task PatchedConic_OutsidePrimarySoi_UsesSecondarySource()
    {
        const double mass = 10.0;
        var primaryGm = 4.0e12;
        const double soi = 5_000.0;
        var secondaryGm = 9.0e11;
        var secondaryPos = new Vector3d(80_000.0, 0, 0);
        var field = new PatchedConicPairField(
            Vector3d.Zero,
            primaryGm,
            soi,
            secondaryPos,
            secondaryGm);

        var bodyPos = new Vector3d(12_000.0, 0, 0);
        var body = new RigidBodyState(bodyPos, Vector3d.Zero, Quaterniond.Identity, Vector3d.Zero, mass, new Vector3d(1, 1, 1));
        var model = new PatchedConicGravityModel();
        var sample = model.Evaluate(body, field, 0);

        var expected = NewtonTowardSource(bodyPos, secondaryPos, secondaryGm, mass);
        var o = NovolisPhysicsTestTrace.Out;
        o.Results("Patched conic - outside SOI outcome");
        o.Table(
            new[]
            {
                new ForceCompareRow("Fx", expected.X, sample.Force.X, Math.Abs(expected.X - sample.Force.X)),
                new ForceCompareRow("Fy", expected.Y, sample.Force.Y, Math.Abs(expected.Y - sample.Force.Y)),
                new ForceCompareRow("Fz", expected.Z, sample.Force.Z, Math.Abs(expected.Z - sample.Force.Z)),
            },
            new TableOptions { MaxCellWidth = 28 },
            caption: "12 km from primary (> SOI): force toward secondary at 80 km",
            columnPropertyOrder: new[] { "Label", "Expected", "Actual", "AbsError" });

        var rel = 1e-9 * Math.Max(1.0, expected.Length());
        await Assert.That((sample.Force - expected).Length()).IsLessThanOrEqualTo(rel);
    }

    [Test]
    public async Task PatchedConic_OnSoiBoundary_InsideUsesPrimary()
    {
        const double mass = 1.0;
        var primaryGm = 1.0e6;
        const double soi = 10_000.0;
        var field = new PatchedConicPairField(
            Vector3d.Zero,
            primaryGm,
            soi,
            new Vector3d(1e9, 0, 0),
            1.0e12);

        var onBoundary = new Vector3d(soi, 0, 0);
        var body = new RigidBodyState(onBoundary, Vector3d.Zero, Quaterniond.Identity, Vector3d.Zero, mass, new Vector3d(1, 1, 1));
        var f = new PatchedConicGravityModel().Evaluate(body, field, 0).Force;
        var expected = NewtonTowardSource(onBoundary, Vector3d.Zero, primaryGm, mass);
        await Assert.That((f - expected).Length()).IsLessThanOrEqualTo(1e-9 * Math.Max(1.0, expected.Length()));
    }

    private static Vector3d NewtonTowardSource(Vector3d body, Vector3d source, double gm, double mass)
    {
        var r = source - body;
        var d2 = r.LengthSquared();
        if (d2 < 1e-24)
            return Vector3d.Zero;

        var invD = 1.0 / Math.Sqrt(d2);
        return r * (invD * (mass * gm / d2));
    }

    private sealed record ForceCompareRow(string Label, double Expected, double Actual, double AbsError);
}
