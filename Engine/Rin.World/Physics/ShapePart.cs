using System.Numerics;

namespace Rin.World.Physics;

/// <summary>
///     One primitive of a compound collider, placed relative to the body's origin.
/// </summary>
public readonly record struct ShapePart(ColliderShape Shape, Vector3 Position, Quaternion Rotation)
{
    public ShapePart(ColliderShape shape, Vector3 position) : this(shape, position, Quaternion.Identity)
    {
    }
}
