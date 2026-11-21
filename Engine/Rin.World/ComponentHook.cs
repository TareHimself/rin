namespace Rin.World;

/// <summary>
///     Called when a component of type <typeparamref name="T" /> is added to or removed from an entity.
/// </summary>
/// <remarks>
///     <paramref name="component" /> points into the entity's row, so it is stale after the handler makes a structural
///     change. Read it again with <see cref="World.Get{T}" />, and change it with <see cref="World.Write{T}" />.
/// </remarks>
public delegate void ComponentHook<T>(Entity entity, in T component) where T : unmanaged, IComponentData;
