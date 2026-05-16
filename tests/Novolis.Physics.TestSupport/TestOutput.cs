namespace Novolis.Physics.TestSupport;

/// <summary>
/// Thread-safe writes to <see cref="Console.Out"/> for numeric / scenario reasoning in tests.
/// With TUnit, pass <c>-- --show-stdout All</c> (and optionally <c>--output Detailed</c>) to <c>dotnet test</c> to see lines.
/// </summary>
/// <remarks>
/// Use <see cref="NotInParallelKey"/> with TUnit <c>[NotInParallel(...)]</c> when multiple fixtures share one trace scope
/// and should not print concurrently. Optionally pass <see cref="SharedConsoleLock"/> so all instances serialize on one lock.
/// </remarks>
public sealed partial class TestOutput
{
    /// <summary>Shared lock for cross-fixture serialized console output.</summary>
    public static object SharedConsoleLock { get; } = new();

    private readonly string _prefix;
    private readonly object _gate;

    /// <param name="scope">Short label; becomes <c>[scope]</c> on each line unless it already starts with <c>[</c>.</param>
    /// <param name="gate">Optional sync root; default is a per-instance lock. Use <see cref="SharedConsoleLock"/> to coordinate across types.</param>
    public TestOutput(string scope, object? gate = null)
    {
        _prefix = string.IsNullOrEmpty(scope)
            ? "[Test]"
            : scope.StartsWith("[", StringComparison.Ordinal)
                ? scope
                : $"[{scope}]";
        _gate = gate ?? new object();
    }

    /// <summary>Stable key for <c>[NotInParallel(...)]</c> so traces from one area do not interleave.</summary>
    public static string NotInParallelKey(string scope) => $"{nameof(TestOutput)}:{scope}";

    public static TestOutput ForScope(string scope, bool useSharedConsoleLock = false) =>
        new(scope, useSharedConsoleLock ? SharedConsoleLock : null);

    public void Section(string title)
    {
        lock (_gate)
        {
            Console.WriteLine();
            Console.WriteLine($"{_prefix} === {title} ===");
        }
    }

    /// <summary>
    /// Visual separator for the numbers assertions use: two blank lines, then a banner (easy to spot in <c>dotnet test -- --show-stdout All</c>).
    /// </summary>
    public void Results(string title)
    {
        lock (_gate)
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"{_prefix} === {title} ===");
        }
    }

    public void Line(string label, double value)
    {
        lock (_gate)
        {
            Console.WriteLine(FormattableString.Invariant($"{_prefix} {label}: {value:G17}"));
        }
    }

    public void Line(string label, int value)
    {
        lock (_gate)
        {
            Console.WriteLine($"{_prefix} {label}: {value}");
        }
    }

    public void Line(string label, string value)
    {
        lock (_gate)
        {
            Console.WriteLine($"{_prefix} {label}: {value}");
        }
    }

    /// <summary>Invariant triple on one line (e.g. position or velocity in SI tests).</summary>
    public void Line(string label, double x, double y, double z)
    {
        lock (_gate)
        {
            Console.WriteLine(FormattableString.Invariant($"{_prefix} {label}: {x:G17}, {y:G17}, {z:G17}"));
        }
    }

    public void Line(string label, bool value) => Line(label, value ? "true" : "false");

    public void Line(string message)
    {
        lock (_gate)
        {
            Console.WriteLine($"{_prefix} {message}");
        }
    }

    public void Blank()
    {
        lock (_gate)
        {
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Prints a caller-built ASCII raster. Indexing is <c>cells[row, col]</c>: row 0 is the first printed line (top),
    /// columns increase left to right. Use spaces or <c>'.'</c> for background and any glyphs you want for the trace.
    /// </summary>
    /// <param name="cells">Height = <see cref="Array.GetLength"/>(0), width = GetLength(1).</param>
    /// <param name="caption">Optional single line printed before the grid (still scoped with <c>_prefix</c>).</param>
    public void AsciiGrid(char[,] cells, string? caption = null)
    {
        ArgumentNullException.ThrowIfNull(cells);
        lock (_gate)
        {
            if (caption is not null)
                Console.WriteLine($"{_prefix} {caption}");

            var rowCount = cells.GetLength(0);
            var colCount = cells.GetLength(1);
            if (colCount == 0)
                return;

            Span<char> line = colCount <= 512 ? stackalloc char[colCount] : new char[colCount];
            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                    line[c] = cells[r, c];
                Console.WriteLine($"{_prefix} {new string(line)}");
            }
        }
    }

    /// <summary>
    /// Prints a raster built from integer palette indices: each cell is <c>palette[index]</c> when in range,
    /// otherwise <paramref name="fallback"/>. Lets you compose layers as ints (e.g. 0 = background, 1 = axis, 2 = curve)
    /// and map them to characters in one place.
    /// </summary>
    public void AsciiGrid(int[,] paletteIndices, ReadOnlySpan<char> palette, char fallback = '?', string? caption = null)
    {
        ArgumentNullException.ThrowIfNull(paletteIndices);
        lock (_gate)
        {
            if (caption is not null)
                Console.WriteLine($"{_prefix} {caption}");

            var rowCount = paletteIndices.GetLength(0);
            var colCount = paletteIndices.GetLength(1);
            if (colCount == 0 || palette.Length == 0)
                return;

            Span<char> line = colCount <= 512 ? stackalloc char[colCount] : new char[colCount];
            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    var i = paletteIndices[r, c];
                    line[c] = (uint)i < (uint)palette.Length ? palette[i] : fallback;
                }

                Console.WriteLine($"{_prefix} {new string(line)}");
            }
        }
    }
}
