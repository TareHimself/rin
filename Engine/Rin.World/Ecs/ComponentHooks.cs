using System.Runtime.CompilerServices;

namespace Rin.World.Ecs;

internal abstract class ComponentHooks
{
    public abstract void FireAdded(ArchetypeStore store, Entity entity);

    public abstract void FireRemoved(ArchetypeStore store, Entity entity);
}

internal sealed class ComponentHooks<T> : ComponentHooks where T : unmanaged, IComponentData
{
    private readonly List<ComponentHook<T>> _added = [];
    private readonly List<ComponentHook<T>> _removed = [];

    public void OnAdd(ComponentHook<T> handler) => _added.Add(handler);

    public void OnRemove(ComponentHook<T> handler) => _removed.Add(handler);

    public override void FireAdded(ArchetypeStore store, Entity entity) => Fire(_added, store, entity);

    public override void FireRemoved(ArchetypeStore store, Entity entity) => Fire(_removed, store, entity);

    private static void Fire(List<ComponentHook<T>> handlers, ArchetypeStore store, Entity entity)
    {
        // Looked up per handler: an earlier one may have moved the entity, removed the component or despawned it.
        for (var i = 0; i < handlers.Count; i++)
        {
            ref var component = ref store.TryGetRef<T>(entity);
            if (Unsafe.IsNullRef(ref component)) return;
            handlers[i](entity, in component);
        }
    }
}
