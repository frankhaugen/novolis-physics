namespace Novolis.Physics.Numerics;

public readonly struct Ray3d(Vector3d origin, Vector3d direction)
{
    public Vector3d Origin { get; } = origin;

    /// <summary>Should be unit direction for distance semantics in raycasts.</summary>
    public Vector3d Direction { get; } = direction;

    public Vector3d PointAt(double distance) => Origin + Direction * distance;
}
