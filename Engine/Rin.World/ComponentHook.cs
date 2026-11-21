namespace Rin.World;

/// <summary>
///     Called when a component of type <typeparamref name="T" /> is added to or removed from an entity.
/// </summary>
/// <remarks>
///     <paramref name="component" /> points into the entity's row, so it is stale after the handler makes a structural
///     change. Use <see cref="World.Get{T}" /> to read the component again, or to write it.
/// </remarks>
public delegate void ComponentHook<T>(Entity entity, in T component) where T : unmanaged, IComponentData;
