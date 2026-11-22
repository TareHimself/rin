using System.Numerics;

namespace Rin.World.Physics;

public enum ColliderShapeKind : byte
{
    Box,
    Sphere,
    Capsule,
    Compound
}

/// <summary>
///     A collider description for the physics system and for compound parts. Only the fields of its <see cref="Kind" />
///     are set; the rest stay zero. Entities hold a collider component instead (see <see cref="Colliders" />).
/// </summary>
public readonly record struct ColliderShape
{
    private ColliderShape(ColliderShapeKind kind, Vector3 size, float radius, float halfHeight, PhysicsShapeHandle compound)
    {
        Kind = kind;
        Size = size;
        Radius = radius;
        HalfHeight = halfHeight;
        Compound = compound;
    }

    public ColliderShapeKind Kind { get; }

    /// <summary>
    ///     The full box extents.
    /// </summary>
    public Vector3 Size { get; }

    public float Radius { get; }

    public float HalfHeight { get; }

    /// <summary>
    ///     A compound's parts in <see cref="PhysicsShapes" />, valid only in this process.
    /// </summary>
    [field: EcsProcessLocal]
    public PhysicsShapeHandle Compound { get; }

    public static ColliderShape Box(in Vector3 size) => new(ColliderShapeKind.Box, size, 0f, 0f, default);

    public static ColliderShape Sphere(float radius) => new(ColliderShapeKind.Sphere, Vector3.Zero, radius, 0f, default);

    public static ColliderShape Capsule(float radius, float halfHeight) =>
        new(ColliderShapeKind.Capsule, Vector3.Zero, radius, halfHeight, default);

    public static ColliderShape CompoundOf(PhysicsShapeHandle parts) =>
        new(ColliderShapeKind.Compound, Vector3.Zero, 0f, 0f, parts);
}
