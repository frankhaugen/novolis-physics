namespace Novolis.Physics.Abstractions;

/// <summary>Advances <typeparamref name="TBody"/> given summed forces and torques for one fixed timestep.</summary>
public interface IIntegrator<TBody>
{
    TBody Step(TBody body, in ForceSample totalForcesAndTorques, double dtSeconds);
}
