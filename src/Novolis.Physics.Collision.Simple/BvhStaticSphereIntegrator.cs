using Novolis.Physics.Abstractions;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Collision.Simple;

/// <summary>
/// Integrates a sphere against a static <see cref="BvhStaticWorld"/> with optional uniform acceleration,
/// optional isotropic linear drag, and Newton restitution at contacts.
/// </summary>
public static class BvhStaticSphereIntegrator
{
    /// <summary>Separation after contact to reduce immediate re-hit / numeric creep through thin geometry.</summary>
    public const double DefaultSurfaceEpsilon = 4e-4;

    /// <summary>Isotropic linear drag acceleration <c>a = −k v</c> with <paramref name="dragPerSecond"/> in 1/s.</summary>
    public static Vector3d LinearDragAcceleration(in Vector3d velocityMps, double dragPerSecond) =>
        -dragPerSecond * velocityMps;

    /// <summary>Same as the <c>double dtSeconds</c> overload using <paramref name="dt"/>.<see cref="TimeSpan.TotalSeconds"/>.</summary>
    public static int AdvanceOneStep(
        BvhStaticWorld world,
        ref Vector3d centerM,
        ref Vector3d velocityMps,
        double radiusM,
        TimeSpan dt,
        double surfaceEpsilon = DefaultSurfaceEpsilon,
        int maxReflectionsPerStep = 96,
        double normalRestitution = 1.0) =>
        AdvanceOneStep(
            world,
            ref centerM,
            ref velocityMps,
            radiusM,
            ToStepSeconds(dt),
            surfaceEpsilon,
            maxReflectionsPerStep,
            normalRestitution);

    /// <summary>
    /// Advances the sphere for <paramref name="dtSeconds"/> with velocity updated at contacts using
    /// <see cref="SphereContactKinematics.ReflectWithRestitution"/>. Returns the number of wall resolutions
    /// (including grazing / multi-hit substeps), not a macroscopic bounce count.
    /// </summary>
    public static int AdvanceOneStep(
        BvhStaticWorld world,
        ref Vector3d centerM,
        ref Vector3d velocityMps,
        double radiusM,
        double dtSeconds,
        double surfaceEpsilon = DefaultSurfaceEpsilon,
        int maxReflectionsPerStep = 96,
        double normalRestitution = 1.0)
    {
        var dtRem = dtSeconds;
        var reflections = 0;
        var grazingStuck = 0;
        while (dtRem > 1e-14 && reflections < maxReflectionsPerStep)
        {
            var displacement = velocityMps * dtRem;
            var len = displacement.Length();
            if (len < 1e-30)
                break;

            var sphere = new Sphere3d(centerM, radiusM);
            if (!world.SweepSphere(in sphere, displacement, out var hit))
            {
                centerM += displacement;
                break;
            }

            var frac = hit.Distance / len;
            if (frac <= 1e-12)
            {
                if (++grazingStuck > 32)
                {
                    centerM += displacement * 1e-8;
                    break;
                }

                centerM = hit.Point + hit.Normal * surfaceEpsilon;
                velocityMps = SphereContactKinematics.ReflectWithRestitution(velocityMps, hit.Normal, normalRestitution);
                reflections++;
                continue;
            }

            grazingStuck = 0;

            dtRem *= 1.0 - frac;
            centerM = hit.Point + hit.Normal * surfaceEpsilon;
            velocityMps = SphereContactKinematics.ReflectWithRestitution(velocityMps, hit.Normal, normalRestitution);
            reflections++;
        }

        return reflections;
    }

    /// <summary>Same as the <c>double dtSeconds</c> overload using <paramref name="dt"/>.<see cref="TimeSpan.TotalSeconds"/>.</summary>
    public static int AdvanceWithUniformAccelerationAndLinearDrag(
        BvhStaticWorld world,
        ref Vector3d centerM,
        ref Vector3d velocityMps,
        double radiusM,
        TimeSpan dt,
        Vector3d uniformAccelerationMps2,
        double linearDragPerSecond,
        int substepsPerStep = 10,
        double surfaceEpsilon = DefaultSurfaceEpsilon,
        int maxReflectionsPerSubstep = 64,
        double normalRestitution = 1.0) =>
        AdvanceWithUniformAccelerationAndLinearDrag(
            world,
            ref centerM,
            ref velocityMps,
            radiusM,
            ToStepSeconds(dt),
            uniformAccelerationMps2,
            linearDragPerSecond,
            substepsPerStep,
            surfaceEpsilon,
            maxReflectionsPerSubstep,
            normalRestitution);

    /// <summary>
    /// Each sub-step: optional linear drag on velocity, then uniform acceleration, then sphere sweep integration.
    /// </summary>
    public static int AdvanceWithUniformAccelerationAndLinearDrag(
        BvhStaticWorld world,
        ref Vector3d centerM,
        ref Vector3d velocityMps,
        double radiusM,
        double dtSeconds,
        Vector3d uniformAccelerationMps2,
        double linearDragPerSecond,
        int substepsPerStep = 10,
        double surfaceEpsilon = DefaultSurfaceEpsilon,
        int maxReflectionsPerSubstep = 64,
        double normalRestitution = 1.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(substepsPerStep, 1);
        var sub = dtSeconds / substepsPerStep;
        var reflections = 0;
        for (var j = 0; j < substepsPerStep; j++)
        {
            if (linearDragPerSecond > 0)
            {
                var d = linearDragPerSecond * sub;
                velocityMps = d < 0.999 ? velocityMps * (1.0 - d) : Vector3d.Zero;
            }

            velocityMps += uniformAccelerationMps2 * sub;
            reflections += AdvanceOneStep(
                world,
                ref centerM,
                ref velocityMps,
                radiusM,
                sub,
                surfaceEpsilon,
                maxReflectionsPerSubstep,
                normalRestitution);
        }

        return reflections;
    }

    private static double ToStepSeconds(TimeSpan dt)
    {
        if (dt < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(dt), "Integration step must be non-negative.");
        return dt.TotalSeconds;
    }
}
