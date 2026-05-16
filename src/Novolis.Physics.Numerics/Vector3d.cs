namespace Novolis.Physics.Numerics;

/// <summary>Right-handed 3D vector in SI meters (or any consistent length unit).</summary>
public readonly struct Vector3d(double x, double y, double z) : IEquatable<Vector3d>
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Z { get; } = z;

    public static Vector3d Zero => new(0, 0, 0);

    public static Vector3d Lerp(Vector3d a, Vector3d b, double t) => a + (b - a) * t;

    public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3d operator -(Vector3d a, Vector3d b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3d operator -(Vector3d v) => new(-v.X, -v.Y, -v.Z);

    public static Vector3d operator *(Vector3d v, double s) => new(v.X * s, v.Y * s, v.Z * s);

    public static Vector3d operator *(double s, Vector3d v) => v * s;

    public static Vector3d operator /(Vector3d v, double s) => new(v.X / s, v.Y / s, v.Z / s);

    public static double Dot(Vector3d a, Vector3d b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3d Cross(Vector3d a, Vector3d b) =>
        new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    public double LengthSquared() => Dot(this, this);

    public double Length() => Math.Sqrt(LengthSquared());

    public Vector3d Normalized()
    {
        var len = Length();
        return len > 1e-30 ? this / len : Zero;
    }

    public bool Equals(Vector3d other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object? obj) => obj is Vector3d other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public static bool operator ==(Vector3d left, Vector3d right) => left.Equals(right);

    public static bool operator !=(Vector3d left, Vector3d right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y}, {Z})";
}
