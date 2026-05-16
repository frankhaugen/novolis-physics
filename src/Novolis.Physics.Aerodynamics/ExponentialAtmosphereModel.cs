namespace Novolis.Physics.Aerodynamics;

/// <summary>ρ(h) = ρ₀ · exp(-h / H).</summary>
public sealed class ExponentialAtmosphereModel : IAtmosphereModel
{
    public ExponentialAtmosphereModel(double seaLevelDensityKgPerM3, double scaleHeightMeters)
    {
        SeaLevelDensity = seaLevelDensityKgPerM3;
        ScaleHeightMeters = scaleHeightMeters;
    }

    public double SeaLevelDensity { get; }
    public double ScaleHeightMeters { get; }

    public double DensityAtAltitude(double altitudeMeters) =>
        SeaLevelDensity * Math.Exp(-altitudeMeters / ScaleHeightMeters);
}
