using Novolis.Physics.Collision.Simple;
using Novolis.Physics.Numerics;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>
/// Documents approximate <see cref="BvhStaticWorld.SweepSphere"/> behavior (see INTEGRATION.md §3).
/// </summary>
public sealed class SweepLimitationScenarioTests
{
    [Test]
    public async Task SweepSphere_LargeStepOvershoot_MissesWhileSubStepsHit()
    {
        var verts = new[]
        {
            new Vector3d(0, 0, 0),
            new Vector3d(10, 0, 0),
            new Vector3d(0, 0, 10),
        };
        var world = new BvhStaticWorld(new StaticTriangleMesh(verts, new[] { 0, 1, 2 }));
        var sphere = new Sphere3d(new Vector3d(1, 5.0, 1), radius: 0.15);
        var largeDisplacement = new Vector3d(0, -4, 0);

        var largeHit = world.SweepSphere(in sphere, largeDisplacement, out _);

        var subStep = new Vector3d(0, -0.2, 0);
        var probe = sphere;
        var anySubHit = false;
        for (var i = 0; i < 30 && !anySubHit; i++)
        {
            if (world.SweepSphere(in probe, subStep, out var subHit))
            {
                anySubHit = true;
                await Assert.That(subHit.Distance).IsGreaterThan(0).And.IsLessThan(subStep.Length());
            }
            else
            {
                probe = new Sphere3d(probe.Center + subStep, probe.Radius);
            }
        }

        await Assert.That(largeHit).IsFalse();
        await Assert.That(anySubHit).IsTrue();
    }
}
