using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>Elastic sphere in axis-aligned rooms: 1D-like slab, thin 2D channel, full 3D box.</summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class BouncingBallCollisionTests
{
    private const double Radius = 0.22;

    private const double Dt = 1.0 / 240.0;

    [Test]
    public async Task BouncingBall_1DSlabAlongX_PreservesSpeedAndStaysBetweenWalls()
    {
        // Wide YZ so motion stays essentially 1D along X between two x-walls.
        var min = new Vector3d(0, -80, -80);
        var max = new Vector3d(10, 80, 80);
        var world = CollisionTestGeometry.BuildAxisAlignedRoom(min, max, edgePad: 8);
        var pos = new Vector3d(5, 0, 0);
        var vel = new Vector3d(-6.5, 0, 0);
        var v0 = vel.Length();

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(BouncingBall_1DSlabAlongX_PreservesSpeedAndStaysBetweenWalls));
        o.Line("room_min_m", min.X, min.Y, min.Z);
        o.Line("room_max_m", max.X, max.Y, max.Z);
        o.Line("radius_m", Radius);
        o.Line("dt_s", Dt);
        o.Line("ic_pos_m", pos.X, pos.Y, pos.Z);
        o.Line("ic_vel_m_s", vel.X, vel.Y, vel.Z);
        o.Line("external_force_N_at_m_ref_1kg", "0, 0, 0 (no gravity/drag in harness)");
        o.Line("trace_note", "X steps along slab; Ux,Uy,Uz is unit(v); wall impulses are not tabulated as F.");

        var minX = min.X + Radius + 0.05;
        var maxX = max.X - Radius - 0.05;
        const double boundSlack = 0.12;
        var totalReflections = 0;
        var rows = new List<BounceSampleRow>(capacity: 64);
        const int steps = 8000;
        var fExt = Vector3d.Zero;
        for (var i = 0; i < steps; i++)
        {
            totalReflections += BvhStaticSphereIntegrator.AdvanceOneStep(
                world,
                ref pos,
                ref vel,
                Radius,
                Dt);
            if (i % 400 == 0 || i == steps - 1)
                rows.Add(BounceSampleRow.FromState(i, pos, vel, totalReflections, fExt));

            await Assert.That(pos.X).IsGreaterThanOrEqualTo(minX - boundSlack).And.IsLessThanOrEqualTo(maxX + boundSlack);
            await Assert.That(pos.Y).IsGreaterThanOrEqualTo(min.Y + Radius * 0.25 - boundSlack);
            await Assert.That(pos.Z).IsGreaterThanOrEqualTo(min.Z + Radius * 0.25 - boundSlack);
        }

        o.Results(nameof(BouncingBall_1DSlabAlongX_PreservesSpeedAndStaysBetweenWalls) + " — samples");
        o.Table(
            rows,
            new TableOptions { MaxRows = 30, RightAlignNumericColumns = true, MaxCellWidth = 22 },
            caption: "Every 400 steps: r (m), v (m/s), unit(v), F_ext (N @ m_ref=1 kg), reflections",
            BounceSampleRow.ColumnOrder);
        o.Line("|v|_initial_m_s", v0);
        o.Line("|v|_final_m_s", vel.Length());
        o.Line("total_reflections", totalReflections);

        await Assert.That(Math.Abs(vel.Length() - v0)).IsLessThanOrEqualTo(0.02);
        await Assert.That(Math.Abs(vel.Y)).IsLessThan(0.05);
        await Assert.That(Math.Abs(vel.Z)).IsLessThan(0.05);
    }

    [Test]
    public async Task BouncingBall_2DChannelXY_KeepsZNearZeroAndConservesSpeed()
    {
        var min = Vector3d.Zero;
        var max = new Vector3d(12, 8, 1.2);
        var world = CollisionTestGeometry.BuildAxisAlignedRoom(min, max, edgePad: 6);
        var pos = new Vector3d(6, 4, max.Z * 0.5);
        var vel = new Vector3d(4.2, -3.1, 0.05);
        var v0 = vel.Length();

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(BouncingBall_2DChannelXY_KeepsZNearZeroAndConservesSpeed));
        o.Line("room_max_m", max.X, max.Y, max.Z);
        o.Line("radius_m", Radius);
        o.Line("ic_vel_m_s", vel.X, vel.Y, vel.Z);

        var zMin = min.Z + Radius;
        var zMax = max.Z - Radius;
        const double zSlack = 0.2;
        var totalReflections = 0;
        var rows = new List<BounceSampleRow>();
        const int steps = 6000;
        var fExt = Vector3d.Zero;
        for (var i = 0; i < steps; i++)
        {
            totalReflections += BvhStaticSphereIntegrator.AdvanceOneStep(world, ref pos, ref vel, Radius, Dt);
            if (i % 500 == 0 || i == steps - 1)
                rows.Add(BounceSampleRow.FromState(i, pos, vel, totalReflections, fExt));

            await Assert.That(pos.Z).IsGreaterThanOrEqualTo(zMin - zSlack).And.IsLessThanOrEqualTo(max.Z + Radius + 0.28);
        }

        o.Results("2D channel — samples");
        o.Table(
            rows,
            new TableOptions { MaxRows = 20, RightAlignNumericColumns = true, MaxCellWidth = 22 },
            caption: "Every 500 steps: r, v, unit(v), F_ext (N @ m_ref=1 kg)",
            BounceSampleRow.ColumnOrder);
        o.Line("|v|_initial_vs_final", v0, vel.Length(), Math.Abs(vel.Length() - v0));

        await Assert.That(Math.Abs(vel.Length() - v0)).IsLessThanOrEqualTo(0.06);
        await Assert.That(Math.Abs(pos.Z - max.Z * 0.5)).IsLessThan(0.35);
    }

    [Test]
    public async Task BouncingBall_3DBox_ManyReflections_StaysInsideAndConservesEnergy()
    {
        var min = new Vector3d(0.5, 0.5, 0.5);
        var max = new Vector3d(9.5, 9.5, 9.5);
        var world = CollisionTestGeometry.BuildAxisAlignedRoom(min, max, edgePad: 10);
        var pos = new Vector3d(3.1, 4.2, 5.3);
        var vel = new Vector3d(2.7, -2.4, 1.9);
        var v0 = vel.Length();

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(BouncingBall_3DBox_ManyReflections_StaysInsideAndConservesEnergy));
        o.Line("radius_m", Radius);
        o.Line("ic_pos_m", pos.X, pos.Y, pos.Z);
        o.Line("ic_vel_m_s", vel.X, vel.Y, vel.Z);

        const double boxSlack = 0.16;
        var innerMin = new Vector3d(min.X + Radius, min.Y + Radius, min.Z + Radius);
        var innerMax = new Vector3d(max.X - Radius, max.Y - Radius, max.Z - Radius);
        var totalReflections = 0;
        var rows = new List<BounceSampleRow>();
        const int steps = 9000;
        var fExt = Vector3d.Zero;
        for (var i = 0; i < steps; i++)
        {
            totalReflections += BvhStaticSphereIntegrator.AdvanceOneStep(world, ref pos, ref vel, Radius, Dt);
            if (i % 900 == 0 || i == steps - 1)
                rows.Add(BounceSampleRow.FromState(i, pos, vel, totalReflections, fExt));

            await Assert.That(pos.X).IsGreaterThanOrEqualTo(innerMin.X - boxSlack).And.IsLessThanOrEqualTo(innerMax.X + boxSlack);
            await Assert.That(pos.Y).IsGreaterThanOrEqualTo(innerMin.Y - boxSlack).And.IsLessThanOrEqualTo(innerMax.Y + boxSlack);
            await Assert.That(pos.Z).IsGreaterThanOrEqualTo(innerMin.Z - boxSlack).And.IsLessThanOrEqualTo(innerMax.Z + boxSlack);
        }

        o.Results("3D box — samples");
        o.Table(
            rows,
            new TableOptions { MaxRows = 16, RightAlignNumericColumns = true, MaxCellWidth = 22 },
            caption: "Every 900 steps: r, v, unit(v), F_ext (N @ m_ref=1 kg)",
            BounceSampleRow.ColumnOrder);
        o.Line("reflections_total", totalReflections);
        o.Line("|v|_initial_m_s", v0);
        o.Line("|v|_final_m_s", vel.Length());

        await Assert.That(totalReflections).IsGreaterThan(20);
        await Assert.That(Math.Abs(vel.Length() - v0)).IsLessThanOrEqualTo(0.08);
    }

    /// <summary>Trace row: position, velocity, speed, velocity direction, external force (N) for a 1 kg reference mass.</summary>
    private sealed record BounceSampleRow(
        int Step,
        double X,
        double Y,
        double Z,
        double Vx,
        double Vy,
        double Vz,
        double Speed,
        double Ux,
        double Uy,
        double Uz,
        double Fx,
        double Fy,
        double Fz,
        int Reflections)
    {
        internal static readonly IReadOnlyList<string> ColumnOrder =
        [
            nameof(Step),
            nameof(X),
            nameof(Y),
            nameof(Z),
            nameof(Vx),
            nameof(Vy),
            nameof(Vz),
            nameof(Speed),
            nameof(Ux),
            nameof(Uy),
            nameof(Uz),
            nameof(Fx),
            nameof(Fy),
            nameof(Fz),
            nameof(Reflections),
        ];

        internal static BounceSampleRow FromState(int step, Vector3d pos, Vector3d vel, int reflections, Vector3d externalForceN)
        {
            var speed = vel.Length();
            double ux, uy, uz;
            if (speed > 1e-30)
            {
                ux = vel.X / speed;
                uy = vel.Y / speed;
                uz = vel.Z / speed;
            }
            else
            {
                ux = uy = uz = 0;
            }

            return new BounceSampleRow(
                step,
                pos.X,
                pos.Y,
                pos.Z,
                vel.X,
                vel.Y,
                vel.Z,
                speed,
                ux,
                uy,
                uz,
                externalForceN.X,
                externalForceN.Y,
                externalForceN.Z,
                reflections);
        }
    }
}
