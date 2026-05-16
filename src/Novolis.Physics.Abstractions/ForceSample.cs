using Novolis.Physics.Numerics;

namespace Novolis.Physics.Abstractions;

/// <summary>World-space force (N) and torque (N·m) from one effect; summed before integration.</summary>
public readonly record struct ForceSample(Vector3d Force, Vector3d Torque)
{
    public static ForceSample Zero => new(Vector3d.Zero, Vector3d.Zero);

    public static ForceSample operator +(ForceSample a, ForceSample b) =>
        new(a.Force + b.Force, a.Torque + b.Torque);

    public static ForceSample operator *(ForceSample a, double s) => new(a.Force * s, a.Torque * s);
}
