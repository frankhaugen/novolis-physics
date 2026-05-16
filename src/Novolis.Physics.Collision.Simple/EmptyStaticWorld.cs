using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Collision.Simple;

public sealed class EmptyStaticWorld : IStaticWorld
{
    public bool Raycast(in Ray3d ray, double maxDistance, out HitInfo hit)
    {
        hit = default;
        return false;
    }

    public bool SweepSphere(in Sphere3d sphere, Vector3d displacement, out HitInfo hit)
    {
        hit = default;
        return false;
    }

    public bool SweepCapsule(in Capsule3d capsule, Vector3d displacement, out HitInfo hit)
    {
        hit = default;
        return false;
    }
}
