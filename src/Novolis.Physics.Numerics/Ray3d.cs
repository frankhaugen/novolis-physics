namespace Novolis.Physics.Numerics;

/// <summary>Ray from <see cref="Origin"/> along <see cref="Direction"/> (prefer unit direction for distance semantics).</summary>
public readonly struct Ray3d(Vector3d origin, Vector3d direction)
{
    public Vector3d Origin { get; } = origin;

    /// <summary>Should be unit direction for distance semantics in raycasts.</summary>
    public Vector3d Direction { get; } = direction;

    public Vector3d PointAt(double distance) => Origin + Direction * distance;
}
