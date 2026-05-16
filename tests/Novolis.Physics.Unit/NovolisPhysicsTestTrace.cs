using Novolis.Physics.TestSupport;

namespace Novolis.Physics.Unit;

/// <summary>Shared console trace and TUnit <c>[NotInParallel]</c> key for all Novolis.Physics unit tests.</summary>
internal static class NovolisPhysicsTestTrace
{
    internal const string NotInParallelKey = TraceParallelismKeys.NovolisPhysicsBallistics;

    internal static readonly TestOutput Out = TestOutput.ForScope("Novolis.Physics", useSharedConsoleLock: true);
}
