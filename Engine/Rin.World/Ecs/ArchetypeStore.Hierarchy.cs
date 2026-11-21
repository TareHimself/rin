namespace Rin.World.Ecs;

/// <summary>
///     Hooks, the hierarchy and despawn. A despawn first marks the entity and its descendants
///     <see cref="EntityState.Despawning" />, then fires the remove hooks of each one's components, depth first with
///     parents before children, and deletes it. A hook may despawn more entities.
/// </summary>
internal sealed partial class ArchetypeStore
{
    private readonly ChildStore _hierarchy = new();
    private readonly List<Entity> _despawning = [];
    private readonly List<int> _walk = [];
    private readonly List<PendingRemoval> _removing = [];
    private ComponentHooks?[] _hooks = [];

    public bool IsDespawning(Entity entity) => _entities.StateOf(entity) == EntityState.Despawning;

    /// <summary>
    ///     Despawns the entity and all of its descendants. Does nothing for an entity that is not alive or already
    ///     despawning.
    /// </summary>
    public void Despawn(Entity entity)
    {
        if (_entities.StateOf(entity) != EntityState.Alive) return;
        Phase.EnsureCanMoveRows();

        using var hooks = Phase.EnterHooks();
        // Hooks may despawn more entities, so each call owns the range it appended.
        var start = _despawning.Count;
        try
        {
            CollectDepthFirst(entity.Id);
            DeleteCollected(start, _despawning.Count);
        }
        finally
        {
            // After a hook threw, the entities not yet deleted stay alive.
            for (var i = start; i < _despawning.Count; i++) _entities.CancelDespawning(_despawning[i]);
            _despawning.RemoveRange(start, _despawning.Count - start);
        }
    }

    public void OnAdd<T>(ComponentHook<T> handler) where T : unmanaged, IComponentData => GetOrCreateHooks<T>().OnAdd(handler);

    public void OnRemove<T>(ComponentHook<T> handler) where T : unmanaged, IComponentData => GetOrCreateHooks<T>().OnRemove(handler);

    /// <summary>
    ///     Appends <paramref name="child" /> to the children of <paramref name="parent" />, moving it from its old parent.
    /// </summary>
    public void SetParent(Entity child, Entity parent)
    {
        Phase.EnsureCanReparent();
        EnsureHierarchyCanChange(child);
        EnsureHierarchyCanChange(parent);
        for (var ancestor = parent.Id; ancestor != EntityTable.NoId; ancestor = _hierarchy.ParentOf(ancestor))
            if (ancestor == child.Id)
                throw new InvalidOperationException($"{child} cannot be parented under itself or its descendant {parent}.");

        _hierarchy.SetParent(child.Id, parent.Id);
    }

    public void ClearParent(Entity child)
    {
        Phase.EnsureCanReparent();
        EnsureHierarchyCanChange(child);
        _hierarchy.ClearParent(child.Id);
    }

    /// <summary>
    ///     The parent, or <c>default</c> for a root.
    /// </summary>
    public Entity GetParent(Entity child)
    {
        Resolve(child);
        var parent = _hierarchy.ParentOf(child.Id);
        return parent == EntityTable.NoId ? default : _entities.HandleOf(parent);
    }

    public EntityChildren GetChildren(Entity parent)
    {
        Resolve(parent);
        return new EntityChildren(_hierarchy, parent.Id, _entities);
    }

    private ComponentHooks? HooksOf(ComponentId id) => id.Value < _hooks.Length ? _hooks[id.Value] : null;

    private ComponentHooks<T> GetOrCreateHooks<T>() where T : unmanaged, IComponentData
    {
        var id = Schema.IdOf<T>();
        if (id.Value >= _hooks.Length) Array.Resize(ref _hooks, int.Max(id.Value + 1, _hooks.Length * 2));
        return (ComponentHooks<T>)(_hooks[id.Value] ??= new ComponentHooks<T>());
    }

    private void FireAddHooks(Entity entity, ComponentId id)
    {
        if (HooksOf(id) is not { } hooks) return;
        using var scope = Phase.EnterHooks();
        hooks.FireAdded(this, entity);
    }

    /// <summary>
    ///     Does nothing while the remove hooks of the same component of the same entity already run, so a hook that
    ///     removes its own component or despawns its own entity does not fire itself again.
    /// </summary>
    private void FireRemoveHooks(Entity entity, ComponentId id)
    {
        if (HooksOf(id) is not { } hooks || IsBeingRemoved(entity, id)) return;
        using var scope = Phase.EnterHooks();
        _removing.Add(new PendingRemoval(entity, id));
        try
        {
            hooks.FireRemoved(this, entity);
        }
        finally
        {
            _removing.RemoveAt(_removing.Count - 1);
        }
    }

    private bool IsBeingRemoved(Entity entity, ComponentId id) => _removing.Contains(new PendingRemoval(entity, id));

    private void EnsureHierarchyCanChange(Entity entity)
    {
        Resolve(entity);
        if (IsDespawning(entity))
            throw new InvalidOperationException($"{entity} is being despawned, so it cannot gain, lose or change a parent or child.");
    }

    // Its remove hooks were chosen when the despawn began, so a new component would never see one.
    private void EnsureCanGainComponents(Entity entity)
    {
        if (IsDespawning(entity))
            throw new InvalidOperationException($"{entity} is being despawned, so it cannot gain a component.");
    }

    private void CollectDepthFirst(int root)
    {
        _walk.Add(root);
        while (_walk.Count > 0)
        {
            var id = _walk[^1];
            _walk.RemoveAt(_walk.Count - 1);
            _entities.MarkDespawning(id);
            _despawning.Add(_entities.HandleOf(id));

            var children = _hierarchy.ChildrenOf(id);
            for (var i = children.Length - 1; i >= 0; i--) _walk.Add(children[i]);
        }
    }

    private void DeleteCollected(int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            var doomed = _despawning[i];
            // The key is read once: hooks may move the entity, and each hook checks the component is still there.
            foreach (var id in _entities.LocationOf(doomed.Id).Archetype.Key) FireRemoveHooks(doomed, id);
            Delete(doomed.Id);
        }
    }

    /// <summary>
    ///     A component whose remove hooks are running.
    /// </summary>
    private readonly record struct PendingRemoval(Entity Entity, ComponentId Id);
}
