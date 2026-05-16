namespace Novolis.Physics.Aerodynamics;

/// <summary>Air density as a function of altitude above a reference surface.</summary>
public interface IAtmosphereModel
{
    /// <summary>Mass density (kg/m³) at the given altitude above a reference surface (meters).</summary>
    double DensityAtAltitude(double altitudeMeters);
}
