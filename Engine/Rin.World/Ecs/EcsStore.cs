using Friflo.Engine.ECS;
using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace Rin.World.Ecs;

/// <summary>
///     The Friflo-backed entity store behind <see cref="World" />. Hooks and cascading despawn are done here rather
///     than with Friflo events, because Friflo does not fire remove events on delete and leaves children orphaned.
/// </summary>
/// <remarks>
///     Ids come from a Rin <see cref="EntityIdAllocator" /> rather than Friflo's free list, which cannot be saved, so a
///     snapshot restores the exact ids the next spawns get.
/// </remarks>
internal sealed partial class EcsStore
{
    private readonly EntityStore _store = new() { RecycleIds = false };
    private readonly EntityIdAllocator _ids = new();
    private readonly ComponentHooks?[] _hooks = new ComponentHooks?[EntityStore.GetEntitySchema().Components.Length];
    private readonly List<Entity> _despawning = [];
    private readonly List<int> _walk = [];
    private Entity _firingDespawnHooks;

    static EcsStore() => ComponentRegistry.EnsureSchema();

    public int EntityCount => _store.Count;

    public Entity Spawn()
    {
        var entity = _ids.Allocate();
        _store.CreateEntity(entity.Id);
        return entity;
    }

    public bool IsAlive(Entity entity) => _ids.IsAlive(entity);

    public bool IsDespawning(Entity entity) => entity.IsValid && entity == _firingDespawnHooks;

    public void Despawn(Entity entity)
    {
        if (!TryResolve(entity, out var root)) return;

        // Hooks may despawn more entities, so each call owns the range it appended.
        var start = _despawning.Count;
        CollectDepthFirst(root);

        var end = _despawning.Count;
        for (var i = start; i < end; i++)
        {
            var doomed = _despawning[i];
            if (!IsAlive(doomed)) continue;
            FireDespawnHooks(doomed);
            if (!IsAlive(doomed)) continue;
            _store.GetEntityById(doomed.Id).DeleteEntity();
            _ids.Free(doomed.Id);
        }

        _despawning.RemoveRange(start, end - start);
    }

    public bool Has<T>(Entity entity) where T : struct, IComponentData => Resolve(entity).HasComponent<T>();

    public ref T Get<T>(Entity entity) where T : struct, IComponentData
    {
        var resolved = Resolve(entity);
        if (!resolved.HasComponent<T>()) throw MissingComponent<T>(entity);
        return ref resolved.GetComponent<T>();
    }

    public void Add<T>(Entity entity, in T component) where T : struct, IComponentData
    {
        var resolved = Resolve(entity);
        ComponentColumns.Register<T>();
        if (resolved.AddComponent(component)) HooksOf<T>()?.FireAdded(resolved, entity);
    }

    public void Set<T>(Entity entity, in T component) where T : struct, IComponentData => Get<T>(entity) = component;

    public bool Remove<T>(Entity entity) where T : struct, IComponentData
    {
        var resolved = Resolve(entity);
        if (!resolved.HasComponent<T>()) return false;
        HooksOf<T>()?.FireRemoved(resolved, entity);
        return resolved.RemoveComponent<T>();
    }

    public bool HasTag<T>(Entity entity) where T : struct, ITagData => Resolve(entity).Tags.Has<T>();

    public void AddTag<T>(Entity entity) where T : struct, ITagData => Resolve(entity).AddTag<T>();

    public void RemoveTag<T>(Entity entity) where T : struct, ITagData => Resolve(entity).RemoveTag<T>();

    public void SetParent(Entity child, Entity parent) => Resolve(parent).AddChild(Resolve(child));

    public void ClearParent(Entity child)
    {
        var resolved = Resolve(child);
        var parent = resolved.Parent;
        if (!parent.IsNull) parent.RemoveChild(resolved);
    }

    public Entity GetParent(Entity child) => HandleOf(Resolve(child).Parent);

    public EntityChildren GetChildren(Entity parent) => new(Resolve(parent).ChildEntities, _ids);

    public void OnAdd<T>(ComponentHook<T> handler) where T : struct, IComponentData => GetOrCreateHooks<T>().OnAdd(handler);

    public void OnRemove<T>(ComponentHook<T> handler) where T : struct, IComponentData => GetOrCreateHooks<T>().OnRemove(handler);

    public Query<T1> Query<T1>()
        where T1 : struct, IComponentData
        => new(_store.Query<T1>(), _ids);

    public Query<T1, T2> Query<T1, T2>()
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        => new(_store.Query<T1, T2>(), _ids);

    public Query<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        => new(_store.Query<T1, T2, T3>(), _ids);

    public Query<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
        => new(_store.Query<T1, T2, T3, T4>(), _ids);

    private void CollectDepthFirst(FrifloEntity root)
    {
        _walk.Add(root.Id);
        while (_walk.Count > 0)
        {
            var entity = _store.GetEntityById(_walk[^1]);
            _walk.RemoveAt(_walk.Count - 1);
            _despawning.Add(_ids.HandleOf(entity.Id));

            var children = entity.ChildIds;
            for (var i = children.Length - 1; i >= 0; i--) _walk.Add(children[i]);
        }
    }

    private void FireRemovedHooks(FrifloEntity entity, Entity handle)
    {
        foreach (var componentType in entity.Archetype.ComponentTypes)
            _hooks[componentType.StructIndex]?.FireRemoved(entity, handle);
    }

    private ComponentHooks<T>? HooksOf<T>() where T : struct, IComponentData =>
        (ComponentHooks<T>?)_hooks[ComponentIndex<T>.Value];

    private ComponentHooks<T> GetOrCreateHooks<T>() where T : struct, IComponentData
    {
        ref var slot = ref _hooks[ComponentIndex<T>.Value];
        slot ??= new ComponentHooks<T>();
        return (ComponentHooks<T>)slot;
    }

    private FrifloEntity Resolve(Entity entity)
    {
        if (!TryResolve(entity, out var resolved)) throw new InvalidOperationException($"{entity} is not alive.");
        return resolved;
    }

    private bool TryResolve(Entity entity, out FrifloEntity resolved)
    {
        if (!_ids.IsAlive(entity))
        {
            resolved = default;
            return false;
        }

        resolved = _store.GetEntityById(entity.Id);
        return true;
    }

    private Entity HandleOf(FrifloEntity entity) => entity.IsNull ? default : _ids.HandleOf(entity.Id);

    private void FireDespawnHooks(Entity doomed)
    {
        var outer = _firingDespawnHooks;
        _firingDespawnHooks = doomed;
        try
        {
            FireRemovedHooks(_store.GetEntityById(doomed.Id), doomed);
        }
        finally
        {
            _firingDespawnHooks = outer;
        }
    }

    private static InvalidOperationException MissingComponent<T>(Entity entity) =>
        new($"{entity} has no {typeof(T).Name}.");
}
