using System.Numerics;

namespace Rin.World.Ecs.Transforms;

/// <summary>
///     Transform setters that add <see cref="LocalTransform" /> and <see cref="WorldTransform" /> when missing.
/// </summary>
/// <remarks>
///     Adding a missing component is a structural change, which the backend rejects inside a query loop. Inside one,
///     write through the <see cref="LocalTransform" /> span instead.
/// </remarks>
public static class TransformExtensions
{
    public static void SetLocalTransform(this EntityWorld world, Entity entity, in LocalTransform transform)
    {
        ref var local = ref world.GetOrAddTransform(entity);
        local = transform;
        local.IsDirty = true;
    }

    public static void SetPosition(this EntityWorld world, Entity entity, in Vector3 position) =>
        world.GetOrAddTransform(entity).Position = position;

    public static void SetRotation(this EntityWorld world, Entity entity, in Quaternion rotation) =>
        world.GetOrAddTransform(entity).Rotation = rotation;

    public static void SetScale(this EntityWorld world, Entity entity, in Vector3 scale) =>
        world.GetOrAddTransform(entity).Scale = scale;

    private static ref LocalTransform GetOrAddTransform(this EntityWorld world, Entity entity)
    {
        if (!world.Has<LocalTransform>(entity)) world.Add(entity, new LocalTransform());
        if (!world.Has<WorldTransform>(entity)) world.Add(entity, new WorldTransform());
        return ref world.Get<LocalTransform>(entity);
    }
}
