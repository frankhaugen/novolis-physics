using Novolis.Physics.Numerics;

namespace Novolis.Physics.Collision.Simple;

/// <summary>Indexed triangle soup; immutable after construction.</summary>
public sealed class StaticTriangleMesh
{
    public StaticTriangleMesh(Vector3d[] vertices, int[] triangleIndices)
    {
        if (triangleIndices.Length % 3 != 0)
        {
            throw new ArgumentException("Triangle index count must be a multiple of 3.", nameof(triangleIndices));
        }

        Vertices = vertices;
        TriangleIndices = triangleIndices;
        TriangleCount = triangleIndices.Length / 3;
    }

    public Vector3d[] Vertices { get; }
    public int[] TriangleIndices { get; }
    public int TriangleCount { get; }

    public void GetTriangle(int triangleIndex, out Vector3d v0, out Vector3d v1, out Vector3d v2)
    {
        var i = triangleIndex * 3;
        v0 = Vertices[TriangleIndices[i]];
        v1 = Vertices[TriangleIndices[i + 1]];
        v2 = Vertices[TriangleIndices[i + 2]];
    }

    public AxisAlignedBox3d TriangleBounds(int triangleIndex)
    {
        GetTriangle(triangleIndex, out var v0, out var v1, out var v2);
        var box = AxisAlignedBox3d.FromMinMax(v0, v0);
        box = AxisAlignedBox3d.Expand(box, v1);
        box = AxisAlignedBox3d.Expand(box, v2);
        return box;
    }

    public AxisAlignedBox3d MeshBounds()
    {
        if (Vertices.Length == 0)
        {
            return new AxisAlignedBox3d(Vector3d.Zero, Vector3d.Zero);
        }

        var b = AxisAlignedBox3d.FromMinMax(Vertices[0], Vertices[0]);
        foreach (var v in Vertices)
        {
            b = AxisAlignedBox3d.Expand(b, v);
        }

        return b;
    }
}
