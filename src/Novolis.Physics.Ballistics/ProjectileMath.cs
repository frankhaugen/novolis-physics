using Novolis.Physics.Numerics;

namespace Novolis.Physics.Ballistics;

public static class ProjectileMath
{
    /// <summary>Linear interpolation between <paramref name="a"/> and <paramref name="b"/> by parameter <paramref name="t"/>.</summary>
    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    /// <summary>
    /// Interpolates the first crossing of <c>Y = 0</c> between two states (expects <paramref name="previous"/> with <c>Y ≥ 0</c>
    /// and <paramref name="current"/> with <c>Y &lt; 0</c>).
    /// </summary>
    public static GroundImpact InterpolateGroundImpact(ProjectileState previous, ProjectileState current)
    {
        var denom = previous.Position.Y - current.Position.Y;
        var t = Math.Abs(denom) < 1e-30 ? 0 : previous.Position.Y / denom;
        t = Math.Clamp(t, 0, 1);
        var position = Vector3d.Lerp(previous.Position, current.Position, t);
        var time = Lerp(previous.TimeSeconds, current.TimeSeconds, t);
        var velocity = Vector3d.Lerp(previous.Velocity, current.Velocity, t);
        return new GroundImpact(position, time, velocity);
    }
}
