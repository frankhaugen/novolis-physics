namespace Novolis.Physics.Ballistics;

/// <summary>Air density for ballistic drag (caller supplies ρ from atmosphere or constant).</summary>
public readonly struct ProjectileDragEnvironment
{
    public ProjectileDragEnvironment(double airDensityKgPerM3)
    {
        AirDensityKgPerM3 = airDensityKgPerM3;
    }

    public double AirDensityKgPerM3 { get; }
}
