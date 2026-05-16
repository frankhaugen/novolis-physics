namespace Novolis.Physics.TestSupport;

/// <summary>Enumerable helpers for sampled console output (e.g. every Nth simulation step).</summary>
public static class TestOutputSequences
{
    /// <summary>Yields elements at indices 0, stride, 2*stride, ... (stride defaults to 1 when less than 1).</summary>
    public static IEnumerable<T> EveryNth<T>(IEnumerable<T> source, int stride)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (stride < 1)
            stride = 1;

        var n = 0;
        foreach (var item in source)
        {
            if (n++ % stride == 0)
                yield return item;
        }
    }
}
