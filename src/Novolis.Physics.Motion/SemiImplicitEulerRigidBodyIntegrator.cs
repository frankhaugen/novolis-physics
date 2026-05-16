using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Motion;

public sealed class SemiImplicitEulerRigidBodyIntegrator : IIntegrator<RigidBodyState>
{
    public RigidBodyState Step(RigidBodyState body, in ForceSample totalForcesAndTorques, double dtSeconds)
    {
        var invMass = body.Mass > 1e-30 ? 1.0 / body.Mass : 0;
        var accel = totalForcesAndTorques.Force * invMass;
        var vel = body.Velocity + accel * dtSeconds;
        var pos = body.Position + vel * dtSeconds;

        var invI = new Vector3d(
            body.InertiaDiagonalBody.X > 1e-30 ? 1.0 / body.InertiaDiagonalBody.X : 0,
            body.InertiaDiagonalBody.Y > 1e-30 ? 1.0 / body.InertiaDiagonalBody.Y : 0,
            body.InertiaDiagonalBody.Z > 1e-30 ? 1.0 / body.InertiaDiagonalBody.Z : 0);

        var worldTau = totalForcesAndTorques.Torque;
        var bodyTau = InverseRotate(worldTau, body.Orientation);
        var bodyOmega = body.AngularVelocity;
        var bodyAlpha = new Vector3d(bodyTau.X * invI.X, bodyTau.Y * invI.Y, bodyTau.Z * invI.Z);
        var newBodyOmega = bodyOmega + bodyAlpha * dtSeconds;
        var newOrientation = Quaterniond.IntegrateAngularVelocity(body.Orientation, newBodyOmega, dtSeconds);

        return new RigidBodyState(pos, vel, newOrientation, newBodyOmega, body.Mass, body.InertiaDiagonalBody);
    }

    private static Vector3d InverseRotate(Vector3d world, Quaterniond orientation)
    {
        var inv = new Quaterniond(-orientation.X, -orientation.Y, -orientation.Z, orientation.W).Normalized();
        return inv.Rotate(world);
    }
}
