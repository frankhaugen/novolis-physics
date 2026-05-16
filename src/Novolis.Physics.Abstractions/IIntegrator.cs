namespace Novolis.Physics.Abstractions;

public interface IIntegrator<TBody>
{
    TBody Step(TBody body, in ForceSample totalForcesAndTorques, double dtSeconds);
}
