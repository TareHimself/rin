using System.Numerics;

namespace Rin.World.Physics;

/// <summary>
///     A box collider for the entity's <see cref="RigidBody" />.
/// </summary>
[EcsRequires<RigidBody>]
public struct BoxCollider() : IComponentData
{
    /// <summary>
    ///     The full box extents.
    /// </summary>
    public Vector3 Size = Vector3.One;
}

[EcsRequires<RigidBody>]
public struct SphereCollider() : IComponentData
{
    public float Radius = 0.5f;
}

[EcsRequires<RigidBody>]
public struct CapsuleCollider() : IComponentData
{
    public float Radius = 0.5f;

    /// <summary>
    ///     Half the length of the straight part, not counting the caps.
    /// </summary>
    public float HalfHeight = 0.5f;
}

/// <summary>
///     A collider made of parts kept in <see cref="PhysicsShapes" />, which only exist in this process.
/// </summary>
[EcsRequires<RigidBody>]
public struct CompoundCollider : IComponentData
{
    [EcsProcessLocal] public PhysicsShapeHandle Parts;
}

/// <summary>
///     Reads and writes an entity's collider component as a <see cref="ColliderShape" />.
/// </summary>
/// <remarks>
///     An entity holds at most one collider component, since a body has one shape; reading two throws.
/// </remarks>
public static class Colliders
{
    public static bool TryGetShape(World world, Entity entity, out ColliderShape shape)
    {
        var found = 0;
        shape = default;
        if (world.Has<BoxCollider>(entity))
        {
            shape = ColliderShape.Box(world.Get<BoxCollider>(entity).Size);
            found++;
        }

        if (world.Has<SphereCollider>(entity))
        {
            shape = ColliderShape.Sphere(world.Get<SphereCollider>(entity).Radius);
            found++;
        }

        if (world.Has<CapsuleCollider>(entity))
        {
            ref readonly var capsule = ref world.Get<CapsuleCollider>(entity);
            shape = ColliderShape.Capsule(capsule.Radius, capsule.HalfHeight);
            found++;
        }

        if (world.Has<CompoundCollider>(entity))
        {
            shape = ColliderShape.CompoundOf(world.Get<CompoundCollider>(entity).Parts);
            found++;
        }

        return found switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidOperationException($"Entity {entity.Id} has {found} colliders; a body has one.")
        };
    }

    /// <summary>
    ///     Replaces the entity's collider component, if any, with the one for <paramref name="shape" />.
    /// </summary>
    public static void Set(World world, Entity entity, in ColliderShape shape)
    {
        RemoveAll(world, entity);
        switch (shape.Kind)
        {
            case ColliderShapeKind.Box:
                world.Add(entity, new BoxCollider { Size = shape.Size });
                break;
            case ColliderShapeKind.Sphere:
                world.Add(entity, new SphereCollider { Radius = shape.Radius });
                break;
            case ColliderShapeKind.Capsule:
                world.Add(entity, new CapsuleCollider { Radius = shape.Radius, HalfHeight = shape.HalfHeight });
                break;
            case ColliderShapeKind.Compound:
                world.Add(entity, new CompoundCollider { Parts = shape.Compound });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape.Kind, "Unknown collider shape.");
        }
    }

    public static void RemoveAll(World world, Entity entity)
    {
        if (world.Has<BoxCollider>(entity)) world.Remove<BoxCollider>(entity);
        if (world.Has<SphereCollider>(entity)) world.Remove<SphereCollider>(entity);
        if (world.Has<CapsuleCollider>(entity)) world.Remove<CapsuleCollider>(entity);
        if (world.Has<CompoundCollider>(entity)) world.Remove<CompoundCollider>(entity);
    }
}
