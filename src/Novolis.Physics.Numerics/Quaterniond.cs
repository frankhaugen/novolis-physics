namespace Novolis.Physics.Numerics;

/// <summary>Unit quaternion (W + Xi + Yj + Zk) for orientation; not fully normalized in all ops—call <see cref="Normalized"/> before use if needed.</summary>
public readonly struct Quaterniond(double x, double y, double z, double w) : IEquatable<Quaterniond>
{
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Z { get; } = z;
    public double W { get; } = w;

    public static Quaterniond Identity => new(0, 0, 0, 1);

    public static Quaterniond FromAxisAngle(Vector3d axis, double radians)
    {
        var a = axis.Normalized();
        var half = radians * 0.5;
        var s = Math.Sin(half);
        return new Quaterniond(a.X * s, a.Y * s, a.Z * s, Math.Cos(half)).Normalized();
    }

    public Quaterniond Normalized()
    {
        var len = Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        return len > 1e-30 ? new Quaterniond(X / len, Y / len, Z / len, W / len) : Identity;
    }

    public static Quaterniond operator *(Quaterniond a, Quaterniond b) =>
        new(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

    public Vector3d Rotate(Vector3d v)
    {
        var qv = new Quaterniond(v.X, v.Y, v.Z, 0);
        var conj = new Quaterniond(-X, -Y, -Z, W);
        var r = this * qv * conj;
        return new Vector3d(r.X, r.Y, r.Z);
    }

    public bool Equals(Quaterniond other) =>
        X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);

    public override bool Equals(object? obj) => obj is Quaterniond other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    public static bool operator ==(Quaterniond left, Quaterniond right) => left.Equals(right);

    public static bool operator !=(Quaterniond left, Quaterniond right) => !left.Equals(right);

    public static Quaterniond Scale(Quaterniond q, double s) => new(q.X * s, q.Y * s, q.Z * s, q.W * s);

    /// <summary>First-order quaternion integration: q̇ = ½ q ⊗ (0,ω).</summary>
    public static Quaterniond IntegrateAngularVelocity(Quaterniond q, Vector3d omega, double dt)
    {
        var wq = new Quaterniond(omega.X, omega.Y, omega.Z, 0);
        var qDot = Scale(q * wq, 0.5);
        return new Quaterniond(
                q.X + qDot.X * dt,
                q.Y + qDot.Y * dt,
                q.Z + qDot.Z * dt,
                q.W + qDot.W * dt)
            .Normalized();
    }
}
