namespace Novolis.Physics.Orbits;

/// <summary>Headless fixed-step propagation for central-body tests.</summary>
public static class CentralOrbitSimulator
{
    /// <summary>
    /// Integrates with fixed <paramref name="deltaSeconds"/> for
    /// <c>floor((durationSeconds + 1e-9) / deltaSeconds)</c> leapfrog steps (no partial final sub-step).
    /// Allocates a new <see cref="LeapfrogCentralBodySoA"/> for each call; prefer
    /// <see cref="SimulateFor(OrbitState, LeapfrogCentralBodySoA, int, double, double, KernelMode)"/> in hot loops.
    /// </summary>
    public static OrbitState SimulateFor(OrbitState initial, double durationSeconds, double deltaSeconds, KernelMode mode, double mu)
    {
        var soa = new LeapfrogCentralBodySoA(mu, bodyCount: 1);
        return SimulateFor(initial, soa, bodyIndex: 0, durationSeconds, deltaSeconds, mode);
    }

    /// <summary>
    /// Integrates using a pre-allocated <paramref name="integrator"/> (no per-call SoA allocation).
    /// Resets body <paramref name="bodyIndex"/> from <paramref name="initial"/> before stepping.
    /// </summary>
    public static OrbitState SimulateFor(
        OrbitState initial,
        LeapfrogCentralBodySoA integrator,
        int bodyIndex,
        double durationSeconds,
        double deltaSeconds,
        KernelMode mode)
    {
        ArgumentNullException.ThrowIfNull(integrator);
        ArgumentOutOfRangeException.ThrowIfNegative(durationSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(deltaSeconds, 0);

        var steps = (int)Math.Floor((durationSeconds + 1e-9) / deltaSeconds);
        integrator.SetState(bodyIndex, initial.Position, initial.Velocity);
        for (var s = 0; s < steps; s++)
            integrator.Step(deltaSeconds, mode);

        return integrator.GetState(bodyIndex);
    }
}
