namespace Novolis.Physics.Numerics;

/// <summary>Line segment from <see cref="A"/> to <see cref="B"/> with radius around the segment.</summary>
public readonly struct Capsule3d(Vector3d a, Vector3d b, double radius)
{
    public Vector3d A { get; } = a;
    public Vector3d B { get; } = b;
    public double Radius { get; } = radius;
}
