using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace Rin.World.Ecs;

internal abstract class ComponentHooks
{
    public abstract void FireRemoved(FrifloEntity entity, Entity handle);
}

internal sealed class ComponentHooks<T> : ComponentHooks where T : struct, IComponentData
{
    private readonly List<ComponentHook<T>> _added = [];
    private readonly List<ComponentHook<T>> _removed = [];

    public void OnAdd(ComponentHook<T> handler) => _added.Add(handler);

    public void OnRemove(ComponentHook<T> handler) => _removed.Add(handler);

    public void FireAdded(FrifloEntity entity, Entity handle) => Fire(_added, entity, handle);

    public override void FireRemoved(FrifloEntity entity, Entity handle)
    {
        // During despawn an earlier remove hook may already have removed this component.
        if (entity.HasComponent<T>()) Fire(_removed, entity, handle);
    }

    private static void Fire(List<ComponentHook<T>> handlers, FrifloEntity entity, Entity handle)
    {
        // The component is looked up per handler because an earlier handler may have moved the entity.
        for (var i = 0; i < handlers.Count; i++) handlers[i](handle, ref entity.GetComponent<T>());
    }
}
