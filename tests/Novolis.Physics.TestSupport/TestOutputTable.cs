using System.Globalization;
using System.Reflection;
using System.Text;

namespace Novolis.Physics.TestSupport;

/// <summary>Layout and safety limits for <see cref="TestOutput.Table{T}"/>.</summary>
public sealed class TableOptions
{
    /// <summary>Hard cap on rendered cell text (after whitespace flattening).</summary>
    public int MaxCellWidth { get; init; } = 48;

    public int MinCellWidth { get; init; } = 3;

    /// <summary>Maximum data rows (excluding header). Extra source rows are omitted with a footnote.</summary>
    public int MaxRows { get; init; } = 200;

    public string Ellipsis { get; init; } = "...";

    /// <summary>Replace CR/LF/tab runs with a single space so each table row stays one console line.</summary>
    public bool FlattenWhitespaceInCells { get; init; } = true;

    /// <summary>When true, body cells that parse as invariant doubles are padded left (headers stay left-padded).</summary>
    public bool RightAlignNumericColumns { get; init; } = false;
}

public sealed partial class TestOutput
{
    /// <summary>
    /// Renders a fixed-width pipe table from <paramref name="rows"/> using public instance properties on <typeparamref name="T"/>
    /// (works well for <c>record</c> types). Cell text is flattened, pipe characters neutralized, then truncated to <see cref="TableOptions.MaxCellWidth"/>.
    /// Property order follows metadata token order on the declared type unless <paramref name="columnPropertyOrder"/> is set.
    /// </summary>
    public void Table<T>(IEnumerable<T> rows, TableOptions? options = null, string? caption = null) =>
        TableCore(rows, options ?? new TableOptions(), caption, columnPropertyOrder: null);

    /// <summary>
    /// Same as <see cref="Table{T}(IEnumerable{T}, TableOptions?, string?)"/> but columns follow <paramref name="columnPropertyOrder"/> (property names on <typeparamref name="T"/>).
    /// </summary>
    public void Table<T>(
        IEnumerable<T> rows,
        TableOptions? options,
        string? caption,
        IReadOnlyList<string> columnPropertyOrder) =>
        TableCore(rows, options ?? new TableOptions(), caption, columnPropertyOrder);

