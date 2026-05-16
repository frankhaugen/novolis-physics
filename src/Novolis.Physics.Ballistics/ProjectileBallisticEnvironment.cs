namespace Novolis.Physics.Ballistics;

/// <summary>Uniform downward gravity (−Y) and optional air density for quadratic drag.</summary>
public readonly record struct ProjectileBallisticEnvironment(
    /// <summary>Positive magnitude; acceleration is <c>(0, −g, 0)</c> m/s².</summary>
    double GravityMetersPerSecondSquared,
    double AirDensityKgPerM3);
