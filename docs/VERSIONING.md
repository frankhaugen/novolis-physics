# Versioning policy

Novolis.Physics follows [Semantic Versioning](https://semver.org/) with pre-release labels.

## Pre-release (`0.x.x-alpha`)

| Version | Guarantees |
|---------|------------|
| **0.1.0-alpha** | Initial public API; breaking changes allowed without a major bump. |
| **0.2.0-alpha** | Breaking removals documented below; still pre-stable. |

### Breaking changes in 0.2.0-alpha

- Removed `IContactResolver<TBody>` from `Novolis.Physics.Abstractions` (no implementations; use `BvhStaticSphereIntegrator` + `SphereContactKinematics`).
- Moved `OrbitalTestConstants` and `OrbitalTestState` out of `Novolis.Physics.Orbits` into `Novolis.Physics.TestSupport` (test-only assembly, not published).

## Stable (`1.0.0` and later)

- **Major** — breaking public API changes.
- **Minor** — backward-compatible features.
- **Patch** — backward-compatible bug fixes.

Public API is considered the types shipped in NuGet product packages under `src/Novolis.Physics.*` (excluding test-only assemblies).

## Package versions

All product packages share one version via `build/Novolis.Physics.Packaging.props` (`NovolisPhysicsVersion`).
