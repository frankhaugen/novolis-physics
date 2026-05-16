using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Gravity;

/// <remarks>Time-invariant; <paramref name="timeSeconds"/> is ignored.</remarks>
public sealed class PatchedConicGravityModel : IForceModel<RigidBodyState, PatchedConicPairField>
{
    public ForceSample Evaluate(RigidBodyState body, PatchedConicPairField environment, double timeSeconds)
    {
        var toPrimary = body.Position - environment.PrimaryPosition;
        var insideSoi = toPrimary.Length() <= environment.PrimarySphereOfInfluenceRadius;
        var source = insideSoi ? environment.PrimaryPosition : environment.SecondaryPosition;
        var gm = insideSoi ? environment.PrimaryGm : environment.SecondaryGm;
        var r = source - body.Position;
        var distSq = r.LengthSquared();
        if (distSq < 1e-12)
        {
            return ForceSample.Zero;
        }

        var dist = Math.Sqrt(distSq);
        var dir = r / dist;
        var f = dir * (gm / distSq) * body.Mass;
        return new ForceSample(f, Vector3d.Zero);
    }
}
