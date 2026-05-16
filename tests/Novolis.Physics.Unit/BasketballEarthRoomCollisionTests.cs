using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Motion;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>
/// Basketball-sized sphere in a 2.5 m cube on Earth: uniform gravity + optional linear air drag,
/// rigid walls with Newton restitution (lively ball, inelastic normal impulse vs concrete/gym wall).
/// Console tables are sparse and rounded; “reflection” counts are integrator wall resolutions, not macro bounces.
/// </summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class BasketballEarthRoomCollisionTests
{
    /// <summary>SI standard gravity (m/s²), +Y up; acceleration vector is <c>(0, −GStd, 0)</c>.</summary>
    private const double GStd = 9.80665;

    /// <summary>Interior free span between opposite faces (m).</summary>
    private const double RoomInteriorSpanM = 2.5;

    /// <summary>
    /// Mesh walls sit at <c>±</c> this offset so the interior cube is <see cref="RoomInteriorSpanM"/> wide
    /// (same pattern as <see cref="BouncingBallCollisionTests.BouncingBall_3DBox_ManyReflections_StaysInsideAndConservesEnergy"/>: avoids axis-aligned sweeps grazing the origin).
    /// </summary>
    private const double RoomWallOffsetM = 0.25;

    /// <summary>~official men’s ball diameter 0.241 m → radius.</summary>
    private const double BallRadiusM = 0.1205;

    /// <summary>Inflated leather ball ~0.62 kg.</summary>
    private const double BallMassKg = 0.62;

    /// <summary>Integration step (s); smaller than slab tests to limit displacement per sub-step under gravity.</summary>
    private const double Dt = 1.0 / 480.0;

    /// <summary>Isotropic linear drag scale (1/s): <c>v ← v(1 − kΔt)</c> per sub-step.</summary>
    private const double LinearDragPerSecond = 0.048;

    /// <summary>
    /// Normal Newton coefficient vs fixed rigid walls (ball–wall system). ~0.8 is a plausible indoor
    /// order-of-magnitude for a pressurized leather ball on a hard surface (not a lab measurement).
    /// </summary>
    private const double WallNormalRestitution = 0.82;

    /// <summary>Target speed for a firm chest-style pass (m/s).</summary>
    private const double QuickPassSpeedMps = 8.05;

    /// <summary>Skew direction so the rally visits all six faces without aligning to a symmetry axis.</summary>
    private static readonly Vector3d QuickPassDirection = new Vector3d(2.35, 4.05, -2.65).Normalized();

    /// <summary>Example uniform acceleration (m/s²), +Y up — numeric magnitude matches SI standard gravity for this scenario only.</summary>
    private static readonly Vector3d UniformGravityExampleMps2 = new(0, -GStd, 0);

    [Test]
    public async Task Basketball_InTwoPointFiveMeterCube_OnEarth_LongRun_GravityLinearDragAndEnergyTrace()
    {
        var roomMin = new Vector3d(RoomWallOffsetM, RoomWallOffsetM, RoomWallOffsetM);
        var roomMax = new Vector3d(
            RoomWallOffsetM + RoomInteriorSpanM,
            RoomWallOffsetM + RoomInteriorSpanM,
            RoomWallOffsetM + RoomInteriorSpanM);
        var world = CollisionTestGeometry.BuildAxisAlignedRoom(roomMin, roomMax, edgePad: 0.35);

        // Waist-ish release, skewed aim so the path threads the volume (quick pass speed, not a spike).
        var pos = new Vector3d(roomMin.X + 0.52, roomMin.Y + 0.38, roomMin.Z + 0.44);
        var vel = QuickPassDirection * QuickPassSpeedMps;
        var v0 = vel.Length();
        var e0Kin = UniformAccelerationEnergy.KineticEnergyJ(BallMassKg, vel);
        var e0Pot = UniformAccelerationEnergy.PotentialEnergyJ(BallMassKg, UniformGravityExampleMps2, pos);
        var e0Tot = UniformAccelerationEnergy.MechanicalEnergyJ(BallMassKg, vel, UniformGravityExampleMps2, pos);

        // ~25 s wall-clock sim; coarser substeps than stress tests — still stable with room inset.
        const int steps = 12_000;
        const int sampleStride = 3000;
        const int milestoneStride = 4000;
        const int substeps = 16;
        const double surfaceEps = 9e-4;

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Basketball_InTwoPointFiveMeterCube_OnEarth_LongRun_GravityLinearDragAndEnergyTrace));
        o.Line(
            "reflection_count_note",
            "Refl = wall contact resolutions inside the sweep integrator (incl. grazing / multi-hit substeps), not one count per audible bounce.");
        PhysicsDashboard.SectionAndTable(
            o,
            "Earth room — physical model",
            new[]
            {
                new ModelKvRow("room_interior_span_m", PhysicsTraceFormatting.Rs(RoomInteriorSpanM, 2)),
                new ModelKvRow("room_wall_offset_m", PhysicsTraceFormatting.Rs(RoomWallOffsetM, 2)),
                new ModelKvRow(nameof(BallRadiusM), PhysicsTraceFormatting.Rs(BallRadiusM, 4)),
                new ModelKvRow(nameof(BallMassKg), PhysicsTraceFormatting.Rs(BallMassKg, 2)),
                new ModelKvRow("|g|_example_m_s2", PhysicsTraceFormatting.Rs(GStd, 3)),
                new ModelKvRow(nameof(LinearDragPerSecond), PhysicsTraceFormatting.Rs(LinearDragPerSecond, 3)),
                new ModelKvRow(nameof(Dt), PhysicsTraceFormatting.Rs(Dt, 5)),
                new ModelKvRow("substeps_per_step", substeps.ToString()),
                new ModelKvRow("surface_epsilon_m", PhysicsTraceFormatting.Rs(surfaceEps, 4)),
                new ModelKvRow("wall_normal_restitution_e", PhysicsTraceFormatting.Rs(WallNormalRestitution, 2)),
                new ModelKvRow("pass_speed_m_s", PhysicsTraceFormatting.Rs(QuickPassSpeedMps, 2)),
                new ModelKvRow(
                    "pass_dir_xyz",
                    $"{PhysicsTraceFormatting.Rs(QuickPassDirection.X, 3)}, {PhysicsTraceFormatting.Rs(QuickPassDirection.Y, 3)}, {PhysicsTraceFormatting.Rs(QuickPassDirection.Z, 3)}"),
            },
            new TableOptions { RightAlignNumericColumns = true, MaxCellWidth = 22 },
            tableCaption: "Rigid walls (Newton e); linear air drag between impacts; tangential slip unchanged");

        PhysicsDashboard.SectionAndTable(
            o,
            "Earth room — initial conditions",
            new[]
            {
                new IcRow("position_m", PhysicsTraceFormatting.Rd(pos.X, 3), PhysicsTraceFormatting.Rd(pos.Y, 3), PhysicsTraceFormatting.Rd(pos.Z, 3)),
                new IcRow("velocity_m_s", PhysicsTraceFormatting.Rd(vel.X, 3), PhysicsTraceFormatting.Rd(vel.Y, 3), PhysicsTraceFormatting.Rd(vel.Z, 3)),
                new IcRow("|v|_m_s", PhysicsTraceFormatting.Rd(v0, 3), 0, 0),
                new IcRow("KE_J", PhysicsTraceFormatting.Rd(e0Kin, 3), 0, 0),
                new IcRow("PE_J_neg_m_g_dot_r", PhysicsTraceFormatting.Rd(e0Pot, 3), 0, 0),
                new IcRow("Etot_J", PhysicsTraceFormatting.Rd(e0Tot, 3), 0, 0),
            },
            new TableOptions { RightAlignNumericColumns = true, MaxCellWidth = 22 },
            tableCaption:
                $"PE = −m(g·r) per Novolis.Physics.Motion.UniformAccelerationEnergy (y up, g=(0,−|g|,0)); floor y = {roomMin.Y} m");

        var samples = new List<SampleRow>(capacity: 16);
        var milestones = new List<MilestoneRow>(capacity: 16);
        var reflAtLastSample = 0;
        var totalReflections = 0;
        var yMax = pos.Y;
        var speedMax = v0;
        var speedMin = v0;
        var eTotMin = e0Tot;
        var eTotMax = e0Tot;

        for (var i = 0; i < steps; i++)
        {
            totalReflections += BvhStaticSphereIntegrator.AdvanceWithUniformAccelerationAndLinearDrag(
                world,
                ref pos,
                ref vel,
                BallRadiusM,
                Dt,
                UniformGravityExampleMps2,
                LinearDragPerSecond,
                substepsPerStep: substeps,
                surfaceEpsilon: surfaceEps,
                maxReflectionsPerSubstep: 96,
                normalRestitution: WallNormalRestitution);

            yMax = Math.Max(yMax, pos.Y);
            var sp = vel.Length();
            speedMax = Math.Max(speedMax, sp);
            speedMin = Math.Min(speedMin, sp);
            var eTot = UniformAccelerationEnergy.MechanicalEnergyJ(BallMassKg, vel, UniformGravityExampleMps2, pos);
            eTotMin = Math.Min(eTotMin, eTot);
            eTotMax = Math.Max(eTotMax, eTot);

            if (i % sampleStride == 0 || i == steps - 1)
            {
                var dRefl = totalReflections - reflAtLastSample;
                reflAtLastSample = totalReflections;
                samples.Add(SampleRow.From(i, i * Dt, pos, sp, eTot, totalReflections, dRefl));
            }

            if (i % milestoneStride == 0 || i == steps - 1)
                milestones.Add(MilestoneRow.From(i, i * Dt, yMax, sp, eTot, totalReflections));
        }

        const double s = 0.12;
        var xMin = roomMin.X + BallRadiusM - s;
        var xMax = roomMax.X - BallRadiusM + s;
        var yMin = roomMin.Y + BallRadiusM - s;
        var yMaxBound = roomMax.Y - BallRadiusM + s;
        var zMin = roomMin.Z + BallRadiusM - s;
        var zMax = roomMax.Z - BallRadiusM + s;
        foreach (var row in samples)
        {
            await Assert.That(row.Xm).IsGreaterThanOrEqualTo(xMin).And.IsLessThanOrEqualTo(xMax);
            await Assert.That(row.Ym).IsGreaterThanOrEqualTo(yMin).And.IsLessThanOrEqualTo(yMaxBound);
            await Assert.That(row.Zm).IsGreaterThanOrEqualTo(zMin).And.IsLessThanOrEqualTo(zMax);
        }

        await Assert.That(pos.X).IsGreaterThanOrEqualTo(xMin).And.IsLessThanOrEqualTo(xMax);
        await Assert.That(pos.Y).IsGreaterThanOrEqualTo(yMin).And.IsLessThanOrEqualTo(yMaxBound);
        await Assert.That(pos.Z).IsGreaterThanOrEqualTo(zMin).And.IsLessThanOrEqualTo(zMax);

        var eEndTot = UniformAccelerationEnergy.MechanicalEnergyJ(BallMassKg, vel, UniformGravityExampleMps2, pos);

        o.Results("Earth room — milestone energy / apex");
        o.Table(
            milestones,
            new TableOptions { MaxRows = 12, RightAlignNumericColumns = true, MaxCellWidth = 14 },
            caption: $"Every {milestoneStride} steps (rounded): apex y, |v|, E_tot (J), cumulative Refl",
            MilestoneRow.ColumnOrder);

        o.Results("Earth room — sparse samples (rounded)");
        o.Table(
            samples,
            new TableOptions { MaxRows = 12, RightAlignNumericColumns = true, MaxCellWidth = 12 },
            caption: $"Every {sampleStride} steps: r (m), |v| (m/s), E_tot (J), Refl total / since last row",
            SampleRow.ColumnOrder);

        PhysicsDashboard.ResultsAndTable(
            o,
            "Earth room — end state",
            new[]
            {
                new EndRow("position_m", PhysicsTraceFormatting.Rd(pos.X, 3), PhysicsTraceFormatting.Rd(pos.Y, 3), PhysicsTraceFormatting.Rd(pos.Z, 3)),
                new EndRow("velocity_m_s", PhysicsTraceFormatting.Rd(vel.X, 3), PhysicsTraceFormatting.Rd(vel.Y, 3), PhysicsTraceFormatting.Rd(vel.Z, 3)),
                new EndRow("|v|_m_s", PhysicsTraceFormatting.Rd(vel.Length(), 3), 0, 0),
                new EndRow("y_max_seen_m", PhysicsTraceFormatting.Rd(yMax, 3), 0, 0),
                new EndRow("speed_max_m_s", PhysicsTraceFormatting.Rd(speedMax, 3), 0, 0),
                new EndRow("speed_min_m_s", PhysicsTraceFormatting.Rd(speedMin, 3), 0, 0),
                new EndRow("Etot_J_end", PhysicsTraceFormatting.Rd(eEndTot, 3), 0, 0),
                new EndRow("reflections_total", totalReflections, 0, 0),
            },
            new TableOptions { RightAlignNumericColumns = true, MaxCellWidth = 22 },
            tableCaption: "Drag + wall restitution remove mechanical energy; tangential motion carries across impacts");

        o.Line("Etot_J_initial", PhysicsTraceFormatting.Rd(e0Tot, 3));
        o.Line("Etot_J_final", PhysicsTraceFormatting.Rd(eEndTot, 3));
        o.Line("delta_Etot_J", PhysicsTraceFormatting.Rd(eEndTot - e0Tot, 3));

        await Assert.That(totalReflections).IsGreaterThan(25);
        await Assert.That(yMax).IsLessThanOrEqualTo(yMaxBound + 0.15);
        await Assert.That(eEndTot).IsLessThan(e0Tot * 0.5);
        await Assert.That(vel.Length()).IsLessThan(v0 * 0.55);
    }

    /// <summary>Same geometry with <c>linearDragPerSecond = 0</c>: mechanical energy should stay in a tight band (no non-conservative damping).</summary>
    [Test]
    public async Task Basketball_GravityNoDrag_Shorter_MechanicalEnergyBoundedAndManyRicochets()
    {
        var roomMin = new Vector3d(RoomWallOffsetM, RoomWallOffsetM, RoomWallOffsetM);
        var roomMax = new Vector3d(
            RoomWallOffsetM + RoomInteriorSpanM,
            RoomWallOffsetM + RoomInteriorSpanM,
            RoomWallOffsetM + RoomInteriorSpanM);
        var world = CollisionTestGeometry.BuildAxisAlignedRoom(roomMin, roomMax, edgePad: 0.35);
        var pos = new Vector3d(roomMin.X + 0.65, roomMin.Y + 0.3, roomMin.Z + 0.8);
        var vel = new Vector3d(2.2, 4.1, -1.55);
        var v0 = vel.Length();

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Basketball_GravityNoDrag_Shorter_MechanicalEnergyBoundedAndManyRicochets));
        o.Line("note", "linear drag 0; wall restitution e=1 (control: no normal energy loss at walls)");

        const int steps = 14_000;
        const int substeps = 24;
        const double surfaceEps = 9e-4;
        var reflections = 0;
        var eMin = double.PositiveInfinity;
        var eMax = double.NegativeInfinity;
        for (var i = 0; i < steps; i++)
        {
            reflections += BvhStaticSphereIntegrator.AdvanceWithUniformAccelerationAndLinearDrag(
                world,
                ref pos,
                ref vel,
                BallRadiusM,
                Dt,
                UniformGravityExampleMps2,
                linearDragPerSecond: 0,
                substepsPerStep: substeps,
                surfaceEpsilon: surfaceEps,
                maxReflectionsPerSubstep: 128,
                normalRestitution: 1.0);

            var e = UniformAccelerationEnergy.MechanicalEnergyJ(BallMassKg, vel, UniformGravityExampleMps2, pos);
            eMin = Math.Min(eMin, e);
            eMax = Math.Max(eMax, e);
        }

        o.Results(nameof(Basketball_GravityNoDrag_Shorter_MechanicalEnergyBoundedAndManyRicochets));
        o.Line("reflections_total", reflections);
        o.Line("|v|_initial_m_s", v0);
        o.Line("|v|_final_m_s", vel.Length());
        o.Line("Etot_min_J", eMin);
        o.Line("Etot_max_J", eMax);
        o.Line("Etot_spread_J", eMax - eMin);

        const double s = 0.14;
        var xMin = roomMin.X + BallRadiusM - s;
        var xMax = roomMax.X - BallRadiusM + s;
        var yMin = roomMin.Y + BallRadiusM - s;
        var yMaxBound = roomMax.Y - BallRadiusM + s;
        var zMin = roomMin.Z + BallRadiusM - s;
        var zMax = roomMax.Z - BallRadiusM + s;
        await Assert.That(pos.X).IsGreaterThanOrEqualTo(xMin).And.IsLessThanOrEqualTo(xMax);
        await Assert.That(pos.Y).IsGreaterThanOrEqualTo(yMin).And.IsLessThanOrEqualTo(yMaxBound);
        await Assert.That(pos.Z).IsGreaterThanOrEqualTo(zMin).And.IsLessThanOrEqualTo(zMax);

        await Assert.That(reflections).IsGreaterThan(45);
        await Assert.That(eMax - eMin).IsLessThan(4.0);
    }

    private sealed record ModelKvRow(string Parameter, string Value);

    private sealed record IcRow(string Label, double A, double B, double C);

    private sealed record EndRow(string Label, double A, double B, double C);

    private sealed record MilestoneRow(int Step, double TimeS, double YMaxSoFar, double Speed, double EtotJ, int Reflections)
    {
        internal static readonly IReadOnlyList<string> ColumnOrder =
        [
            nameof(Step),
            nameof(TimeS),
            nameof(YMaxSoFar),
            nameof(Speed),
            nameof(EtotJ),
            nameof(Reflections),
        ];

        internal static MilestoneRow From(int step, double timeS, double yMax, double speed, double eTot, int reflections) =>
            new(
                step,
                PhysicsTraceFormatting.Rd(timeS, 2),
                PhysicsTraceFormatting.Rd(yMax, 3),
                PhysicsTraceFormatting.Rd(speed, 3),
                PhysicsTraceFormatting.Rd(eTot, 2),
                reflections);
    }

    /// <summary>Rounded sparse trace row for readable console tables.</summary>
    private sealed record SampleRow(
        int Step,
        double TimeS,
        double Xm,
        double Ym,
        double Zm,
        double Speed,
        double EtotJ,
        int ReflTotal,
        int ReflSincePrev)
    {
        internal static readonly IReadOnlyList<string> ColumnOrder =
        [
            nameof(Step),
            nameof(TimeS),
            nameof(Xm),
            nameof(Ym),
            nameof(Zm),
            nameof(Speed),
            nameof(EtotJ),
            nameof(ReflTotal),
            nameof(ReflSincePrev),
        ];

        internal static SampleRow From(
            int step,
            double timeS,
            Vector3d pos,
            double speed,
            double eTot,
            int reflTotal,
            int reflSincePrev) =>
            new(
                step,
                PhysicsTraceFormatting.Rd(timeS, 2),
                PhysicsTraceFormatting.Rd(pos.X, 3),
                PhysicsTraceFormatting.Rd(pos.Y, 3),
                PhysicsTraceFormatting.Rd(pos.Z, 3),
                PhysicsTraceFormatting.Rd(speed, 3),
                PhysicsTraceFormatting.Rd(eTot, 2),
                reflTotal,
                reflSincePrev);
    }
}