    private void TableCore<T>(
        IEnumerable<T> rows,
        TableOptions opt,
        string? caption,
        IReadOnlyList<string>? columnPropertyOrder)
    {
        ArgumentNullException.ThrowIfNull(rows);

        lock (_gate)
        {
            if (caption is not null)
                Console.WriteLine($"{_prefix} {caption}");

            var rowList = new List<T>(Math.Min(opt.MaxRows, 64));
            var truncated = false;
            foreach (var row in rows)
            {
                if (row is null)
                    throw new ArgumentException("Row collection contains a null reference.", nameof(rows));

                if (rowList.Count >= opt.MaxRows)
                {
                    truncated = true;
                    break;
                }

                rowList.Add(row);
            }

            if (rowList.Count == 0)
            {
                Console.WriteLine($"{_prefix} (table: no rows)");
                return;
            }

            var props = ResolveProperties(typeof(T), columnPropertyOrder);
            if (props.Length == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no public instance properties to use as columns.");

            var headers = props.Select(p => NormalizeHeader(p.Name)).ToArray();
            var formatted = new string[rowList.Count][];
            for (var i = 0; i < rowList.Count; i++)
            {
                formatted[i] = new string[props.Length];
                for (var j = 0; j < props.Length; j++)
                {
                    var raw = FormatCellValue(props[j].GetValue(rowList[i]));
                    formatted[i][j] = PrepareCellText(raw, opt);
                }
            }

            var colWidths = new int[props.Length];
            for (var j = 0; j < props.Length; j++)
            {
                var w = Math.Max(opt.MinCellWidth, Math.Min(opt.MaxCellWidth, headers[j].Length));
                for (var i = 0; i < formatted.Length; i++)
                    w = Math.Max(w, Math.Min(opt.MaxCellWidth, formatted[i][j].Length));
                colWidths[j] = Math.Min(opt.MaxCellWidth, Math.Max(opt.MinCellWidth, w));
            }

            for (var i = 0; i < formatted.Length; i++)
            {
                for (var j = 0; j < props.Length; j++)
                    formatted[i][j] = TruncateCell(formatted[i][j], colWidths[j], opt.Ellipsis);
            }

            var headerCells = new string[props.Length];
            for (var j = 0; j < props.Length; j++)
                headerCells[j] = TruncateCell(headers[j], colWidths[j], opt.Ellipsis);

            var rightNumeric = opt.RightAlignNumericColumns ? ComputeNumericColumns(formatted) : null;

            WriteTableLine(BuildHorizontalRule(colWidths));
            WriteTableLine(BuildDataRow(headerCells, colWidths, rightNumeric, isHeader: true));
            WriteTableLine(BuildHorizontalRule(colWidths));
            foreach (var line in formatted)
                WriteTableLine(BuildDataRow(line, colWidths, rightNumeric, isHeader: false));

            WriteTableLine(BuildHorizontalRule(colWidths));
            if (truncated)
                Console.WriteLine(FormattableString.Invariant($"{_prefix} (table truncated: showing first {opt.MaxRows} rows)"));
        }

        void WriteTableLine(string line) => Console.WriteLine($"{_prefix} {line}");
    }

    private static bool[]? ComputeNumericColumns(string[][] formatted)
    {
        if (formatted.Length == 0)
            return null;

        var cols = formatted[0].Length;
        var flags = new bool[cols];
        for (var j = 0; j < cols; j++)
        {
            var allNumeric = true;
            for (var i = 0; i < formatted.Length; i++)
            {
                if (!double.TryParse(formatted[i][j], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    allNumeric = false;
                    break;
                }
            }

            flags[j] = allNumeric;
        }

        return flags;
    }

    private static PropertyInfo[] ResolveProperties(Type t, IReadOnlyList<string>? columnPropertyOrder)
    {
        var all = GetTableProperties(t);
        if (columnPropertyOrder is null || columnPropertyOrder.Count == 0)
            return all;

        var map = all.ToDictionary(static p => p.Name, StringComparer.Ordinal);
        var ordered = new List<PropertyInfo>(columnPropertyOrder.Count);
        foreach (var name in columnPropertyOrder)
        {
            if (!map.TryGetValue(name, out var p))
                throw new ArgumentException($"Property '{name}' not found on {t.Name} for table column order.", nameof(columnPropertyOrder));

            ordered.Add(p);
        }

        return ordered.ToArray();
    }

    private static PropertyInfo[] GetTableProperties(Type t) =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static p => p.CanRead && p.GetIndexParameters().Length == 0 && !IsRecordCompilerProperty(p.Name))
            .OrderBy(static p => p.MetadataToken)
            .ToArray();

    private static bool IsRecordCompilerProperty(string name) =>
        name.Equals("EqualityContract", StringComparison.Ordinal);

    private static string NormalizeHeader(string name) =>
        string.IsNullOrEmpty(name) ? "?" : name;

    private static string FormatCellValue(object? value)
    {
        if (value is null)
            return "";

        if (value is IFormattable f)
            return f.ToString(null, CultureInfo.InvariantCulture) ?? "";

        if (value is bool b)
            return b ? "true" : "false";

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
    }

    private static string PrepareCellText(string raw, TableOptions opt)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        var s = raw.Replace('|', '/');
        if (!opt.FlattenWhitespaceInCells)
            return s;

        var sb = new StringBuilder(s.Length);
        var pendingSpace = false;
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (ch is '\r' or '\n' or '\t' or '\v' or '\f')
            {
                pendingSpace = true;
                continue;
            }

            if (ch == ' ')
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && sb.Length > 0)
                sb.Append(' ');

            pendingSpace = false;
            sb.Append(ch);
        }

        return sb.ToString().Trim();
    }

    private static string TruncateCell(string text, int colWidth, string ellipsis)
    {
        if (text.Length <= colWidth)
            return text;

        var ell = ellipsis.Length == 0 ? "..." : ellipsis;
        var take = colWidth - ell.Length;
        if (take <= 0)
            return ell.Length <= colWidth ? ell[..colWidth] : text[..colWidth];

        return string.Concat(text.AsSpan(0, take), ell);
    }

    private static string BuildDataRow(IReadOnlyList<string> cells, int[] colWidths, bool[]? rightNumeric, bool isHeader)
    {
        var sb = new StringBuilder(32 + cells.Count * 8);
        sb.Append('|');
        for (var i = 0; i < cells.Count; i++)
        {
            sb.Append(' ');
            var c = cells[i];
            var w = colWidths[i];
            var useRight = !isHeader && rightNumeric is not null && i < rightNumeric.Length && rightNumeric[i];
            if (c.Length < w)
            {
                if (useRight)
                    sb.Append(' ', w - c.Length).Append(c);
                else
                    sb.Append(c).Append(' ', w - c.Length);
            }
            else
            {
                sb.Append(c, 0, w);
            }

            sb.Append(" |");
        }

        return sb.ToString();
    }

    private static string BuildHorizontalRule(int[] colWidths)
    {
        var sb = new StringBuilder(8 + colWidths.Sum(w => w + 3));
        sb.Append('+');
        foreach (var w in colWidths)
        {
            sb.Append('-', w + 2);
            sb.Append('+');
        }

        return sb.ToString();
    }
}
