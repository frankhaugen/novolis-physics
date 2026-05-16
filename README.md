# Novolis.Physics

Force-first physics simulation for .NET — numerics, motion pipeline, gravity, ballistics, collision, orbits, and KSP-style composition.

## Install

```bash
dotnet add package Novolis.Physics
```

Or reference individual packages (`Novolis.Physics.Motion`, `Novolis.Physics.Orbits`, …).

## Packages

| Package | Role |
|---------|------|
| `Novolis.Physics` | Aggregate — all product packages |
| `Novolis.Physics.Numerics` | Vectors, rays, primitives |
| `Novolis.Physics.Abstractions` | Force models, integrators, contacts |
| `Novolis.Physics.Motion` | Rigid-body motion pipeline |
| `Novolis.Physics.Gravity` | Point / spherical gravity |
| `Novolis.Physics.Aerodynamics` | Lift / drag models |
| `Novolis.Physics.Collision.Simple` | Static mesh BVH queries |
| `Novolis.Physics.Ballistics` | Projectile drag and sweeps |
| `Novolis.Physics.Orbits` | Two-body orbital helpers |
| `Novolis.Physics.KspLite` | DI presets composing the stack |

## Build

```bash
dotnet build Novolis.Physics.slnx
dotnet run --project tests/Novolis.Physics.Unit -c Release
```
