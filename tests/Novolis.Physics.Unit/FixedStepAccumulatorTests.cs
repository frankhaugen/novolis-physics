using System.Linq;
using Novolis.Physics.Motion;
using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class FixedStepAccumulatorTests
{
    [Test]
    public async Task AddTimeAndDrain_ExactMultiple_ReturnsCorrectCountAndDrainsCarry()
    {
        var acc = new FixedStepAccumulator(0.25);
        var dts = new List<double>();
        var n = acc.AddTimeAndDrain(1.0, dt => dts.Add(dt));
        await Assert.That(n).IsEqualTo(4);
        await Assert.That(dts.Count).IsEqualTo(4);
        await Assert.That(dts.All(d => Math.Abs(d - 0.25) < 1e-15)).IsTrue();
        var n2 = acc.AddTimeAndDrain(0.0, _ => { });
        await Assert.That(n2).IsEqualTo(0);
    }

    [Test]
    public async Task AddTimeAndDrain_FractionalCarry_CombinesAcrossCalls()
    {
        var acc = new FixedStepAccumulator(0.5);
        var steps = new List<double>();
        await Assert.That(acc.AddTimeAndDrain(0.3, dt => steps.Add(dt))).IsEqualTo(0);
        await Assert.That(acc.AddTimeAndDrain(0.3, dt => steps.Add(dt))).IsEqualTo(1);
        await Assert.That(steps.Count).IsEqualTo(1);
        await Assert.That(Math.Abs(steps[0] - 0.5)).IsLessThan(1e-15);
    }

    [Test]
    public async Task AddTimeAndDrain_LargeElapsed_ManyStepsDeterministic()
    {
        const double dtFixed = 1.0 / 60.0;
        var acc = new FixedStepAccumulator(dtFixed);
        var count = 0;
        var n = acc.AddTimeAndDrain(2.5, _ => count++);
        await Assert.That(n).IsEqualTo(150);
        await Assert.That(count).IsEqualTo(150);
        var n2 = acc.AddTimeAndDrain(0, _ => count++);
        await Assert.That(n2).IsEqualTo(0);
    }

    [Test]
    public async Task AddTimeAndDrain_TraceTable_ShowsStepPattern()
    {
        var acc = new FixedStepAccumulator(0.125);
        var log = new List<(int Call, double Elapsed, int Steps)>();
        var call = 0;
        log.Add((call, 0.4, acc.AddTimeAndDrain(0.4, _ => { })));
        call++;
        log.Add((call, 0.1, acc.AddTimeAndDrain(0.1, _ => { })));
        call++;
        log.Add((call, 0.2, acc.AddTimeAndDrain(0.2, _ => { })));

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("FixedStepAccumulator - carry trace");
        PhysicsDashboard.SectionAndTable(
            o,
            "Drain batches",
            log.Select(static e => new DrainBatchRow(e.Call, e.Elapsed, e.Steps)),
            new TableOptions { MaxCellWidth = 16, RightAlignNumericColumns = true },
            tableCaption: "dt_fixed=0.125 s; steps = floor((carry+elapsed)/dt)");

        await Assert.That(log[0].Steps).IsEqualTo(3);
        await Assert.That(log[1].Steps).IsEqualTo(1);
        await Assert.That(log[2].Steps).IsEqualTo(1);
    }

    private sealed record DrainBatchRow(int Call, double ElapsedSeconds, int StepsDrained);
}
