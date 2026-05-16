using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Collision.Simple;

/// <summary>Binary BVH over triangle indices; immutable after construction.</summary>
public sealed class BvhStaticWorld : IStaticWorld
{
    private readonly StaticTriangleMesh _mesh;
    private readonly BvhNode[] _nodes;
    private readonly int[] _triangleOrder;
    private readonly int _rootIndex;

    public BvhStaticWorld(StaticTriangleMesh mesh)
    {
        _mesh = mesh;
        var n = mesh.TriangleCount;
        _triangleOrder = new int[n];
        for (var i = 0; i < n; i++)
        {
            _triangleOrder[i] = i;
        }

        if (n == 0)
        {
            _nodes = [];
            _rootIndex = -1;
            return;
        }

        var nodes = new List<BvhNode>();
        _rootIndex = BuildRecursive(mesh, _triangleOrder, 0, n, nodes, 0);
        _nodes = nodes.ToArray();
    }

    public bool Raycast(in Ray3d ray, double maxDistance, out HitInfo hit)
    {
        hit = default;
        if (_rootIndex < 0)
        {
            return false;
        }

        var bestT = maxDistance;
        var found = false;
        var best = default(HitInfo);
        Traverse(_rootIndex, in ray, maxDistance, ref bestT, ref found, ref best);
        hit = best;
        return found;
    }

    public bool SweepSphere(in Sphere3d sphere, Vector3d displacement, out HitInfo hit)
    {
        hit = default;
        var len = displacement.Length();
        if (len < 1e-30)
        {
            return false;
        }

        var dir = displacement / len;
        var ray = new Ray3d(sphere.Center, dir);
        if (!Raycast(in ray, len + sphere.Radius, out var raw))
        {
            return false;
        }

        var adjusted = raw.Distance - sphere.Radius;
        if (adjusted > len)
        {
            return false;
        }

        // Ray origin already lies within one radius of the hit along the motion direction (shallow
        // penetration / envelope). Treat as a hair-thick forward contact so integrators can
        // depenetrate instead of declaring a miss and stepping through geometry.
        if (adjusted < 0)
        {
            if (adjusted < -sphere.Radius * 0.35)
            {
                return false;
            }

            adjusted = Math.Min(len * 1e-5, len * 0.5);
            if (adjusted < 1e-14)
            {
                adjusted = 1e-14;
            }
        }

        var point = ray.PointAt(adjusted);
        hit = new HitInfo(adjusted, point, raw.Normal, raw.PrimitiveIndex);
        return true;
    }

    public bool SweepCapsule(in Capsule3d capsule, Vector3d displacement, out HitInfo hit)
    {
        var s0 = new Sphere3d(capsule.A, capsule.Radius);
        var s1 = new Sphere3d(capsule.B, capsule.Radius);
        var h0 = SweepSphere(in s0, displacement, out var hit0);
        var h1 = SweepSphere(in s1, displacement, out var hit1);
        if (h0 && h1)
        {
            hit = hit0.Distance <= hit1.Distance ? hit0 : hit1;
            return true;
        }

        if (h0)
        {
            hit = hit0;
            return true;
        }

        if (h1)
        {
            hit = hit1;
            return true;
        }

        hit = default;
        return false;
    }

    private void Traverse(int nodeIndex, in Ray3d ray, double maxDistance, ref double bestT, ref bool found, ref HitInfo best)
    {
        ref readonly var node = ref _nodes[nodeIndex];
        if (!RaySlabIntersect(node.Bounds, ray.Origin, ray.Direction, 0, maxDistance, out var tEnter, out var tExit))
        {
            return;
        }

        if (tExit < 0 || tEnter > bestT)
        {
            return;
        }

        if (node.IsLeaf)
        {
            for (var i = 0; i < node.TriangleCount; i++)
            {
                var tri = _triangleOrder[node.TriangleOrderOffset + i];
                _mesh.GetTriangle(tri, out var v0, out var v1, out var v2);
                if (!TriangleRay.TryHit(in ray, v0, v1, v2, bestT, out var t, out var n))
                {
                    continue;
                }

                found = true;
                bestT = t;
                var p = ray.PointAt(t);
                best = new HitInfo(t, p, n, tri);
            }

            return;
        }

        Traverse(node.LeftChild, in ray, maxDistance, ref bestT, ref found, ref best);
        Traverse(node.RightChild, in ray, maxDistance, ref bestT, ref found, ref best);
    }

