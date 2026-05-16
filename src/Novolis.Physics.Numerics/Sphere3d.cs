namespace Novolis.Physics.Numerics;

public readonly struct Sphere3d(Vector3d center, double radius)
{
    public Vector3d Center { get; } = center;
    public double Radius { get; } = radius;
}
