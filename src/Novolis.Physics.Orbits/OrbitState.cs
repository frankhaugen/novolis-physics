using Novolis.Physics.Numerics;

namespace Novolis.Physics.Orbits;

/// <summary>Test-particle state in 3D; planar Earth orbit uses Z = 0 and Vz = 0.</summary>
public readonly record struct OrbitState(Vector3d Position, Vector3d Velocity);
