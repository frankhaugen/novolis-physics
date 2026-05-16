namespace Novolis.Physics.TestSupport;

/// <summary>Opinionated trace layout for physics-style diagnostics (section + optional table).</summary>
public static class PhysicsDashboard
{
    /// <summary>Writes <see cref="TestOutput.Section"/> then <see cref="TestOutput.Table{T}"/>.</summary>
    public static void SectionAndTable<T>(
        TestOutput output,
        string sectionTitle,
        IEnumerable<T> rows,
        TableOptions? tableOptions = null,
        string? tableCaption = null,
        IReadOnlyList<string>? columnPropertyOrder = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(rows);
        output.Section(sectionTitle);
        if (columnPropertyOrder is null)
            output.Table(rows, tableOptions, tableCaption);
        else
            output.Table(rows, tableOptions, tableCaption, columnPropertyOrder);
    }

    /// <summary>Writes <see cref="TestOutput.Results"/> then <see cref="TestOutput.Table{T}"/>.</summary>
    public static void ResultsAndTable<T>(
        TestOutput output,
        string resultsTitle,
        IEnumerable<T> rows,
        TableOptions? tableOptions = null,
        string? tableCaption = null,
        IReadOnlyList<string>? columnPropertyOrder = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(rows);
        output.Results(resultsTitle);
        if (columnPropertyOrder is null)
            output.Table(rows, tableOptions, tableCaption);
        else
            output.Table(rows, tableOptions, tableCaption, columnPropertyOrder);
    }
}
