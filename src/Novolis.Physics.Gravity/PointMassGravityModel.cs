using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Gravity;

public sealed class PointMassGravityModel : IForceModel<RigidBodyState, PointMassField>
{
    public ForceSample Evaluate(RigidBodyState body, PointMassField environment, double timeSeconds)
    {
        var total = Vector3d.Zero;
        var span = environment.Sources.Span;
        for (var i = 0; i < span.Length; i++)
        {
            var (pos, gm) = span[i];
            var r = pos - body.Position;
            var distSq = r.LengthSquared();
            if (distSq < 1e-12)
            {
                continue;
            }

            var dist = Math.Sqrt(distSq);
            var dir = r / dist;
            var magnitude = gm / distSq;
            total += dir * magnitude;
        }

        return new ForceSample(total * body.Mass, Vector3d.Zero);
    }
}
