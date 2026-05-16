namespace Novolis.Physics.Abstractions;

public interface IContactResolver<TBody>
{
    TBody Resolve(in TBody body, in HitInfo hit, double dtSeconds);
}
