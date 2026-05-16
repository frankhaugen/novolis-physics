namespace Novolis.Physics.Abstractions;

public interface IForceModel<in TBody, in TEnvironment>
{
    ForceSample Evaluate(TBody body, TEnvironment environment, double timeSeconds);
}
