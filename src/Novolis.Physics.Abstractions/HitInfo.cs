using Novolis.Physics.Numerics;

namespace Novolis.Physics.Abstractions;

/// <summary>Ray or sweep hit: parametric distance, contact point, outward normal, and primitive index.</summary>
public readonly struct HitInfo
{
    public HitInfo(double distance, Vector3d point, Vector3d normal, int primitiveIndex)
    {
        Distance = distance;
        Point = point;
        Normal = normal.Normalized();
        PrimitiveIndex = primitiveIndex;
    }

    public double Distance { get; }
    public Vector3d Point { get; }
    public Vector3d Normal { get; }
    public int PrimitiveIndex { get; }
}
