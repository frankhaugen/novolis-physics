using Novolis.Physics.Numerics;

namespace Novolis.Physics.Orbits;

/// <summary>Two-body Newtonian point mass in 3D: <c>a = −μ r / |r|³</c> (reduces to planar when Z and Vz are zero).</summary>
public static class OrbitalMath
{
    public static Vector3d CentralAcceleration(Vector3d position, double mu)
    {
        var r2 = position.LengthSquared();
        if (r2 < 1e-24)
            return Vector3d.Zero;

        var invR = 1.0 / Math.Sqrt(r2);
        var invR3 = invR / r2;
        return position * (-mu * invR3);
    }

    /// <summary>Specific orbital energy ε = v²/2 − μ/r (J/kg).</summary>
    public static double SpecificOrbitalEnergy(Vector3d position, Vector3d velocity, double mu) =>
        0.5 * velocity.LengthSquared() - mu / position.Length();

    /// <summary>Specific angular momentum vector h = r × v (m²/s).</summary>
    public static Vector3d SpecificAngularMomentumVector(Vector3d position, Vector3d velocity) =>
        Vector3d.Cross(position, velocity);
}
