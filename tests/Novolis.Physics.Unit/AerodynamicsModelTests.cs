using Novolis.Physics.Abstractions;
using Novolis.Physics.Aerodynamics;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class AerodynamicsModelTests
{
    [Test]
    public async Task ExponentialAtmosphere_MatchesExpLaw()
    {
        var atm = new ExponentialAtmosphereModel(seaLevelDensityKgPerM3: 1.225, scaleHeightMeters: 8500.0);
        var h = 17_000.0;
        var expected = 1.225 * Math.Exp(-h / 8500.0);
        var rho = atm.DensityAtAltitude(h);

        var o = NovolisPhysicsTestTrace.Out;
        PhysicsDashboard.SectionAndTable(
            o,
            "Exponential atmosphere",
            new[] { new DensityRow("sea level", 0, atm.DensityAtAltitude(0)), new DensityRow("17 km", h, rho) },
            new TableOptions { MaxCellWidth = 20, RightAlignNumericColumns = true },
            tableCaption: "rho(h) = rho0 * exp(-h/H)");

        await Assert.That(Math.Abs(rho - expected)).IsLessThanOrEqualTo(1e-12);
        await Assert.That(atm.DensityAtAltitude(0)).IsEqualTo(1.225);
    }

    [Test]
    public async Task SimpleLiftDrag_DragOpposesRelativeVelocity_WithWind()
    {
        var atm = new ExponentialAtmosphereModel(1.225, 8500);
        var wind = new Vector3d(5, 0, 0);
        var bodyVel = new Vector3d(25, 0, 0);
        var body = new RigidBodyState(
            Vector3d.Zero,
            bodyVel,
            Quaterniond.Identity,
            Vector3d.Zero,
            mass: 10,
            inertiaDiagonalBody: new Vector3d(1, 1, 1));
        var forward = new Vector3d(0, 0, 1);
        var env = new SimpleAeroEnvironment(atm, altitudeMeters: 0, wind, referenceAreaM2: 2, dragCoefficient: 0.4, liftCoefficient: 0, liftReferenceForwardWorld: forward);
        var model = new SimpleLiftDragModel();
        var f = model.Evaluate(body, env, 0).Force;
        var vRel = bodyVel - wind;

        var o = NovolisPhysicsTestTrace.Out;
        o.Results("Lift/drag - relative wind outcome");
        o.Table(
            new[]
            {
                new AeroSampleRow("v_rel.X", vRel.X),
                new AeroSampleRow("F.X", f.X),
                new AeroSampleRow("F.Y", f.Y),
                new AeroSampleRow("F.Z", f.Z),
            },
            new TableOptions { MaxCellWidth = 18, RightAlignNumericColumns = true },
            caption: "expect drag roughly opposite v_rel (lift adds orthogonal component)");

        var dot = Vector3d.Dot(f, vRel);
        await Assert.That(dot).IsLessThan(0);
    }

    [Test]
    public async Task SimpleLiftDrag_ZeroRelativeVelocity_ReturnsZero()
    {
        var atm = new ExponentialAtmosphereModel(1.225, 8500);
        var wind = new Vector3d(10, 0, 0);
        var body = new RigidBodyState(
            Vector3d.Zero,
            new Vector3d(10, 0, 0),
            Quaterniond.Identity,
            Vector3d.Zero,
            1,
            new Vector3d(1, 1, 1));
        var env = new SimpleAeroEnvironment(atm, 0, wind, 1, 0.5, 0.2, new Vector3d(0, 1, 0));
        var f = new SimpleLiftDragModel().Evaluate(body, env, 0).Force;
        await Assert.That(f.Length()).IsLessThanOrEqualTo(1e-8);
    }

    private sealed record DensityRow(string Label, double AltitudeM, double DensityKgM3);

    private sealed record AeroSampleRow(string Label, double Value);
}
