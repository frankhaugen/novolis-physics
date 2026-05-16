namespace Novolis.Physics.Orbits;

public enum KernelMode
{
    Scalar,

    /// <summary>Uses <see cref="System.Numerics.Vector{T}"/> for central acceleration; requires hardware acceleration at test time.</summary>
    Vectorized,
}
