using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Aerodynamics;

/// <summary>Quadratic drag plus a crude lift term along (forward × relative velocity).</summary>
/// <remarks>Time-invariant; simulation time passed to <see cref="Evaluate"/> is ignored.</remarks>
public sealed class SimpleLiftDragModel : IForceModel<RigidBodyState, SimpleAeroEnvironment>
{
    public ForceSample Evaluate(RigidBodyState body, SimpleAeroEnvironment environment, double timeSeconds)
    {
        var rho = environment.Atmosphere.DensityAtAltitude(environment.AltitudeMeters);
        var v = body.Velocity - environment.WindWorld;
        var speed = v.Length();
        if (speed < 1e-6 || rho < 1e-30)
        {
            return ForceSample.Zero;
        }

        var q = 0.5 * rho * environment.ReferenceAreaM2 * speed * speed;
        var drag = -environment.DragCoefficient * q * (v / speed);
        var liftAxis = Vector3d.Cross(environment.LiftReferenceForwardWorld, v);
        var liftMag = liftAxis.Length();
        var lift = liftMag > 1e-12
            ? environment.LiftCoefficient * q * (liftAxis / liftMag)
            : Vector3d.Zero;
        return new ForceSample(drag + lift, Vector3d.Zero);
    }
}
