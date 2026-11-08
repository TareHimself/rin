using System.Buffers;
using System.Runtime.InteropServices;
using Rin.World.Scheduling;
using Rin.World.Tasks;

namespace Rin.World.Scripting;

/// <summary>
///     Runs an ordered list of <see cref="Script" />s per entity, entity by entity in first-attach order.
/// </summary>
/// <remarks>
///     Scripts are not in a snapshot: a restore keeps an entity's scripts only if it had a <see cref="ScriptRef" /> at
///     save time.
/// </remarks>
public sealed class ScriptFeature : IWorldRestoreListener
{
    private readonly List<List<Script>?> _slots = [];
    private readonly Stack<int> _freeSlots = [];
    private readonly Stack<List<Script>> _pooledLists = [];
    private readonly List<int> _tickOrder = [];
    private readonly Query<ScriptRef> _scriptRefs;
    private readonly List<Entity> _unboundRefs = [];

    public ScriptFeature(World world)
    {
        World = world;
        world.OnRemove<ScriptRef>(OnScriptRefRemoved);
        world.AddSystem(TickPhase.Gameplay, new ScriptTick(this));
        Tasks = new GameTaskScheduler(world);
        world.AddRestoreListener(this);
        _scriptRefs = world.Query<ScriptRef>();
    }

    public World World { get; }

    public GameTaskScheduler Tasks { get; }

    public int Count { get; private set; }

    /// <summary>
    ///     The entity's first script adds a <see cref="ScriptRef" />, so not inside a query loop.
    /// </summary>
    public void Attach(Entity entity, Script script) => Insert(entity, GetScripts(entity).Length, script);

    public void Insert(Entity entity, int index, Script script)
    {
        if (script.IsAttached) throw new InvalidOperationException($"{script.GetType().Name} is already attached.");
        var scripts = FindScripts(entity);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, scripts?.Count ?? 0);

        scripts ??= AddScriptList(entity);
        scripts.Insert(index, script);
        Count++;
        script.Bind(this, entity);
        script.Start();
    }

    /// <summary>
    ///     Detaching the last script removes the <see cref="ScriptRef" />.
    /// </summary>
    public bool Detach(Script script)
    {
        if (!script.IsAttachedTo(this) || FindScripts(script.Entity) is not { } scripts) return false;
        if (scripts.Count == 1) return World.Remove<ScriptRef>(script.Entity);

        scripts.Remove(script);
        Release(script, CancelReason.Detached);
        return true;
    }

    /// <summary>
    ///     Stops every script in reverse order.
    /// </summary>
    public bool DetachAll(Entity entity) => World.Remove<ScriptRef>(entity);

    public ReadOnlySpan<Script> GetScripts(Entity entity) =>
        FindScripts(entity) is { } scripts ? CollectionsMarshal.AsSpan(scripts) : [];

    public T? GetScript<T>(Entity entity) where T : Script
    {
        foreach (var script in GetScripts(entity))
            if (script is T match) return match;
        return null;
    }

    /// <summary>
    ///     Cancels every task, then calls <see cref="Script.Restore" /> on each script.
    /// </summary>
    public void Restore()
    {
        Tasks.CancelAll(CancelReason.Restored);
        var scripts = RentScriptsInTickOrder(out var count);
        for (var i = 0; i < count; i++)
            if (scripts[i].IsAttachedTo(this)) scripts[i].Restore();
        ArrayPool<Script>.Shared.Return(scripts, clearArray: true);
    }

    internal void TickScripts(in TickContext context)
    {
        // Copied so attaches and detaches during the tick can't shift the order.
        var scripts = RentScriptsInTickOrder(out var count);
        for (var i = 0; i < count; i++)
            if (scripts[i].IsAttachedTo(this)) scripts[i].Tick(context);
        ArrayPool<Script>.Shared.Return(scripts, clearArray: true);
    }

    void IWorldRestoreListener.BeforeRestore()
    {
    }

    void IWorldRestoreListener.AfterRestore()
    {
        for (var slot = 0; slot < _slots.Count; slot++)
        {
            if (_slots[slot] is not { } scripts) continue;
            var entity = scripts[0].Entity;
            if (World.IsAlive(entity) && World.Has<ScriptRef>(entity)) World.Get<ScriptRef>(entity) = new ScriptRef(slot);
            else ReleaseAll(slot, CancelReason.Restored);
        }

        foreach (var (scriptRefs, entities) in _scriptRefs)
            for (var i = 0; i < scriptRefs.Length; i++)
                if (!scriptRefs[i].IsBound) _unboundRefs.Add(entities[i]);
        foreach (var entity in _unboundRefs) World.Remove<ScriptRef>(entity);
        _unboundRefs.Clear();

        Restore();
    }

    private List<Script>? FindScripts(Entity entity) =>
        World.Has<ScriptRef>(entity) && World.Get<ScriptRef>(entity) is { IsBound: true } scriptRef ? _slots[scriptRef.Slot] : null;

    private List<Script> AddScriptList(Entity entity)
    {
        // Added first, so a throwing Add leaves the feature untouched.
        var slot = _freeSlots.TryPeek(out var free) ? free : _slots.Count;
        World.Add(entity, new ScriptRef(slot));
        if (slot == _slots.Count) _slots.Add(null);
        else _freeSlots.Pop();

        var scripts = _pooledLists.TryPop(out var pooled) ? pooled : [];
        _slots[slot] = scripts;
        _tickOrder.Add(slot);
        return scripts;
    }

    private Script[] RentScriptsInTickOrder(out int count)
    {
        var scripts = ArrayPool<Script>.Shared.Rent(Count);
        count = 0;
        foreach (var slot in _tickOrder)
        {
            _slots[slot]!.CopyTo(scripts, count);
            count += _slots[slot]!.Count;
        }

        return scripts;
    }

    private void OnScriptRefRemoved(Entity entity, ref ScriptRef scriptRef)
    {
        if (!scriptRef.IsBound) return;
        var reason = World.IsDespawning(entity) ? CancelReason.Despawned : CancelReason.Detached;
        ReleaseAll(scriptRef.Slot, reason);
    }

    private void ReleaseAll(int slot, CancelReason reason)
    {
        var scripts = _slots[slot]!;
        _slots[slot] = null;
        _freeSlots.Push(slot);
        _tickOrder.Remove(slot);

        for (var i = scripts.Count - 1; i >= 0; i--) Release(scripts[i], reason);
        scripts.Clear();
        _pooledLists.Push(scripts);
    }

    private void Release(Script script, CancelReason reason)
    {
        Count--;
        Tasks.Cancel(script, reason);
        script.Stop();
        script.Unbind();
    }
}
