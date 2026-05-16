using Novolis.Physics.Numerics;

namespace Novolis.Physics.Collision.Simple;

internal static class TriangleRay
{
    /// <summary>Möller–Trumbore ray–triangle test; <paramref name="maxDistance"/> along ray direction.</summary>
    public static bool TryHit(in Ray3d ray, Vector3d v0, Vector3d v1, Vector3d v2, double maxDistance, out double distance, out Vector3d normal)
    {
        distance = 0;
        var e1 = v1 - v0;
        var e2 = v2 - v0;
        normal = Vector3d.Cross(e1, e2).Normalized();

        const double epsilon = 1e-12;
        var h = Vector3d.Cross(ray.Direction, e2);
        var a = Vector3d.Dot(e1, h);
        if (a > -epsilon && a < epsilon)
        {
            return false;
        }

        var f = 1.0 / a;
        var s = ray.Origin - v0;
        var u = f * Vector3d.Dot(s, h);
        if (u < 0.0 || u > 1.0)
        {
            return false;
        }

        var q = Vector3d.Cross(s, e1);
        var v = f * Vector3d.Dot(ray.Direction, q);
        if (v < 0.0 || u + v > 1.0)
        {
            return false;
        }

        var t = f * Vector3d.Dot(e2, q);
        if (t <= epsilon || t > maxDistance)
        {
            return false;
        }

        distance = t;
        if (Vector3d.Dot(normal, ray.Direction) > 0)
        {
            normal = -normal;
        }

        return true;
    }
}
