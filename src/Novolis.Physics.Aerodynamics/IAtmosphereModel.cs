namespace Novolis.Physics.Aerodynamics;

public interface IAtmosphereModel
{
    /// <summary>Mass density (kg/m³) at the given altitude above a reference surface (meters).</summary>
    double DensityAtAltitude(double altitudeMeters);
}
