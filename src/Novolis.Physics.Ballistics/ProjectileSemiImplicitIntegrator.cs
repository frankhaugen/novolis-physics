using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Ballistics;

public sealed class ProjectileSemiImplicitIntegrator : IIntegrator<ProjectileState>
{
    public ProjectileState Step(ProjectileState body, in ForceSample totalForcesAndTorques, double dtSeconds)
    {
        var invM = body.MassKg > 1e-30 ? 1.0 / body.MassKg : 0;
        var a = totalForcesAndTorques.Force * invM;
        var v = body.Velocity + a * dtSeconds;
        var p = body.Position + v * dtSeconds;
        return new ProjectileState(p, v, body.MassKg, body.TimeSeconds);
    }
}
