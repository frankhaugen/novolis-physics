using Novolis.Physics.Numerics;

namespace Novolis.Physics.Ballistics;

/// <summary>Interpolated crossing of the <c>Y = 0</c> plane (descending).</summary>
public readonly struct GroundImpact(Vector3d position, double timeSeconds, Vector3d velocity)
{
    public Vector3d Position { get; } = position;
    public double TimeSeconds { get; } = timeSeconds;
    public Vector3d Velocity { get; } = velocity;

    public double ImpactSpeed => Velocity.Length();
}
