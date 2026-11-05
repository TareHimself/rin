using System.Numerics;

namespace Rin.World.Ecs.Physics;

public enum ColliderShapeKind : byte
{
    Box,
    Sphere,
    Capsule
}

/// <summary>
///     An unmanaged collider description. Only the fields of its <see cref="Kind" /> are meaningful.
/// </summary>
public readonly record struct ColliderShape
{
    private ColliderShape(ColliderShapeKind kind, Vector3 size, float radius, float halfHeight)
    {
        Kind = kind;
        Size = size;
        Radius = radius;
        HalfHeight = halfHeight;
    }

    public ColliderShapeKind Kind { get; }

    /// <summary>
    ///     The full box extents.
    /// </summary>
    public Vector3 Size { get; }

    public float Radius { get; }

    public float HalfHeight { get; }

    public static ColliderShape Box(in Vector3 size) => new(ColliderShapeKind.Box, size, 0f, 0f);

    public static ColliderShape Sphere(float radius) => new(ColliderShapeKind.Sphere, Vector3.Zero, radius, 0f);

    public static ColliderShape Capsule(float radius, float halfHeight) =>
        new(ColliderShapeKind.Capsule, Vector3.Zero, radius, halfHeight);
}
