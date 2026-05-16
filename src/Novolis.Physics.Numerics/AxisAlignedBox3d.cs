namespace Novolis.Physics.Numerics;

public readonly struct AxisAlignedBox3d(Vector3d min, Vector3d max)
{
    public Vector3d Min { get; } = min;
    public Vector3d Max { get; } = max;

    public static AxisAlignedBox3d FromMinMax(Vector3d min, Vector3d max) =>
        new(
            new Vector3d(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y), Math.Min(min.Z, max.Z)),
            new Vector3d(Math.Max(min.X, max.X), Math.Max(min.Y, max.Y), Math.Max(min.Z, max.Z)));

    public static AxisAlignedBox3d Expand(AxisAlignedBox3d box, Vector3d point) =>
        FromMinMax(
            new Vector3d(Math.Min(box.Min.X, point.X), Math.Min(box.Min.Y, point.Y), Math.Min(box.Min.Z, point.Z)),
            new Vector3d(Math.Max(box.Max.X, point.X), Math.Max(box.Max.Y, point.Y), Math.Max(box.Max.Z, point.Z)));

    public bool Intersects(AxisAlignedBox3d other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X
        && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y
        && Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;

    public bool Contains(Vector3d p) =>
        p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y && p.Z >= Min.Z && p.Z <= Max.Z;
}
