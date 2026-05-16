using Novolis.Physics.Abstractions;
using Novolis.Physics.Ballistics;
using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Numerics;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class CollisionSweepScenarioTests
{
    [Test]
    public async Task SweepProjectileSphere_HitsGroundTriangle()
    {
        var verts = new[]
        {
            new Vector3d(0, 0, 0),
            new Vector3d(10, 0, 0),
            new Vector3d(0, 0, 10),
        };
        var mesh = new StaticTriangleMesh(verts, new[] { 0, 1, 2 });
        var world = new BvhStaticWorld(mesh);
        var sphere = new Sphere3d(new Vector3d(1, 2.0, 1), radius: 0.15);
        var displacement = new Vector3d(0, -4, 0);
        var hit = BallisticsQueries.SweepProjectileSphere(world, in sphere, displacement, out var info);

        var o = NovolisPhysicsTestTrace.Out;
        PhysicsDashboard.SectionAndTable(
            o,
            "BallisticsQueries.SweepProjectileSphere",
            new[]
            {
                new SweepRow("sphereCenter.Y", sphere.Center.Y),
                new SweepRow("displacement.Y", displacement.Y),
                new SweepRow("hit", hit ? 1 : 0),
                new SweepRow("distance", info.Distance),
            },
            new TableOptions { MaxCellWidth = 20, RightAlignNumericColumns = true },
            tableCaption: "sweeps inflated ray along displacement; expect hit before full travel");

        await Assert.That(hit).IsTrue();
        await Assert.That(info.Distance).IsGreaterThan(0).And.IsLessThan(displacement.Length());
    }

    [Test]
    public async Task Raycast_Miss_ReturnsFalse()
    {
        var verts = new[]
        {
            new Vector3d(0, 0, 0),
            new Vector3d(1, 0, 0),
            new Vector3d(0, 0, 1),
        };
        var world = new BvhStaticWorld(new StaticTriangleMesh(verts, new[] { 0, 1, 2 }));
        var ray = new Ray3d(new Vector3d(5, 5, 5), new Vector3d(0, 1, 0).Normalized());
        var hit = world.Raycast(in ray, maxDistance: 100, out _);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Raycast_Miss_ReturnsFalse));
        o.Line("ray_origin_m", ray.Origin.X, ray.Origin.Y, ray.Origin.Z);
        o.Line("ray_dir", ray.Direction.X, ray.Direction.Y, ray.Direction.Z);
        o.Line("maxDistance_m", 100);
        o.Results(nameof(Raycast_Miss_ReturnsFalse) + " — outcome");
        o.Line("hit_expected", false);
        o.Line("hit_actual", hit);

        await Assert.That(hit).IsFalse();
    }

    private sealed record SweepRow(string Label, double Value);
}
