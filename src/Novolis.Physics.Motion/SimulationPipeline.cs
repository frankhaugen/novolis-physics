using Novolis.Physics.Abstractions;

namespace Novolis.Physics.Motion;

public sealed class SimulationPipeline<TBody, TEnvironment>
{
    private readonly IForceModel<TBody, TEnvironment>[] _forces;
    private readonly IIntegrator<TBody> _integrator;

    public SimulationPipeline(IIntegrator<TBody> integrator, params IForceModel<TBody, TEnvironment>[] forces)
    {
        _integrator = integrator;
        _forces = forces;
    }

    public IReadOnlyList<IForceModel<TBody, TEnvironment>> Forces => _forces;

    /// <summary>
    /// Sums all force models and integrates one fixed step.
    /// Does not advance <paramref name="timeSeconds"/>; the caller must pass <c>timeSeconds + dtSeconds</c> on the next step.
    /// </summary>
    public TBody Step(TBody body, TEnvironment environment, double dtSeconds, double timeSeconds = 0)
    {
        var total = ForceSample.Zero;
        foreach (var force in _forces)
        {
            total += force.Evaluate(body, environment, timeSeconds);
        }

        return _integrator.Step(body, in total, dtSeconds);
    }
}
