using System.Globalization;

namespace Novolis.Physics.TestSupport;

/// <summary>Rounded numeric helpers for physics scenario traces (console tables, dashboards).</summary>
public static class PhysicsTraceFormatting
{
    public static double Rd(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    public static string Rs(double value, int digits) =>
        Rd(value, digits).ToString("F" + digits, CultureInfo.InvariantCulture);
}
