using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Ballistics;

/// <summary>Point mass with quadratic drag F = -½ ρ Cd A |v| v in world space.</summary>
/// <remarks>Time-invariant; <paramref name="timeSeconds"/> is ignored.</remarks>
public sealed class ProjectileQuadraticDragModel : IForceModel<ProjectileState, ProjectileDragEnvironment>
{
    private readonly ProjectileProfile _profile;

    public ProjectileQuadraticDragModel(ProjectileProfile profile) => _profile = profile;

    public ForceSample Evaluate(ProjectileState body, ProjectileDragEnvironment environment, double timeSeconds)
    {
        var v = body.Velocity;
        var speed = v.Length();
        if (speed < 1e-9 || environment.AirDensityKgPerM3 < 1e-30)
        {
            return ForceSample.Zero;
        }

        var q = 0.5 * environment.AirDensityKgPerM3 * _profile.DragCoefficient * _profile.ReferenceAreaM2 * speed * speed;
        var f = -q * (v / speed);
        return new ForceSample(f, Vector3d.Zero);
    }
}
