using Novolis.Physics.Ballistics;
using Novolis.Physics.Numerics;
using TUnit.Core;

namespace Novolis.Physics.Unit;

/// <summary>Vacuum trajectories using 3D vectors with motion confined to the X–Y plane (<c>Z = 0</c>).</summary>
[NotInParallel(NovolisPhysicsTestTrace.NotInParallelKey)]
public sealed class AnalyticalProjectileTests
{
    [Test]
    public async Task Projectile_45Degrees_NoDrag_MatchesAnalyticalRange()
    {
        const double gravity = 9.80665;
        const double speed = 100.0;
        const double angle = Math.PI / 4.0;
        const double dt = 1.0 / 120.0;

        var vx = speed * Math.Cos(angle);
        var vy = speed * Math.Sin(angle);
        var velocity = new Vector3d(vx, vy, 0);
        var state = new ProjectileState(Vector3d.Zero, velocity, massKg: 1.0, timeSeconds: 0);
        var environment = new ProjectileBallisticEnvironment(gravity, AirDensityKgPerM3: 0);
        var simulation = new ProjectileBallisticSimulation(dragProfile: null);

        var maxHeight = 0.0;
        var previous = state;
        while (state.Position.Y >= 0)
        {
            previous = state;
            state = simulation.Step(state, dt, environment);
            maxHeight = Math.Max(maxHeight, state.Position.Y);
        }

        var impact = ProjectileMath.InterpolateGroundImpact(previous, state);

        var expectedTime = 2.0 * vy / gravity;
        var expectedRange = vx * expectedTime;
        var expectedHeight = vy * vy / (2.0 * gravity);

        var o = NovolisPhysicsTestTrace.Out;
        o.Section("Vacuum 45 deg (compact)");
        o.Line(
            "setup",
            FormattableString.Invariant($"g={gravity} m/s2, v0={speed} m/s, angle={angle} rad, dt={dt} s, vx={vx:G9}, vy={vy:G9}"));

        var arc = BuildVacuumTrajectoryAscii(vx, vy, gravity, cols: 44, plotRows: 10);
        o.AsciiGrid(arc, "Vacuum arc (char[,]): * path, . sky, - ground");

        // Same geometry via int layers → palette (background / path / ground).
        var layers = BuildVacuumTrajectoryLayers(vx, vy, gravity, cols: 44, plotRows: 10);
        o.AsciiGrid(layers, " .-*", fallback: '?', caption: "Vacuum arc (int[,] palette): 1=sky 2=path 3=ground");

        o.Results("Vacuum 45 deg - outcome (vs analytical)");
        o.Line(
            "analytical  t(s), rangeX(m), maxH(m)",
            FormattableString.Invariant($"{expectedTime:G9}, {expectedRange:G9}, {expectedHeight:G9}"));
        o.Line(
            "simulated   t(s), rangeX(m), maxH(m), impactVy(m/s)",
            FormattableString.Invariant(
                $"{impact.TimeSeconds:G9}, {impact.Position.X:G9}, {maxHeight:G9}, {impact.Velocity.Y:G9}"));
        o.Line(
            "delta         dRangeX, dTime, dMaxH",
            FormattableString.Invariant(
                $"{impact.Position.X - expectedRange:G9}, {impact.TimeSeconds - expectedTime:G9}, {maxHeight - expectedHeight:G9}"));
        o.Line("assert limits |dRange|<=0.65m |dTime|<=0.02s |dMaxH|<=0.35m Z~0", "(see assertions below)");

        // Semi-implicit Euler vs continuous-time formulas: expect sub-meter range drift at dt = 1/120 s.
        await Assert.That(Math.Abs(impact.Position.X - expectedRange)).IsLessThanOrEqualTo(0.65);
        await Assert.That(Math.Abs(impact.TimeSeconds - expectedTime)).IsLessThanOrEqualTo(0.02);
        await Assert.That(Math.Abs(maxHeight - expectedHeight)).IsLessThanOrEqualTo(0.35);
        await Assert.That(Math.Abs(impact.Position.Z)).IsLessThanOrEqualTo(1e-9);
    }

    /// <summary>Row 0 = highest y; bottom row = ground line. Columns = x along range.</summary>
    private static char[,] BuildVacuumTrajectoryAscii(double vx, double vy, double g, int cols, int plotRows)
    {
        var rows = plotRows + 1;
        var grid = new char[rows, cols];
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
                grid[r, c] = r == rows - 1 ? '-' : '.';
        }

        var tFlight = 2.0 * vy / g;
        var maxX = vx * tFlight;
        var maxY = vy * vy / (2.0 * g);
        if (maxX <= 0 || maxY <= 0)
            return grid;

        const int samples = 96;
        for (var s = 0; s <= samples; s++)
        {
            var t = tFlight * (s / (double)samples);
            var x = vx * t;
            var y = vy * t - 0.5 * g * t * t;
            var c = (int)Math.Round(x / maxX * (cols - 1));
            var r = plotRows - 1 - (int)Math.Round(y / maxY * (plotRows - 1));
            if ((uint)r < (uint)plotRows && (uint)c < (uint)cols)
                grid[r, c] = '*';
        }

        return grid;
    }

    /// <summary>0 = space, 1 = dot (sky), 2 = star (path), 3 = dash (ground). Mapped by <see cref="TestOutput.AsciiGrid(int[,], ReadOnlySpan{char}, char, string?)"/>.</summary>
    private static int[,] BuildVacuumTrajectoryLayers(double vx, double vy, double g, int cols, int plotRows)
    {
        var ch = BuildVacuumTrajectoryAscii(vx, vy, g, cols, plotRows);
        var rows = ch.GetLength(0);
        var colCount = ch.GetLength(1);
        var layers = new int[rows, colCount];
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < colCount; c++)
            {
                layers[r, c] = ch[r, c] switch
                {
                    '*' => 2,
                    '-' => 3,
                    '.' => 1,
                    _ => 1,
                };
            }
        }

        return layers;
    }
}
