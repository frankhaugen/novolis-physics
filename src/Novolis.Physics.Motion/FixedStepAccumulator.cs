namespace Novolis.Physics.Motion;

/// <summary>Accumulates real time and invokes a fixed physics step one or more times per frame.</summary>
public sealed class FixedStepAccumulator
{
    private double _carry;

    public FixedStepAccumulator(double fixedDeltaSeconds)
    {
        if (fixedDeltaSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedDeltaSeconds));
        }

        FixedDeltaSeconds = fixedDeltaSeconds;
    }

    public double FixedDeltaSeconds { get; }

    /// <summary>Returns number of fixed steps consumed.</summary>
    public int AddTimeAndDrain(double elapsedSeconds, Action<double> step)
    {
        _carry += elapsedSeconds;
        var count = 0;
        while (_carry >= FixedDeltaSeconds)
        {
            step(FixedDeltaSeconds);
            _carry -= FixedDeltaSeconds;
            count++;
        }

        return count;
    }
}
