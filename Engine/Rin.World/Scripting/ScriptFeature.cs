using System.Buffers;
using System.Runtime.InteropServices;
using Rin.World.Scheduling;
using Rin.World.Tasks;

namespace Rin.World.Scripting;

/// <summary>
///     Attaches managed <see cref="Script" />s to entities through a <see cref="ScriptRef" /> component, ticks them in
///     <see cref="TickPhase.Gameplay" /> and owns the <see cref="GameTaskScheduler" /> their tasks run on.
/// </summary>
/// <remarks>
///     Each entity holds an ordered list of scripts. Ticks go entity by entity, in the order each entity got its first
///     script, then through that entity's list in order.
///     <para />
///     Scripts are not in a snapshot. After a restore an entity keeps its scripts when it exists and had a
///     <see cref="ScriptRef" /> in the snapshot; every other entity's scripts are stopped and detached (the entity is gone
///     or got its first script after the save). Then <see cref="Restore" /> runs. An entity that had scripts at save time
///     but lost them since gets none back.
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

    /// <summary>
    ///     Attached scripts across every entity.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    ///     Appends <paramref name="script" /> to the entity's scripts and calls <see cref="Script.Start" /> right away.
    ///     The entity's first script adds a <see cref="ScriptRef" />, a structural change not for use inside a query loop.
    /// </summary>
    public void Attach(Entity entity, Script script) => Insert(entity, GetScripts(entity).Length, script);

    /// <summary>
    ///     Like <see cref="Attach" />, but at <paramref name="index" /> in the entity's scripts.
    /// </summary>
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
    ///     Detaches one script, which cancels its tasks and calls <see cref="Script.Stop" />. Detaching an entity's last
    ///     script removes its <see cref="ScriptRef" />.
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
    ///     Removes the <see cref="ScriptRef" />, which detaches every script of the entity in reverse order.
    /// </summary>
    public bool DetachAll(Entity entity) => World.Remove<ScriptRef>(entity);

    public ReadOnlySpan<Script> GetScripts(Entity entity) =>
        FindScripts(entity) is { } scripts ? CollectionsMarshal.AsSpan(scripts) : [];

    /// <summary>
    ///     The entity's first script of type <typeparamref name="T" />, or null.
    /// </summary>
    public T? GetScript<T>(Entity entity) where T : Script
    {
        foreach (var script in GetScripts(entity))
            if (script is T match) return match;
        return null;
    }

    /// <summary>
    ///     The entry point for a snapshot restore: cancels every task with <see cref="CancelReason.Restored" />, then
    ///     calls <see cref="Script.Restore" /> on each script in tick order.
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
        // A copy, so attaches and detaches during the tick cannot shift the order; new scripts tick from the next tick.
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
