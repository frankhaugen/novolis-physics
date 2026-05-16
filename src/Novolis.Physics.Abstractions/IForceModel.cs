namespace Novolis.Physics.Abstractions;

/// <summary>Computes force and torque contributions for one physical effect.</summary>
public interface IForceModel<in TBody, in TEnvironment>
{
    /// <summary>
    /// Evaluates forces at simulation time <paramref name="timeSeconds"/> (seconds).
    /// Time-invariant models may ignore <paramref name="timeSeconds"/>.
    /// </summary>
    ForceSample Evaluate(TBody body, TEnvironment environment, double timeSeconds);
}
