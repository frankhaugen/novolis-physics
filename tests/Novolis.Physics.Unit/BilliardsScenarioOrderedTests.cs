using Novolis.Physics.Abstractions;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>
/// Multi-step "billiards room" scenario split across TUnit <c>[DependsOn]</c> tests so earlier stages can fail fast
/// while later steps assume shared static mesh state (see TUnit ordering docs).
/// </summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class BilliardsScenarioOrderedTests
{
    private const double BallRadius = 0.18;

    private const double Dt = 1.0 / 360.0;

    /// <summary>Populated by <see cref="Billiards_Step01_BuildTableMesh_WithCeiling"/>.</summary>
    internal static BvhStaticWorld? SharedTableWorld { get; private set; }

    internal static int SharedRallyReflections { get; private set; }

    [Test]
    public async Task Billiards_Step01_BuildTableMesh_WithCeiling()
    {
        SharedTableWorld = null;
        SharedRallyReflections = 0;

        SharedTableWorld = CollisionTestGeometry.BuildBilliardsTableWithBumpers(
            tableX0: 0,
            tableX1: 16,
            tableZ0: 0,
            tableZ1: 9,
            bumperHeight: 2.6,
            padBeyondTable: 3,
            ceilingY: 3.2);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Billiards_Step01_BuildTableMesh_WithCeiling));
        o.Line("table_x0_m", 0.0);
        o.Line("table_x1_m", 16.0);
        o.Line("table_z0_m", 0.0);
        o.Line("table_z1_m", 9.0);
        o.Line("ceiling_y_m", 3.2);
        o.Line("ball_radius_m", BallRadius);

        await Assert.That(SharedTableWorld).IsNotNull();
        var rayDown = new Ray3d(new Vector3d(8, 2.0, 4.5), new Vector3d(0, -1, 0).Normalized());
        var hitFloor = SharedTableWorld.Raycast(in rayDown, maxDistance: 10, out var floorHit);
        o.Results("Step01 — floor raycast");
        o.Line("hit", hitFloor);
        if (hitFloor)
            o.Line("floor_hit_distance_m", floorHit.Distance);

        await Assert.That(hitFloor).IsTrue();
        await Assert.That(floorHit.Distance).IsGreaterThan(1.0).And.IsLessThan(2.5);
        await Assert.That(floorHit.Normal.Y).IsGreaterThan(0.5);
    }

    [Test]
    [DependsOn(nameof(Billiards_Step01_BuildTableMesh_WithCeiling))]
    public async Task Billiards_Step02_SweepCueTowardLongRail_HitsBeforeLeavingTable()
    {
        var world = SharedTableWorld ?? throw new InvalidOperationException("Expected Step01 to set SharedTableWorld.");
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Billiards_Step02_SweepCueTowardLongRail_HitsBeforeLeavingTable));

        // Stay well above the felt so the sphere does not start penetrated into the floor mesh.
        var center = new Vector3d(12, 0.95, 4.5);
        var displacement = new Vector3d(-14, 0, 0);
        var sphere = new Sphere3d(center, BallRadius);
        var swept = world.SweepSphere(in sphere, displacement, out var hit);

        PhysicsDashboard.ResultsAndTable(
            o,
            "Step02 — sweep toward -X rail",
            new[]
            {
                new SweepSummaryRow("cue_sweep_hit", swept ? 1 : 0, swept ? hit.Distance : 0, swept ? hit.PrimitiveIndex : -1),
            },
            new TableOptions { RightAlignNumericColumns = true },
            tableCaption: "Inflated sphere sweep along cue displacement");

        await Assert.That(swept).IsTrue();
        await Assert.That(hit.Distance).IsGreaterThan(0.01).And.IsLessThan(displacement.Length());
    }

    [Test]
    [DependsOn(nameof(Billiards_Step02_SweepCueTowardLongRail_HitsBeforeLeavingTable))]
    public async Task Billiards_Step03_ElasticRally_InClosedBilliardsRoom()
    {
        var world = SharedTableWorld ?? throw new InvalidOperationException("Expected Step01 to set SharedTableWorld.");
        var pos = new Vector3d(4.0, 0.85, 2.2);
        var vel = new Vector3d(5.2, 1.8, 2.4);
        var v0 = vel.Length();
        var reflections = 0;
        const int steps = 5000;

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Billiards_Step03_ElasticRally_InClosedBilliardsRoom));
        o.Line("ic_pos_m", pos.X, pos.Y, pos.Z);
        o.Line("ic_vel_m_s", vel.X, vel.Y, vel.Z);

        var rows = new List<RallyRow>(capacity: 32);
        for (var i = 0; i < steps; i++)
        {
            reflections += BvhStaticSphereIntegrator.AdvanceOneStep(world, ref pos, ref vel, BallRadius, Dt);
            if (i % 625 == 0 || i == steps - 1)
                rows.Add(new RallyRow(i, pos.X, pos.Y, pos.Z, vel.Length(), reflections));
        }

        // Post-run: outer perimeter walls align with padded table footprint (see CollisionTestGeometry).
        await Assert.That(pos.X).IsGreaterThanOrEqualTo(-0.25).And.IsLessThanOrEqualTo(19.4);
        await Assert.That(pos.Z).IsGreaterThanOrEqualTo(-0.25).And.IsLessThanOrEqualTo(12.4);
        await Assert.That(pos.Y).IsGreaterThanOrEqualTo(-0.05).And.IsLessThanOrEqualTo(3.35);

        SharedRallyReflections = reflections;

        o.Results("Step03 — rally samples");
        o.Table(rows, new TableOptions { MaxRows = 16, RightAlignNumericColumns = true }, caption: "Every 625 steps");
        o.Line("reflections_total", reflections);
        o.Line("|v|_initial_m_s", v0);
        o.Line("|v|_final_m_s", vel.Length());

        await Assert.That(reflections).IsGreaterThan(15);
        await Assert.That(Math.Abs(vel.Length() - v0)).IsLessThanOrEqualTo(0.1);
    }

    [Test]
    [DependsOn(nameof(Billiards_Step03_ElasticRally_InClosedBilliardsRoom))]
    public async Task Billiards_Step04_AssertPriorRallyProducedManyContacts()
    {
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Billiards_Step04_AssertPriorRallyProducedManyContacts));
        o.Results("Step04 — dependency summary");
        o.Line("SharedRallyReflections_from_step03", SharedRallyReflections);
        o.Line("note", "DependsOn Step03 ensures elastic rally ran before this assertion-only check.");

        await Assert.That(SharedRallyReflections).IsGreaterThan(14);
    }

    private sealed record SweepSummaryRow(string Label, int HitFlag, double DistanceAlongSweep, int PrimitiveIndex);

    private sealed record RallyRow(int Step, double X, double Y, double Z, double Speed, int Reflections);
}
