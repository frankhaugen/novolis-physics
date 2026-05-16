using Novolis.Physics.TestSupport;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>Closed-form SI reference values physicists use as sanity checks (independent of the integrator).</summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class PhysicsLookupTableTests
{
    private const double G = 9.80665;

    [Test]
    public async Task VacuumBallistics_ReferenceTable_StandardLaunch()
    {
        const double v = 100.0;
        const double theta = Math.PI / 4.0;
        var sinT = Math.Sin(theta);
        var cosT = Math.Cos(theta);
        var tFlight = 2.0 * v * sinT / G;
        var range = v * v * Math.Sin(2.0 * theta) / G;
        var apex = v * v * sinT * sinT / (2.0 * G);

        var rows = new[]
        {
            new ClosedFormRow("Time of flight", "2 v sin(theta) / g", tFlight, "s"),
            new ClosedFormRow("Ground range", "v^2 sin(2 theta) / g", range, "m"),
            new ClosedFormRow("Max height", "v^2 sin^2(theta) / (2 g)", apex, "m"),
            new ClosedFormRow("Horiz. speed const.", "v cos(theta)", v * cosT, "m/s"),
            new ClosedFormRow("Vert. speed at launch", "v sin(theta)", v * sinT, "m/s"),
            new ClosedFormRow("ISA sea-level air density (dry)", "lookup / std atmosphere", 1.225, "kg/m^3"),
            new ClosedFormRow(
                "Cell truncation demo",
                "This formula string is deliberately long so table output clips with ellipsis per MaxCellWidth",
                0,
                "-"),
        };

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Physics reference - vacuum ballistics (v=100 m/s, theta=45 deg, g=9.80665)");
        o.Results("Reference table - closed forms (SI)");
        o.Table(
            rows,
            new TableOptions { MaxCellWidth = 36, MinCellWidth = 4 },
            caption: "Analytical values only; compare against simulator traces separately.");

        await Assert.That(range).IsGreaterThan(1000).And.IsLessThan(1100);
        await Assert.That(apex).IsGreaterThan(250).And.IsLessThan(260);
    }

    private sealed record ClosedFormRow(string Quantity, string Formula, double Value, string Unit);
}
