using System.Numerics;
using Novolis.Physics.Numerics;

namespace Novolis.Physics.Orbits;

/// <summary>
/// Fixed-step leapfrog (kick–drift–kick) for Newtonian central gravity using minimal SoA storage.
/// Planar tests use Z = Vz = 0; acceleration uses full 3D inverse square.
/// </summary>
public sealed class LeapfrogCentralBodySoA
{
    private readonly double _mu;
    private readonly int _n;
    private readonly double[] _px;
    private readonly double[] _py;
    private readonly double[] _pz;
    private readonly double[] _vx;
    private readonly double[] _vy;
    private readonly double[] _vz;

    public LeapfrogCentralBodySoA(double mu, int bodyCount)
    {
        if (bodyCount < 1)
            throw new ArgumentOutOfRangeException(nameof(bodyCount));

        _mu = mu;
        _n = bodyCount;
        _px = new double[_n];
        _py = new double[_n];
        _pz = new double[_n];
        _vx = new double[_n];
        _vy = new double[_n];
        _vz = new double[_n];
    }

    public void SetState(int index, Vector3d position, Vector3d velocity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= _n)
            throw new ArgumentOutOfRangeException(nameof(index));

        _px[index] = position.X;
        _py[index] = position.Y;
        _pz[index] = position.Z;
        _vx[index] = velocity.X;
        _vy[index] = velocity.Y;
        _vz[index] = velocity.Z;
    }

    public OrbitState GetState(int index) =>
        new(
            new Vector3d(_px[index], _py[index], _pz[index]),
            new Vector3d(_vx[index], _vy[index], _vz[index]));

    public void Step(double deltaSeconds, KernelMode mode)
    {
        for (var i = 0; i < _n; i++)
        {
            if (mode == KernelMode.Scalar)
                StepScalar(i, deltaSeconds);
            else
                StepVectorized(i, deltaSeconds);
        }
    }

    private void StepScalar(int i, double dt)
    {
        ref var px = ref _px[i];
        ref var py = ref _py[i];
        ref var pz = ref _pz[i];
        ref var vx = ref _vx[i];
        ref var vy = ref _vy[i];
        ref var vz = ref _vz[i];

        var a0 = OrbitalMath.CentralAcceleration(new Vector3d(px, py, pz), _mu);
        vx += 0.5 * a0.X * dt;
        vy += 0.5 * a0.Y * dt;
        vz += 0.5 * a0.Z * dt;

        px += vx * dt;
        py += vy * dt;
        pz += vz * dt;

        var a1 = OrbitalMath.CentralAcceleration(new Vector3d(px, py, pz), _mu);
        vx += 0.5 * a1.X * dt;
        vy += 0.5 * a1.Y * dt;
        vz += 0.5 * a1.Z * dt;
    }

    private void StepVectorized(int i, double dt)
    {
        ref var px = ref _px[i];
        ref var py = ref _py[i];
        ref var pz = ref _pz[i];
        ref var vx = ref _vx[i];
        ref var vy = ref _vy[i];
        ref var vz = ref _vz[i];

        CentralAccelerationVectorizedLanes(px, py, pz, _mu, out var ax0, out var ay0, out var az0);
        vx += 0.5 * ax0 * dt;
        vy += 0.5 * ay0 * dt;
        vz += 0.5 * az0 * dt;

        px += vx * dt;
        py += vy * dt;
        pz += vz * dt;

        CentralAccelerationVectorizedLanes(px, py, pz, _mu, out var ax1, out var ay1, out var az1);
        vx += 0.5 * ax1 * dt;
        vy += 0.5 * ay1 * dt;
        vz += 0.5 * az1 * dt;
    }

    /// <summary>First three lanes are x,y,z; remaining lanes zero so <see cref="Vector{T}.Count"/> is satisfied.</summary>
    internal static void CentralAccelerationVectorizedLanes(double px, double py, double pz, double mu, out double ax, out double ay, out double az)
    {
        Span<double> lanes = stackalloc double[Vector<double>.Count];
        lanes[0] = px;
        lanes[1] = py;
        lanes[2] = pz;
        for (var k = 3; k < lanes.Length; k++)
            lanes[k] = 0;

        var p = new Vector<double>(lanes);
        var p2 = p * p;
        var r2 = 0.0;
        for (var j = 0; j < 3; j++)
            r2 += p2[j];

        var invR = 1.0 / Math.Sqrt(r2);
        var invR3 = invR / r2;
        var acc = p * new Vector<double>(-mu * invR3);
        ax = acc[0];
        ay = acc[1];
        az = acc[2];
    }
}
