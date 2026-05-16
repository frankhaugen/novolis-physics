using System.Numerics;
using Novolis.Physics.Numerics;
using Novolis.Physics.Orbits;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>Elliptical Earth two-body checks: planar motion as <see cref="Vector3d"/> with Z = Vz = 0, leapfrog + inverse-square acceleration.</summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class EllipticalOrbitTwoBodyTests
{
    private const double DtSeconds = 1.0;

    /// <summary>Closed orbit: compare final state to periapsis IC (m / m/s).</summary>
    private const double ClosedOrbitPositionToleranceM = 2000.0;

    private const double ClosedOrbitVelocityToleranceMs = 2.0;

    private const double EnergyRelativeTolerance = 1e-5;

    private const double AngularMomentumZRelativeTolerance = 1e-6;

    /// <summary>Half orbit: apoapsis geometry (m / m/s).</summary>
    private const double HalfOrbitRadiusToleranceM = 5000.0;

    private const double HalfOrbitSpeedToleranceMs = 5.0;

    private const double HalfOrbitYPositionToleranceM = 5000.0;

    private const double ScalarVectorizedMaxAbsDiff = 1e-9;

    private static int StepCount(double durationSeconds) =>
        (int)Math.Floor((durationSeconds + 1e-9) / DtSeconds);

    [Test]
    public async Task OneOrbit_ReturnsNearInitialState()
    {
        var ic = OrbitalTestState.CreatePeriapsisState();
        var mu = OrbitalTestConstants.Mu;
        var duration = OrbitalTestConstants.Period;
        var steps = StepCount(duration);
        var final = CentralOrbitSimulator.SimulateFor(ic, duration, DtSeconds, KernelMode.Scalar, mu);
        var dp = (final.Position - ic.Position).Length();
        var dv = (final.Velocity - ic.Velocity).Length();
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(OneOrbit_ReturnsNearInitialState));
        o.Line("kernel", nameof(KernelMode.Scalar));
        o.Line("dt_s", DtSeconds);
        o.Line("duration_s", duration);
        o.Line("steps", steps);
        o.Line("mu_m3_s2", mu);
        o.Line("ic_position_m", ic.Position.X, ic.Position.Y, ic.Position.Z);
        o.Line("ic_velocity_m_s", ic.Velocity.X, ic.Velocity.Y, ic.Velocity.Z);
        o.Results(nameof(OneOrbit_ReturnsNearInitialState) + " — pre-assert");
        o.Line("|dpos|_m", dp);
        o.Line("|dvel|_m_s", dv);
        o.Line(
            "assert",
            FormattableString.Invariant(
                $"|dpos|<={ClosedOrbitPositionToleranceM} m, |dvel|<={ClosedOrbitVelocityToleranceMs} m/s"));
        try
        {
            await Assert.That(dp).IsLessThanOrEqualTo(ClosedOrbitPositionToleranceM);
            await Assert.That(dv).IsLessThanOrEqualTo(ClosedOrbitVelocityToleranceMs);
        }
        catch (Exception ex)
        {
            DumpOrbitFailure(
                NovolisPhysicsTestTrace.Out,
                nameof(OneOrbit_ReturnsNearInitialState),
                ex,
                ic,
                final,
                observed: FormattableString.Invariant($"|dpos|={dp:G17} |dvel|={dv:G17}"),
                expected: FormattableString.Invariant(
                    $"|dpos|<={ClosedOrbitPositionToleranceM} |dvel|<={ClosedOrbitVelocityToleranceMs}"),
                KernelMode.Scalar,
                DtSeconds,
                steps,
                duration,
                mu);
            throw;
        }
    }

    [Test]
    public async Task HalfOrbit_ReachesApoapsis()
    {
        var ic = OrbitalTestState.CreatePeriapsisState();
        var mu = OrbitalTestConstants.Mu;
        var duration = OrbitalTestConstants.Period * 0.5;
        var steps = StepCount(duration);
        var ra = OrbitalTestConstants.ApoapsisRadius;
        var va = OrbitalTestConstants.ApoapsisSpeed;
        var final = CentralOrbitSimulator.SimulateFor(ic, duration, DtSeconds, KernelMode.Scalar, mu);
        var r = final.Position.Length();
        var v = final.Velocity.Length();
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(HalfOrbit_ReachesApoapsis));
        o.Line("kernel", nameof(KernelMode.Scalar));
        o.Line("dt_s", DtSeconds);
        o.Line("duration_s", duration);
        o.Line("steps", steps);
        o.Line("mu_m3_s2", mu);
        o.Line("ra_target_m", ra);
        o.Line("va_target_m_s", va);
        o.Line("ic_position_m", ic.Position.X, ic.Position.Y, ic.Position.Z);
        o.Line("ic_velocity_m_s", ic.Velocity.X, ic.Velocity.Y, ic.Velocity.Z);
        o.Results(nameof(HalfOrbit_ReachesApoapsis) + " — pre-assert");
        o.Line("final_position_m", final.Position.X, final.Position.Y, final.Position.Z);
        o.Line("r_m", r);
        o.Line("v_m_s", v);
        o.Line("dr_vs_ra_m", r - ra);
        o.Line("dv_vs_va_m_s", v - va);
        try
        {
            await Assert.That(final.Position.X).IsLessThan(0);
            await Assert.That(Math.Abs(final.Position.Y)).IsLessThanOrEqualTo(HalfOrbitYPositionToleranceM);
            await Assert.That(Math.Abs(r - ra)).IsLessThanOrEqualTo(HalfOrbitRadiusToleranceM);
            await Assert.That(Math.Abs(v - va)).IsLessThanOrEqualTo(HalfOrbitSpeedToleranceMs);
        }
        catch (Exception ex)
        {
            DumpOrbitFailure(
                NovolisPhysicsTestTrace.Out,
                nameof(HalfOrbit_ReachesApoapsis),
                ex,
                ic,
                final,
                observed: FormattableString.Invariant(
                    $"r={r:G17} ra={ra:G17} dr={r - ra:G17} v={v:G17} va={va:G17} dv={v - va:G17} X={final.Position.X:G17} Y={final.Position.Y:G17}"),
                expected: FormattableString.Invariant(
                    $"X<0 |Y|<={HalfOrbitYPositionToleranceM} |r-ra|<={HalfOrbitRadiusToleranceM} |v-va|<={HalfOrbitSpeedToleranceMs}"),
                KernelMode.Scalar,
                DtSeconds,
                steps,
                duration,
                mu);
            throw;
        }
    }

    [Test]
    public async Task Energy_IsApproximatelyConserved()
    {
        var ic = OrbitalTestState.CreatePeriapsisState();
        var mu = OrbitalTestConstants.Mu;
        var duration = OrbitalTestConstants.Period;
        var steps = StepCount(duration);
        var e0 = OrbitalMath.SpecificOrbitalEnergy(ic.Position, ic.Velocity, mu);
        var denom = Math.Max(Math.Abs(e0), 1e-300);
        var final = CentralOrbitSimulator.SimulateFor(ic, duration, DtSeconds, KernelMode.Scalar, mu);
        var e1 = OrbitalMath.SpecificOrbitalEnergy(final.Position, final.Velocity, mu);
        var rel = Math.Abs(e1 - e0) / denom;
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(Energy_IsApproximatelyConserved));
        o.Line("kernel", nameof(KernelMode.Scalar));
        o.Line("dt_s", DtSeconds);
        o.Line("duration_s", duration);
        o.Line("steps", steps);
        o.Line("mu_m3_s2", mu);
        o.Line("ic_position_m", ic.Position.X, ic.Position.Y, ic.Position.Z);
        o.Line("ic_velocity_m_s", ic.Velocity.X, ic.Velocity.Y, ic.Velocity.Z);
        o.Results(nameof(Energy_IsApproximatelyConserved) + " — pre-assert");
        o.Line("E0_J_per_kg", e0);
        o.Line("E1_J_per_kg", e1);
        o.Line("rel_dE", rel);
        o.Line("assert_rel_dE_max", EnergyRelativeTolerance);
        try
        {
            await Assert.That(rel).IsLessThanOrEqualTo(EnergyRelativeTolerance);
        }
        catch (Exception ex)
        {
            DumpOrbitFailure(
                NovolisPhysicsTestTrace.Out,
                nameof(Energy_IsApproximatelyConserved),
                ex,
                ic,
                final,
                observed: FormattableString.Invariant($"rel|dE|={rel:G17} E0={e0:G17} E1={e1:G17}"),
                expected: FormattableString.Invariant($"rel|dE|<={EnergyRelativeTolerance} (baseline |E0|={denom:G17})"),
                KernelMode.Scalar,
                DtSeconds,
                steps,
                duration,
                mu,
                e0,
                e1);
            throw;
        }
    }

    [Test]
    public async Task AngularMomentum_IsApproximatelyConserved()
    {
        var ic = OrbitalTestState.CreatePeriapsisState();
        var mu = OrbitalTestConstants.Mu;
        var duration = OrbitalTestConstants.Period;
        var steps = StepCount(duration);
        var h0z = OrbitalMath.SpecificAngularMomentumVector(ic.Position, ic.Velocity).Z;
        var denom = Math.Max(Math.Abs(h0z), 1e-300);
        var final = CentralOrbitSimulator.SimulateFor(ic, duration, DtSeconds, KernelMode.Scalar, mu);
        var h1z = OrbitalMath.SpecificAngularMomentumVector(final.Position, final.Velocity).Z;
        var rel = Math.Abs(h1z - h0z) / denom;
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(AngularMomentum_IsApproximatelyConserved));
        o.Line("kernel", nameof(KernelMode.Scalar));
        o.Line("dt_s", DtSeconds);
        o.Line("duration_s", duration);
        o.Line("steps", steps);
        o.Line("mu_m3_s2", mu);
        o.Line("ic_position_m", ic.Position.X, ic.Position.Y, ic.Position.Z);
        o.Line("ic_velocity_m_s", ic.Velocity.X, ic.Velocity.Y, ic.Velocity.Z);
        o.Results(nameof(AngularMomentum_IsApproximatelyConserved) + " — pre-assert");
        o.Line("hz0_m2_per_s", h0z);
        o.Line("hz1_m2_per_s", h1z);
        o.Line("rel_dhz", rel);
        o.Line("assert_rel_dhz_max", AngularMomentumZRelativeTolerance);
        try
        {
            await Assert.That(rel).IsLessThanOrEqualTo(AngularMomentumZRelativeTolerance);
        }
        catch (Exception ex)
        {
            DumpOrbitFailure(
                NovolisPhysicsTestTrace.Out,
                nameof(AngularMomentum_IsApproximatelyConserved),
                ex,
                ic,
                final,
                observed: FormattableString.Invariant($"rel|dhz|={rel:G17} hz0={h0z:G17} hz1={h1z:G17}"),
                expected: FormattableString.Invariant($"rel|dhz|<={AngularMomentumZRelativeTolerance} (baseline |hz0|={denom:G17})"),
                KernelMode.Scalar,
                DtSeconds,
                steps,
                duration,
                mu,
                hzInitial: h0z,
                hzFinal: h1z);
            throw;
        }
    }

    [Test]
    public async Task ScalarAndVectorized_ProduceEquivalentResults()
    {
        Skip.Unless(
            Vector.IsHardwareAccelerated && Vector<double>.Count >= 4,
            "Portable Vector<double> central acceleration skipped: Vector.IsHardwareAccelerated is false or Vector<double>.Count < 4.");

        var ic = OrbitalTestState.CreatePeriapsisState();
        var mu = OrbitalTestConstants.Mu;
        var duration = OrbitalTestConstants.Period;
        var steps = StepCount(duration);
        var scalar = CentralOrbitSimulator.SimulateFor(ic, duration, DtSeconds, KernelMode.Scalar, mu);
        var vectorized = CentralOrbitSimulator.SimulateFor(ic, duration, DtSeconds, KernelMode.Vectorized, mu);
        var o = NovolisPhysicsTestTrace.Out;
        o.Section(nameof(ScalarAndVectorized_ProduceEquivalentResults));
        o.Line("Vector.IsHardwareAccelerated", Vector.IsHardwareAccelerated);
        o.Line("Vector_double_Count", Vector<double>.Count);
        o.Line("dt_s", DtSeconds);
        o.Line("duration_s", duration);
        o.Line("steps", steps);
        o.Line("mu_m3_s2", mu);
        o.Line("ic_position_m", ic.Position.X, ic.Position.Y, ic.Position.Z);
        o.Line("ic_velocity_m_s", ic.Velocity.X, ic.Velocity.Y, ic.Velocity.Z);
        o.Results(nameof(ScalarAndVectorized_ProduceEquivalentResults) + " — final states");
        o.Table(
            new[]
            {
                new OrbitFinalTraceRow(
                    "Scalar",
                    scalar.Position.X,
                    scalar.Position.Y,
                    scalar.Position.Z,
                    scalar.Velocity.X,
                    scalar.Velocity.Y,
                    scalar.Velocity.Z),
                new OrbitFinalTraceRow(
                    "Vectorized",
                    vectorized.Position.X,
                    vectorized.Position.Y,
                    vectorized.Position.Z,
                    vectorized.Velocity.X,
                    vectorized.Velocity.Y,
                    vectorized.Velocity.Z),
            },
            new TableOptions { RightAlignNumericColumns = true },
            caption: "Position (m) and velocity (m/s) after same duration");
        var dpx = Math.Abs(scalar.Position.X - vectorized.Position.X);
        var dpy = Math.Abs(scalar.Position.Y - vectorized.Position.Y);
        var dpz = Math.Abs(scalar.Position.Z - vectorized.Position.Z);
        var dvx = Math.Abs(scalar.Velocity.X - vectorized.Velocity.X);
        var dvy = Math.Abs(scalar.Velocity.Y - vectorized.Velocity.Y);
        var dvz = Math.Abs(scalar.Velocity.Z - vectorized.Velocity.Z);
        o.Results(nameof(ScalarAndVectorized_ProduceEquivalentResults) + " — max abs component deltas");
        o.Line("|dpx|", dpx);
        o.Line("|dpy|", dpy);
        o.Line("|dpz|", dpz);
        o.Line("|dvx|", dvx);
        o.Line("|dvy|", dvy);
        o.Line("|dvz|", dvz);
        o.Line("assert_max_each", ScalarVectorizedMaxAbsDiff);
        try
        {
            await Assert.That(Math.Abs(scalar.Position.X - vectorized.Position.X)).IsLessThanOrEqualTo(ScalarVectorizedMaxAbsDiff);
            await Assert.That(Math.Abs(scalar.Position.Y - vectorized.Position.Y)).IsLessThanOrEqualTo(ScalarVectorizedMaxAbsDiff);
            await Assert.That(Math.Abs(scalar.Position.Z - vectorized.Position.Z)).IsLessThanOrEqualTo(ScalarVectorizedMaxAbsDiff);
            await Assert.That(Math.Abs(scalar.Velocity.X - vectorized.Velocity.X)).IsLessThanOrEqualTo(ScalarVectorizedMaxAbsDiff);
            await Assert.That(Math.Abs(scalar.Velocity.Y - vectorized.Velocity.Y)).IsLessThanOrEqualTo(ScalarVectorizedMaxAbsDiff);
            await Assert.That(Math.Abs(scalar.Velocity.Z - vectorized.Velocity.Z)).IsLessThanOrEqualTo(ScalarVectorizedMaxAbsDiff);
        }
        catch (Exception ex)
        {
            DumpParityFailure(NovolisPhysicsTestTrace.Out, ex, ic, scalar, vectorized, duration, steps, mu);
            throw;
        }
    }

    private sealed record OrbitFinalTraceRow(string Mode, double Px, double Py, double Pz, double Vx, double Vy, double Vz);

    private static void DumpParityFailure(
        TestOutput o,
        Exception ex,
        OrbitState ic,
        OrbitState scalar,
        OrbitState vectorized,
        double duration,
        int steps,
        double mu)
    {
        o.Section("Elliptical orbit — SIMD parity failure");
        o.Line("assertion", nameof(ScalarAndVectorized_ProduceEquivalentResults));
        o.Line("exception", ex.GetType().FullName ?? ex.GetType().Name);
        o.Line("message", ex.Message);
        o.Line("Vector.IsHardwareAccelerated", Vector.IsHardwareAccelerated ? "true" : "false");
        o.Line("Vector<double>.Count", Vector<double>.Count);
        o.Line("dt", DtSeconds);
        o.Line("steps", steps);
        o.Line("duration_s", duration);
        o.Line("mu", mu);
        o.Line("ic_pos", FormattableString.Invariant($"{ic.Position.X:G17},{ic.Position.Y:G17},{ic.Position.Z:G17}"));
        o.Line("ic_vel", FormattableString.Invariant($"{ic.Velocity.X:G17},{ic.Velocity.Y:G17},{ic.Velocity.Z:G17}"));
        o.Line("scalar_pos", FormattableString.Invariant($"{scalar.Position.X:G17},{scalar.Position.Y:G17},{scalar.Position.Z:G17}"));
        o.Line("vector_pos", FormattableString.Invariant($"{vectorized.Position.X:G17},{vectorized.Position.Y:G17},{vectorized.Position.Z:G17}"));
        o.Line("dpos", FormattableString.Invariant($"{scalar.Position.X - vectorized.Position.X:G17},{scalar.Position.Y - vectorized.Position.Y:G17},{scalar.Position.Z - vectorized.Position.Z:G17}"));
        o.Line("scalar_vel", FormattableString.Invariant($"{scalar.Velocity.X:G17},{scalar.Velocity.Y:G17},{scalar.Velocity.Z:G17}"));
        o.Line("vector_vel", FormattableString.Invariant($"{vectorized.Velocity.X:G17},{vectorized.Velocity.Y:G17},{vectorized.Velocity.Z:G17}"));
        o.Line("dvel", FormattableString.Invariant($"{scalar.Velocity.X - vectorized.Velocity.X:G17},{scalar.Velocity.Y - vectorized.Velocity.Y:G17},{scalar.Velocity.Z - vectorized.Velocity.Z:G17}"));
        var eS = OrbitalMath.SpecificOrbitalEnergy(scalar.Position, scalar.Velocity, mu);
        var eV = OrbitalMath.SpecificOrbitalEnergy(vectorized.Position, vectorized.Velocity, mu);
        o.Line("energy_scalar", eS);
        o.Line("energy_vector", eV);
        o.Line("energy_delta", eS - eV);
        var hzS = OrbitalMath.SpecificAngularMomentumVector(scalar.Position, scalar.Velocity).Z;
        var hzV = OrbitalMath.SpecificAngularMomentumVector(vectorized.Position, vectorized.Velocity).Z;
        o.Line("hz_scalar", hzS);
        o.Line("hz_vector", hzV);
        o.Line("hz_delta", hzS - hzV);
    }

    private static void DumpOrbitFailure(
        TestOutput o,
        string assertion,
        Exception ex,
        OrbitState ic,
        OrbitState final,
        string observed,
        string expected,
        KernelMode kernel,
        double dt,
        int steps,
        double durationSeconds,
        double mu,
        double? energyInitial = null,
        double? energyFinal = null,
        double? hzInitial = null,
        double? hzFinal = null)
    {
        o.Section("Elliptical orbit — assertion failure diagnostics");
        o.Line("assertion", assertion);
        o.Line("exception", ex.GetType().FullName ?? ex.GetType().Name);
        o.Line("message", ex.Message);
        o.Line("kernel", kernel.ToString());
        o.Line("dt", dt);
        o.Line("steps", steps);
        o.Line("duration_s", durationSeconds);
        o.Line("mu", mu);
        o.Line("ic_pos", FormattableString.Invariant($"{ic.Position.X:G17},{ic.Position.Y:G17},{ic.Position.Z:G17}"));
        o.Line("ic_vel", FormattableString.Invariant($"{ic.Velocity.X:G17},{ic.Velocity.Y:G17},{ic.Velocity.Z:G17}"));
        o.Line("final_pos", FormattableString.Invariant($"{final.Position.X:G17},{final.Position.Y:G17},{final.Position.Z:G17}"));
        o.Line("final_vel", FormattableString.Invariant($"{final.Velocity.X:G17},{final.Velocity.Y:G17},{final.Velocity.Z:G17}"));
        o.Line("observed", observed);
        o.Line("expected", expected);

        var e0 = energyInitial ?? OrbitalMath.SpecificOrbitalEnergy(ic.Position, ic.Velocity, mu);
        var e1 = energyFinal ?? OrbitalMath.SpecificOrbitalEnergy(final.Position, final.Velocity, mu);
        o.Line("E0_J_per_kg", e0);
        o.Line("E1_J_per_kg", e1);
        if (Math.Abs(e0) > 0)
            o.Line("rel_dE", Math.Abs(e1 - e0) / Math.Abs(e0));

        var z0 = hzInitial ?? OrbitalMath.SpecificAngularMomentumVector(ic.Position, ic.Velocity).Z;
        var z1 = hzFinal ?? OrbitalMath.SpecificAngularMomentumVector(final.Position, final.Velocity).Z;
        o.Line("hz0_m2_per_s", z0);
        o.Line("hz1_m2_per_s", z1);
        if (Math.Abs(z0) > 0)
            o.Line("rel_dhz", Math.Abs(z1 - z0) / Math.Abs(z0));

        o.Line("Vector.IsHardwareAccelerated", Vector.IsHardwareAccelerated ? "true" : "false");
        o.Line("Vector<double>.Count", Vector<double>.Count);
    }
}