    private static bool RaySlabIntersect(
        AxisAlignedBox3d box,
        Vector3d origin,
        Vector3d dir,
        double minT,
        double maxT,
        out double tEnter,
        out double tExit)
    {
        tEnter = minT;
        tExit = maxT;
        for (var axis = 0; axis < 3; axis++)
        {
            var o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var d = axis == 0 ? dir.X : axis == 1 ? dir.Y : dir.Z;
            var min = axis == 0 ? box.Min.X : axis == 1 ? box.Min.Y : box.Min.Z;
            var max = axis == 0 ? box.Max.X : axis == 1 ? box.Max.Y : box.Max.Z;
            if (Math.Abs(d) < 1e-15)
            {
                if (o < min || o > max)
                {
                    return false;
                }

                continue;
            }

            var invD = 1.0 / d;
            var t0 = (min - o) * invD;
            var t1 = (max - o) * invD;
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }

            tEnter = Math.Max(tEnter, t0);
            tExit = Math.Min(tExit, t1);
            if (tEnter > tExit)
            {
                return false;
            }
        }

        return true;
    }

    private static int BuildRecursive(
        StaticTriangleMesh mesh,
        int[] triangleOrder,
        int offset,
        int count,
        List<BvhNode> nodes,
        int depth)
    {
        var bounds = ComputeTriangleListBounds(mesh, triangleOrder, offset, count);
        if (count <= 4 || depth > 24)
        {
            var index = nodes.Count;
            nodes.Add(new BvhNode(bounds, triangleOrderOffset: offset, triangleCount: count, leftChild: 0, rightChild: 0, isLeaf: true));
            return index;
        }

        var span = triangleOrder.AsSpan(offset, count);
        var box = bounds;
        span.Sort((a, b) =>
        {
            mesh.GetTriangle(a, out var va0, out var va1, out var va2);
            mesh.GetTriangle(b, out var vb0, out var vb1, out var vb2);
            var ca = Centroid(va0, va1, va2);
            var cb = Centroid(vb0, vb1, vb2);
            var axis = LongestAxis(box);
            var ka = axis == 0 ? ca.X : axis == 1 ? ca.Y : ca.Z;
            var kb = axis == 0 ? cb.X : axis == 1 ? cb.Y : cb.Z;
            return ka.CompareTo(kb);
        });

        var mid = count / 2;
        if (mid == 0 || mid == count)
        {
            var leaf = nodes.Count;
            nodes.Add(new BvhNode(bounds, offset, count, 0, 0, true));
            return leaf;
        }

        var thisIndex = nodes.Count;
        nodes.Add(default);
        var left = BuildRecursive(mesh, triangleOrder, offset, mid, nodes, depth + 1);
        var right = BuildRecursive(mesh, triangleOrder, offset + mid, count - mid, nodes, depth + 1);
        nodes[thisIndex] = new BvhNode(bounds, 0, 0, left, right, false);
        return thisIndex;
    }

    private static AxisAlignedBox3d ComputeTriangleListBounds(StaticTriangleMesh mesh, int[] order, int offset, int count)
    {
        var b = mesh.TriangleBounds(order[offset]);
        for (var i = 1; i < count; i++)
        {
            var tb = mesh.TriangleBounds(order[offset + i]);
            b = Union(b, tb);
        }

        return b;
    }

    private static Vector3d Centroid(Vector3d a, Vector3d b, Vector3d c) => (a + b + c) / 3.0;

    private static int LongestAxis(AxisAlignedBox3d b)
    {
        var e = b.Max - b.Min;
        if (e.X >= e.Y && e.X >= e.Z)
        {
            return 0;
        }

        return e.Y >= e.Z ? 1 : 2;
    }

    private static AxisAlignedBox3d Union(AxisAlignedBox3d a, AxisAlignedBox3d b) =>
        AxisAlignedBox3d.FromMinMax(
            new Vector3d(Math.Min(a.Min.X, b.Min.X), Math.Min(a.Min.Y, b.Min.Y), Math.Min(a.Min.Z, b.Min.Z)),
            new Vector3d(Math.Max(a.Max.X, b.Max.X), Math.Max(a.Max.Y, b.Max.Y), Math.Max(a.Max.Z, b.Max.Z)));

    private readonly struct BvhNode
    {
        public BvhNode(
            AxisAlignedBox3d bounds,
            int triangleOrderOffset,
            int triangleCount,
            int leftChild,
            int rightChild,
            bool isLeaf)
        {
            Bounds = bounds;
            TriangleOrderOffset = triangleOrderOffset;
            TriangleCount = triangleCount;
            LeftChild = leftChild;
            RightChild = rightChild;
            IsLeaf = isLeaf;
        }

        public AxisAlignedBox3d Bounds { get; }
        public int TriangleOrderOffset { get; }
        public int TriangleCount { get; }
        public int LeftChild { get; }
        public int RightChild { get; }
        public bool IsLeaf { get; }
    }
}
