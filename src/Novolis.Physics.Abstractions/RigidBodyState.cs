using Novolis.Physics.Numerics;

namespace Novolis.Physics.Abstractions;

/// <summary>Diagonal inertia in body space; torque in world space is mapped to body for integration.</summary>
public readonly struct RigidBodyState
{
    public RigidBodyState(
        Vector3d position,
        Vector3d velocity,
        Quaterniond orientation,
        Vector3d angularVelocity,
        double mass,
        Vector3d inertiaDiagonalBody)
    {
        Position = position;
        Velocity = velocity;
        Orientation = orientation.Normalized();
        AngularVelocity = angularVelocity;
        Mass = mass;
        InertiaDiagonalBody = inertiaDiagonalBody;
    }

    public Vector3d Position { get; init; }
    public Vector3d Velocity { get; init; }
    public Quaterniond Orientation { get; init; }
    public Vector3d AngularVelocity { get; init; }
    public double Mass { get; init; }
    public Vector3d InertiaDiagonalBody { get; init; }
}
