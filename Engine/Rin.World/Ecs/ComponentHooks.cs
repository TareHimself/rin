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

    public override void FireRemoved(ArchetypeStore store, Entity entity)
    {
        // During despawn an earlier remove hook may already have removed this component or despawned the entity.
        if (store.IsAlive(entity) && store.Has<T>(entity)) Fire(_removed, store, entity);
    }

    private static void Fire(List<ComponentHook<T>> handlers, ArchetypeStore store, Entity entity)
    {
        // The component is looked up per handler because an earlier handler may have moved the entity.
        for (var i = 0; i < handlers.Count; i++) handlers[i](entity, ref store.Get<T>(entity));
    }
}
