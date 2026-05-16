using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Ballistics;

/// <summary>Raycast and sweep helpers over <see cref="IStaticWorld"/> for projectile-sized queries.</summary>
public static class BallisticsQueries
{
    public static bool LineOfSight(IStaticWorld world, in Ray3d ray, double maxDistance, out HitInfo hit) =>
        world.Raycast(in ray, maxDistance, out hit);

    public static bool SweepProjectileSphere(
        IStaticWorld world,
        in Sphere3d sphere,
        Vector3d displacement,
        out HitInfo hit) =>
        world.SweepSphere(in sphere, displacement, out hit);
}
