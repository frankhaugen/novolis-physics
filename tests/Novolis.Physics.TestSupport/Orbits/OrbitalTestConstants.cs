namespace Novolis.Physics.TestSupport.Orbits;

/// <summary>Earth-centered elliptical test orbit (planar motion in XY, Z and Vz zero).</summary>
public static class OrbitalTestConstants
{
    public const double Mu = 3.986004418e14;

    public const double SemiMajorAxis = 10_000_000.0;

    public const double Eccentricity = 0.25;

    public static double PeriapsisRadius => SemiMajorAxis * (1.0 - Eccentricity);

    public static double ApoapsisRadius => SemiMajorAxis * (1.0 + Eccentricity);

    /// <summary>Kepler period for semi-major axis <see cref="SemiMajorAxis"/> (s).</summary>
    public static double Period => 2.0 * Math.PI * Math.Sqrt(Math.Pow(SemiMajorAxis, 3.0) / Mu);

    /// <summary>Speed at periapsis from vis-viva (m/s).</summary>
    public static double PeriapsisSpeed => Math.Sqrt(Mu * (1.0 + Eccentricity) / PeriapsisRadius);

    /// <summary>Speed at apoapsis from vis-viva (m/s).</summary>
    public static double ApoapsisSpeed => Math.Sqrt(Mu * (1.0 - Eccentricity) / ApoapsisRadius);

    /// <summary>Specific orbital energy E = v²/2 − μ/r = −μ/(2a) (J/kg).</summary>
    public static double SpecificEnergyReference => -Mu / (2.0 * SemiMajorAxis);

    /// <summary>Magnitude of specific angular momentum |h| = √(μ a (1−e²)) (m²/s); planar IC gives h along ±Z.</summary>
    public static double SpecificAngularMomentumMagnitude => Math.Sqrt(Mu * SemiMajorAxis * (1.0 - Eccentricity * Eccentricity));
}
