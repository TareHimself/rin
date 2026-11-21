namespace Rin.World.Ecs;

/// <summary>
///     Hooks, the hierarchy and despawn. A despawn fires the remove hooks of every component of the entity and its
///     descendants, depth first with parents before children, and a hook may despawn more entities.
/// </summary>
internal sealed partial class ArchetypeStore
{
    private readonly ChildStore _hierarchy = new();
    private readonly List<Entity> _despawning = [];
    private readonly List<int> _walk = [];
    private ComponentHooks?[] _hooks = [];
    private Entity _firingDespawnHooks;

    public bool IsDespawning(Entity entity) => entity.IsValid && entity == _firingDespawnHooks;

    /// <summary>
    ///     Despawns the entity and all of its descendants. Does nothing for an entity that is not alive.
    /// </summary>
    public void Despawn(Entity entity)
    {
        if (!IsAlive(entity)) return;
        EnsureNotIterating();

        // Hooks may despawn more entities, so each call owns the range it appended.
        var start = _despawning.Count;
        CollectDepthFirst(entity.Id);

        var end = _despawning.Count;
        for (var i = start; i < end; i++)
        {
            var doomed = _despawning[i];
            if (!IsAlive(doomed)) continue;
            FireDespawnHooks(doomed);
            if (IsAlive(doomed)) Delete(doomed.Id);
        }

        _despawning.RemoveRange(start, end - start);
    }

    public void OnAdd<T>(ComponentHook<T> handler) where T : unmanaged, IComponentData => GetOrCreateHooks<T>().OnAdd(handler);

    public void OnRemove<T>(ComponentHook<T> handler) where T : unmanaged, IComponentData => GetOrCreateHooks<T>().OnRemove(handler);

    /// <summary>
    ///     Appends <paramref name="child" /> to the children of <paramref name="parent" />, moving it from its old parent.
    /// </summary>
    public void SetParent(Entity child, Entity parent)
    {
        Resolve(child);
        Resolve(parent);
        for (var ancestor = parent.Id; ancestor != 0; ancestor = _hierarchy.ParentOf(ancestor))
            if (ancestor == child.Id)
                throw new InvalidOperationException($"{child} cannot be parented under itself or its descendant {parent}.");

        _hierarchy.SetParent(child.Id, parent.Id);
    }

    public void ClearParent(Entity child)
    {
        Resolve(child);
        _hierarchy.ClearParent(child.Id);
    }

    /// <summary>
    ///     The parent, or <c>default</c> for a root.
    /// </summary>
    public Entity GetParent(Entity child)
    {
        Resolve(child);
        var parent = _hierarchy.ParentOf(child.Id);
        return parent == 0 ? default : _ids.HandleOf(parent);
    }

    public EntityChildren GetChildren(Entity parent)
    {
        Resolve(parent);
        return new EntityChildren(_hierarchy, parent.Id, _ids);
    }

    private ComponentHooks? HooksOf(ComponentId id) => id.Value < _hooks.Length ? _hooks[id.Value] : null;

    private ComponentHooks<T> GetOrCreateHooks<T>() where T : unmanaged, IComponentData
    {
        var id = Schema.IdOf<T>();
        if (id.Value >= _hooks.Length) Array.Resize(ref _hooks, int.Max(id.Value + 1, _hooks.Length * 2));
        return (ComponentHooks<T>)(_hooks[id.Value] ??= new ComponentHooks<T>());
    }

    private void CollectDepthFirst(int root)
    {
        _walk.Add(root);
        while (_walk.Count > 0)
        {
            var id = _walk[^1];
            _walk.RemoveAt(_walk.Count - 1);
            _despawning.Add(_ids.HandleOf(id));

            var children = _hierarchy.ChildrenOf(id);
            for (var i = children.Length - 1; i >= 0; i--) _walk.Add(children[i]);
        }
    }

    private void FireDespawnHooks(Entity doomed)
    {
        var outer = _firingDespawnHooks;
        _firingDespawnHooks = doomed;
        try
        {
            // The key is read once: hooks may move the entity, and each hook checks the component is still there.
            foreach (var id in _locations[doomed.Id].Archetype.Key) HooksOf(id)?.FireRemoved(this, doomed);
        }
        finally
        {
            _firingDespawnHooks = outer;
        }
    }
}
