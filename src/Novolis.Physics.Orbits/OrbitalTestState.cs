using Novolis.Physics.Numerics;

namespace Novolis.Physics.Orbits;

/// <summary>Canonical initial conditions for the elliptical Earth test scenario (periapsis on +X, velocity along +Y).</summary>
public static class OrbitalTestState
{
    public static OrbitState CreatePeriapsisState()
    {
        var rp = OrbitalTestConstants.PeriapsisRadius;
        var vp = OrbitalTestConstants.PeriapsisSpeed;
        return new OrbitState(new Vector3d(rp, 0, 0), new Vector3d(0, vp, 0));
    }
}
