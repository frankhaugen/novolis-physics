using Novolis.Physics.Numerics;

namespace Novolis.Physics.Gravity;

/// <summary>Two-body patched conic lite: primary inside SOI, otherwise secondary point mass.</summary>
public readonly struct PatchedConicPairField
{
    public PatchedConicPairField(
        Vector3d primaryPosition,
        double primaryGm,
        double primarySphereOfInfluenceRadius,
        Vector3d secondaryPosition,
        double secondaryGm)
    {
        PrimaryPosition = primaryPosition;
        PrimaryGm = primaryGm;
        PrimarySphereOfInfluenceRadius = primarySphereOfInfluenceRadius;
        SecondaryPosition = secondaryPosition;
        SecondaryGm = secondaryGm;
    }

    public Vector3d PrimaryPosition { get; }
    public double PrimaryGm { get; }
    public double PrimarySphereOfInfluenceRadius { get; }
    public Vector3d SecondaryPosition { get; }
    public double SecondaryGm { get; }
}
