namespace Novolis.Physics.Orbits;

/// <summary>Headless fixed-step propagation for central-body tests.</summary>
public static class CentralOrbitSimulator
{
    /// <summary>
    /// Integrates with fixed <paramref name="deltaSeconds"/> for
    /// <c>floor((durationSeconds + 1e-9) / deltaSeconds)</c> leapfrog steps (no partial final sub-step).
    /// </summary>
    public static OrbitState SimulateFor(OrbitState initial, double durationSeconds, double deltaSeconds, KernelMode mode, double mu)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(durationSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(deltaSeconds, 0);

        var steps = (int)Math.Floor((durationSeconds + 1e-9) / deltaSeconds);
        var soa = new LeapfrogCentralBodySoA(mu, bodyCount: 1);
        soa.SetState(0, initial.Position, initial.Velocity);
        for (var s = 0; s < steps; s++)
            soa.Step(deltaSeconds, mode);

        return soa.GetState(0);
    }
}
