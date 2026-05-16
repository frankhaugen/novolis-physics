using Novolis.Physics.Numerics;

namespace Novolis.Physics.Abstractions;

/// <summary>Query-only static geometry: raycasts and approximate swept primitives.</summary>
public interface IStaticWorld
{
    bool Raycast(in Ray3d ray, double maxDistance, out HitInfo hit);

    /// <summary>Approximate swept sphere vs static mesh (radius-inflated raycast; corners may be wrong).</summary>
    bool SweepSphere(in Sphere3d sphere, Vector3d displacement, out HitInfo hit);

    /// <summary>Conservative capsule sweep (samples segment endpoints as spheres).</summary>
    bool SweepCapsule(in Capsule3d capsule, Vector3d displacement, out HitInfo hit);
}
