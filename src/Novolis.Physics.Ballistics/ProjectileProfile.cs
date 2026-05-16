namespace Novolis.Physics.Ballistics;

public readonly struct ProjectileProfile
{
    public ProjectileProfile(double massKg, double referenceAreaM2, double dragCoefficient)
    {
        MassKg = massKg;
        ReferenceAreaM2 = referenceAreaM2;
        DragCoefficient = dragCoefficient;
    }

    public double MassKg { get; }
    public double ReferenceAreaM2 { get; }
    public double DragCoefficient { get; }
}
