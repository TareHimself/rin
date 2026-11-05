namespace Rin.World;

/// <summary>
///     Called when a component of type <typeparamref name="T" /> is added to or removed from an entity.
/// </summary>
public delegate void ComponentHook<T>(Entity entity, ref T component) where T : struct, IComponentData;
