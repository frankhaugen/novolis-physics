namespace Novolis.Physics.Ballistics;

/// <summary>
/// Uniform downward gravity (−Y) and optional air density for quadratic drag.
/// <see cref="GravityMetersPerSecondSquared"/> is a positive magnitude; acceleration is <c>(0, −g, 0)</c> m/s².
/// </summary>
public readonly record struct ProjectileBallisticEnvironment(
    double GravityMetersPerSecondSquared,
    double AirDensityKgPerM3);
