# Novolis.Physics documentation

Force-first physics for .NET: compose `IForceModel` implementations, sum them in `SimulationPipeline`, and integrate with `IIntegrator`.

## Start here

| Document | Audience |
|----------|----------|
| [../README.md](../README.md) | Install, conventions, quick start |
| [INTEGRATION.md](INTEGRATION.md) | Which API to use (rigid body, ballistics, collision, orbits) |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Package graph and integration styles |
| [VERSIONING.md](VERSIONING.md) | Semver and breaking-change policy |
| [DESIGN_REVIEW_FINDINGS.md](DESIGN_REVIEW_FINDINGS.md) | Library design review (0.2.0-alpha) |

## Examples

| Example | Topic |
|---------|--------|
| [examples/ballistics.md](examples/ballistics.md) | Cannon / drag: facade vs pipeline |
| [examples/collision-room.md](examples/collision-room.md) | Static mesh + bouncing sphere |
| [examples/dependency-injection.md](examples/dependency-injection.md) | Optional `Microsoft.Extensions.DependencyInjection` wiring |

## Runnable reference

The unit project includes end-to-end scenarios with console tables:

```bash
dotnet run --project tests/Novolis.Physics.Unit -c Release -- --show-stdout All
```

Notable tests:

- `MinimalSimulationExampleTests` — `FixedStepAccumulator` + `SimulationPipeline` + gravity
- `ProjectileDragPipelineParityTests` — facade vs pipeline equivalence
- `CollisionSweepScenarioTests` / `SweepLimitationScenarioTests` — sweep behavior

See [tests/Novolis.Physics.TestSupport/README.md](../tests/Novolis.Physics.TestSupport/README.md) for trace helpers.
